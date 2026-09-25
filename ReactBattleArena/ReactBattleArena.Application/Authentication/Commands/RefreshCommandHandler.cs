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
        var hash = _refreshTokens.Hash(request.RefreshToken);// önyüzden gelen raw refresh token

        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null)
            return null;

        //Satırın RevokedAtUtc alanı doluysa bu ham token daha önce kullanılmıştır.
        //Rotation her ham token'ı bir kez kullanır.
        //İptal edilmiş token tekrar geldiyse handler o kullanıcının RevokedAtUtc alanı boş olan satırlarını da Revoke ile doldurur, kaydeder ve null döner.
        //Yeni access token basılmaz. refreshSession 200 görmez, clearToken çalışır.
        if (existing.RevokedAtUtc is not null)// iptal edilmiş refresh token birinin eline geçmiş ve tekrar gelmişse
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
                // İptal edilmiş satır tekrar geldiyse o kullanıcının RevokedAtUtc alanı boş olan bütün satırları da iptal edilir.
            }
                
            return null;
        }


        if (existing.ExpiresAtUtc <= utcNow)
            return null;
        // 27-59 arası : Satır yoksa, RevokedAtUtc doluysa veya ExpiresAtUtc geçmişse handler null döner. Controller 401 yazar. 
        // İptal edilmiş satır tekrar geldiyse o kullanıcının RevokedAtUtc alanı boş olan bütün satırları da iptal edilir.

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken);
        if (user is null)
            return null;
        existing.Revoke(utcNow); // Satır kullanılabilirse handler onu Revoke ile iptal eder.
                                //Dört ayrı başarısızlık durumunun hepsi aynı null'u döndürüyor; login'deki "email sızdırmama" mantığının aynısı.
                                //existing.Revoke(utcNow) satırından sonra ayrıca bir Update çağırmıyoruz, çünkü satırı sorguyla çektiğimiz an EF onu takibe alıyor;
                                //SaveChangesAsync değişikliği kendisi UPDATE'e çeviriyor. Aynı SaveChanges hem eski satırın RevokedAtUtc'sini hem yeni satırın INSERT'ünü tek transaction'da yazıyor — rotation tam olarak bu.

        var (rawRefresh, newHash, expires) = _refreshTokens.Create(utcNow); // Create yeni ham token, yeni hash ve yeni bitiş üretir. 
        _db.RefreshTokens.Add(RefreshToken.Create(user.Id, newHash, expires, utcNow));

        await _db.SaveChangesAsync(cancellationToken); //Yeni satır eklenir.Aynı SaveChangesAsync eski satırın RevokedAtUtc değerini ve yeni satırı yazar.
                                                       //SaveChangesAsync eski satır için UPDATE, yeni satır için INSERT yazar.
                                                       //İkisi aynı kayıtta durur. Access token bu SaveChanges ile tabloya gitmez.
                                                       //CreateToken onu SaveChanges sonrasında basar. LoginResult.Token o JWT'dir. LoginResult.RefreshToken rawRefresh olur.
                                                       //Yeni satır eklenir. Aynı SaveChangesAsync eski satırın RevokedAtUtc değerini ve yeni satırı yazar.


        var token = _jwtTokenService.CreateToken(user);
        return new LoginResult(user.Id, user.UserName, user.Email, token, rawRefresh);
        //JwtTokenService.CreateToken yeni access token basar.LoginResult hem onu hem yeni ham refresh token'ı taşır. Controller 200 yazar.
    }

}