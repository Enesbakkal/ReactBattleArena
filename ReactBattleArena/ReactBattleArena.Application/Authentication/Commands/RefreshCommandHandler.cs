using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Abstractions;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Authentication;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed class RefreshCommandHandler : IRequestHandler<RefreshCommand, LoginResult?>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenGenerator _refreshTokens;

    public RefreshCommandHandler(
        IApplicationDbContext db,
        IJwtTokenService jwtTokenService,
        IRefreshTokenGenerator refreshTokens)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _refreshTokens = refreshTokens;
    }

    public async Task<LoginResult?> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var hash = _refreshTokens.Hash(request.RefreshToken);

        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null)
            return null;

        if (existing.RevokedAtUtc is not null)
        {
            // Rotation yüzünden her refresh token tek kullanımlık. İptal edilmiş bir token
            // ikinci kez geldiyse aynı zinciri iki taraf tutuyor demektir; çalınmış varsayıyoruz.

            var activeTokens = await _db.RefreshTokens
                .Where(t => t.UserId == existing.UserId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            
            if (activeTokens.Count > 0)
            {
                foreach (var activeToken in activeTokens)
                    activeToken.Revoke(utcNow);

                await _db.SaveChangesAsync(cancellationToken);
            }
                
            return null;
        }


        if (existing.ExpiresAtUtc <= utcNow)
            return null;

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken);
        if (user is null)
            return null;
        existing.Revoke(utcNow);
        //Dört ayrı başarısızlık durumunun hepsi aynı null'u döndürüyor; login'deki "email sızdırmama" mantığının aynısı.
        //existing.Revoke(utcNow) satırından sonra ayrıca bir Update çağırmıyoruz, çünkü satırı sorguyla çektiğimiz an EF onu takibe alıyor;
        //SaveChangesAsync değişikliği kendisi UPDATE'e çeviriyor. Aynı SaveChanges hem eski satırın RevokedAtUtc'sini hem yeni satırın INSERT'ünü tek transaction'da yazıyor — rotation tam olarak bu.

        var (rawRefresh, newHash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(RefreshToken.Create(user.Id, newHash, expires, utcNow));

        await _db.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.CreateToken(user);
        return new LoginResult(user.Id, user.UserName, user.Email, token, rawRefresh);
    }

}