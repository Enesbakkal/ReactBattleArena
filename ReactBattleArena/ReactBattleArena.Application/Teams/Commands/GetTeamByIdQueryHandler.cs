using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Teams;
using System.Runtime.ConstrainedExecution;
using static System.Net.WebRequestMethods;

namespace ReactBattleArena.Application.Teams.Queries;

public sealed class GetTeamByIdQueryHandler
    : IRequestHandler<GetTeamByIdQuery, TeamRowDto?>
{
    private readonly IApplicationDbContext _db;

    public GetTeamByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TeamRowDto?> Handle(
        GetTeamByIdQuery request,
        CancellationToken cancellationToken)
    {
        var team = await _db.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == request.TeamId && t.OwnerUserId == request.OwnerUserId,
                cancellationToken);

        if (team is null)
            return null;

        var members = await _db.TeamMembers
    .AsNoTracking()
    .Where(m => m.TeamId == team.Id)
    .Join(
        _db.Characters.AsNoTracking(),
        m => m.CharacterId,
        c => c.Id,
        (m, c) => new
        {
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

        var bySlot = members.ToDictionary(m => m.SlotIndex);

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

        return new TeamRowDto(team.Id, team.Name, team.CreatedAtUtc, slots);
    }

    // Önce takım okunur; sahip süzgeci yine birlikte gelir.Yoksa null döner ve controller bunu 404 yapar.
    // Üyeler karakter kataloğuyla birleşir çünkü ad ve görsel üyelik satırında değil, Characters kaydındadır.
    // Enumerable.Range(0, Team.SlotCount) her zaman beş yer üretir; dolu yerler sözlükten dolar, boş yerler null alanlı TeamSlotDto olur.
    // Bu, GetMyTeams handler’ındaki slot mantığının tek takıma indirgenmiş hâlidir.İleride ortak bir yardımcıya çekilebilir;
    // şimdilik iki handler’da açık durması öğrenirken okumayı kolaylaştırır.React düzenleme sayfası bu GET’i apiFetch(\/api/teams/${ id}`)ile atacak;
    // ASP.NET’te karşılığı, action’ın MediatR’aGetTeamByIdQuery` göndermesidir.
}