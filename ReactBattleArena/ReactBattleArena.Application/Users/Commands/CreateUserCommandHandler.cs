using MediatR;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Users;

namespace ReactBattleArena.Application.Users.Commands;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateUserCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var entity = User.Create(
            request.UserName,
            request.Email,
            request.DisplayName,
            DateTime.UtcNow);

        _db.Users.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}