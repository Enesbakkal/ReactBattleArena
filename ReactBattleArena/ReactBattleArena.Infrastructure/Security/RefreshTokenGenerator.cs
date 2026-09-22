using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Server;
using ReactBattleArena.Abstractions;
using ReactBattleArena.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;

namespace ReactBattleArena.Infrastructure.Security;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly JwtOptions _options;

    public RefreshTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string Hash(string raw)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        //Hash aynı byte dizisini UTF-8 diye değil, ham string'in UTF-8 byte'larını SHA256'dan geçirir.
        //SHA256.HashData 32 byte döner. Convert.ToHexString bunu 64 karakterlik hex yapar. Kolon uzunluğu da 64'tür.
    }

    public (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow)
    {
        var bytes = RandomNumberGenerator.GetBytes(32); // RandomNumberGenerator.GetBytes(32) 32 byte üretir.
        var raw = Convert.ToBase64String(bytes); // Ham token bu byte'ların Base64 yazımıdır.
        var hash = Hash(raw); // Hash aynı byte dizisini UTF-8 diye değil, ham string'in UTF-8 byte'larını SHA256'dan geçirir.
                              // Burayı metod yaptık. Böylece iki yerde iki ayrı formül kalma riski kalkıyor.
                              // Hash aynı byte dizisini UTF - 8 diye değil, ham string'in UTF-8 byte'larını SHA256'dan geçirir.
                              // SHA256.HashData 32 byte döner. Convert.ToHexString bunu 64 karakterlik hex yapar. Kolon uzunluğu da 64'tür.Bu hash BCrypt değildir.
                              // BCrypt her çağrıda yeni bir salt koyduğu için aynı ham token ikinci kez farklı string verirdi.
                              // Login'de yazılan TokenHash ile sonraki istekteki arama birbirini bulamazdı. SHA256 aynı girdiye aynı hex'i verir.
                              // Formül Hash metodunda tek yerde durur. Arayüz de bunu söyler: login ve refresh aynı SHA256'yı kullanır.
        var expires = utcNow.AddDays(_options.RefreshExpireDays);
        return (raw, hash, expires);
        // Şifre hasher’ını (BCrypt) kullanma. BCrypt her seferinde farklı tuz üretir; TokenHash unique index ile arama bozulur. 
    }

}