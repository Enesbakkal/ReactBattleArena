using MediatR;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest<bool>;