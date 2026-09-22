using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;

namespace ReactBattleArena.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpireMinutes { get; set; } = 60;
    public int RefreshExpireDays { get; set; } = 7;

    //Anahtar Jwt:Key değeridir.Sunucu token'ı bir tabloda aramaz.
    //Gelen string'i aynı anahtar ile doğrular.Süre ExpireMinutes kadar sonradır.
    //JwtOptions içinde bu değerin varsayılanı 60 dakikadır.Issuer ve audience da configuration'daki Jwt section'ından okunur.
    //SectionName sabiti o section'ın adıdır: "Jwt".
}