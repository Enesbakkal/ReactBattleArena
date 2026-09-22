using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ReactBattleArena.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            //CreateToken dört claim yazar: sub, unique_name, email ve ClaimTypes.NameIdentifier.
            //sub ile NameIdentifier aynı kullanıcı Guid değeridir. Permission kodu ve rol adı bu diziye konmaz.
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        //İmza HMAC-SHA256'dır. Anahtar Jwt:Key değeridir. Sunucu token'ı bir tabloda aramaz.
        //Gelen string'i aynı anahtar ile doğrular. Süre ExpireMinutes kadar sonradır.
        //JwtOptions içinde bu değerin varsayılanı 60 dakikadır. Issuer ve audience da aynı bölümden okunur.

        //BCrypt hash'i ile JWT imzası aynı işlem değildir.
        //BCrypt parolayı PasswordHash kolonunda saklar.
        //HMAC-SHA256 access token'ı imzalar. Access token'ın veritabanı satırı yoktur.

        // Bu noktadan sonra program.cs deki AddInfrastructure'a bak 

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    //Application User alır, string token döner; JWT kütüphanesini Infrastructure bilir.
    //Görünmeyen mekanizma imzadır: sunucu token’ı tabloda saklamaz.
    //Üç parça (header.payload.imza) HMAC-SHA256 ile Key’e bağlanır;
    //Api gelen token’ı aynı Key ile doğrular. Key sızarsa herkes token basar.
    // ExpireMinutes (varsayılan 60) expires claim’i. BCrypt hash ≠ JWT imzası:
    // biri parolayı saklar, öbürü isteği imzalar.
}