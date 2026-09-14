using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
        // Aynı parola her Hash’te farklı string üretebilir (salt);
        // bu yüzden DB’de hash saklanır, login’de düz karşılaştırma yapılmaz.
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}