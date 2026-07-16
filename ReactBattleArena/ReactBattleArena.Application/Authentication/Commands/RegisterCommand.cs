using MediatR;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed record RegisterCommand(
    string UserName,
    string Email,
    string? DisplayName,
    string Password) : IRequest<Guid>;