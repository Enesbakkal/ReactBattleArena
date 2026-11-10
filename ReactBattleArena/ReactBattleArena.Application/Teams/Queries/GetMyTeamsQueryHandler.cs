using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Teams;

namespace ReactBattleArena.Application.Teams.Queries;

public sealed class GetMyTeamsQueryHandler
    : IRequestHandler<GetMyTeamsQuery, IReadOnlyList<TeamRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetMyTeamsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TeamRowDto>> Handle(
        GetMyTeamsQuery request,
        CancellationToken cancellationToken)
    {
        var teams = await _db.Teams
            .AsNoTracking()
            .Where(t => t.OwnerUserId == request.OwnerUserId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var teamIds = teams.Select(t => t.Id).ToList();

        var members = await _db.TeamMembers
            .AsNoTracking()
            .Where(m => teamIds.Contains(m.TeamId))
            .Join(
                _db.Characters.AsNoTracking(),
                m => m.CharacterId,
                c => c.Id,
                (m, c) => new // join seçimi
                {
                    m.TeamId,
                    m.SlotIndex,
                    m.CharacterId,
                    c.Name,
                    c.ImageUrl,
                    c.BaseAttack,
                    c.BaseDefense,
                    c.BaseSpeed,
                    c.BaseHealth,
                    c.BaseTechnique,
                    c.BaseAccuracy,
                    c.BaseForesight
                })
            .ToListAsync(cancellationToken);

        return teams.Select(t =>
        {
            var bySlot = members
                .Where(m => m.TeamId == t.Id)
                .ToDictionary(m => m.SlotIndex);

            var slots = Enumerable.Range(0, Team.SlotCount)
                .Select(i =>
                {
                    if (bySlot.TryGetValue(i, out var m))
                    {
                        return new TeamSlotDto(
                            i,
                            m.CharacterId,
                            m.Name,
                            m.ImageUrl,
                            m.BaseAttack,
                            m.BaseDefense,
                            m.BaseSpeed,
                            m.BaseHealth,
                            m.BaseTechnique,
                            m.BaseAccuracy,
                            m.BaseForesight);
                    }
                    return new TeamSlotDto(
                        i, null, null, null, null, null, null, null, null, null, null);
                })
                .ToList();

            return new TeamRowDto(t.Id, t.Name, t.CreatedAtUtc, slots);
        }).ToList();
    }
}