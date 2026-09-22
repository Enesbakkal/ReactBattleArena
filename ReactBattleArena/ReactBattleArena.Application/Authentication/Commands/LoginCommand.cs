using MediatR;
using ReactBattleArena.Domain.Authentication;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed record LoginCommand(string UserNameOrEmail, string Password)
    : IRequest<LoginResult?>;// Komut iki alan taşır. Dönüş LoginResult? olduğu için handler null dönebilir.
//LoginResult içindeki Token access token'dır. RefreshToken ham refresh token'dır.
//İkisi de bu record'da string'dir. Ayrı cevap sınıfları yoktur. Contracts/LoginResponse.cs bu metotta kullanılmaz. Ok(result) bu record'u yazar.
//ASP.NET Core JSON alan adlarını camelCase basar.Sayfanın okuduğu adlar token ve refreshToken olur.

public sealed record LoginResult(
    Guid UserId,
    string UserName,
    string Email,
    string Token,
    string RefreshToken);
//null → kullanıcı yok / şifre yanlış → controller 401.
