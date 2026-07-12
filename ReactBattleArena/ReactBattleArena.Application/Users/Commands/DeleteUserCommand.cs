using MediatR;

namespace ReactBattleArena.Application.Users.Commands;

public sealed record DeleteUserCommand(Guid Id) : IRequest<bool>;