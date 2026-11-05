using MediatR;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed record CreateTeamCommand(Guid OwnerUserId, string Name) : IRequest<Guid>;