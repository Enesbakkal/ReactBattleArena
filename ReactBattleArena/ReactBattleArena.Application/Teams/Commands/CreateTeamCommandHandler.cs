using MediatR;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Teams;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateTeamCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var entity = Team.Create(request.OwnerUserId, request.Name, DateTime.UtcNow);
        _db.Teams.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}