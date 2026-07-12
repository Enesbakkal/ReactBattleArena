using MediatR;

namespace ReactBattleArena.Application.Users.Commands;

public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string? DisplayName) : IRequest<Guid>;