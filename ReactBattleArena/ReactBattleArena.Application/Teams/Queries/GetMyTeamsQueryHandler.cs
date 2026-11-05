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
                (m, c) => new { m.TeamId, m.SlotIndex, m.CharacterId, c.Name, c.ImageUrl })
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
                        return new TeamSlotDto(i, m.CharacterId, m.Name, m.ImageUrl);
                    }

                    return new TeamSlotDto(i, null, null, null);
                })
                .ToList();

            return new TeamRowDto(t.Id, t.Name, t.CreatedAtUtc, slots);
        }).ToList();
    }
}