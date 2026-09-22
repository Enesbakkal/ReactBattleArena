using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);

        // Aynı parola her Hash’te farklı string üretebilir (salt);
        // bu yüzden DB’de hash saklanır, login’de düz karşılaştırma yapılmaz. 
        // BCrypt.Net.BCrypt.HashPassword ürettiği string'in içine salt koyar.
        // Aynı parola iki kayıtta aynı kolon değerini vermek zorunda değildir.
        // Bu yüzden kayıt sırasında parola ile kolon == ile kıyaslanmaz.

        //BCrypt hash'i ile JWT imzası aynı işlem değildir.
        //BCrypt parolayı PasswordHash kolonunda saklar.
        //HMAC-SHA256 access token'ı imzalar. Access token'ın veritabanı satırı yoktur.
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}