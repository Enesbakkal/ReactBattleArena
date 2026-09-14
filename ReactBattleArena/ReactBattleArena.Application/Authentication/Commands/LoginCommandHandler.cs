using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Abstractions;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Authentication;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult?>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenGenerator _refreshTokens;

    public LoginCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenGenerator refreshTokens)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokens = refreshTokens;
    }

    public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(
                u => u.UserName == request.UserNameOrEmail || u.Email == request.UserNameOrEmail,
                cancellationToken);

        if (user is null)
            return null;

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        // Kullanıcı yok ve şifre yanlış aynı null. “Bu email kayıtlı değil” sızmaz.
        // Sık düşülen hata: kullanıcı yokta 404, yanlış şifrede 401 — email sızdırır.
        // Bir diğeri: UseAuthorization’ı UseAuthentication’dan önce yazmak.
        // Bir diğeri: JWT Key’i git’e kısa string koymak — HMAC zayıf kalır.
        // Token’ı URL query’de taşımak da log’a sızar; header’da Bearer

        var token = _jwtTokenService.CreateToken(user);

        var utcNow = DateTime.UtcNow;
        var (rawRefresh, hash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(
            RefreshToken.Create(user.Id, hash, expires, utcNow));

        await _db.SaveChangesAsync(cancellationToken);


        return new LoginResult(user.Id, user.UserName, user.Email, token, rawRefresh);
    }
}