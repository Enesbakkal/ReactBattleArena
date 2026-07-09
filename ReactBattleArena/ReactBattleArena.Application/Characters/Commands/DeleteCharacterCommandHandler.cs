using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Application.Characters.Commands;

// IRequestHandler<DeleteCharacterCommand, bool> → MediatR, Send(DeleteCharacterCommand) gelince bu sınıfı seçer.
// Akış: ValidationBehavior (varsa) → Handle → bool döner. "Yakalama" = generic interface eşleşmesi.
public sealed class DeleteCharacterCommandHandler : IRequestHandler<DeleteCharacterCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public DeleteCharacterCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteCharacterCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Characters
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity is null)
            return false;

        _db.Characters.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
