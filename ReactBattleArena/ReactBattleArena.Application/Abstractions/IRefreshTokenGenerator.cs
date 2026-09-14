namespace ReactBattleArena.Abstractions;

public interface IRefreshTokenGenerator
{
    string Hash(string Raw);
    // Login ve refresh aynı SHA256'yı kullansın. BCrypt değil — her seferinde farklı tuz üretir, unique index araması bozulur.
    // Refresh isteği ham token'ı gönderecek, sunucu onu hash'leyip TokenHash kolonundan arayacak.
    // Aramanın çalışması için hash formülünün login ile birebir aynı olması şart; o yüzden formülü tek metoda topluyoruz.
    (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow);
    // Yukarıdaki Create üç değeri birden döndürüyor. Buna tuple (demet) denir.
}
