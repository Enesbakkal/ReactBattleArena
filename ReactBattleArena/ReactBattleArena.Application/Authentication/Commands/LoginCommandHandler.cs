using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Abstractions;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Authentication;
using System.Runtime.ConstrainedExecution;
using static System.Net.WebRequestMethods;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult?>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenGenerator _refreshTokens;

    public LoginCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenGenerator refreshTokens)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokens = refreshTokens;
    }

    public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        //Kurallar geçince handler Users tablosunda kullanıcı adı veya e-posta eşleşen satırı arar. Yoksa null döner.
        //Varsa BCrypt Verify, yazılan parolayı satırdaki PasswordHash ile kontrol eder. Uymazsa yine null döner.
        var user = await _db.Users
            .FirstOrDefaultAsync(
                u => u.UserName == request.UserNameOrEmail || u.Email == request.UserNameOrEmail,
                cancellationToken);

        if (user is null)
            return null;

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        //Kurallar geçince handler Users tablosunda kullanıcı adı veya e - posta eşleşen satırı arar.Yoksa null döner.
        //Varsa BCrypt Verify, yazılan parolayı satırdaki PasswordHash ile kontrol eder.Uymazsa yine null döner.

        //İki başarısızlık da null olduğu için controller ikisine de 401 verir.
        //Cevap, bu e-postanın kayıtlı olup olmadığını ayırmaz.
        //BCrypt karşılaştırması == değildir.
        //Kayıttaki Hash her çağrıda(api/auth/me) farklı string üretebilir, çünkü çıktının içinde salt vardır. Verify o salt'ı hash string'inin içinden okur.

        //Parola tutunca handler iki token üretir. Access token IJwtTokenService.CreateToken ile gelir. Bu string veritabanına yazılmaz.
        var token = _jwtTokenService.CreateToken(user);

        var utcNow = DateTime.UtcNow;

        //Access token basıldıktan hemen sonra refresh token üretilir. Create bir tuple döner. Tuple'daki adlar Raw, Hash ve ExpiresAtUtc olur.
        //Raw ham refresh token'dır. Handler bu üç değeri şöyle karşılar: rawRefresh, hash, expires. rawRefresh yeni bir token değildir.
        //Raw ile gelen aynı string'in handler içindeki adıdır.
        var (rawRefresh, hash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(
            RefreshToken.Create(user.Id, hash, expires, utcNow));
        //Handler ham değeri LoginResult.RefreshToken alanına koyar. Hash'i entity'ye verir. Ham token RefreshTokens tablosuna yazılmaz.

        await _db.SaveChangesAsync(cancellationToken); // configurationa bak 


        return new LoginResult(user.Id, user.UserName, user.Email, token, rawRefresh);

        //Süre RefreshExpireDays kadardır. Varsayılan 7 gündür.
        //Access token 60 dakika, refresh token satırı 7 gün.
        //İkisi aynı saat değildir. SaveChangesAsync yalnız refresh token satırını yazar.
        //JWT bu SaveChanges ile diske gitmez. Kayıt başarılı olursa LoginResult hem JWT'yi (Token) hem ham refresh token'ı (RefreshToken) taşır.
        //Bu ikinci alan handler'daki rawRefresh değişkenidir. SaveChanges patlarsa return satırına gelinmez, 200 gitmez, rawRefresh de cevapta yer almaz.
        //Her başarılı giriş yeni bir RefreshTokens satırı ekler. Eski satırı silmez.İki tarayıcı iki ham token tutabilir.Eski satırın iptali ve yenisiyle değişmesi oturumu uzatma işidir.
    }
}