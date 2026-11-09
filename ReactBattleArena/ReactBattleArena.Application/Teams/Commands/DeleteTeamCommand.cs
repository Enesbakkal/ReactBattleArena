using MediatR;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed record DeleteTeamCommand(Guid TeamId, Guid OwnerUserId) : IRequest<bool>;