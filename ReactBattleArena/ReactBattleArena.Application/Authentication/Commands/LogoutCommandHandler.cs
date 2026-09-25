using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Abstractions;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Authentication;


namespace ReactBattleArena.Application.Authentication.Commands;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly IRefreshTokenGenerator _refreshTokens;
    public LogoutCommandHandler(
        IApplicationDbContext db,
        IRefreshTokenGenerator refreshTokens)
    {
        _db = db;
        _refreshTokens = refreshTokens;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = _refreshTokens.Hash(request.RefreshToken);

        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null || existing.RevokedAtUtc is not null)
            return false;
        //ValidationBehavior boş gövdede 400 döner. Sayfa boş gövde göndermez. Ham string yoksa istek hiç çıkmaz.Handler ham string'i RefreshTokenGenerator.
        //Hash ile SHA256 hex yapar. TokenHash kolonunda arar. Satır yoksa veya RevokedAtUtc doluysa false döner. SaveChangesAsync çalışmaz.

        existing.Revoke(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
    //Refresh handler'ının ilk yarısıyla aynı: hamı hash'le, satırı bul. Fark, yeni çift üretmemesi — burada oturumu kapatıyoruz, uzatmıyoruz.
    //Süre kontrolü de yok; süresi dolmuş bir satırı iptal etmek zararsız, gereksiz bir if olur. Sadece o cihazın satırını iptal ediyoruz;
    //"tüm cihazlardan çık" ayrı bir özellik, UserId ile tüm satırları dolaşmak gerekir.
}

