using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed class DeleteTeamCommandHandler : IRequestHandler<DeleteTeamCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public DeleteTeamCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Teams
            .FirstOrDefaultAsync(
                t => t.Id == request.TeamId && t.OwnerUserId == request.OwnerUserId,
                cancellationToken);

        if (entity is null)
            return false;

        _db.Teams.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

// Rename ile aynı sahiplik filtresi burada da durur. Remove takım satırını siler;
// TeamMembers tarafında Cascade tanımlı olduğu için o takıma bağlı üyelik satırları da düşer.
// Elle üye silmeye gerek yoktur. Validator istersen TeamId ve OwnerUserId için NotEmpty eklersin;
// Character delete’te çoğu zaman handler yeterli görülmüştü, sen de aynı sadelikte bırakabilirsin.