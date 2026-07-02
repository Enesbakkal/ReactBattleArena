using MediatR;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Characters;

namespace ReactBattleArena.Application.Characters.Commands;

public sealed class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateCharacterCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateCharacterCommand request, CancellationToken cancellationToken)
    {
        var entity = Character.Create(
            request.Name,
            request.Universe,
            request.Biography,
            request.Rarity,
            request.BaseAttack,
            request.BaseDefense,
            request.BaseSpeed,
            request.ImageUrl,
            DateTime.UtcNow);

        _db.Characters.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}