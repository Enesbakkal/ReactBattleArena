using MediatR;

namespace ReactBattleArena.Application.Users.Commands;

public sealed record UpdateUserCommand(
    Guid Id,
    string UserName,
    string Email,
    string? DisplayName) : IRequest<bool>;