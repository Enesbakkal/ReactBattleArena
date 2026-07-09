using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Application.Characters.Commands;

public sealed class UpdateCharacterCommandHandler : IRequestHandler<UpdateCharacterCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public UpdateCharacterCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(UpdateCharacterCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Characters
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity is null)
            return false;

        entity.Update(
            request.Name,
            request.Universe,
            request.Biography,
            request.Rarity,
            request.BaseAttack,
            request.BaseDefense,
            request.BaseSpeed,
            request.ImageUrl);

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}