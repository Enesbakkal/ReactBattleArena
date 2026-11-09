using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed class RemoveTeamMemberCommandHandler
    : IRequestHandler<RemoveTeamMemberCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public RemoveTeamMemberCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(
        RemoveTeamMemberCommand request,
        CancellationToken cancellationToken)
    {
        var teamExists = await _db.Teams.AnyAsync(
            t => t.Id == request.TeamId && t.OwnerUserId == request.OwnerUserId,
            cancellationToken);

        if (!teamExists)
            return false;

        var member = await _db.TeamMembers.FirstOrDefaultAsync(
            m => m.TeamId == request.TeamId && m.SlotIndex == request.SlotIndex,
            cancellationToken);

        if (member is null)
            return false;

        _db.TeamMembers.Remove(member);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
    //  Üyelik satırını siler. Slot boşsa veya takım senin değilse false → controller 404. Karakter id’sine gerek yok; UI hangi kutuya bastığını slotIndex ile bilir.
}