using MediatR;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed record LoginCommand(string UserNameOrEmail, string Password)
    : IRequest<LoginResult?>;

public sealed record LoginResult(
    Guid UserId,
    string UserName,
    string Email,
    string Token,
    string RefreshToken);
//null → kullanıcı yok / şifre yanlış → controller 401.
