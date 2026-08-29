using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ReactBattleArena.Abstractions;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Infrastructure.Security;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly JwtOptions _options;

    public RefreshTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var expires = utcNow.AddDays(_options.RefreshExpireDays);
        return (raw, hash, expires);
        // Şifre hasher’ını (BCrypt) kullanma. BCrypt her seferinde farklı tuz üretir; TokenHash unique index ile arama bozulur. 
    }

}