using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Teams;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed class AddTeamMemberCommandHandler
    : IRequestHandler<AddTeamMemberCommand, AddTeamMemberResult>
{
    private readonly IApplicationDbContext _db;

    public AddTeamMemberCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AddTeamMemberResult> Handle(
        AddTeamMemberCommand request,
        CancellationToken cancellationToken)
    {
        var teamExists = await _db.Teams.AnyAsync(
            t => t.Id == request.TeamId && t.OwnerUserId == request.OwnerUserId,
            cancellationToken);

        if (!teamExists)
            return AddTeamMemberResult.TeamNotFound;

        var characterExists = await _db.Characters.AnyAsync(
            c => c.Id == request.CharacterId,
            cancellationToken);

        if (!characterExists)
            return AddTeamMemberResult.CharacterNotFound;

        var memberCount = await _db.TeamMembers.CountAsync(
            m => m.TeamId == request.TeamId,
            cancellationToken);

        if (memberCount >= Team.SlotCount)
            return AddTeamMemberResult.TeamFull;

        var slotTaken = await _db.TeamMembers.AnyAsync(
            m => m.TeamId == request.TeamId && m.SlotIndex == request.SlotIndex,
            cancellationToken);
        // SlotIndex üye sayısının “kaçıncı kişisisin” bilgisi değildir. Takımdaki sabit kutunun numarasıdır: 0, 1, 2, 3, 4.
        // Üç karakter eklediğinde tabloda üç satır olur; her satırın SlotIndex’i o karakterin hangi kutuda durduğunu tutar.
        // Örneğin Luffy 0’da, Zoro 2’de, Sanji 4’te olabilir — indeksler 0, 1, 2 diye sırayla dolmak zorunda değildir; boş kutu için satır yoktur.
        // Bu sorgu şunu sorar: “Bu takımda, isteğin gönderdiği SlotIndex ile aynı numaralı yerde zaten bir üyelik satırı var mı?” request.
        // SlotIndex 0 ise yalnız SlotIndex == 0 satırına bakar. Üç üyen olsa bile 1. kutu boşsa slotIndex: 1 ekleme SlotOccupied dönmez.
        // Dolu olan 0. kutuya tekrar yazmaya kalkarsan true olur ve SlotOccupied gelir. PK (TeamId, SlotIndex) de aynı fikri DB’de kilitleler;
        // handler bunu kullanıcıya anlaşılır sonuç olarak önden verir.

        if (slotTaken)
            return AddTeamMemberResult.SlotOccupied;

        var alreadyInTeam = await _db.TeamMembers.AnyAsync(
            m => m.TeamId == request.TeamId && m.CharacterId == request.CharacterId,
            cancellationToken);

        if (alreadyInTeam)
            return AddTeamMemberResult.CharacterAlreadyInTeam;

        var member = TeamMember.Create(request.TeamId, request.CharacterId, request.SlotIndex);
        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync(cancellationToken);
        return AddTeamMemberResult.Ok;
    }
}

// Sıra bilinçli: önce sahiplik (yoksa 404), sonra karakter kataloğu, sonra doluluk / slot / aynı karakter. Unique index (TeamId, CharacterId) ve PK (TeamId, SlotIndex) DB’de de korur;,
// handler kullanıcıya anlaşılır sonuç verir, çiğ SqlException bırakmaz. ASP.NET eşlemesi: action body’den characterId + slotIndex alır, JWT’den sahibi okur, MediatR bu command’ı işler.