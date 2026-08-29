namespace ReactBattleArena.Abstractions;

public interface IRefreshTokenGenerator
{
    (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow);
    // Yukarıdaki Create üç değeri birden döndürüyor. Buna tuple (demet) denir.
}
