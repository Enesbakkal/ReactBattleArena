# Authentication sırası — 7 ile 9 arası, sınıf sınıf

Bu dosya `REACT-OGRENIM-V2.md` bölüm 7–9. Aşağıda işin **istek sırası** var: önce kayıt, sonra giriş, sonra token’lı yazma. Her adımda hangi sınıf ne yapıyor. VS Code’da o sınıfa git.

Bugünkü dosyalar şişmiş olabilir. Register handler’da `UserRoles`, login cevabında `RefreshToken`, karakter POST’ta `[HasPermission]` sonradan geldi. 16–28 Temmuz’un parçası değil.

Authentication kim olduğunu sorar. Authorization ne yapabileceğini sorar. 16 ve 22 Temmuz authentication. 27 Temmuz kapı: token yoksa yazamazsın. 28 Temmuz ilk authorization: Admin yazar, Player yazamaz.

---

## 1. Kayıt — `POST /api/auth/register` (16 Temmuz)

İstek tarayıcıdan veya Scalar’dan gelir. İlk karşılayan `RegisterRequest` (`Api/Contracts/RegisterRequest.cs`). JSON’daki `userName`, `email`, `displayName`, `password` burada durur. Hash yok, düz şifre var. Cevap bu sınıf değil.

`AuthController` (`Api/Controllers/AuthController.cs`) `POST register` action’ı bu gövdeyi alır. Kendisi hash yapmaz, kullanıcı yazmaz. `RegisterCommand` üretir, MediatR `Send` eder. İş biterse 201 ve `/api/users/{id}` döner. Cevapta hash yok, yalnız id. 16 Temmuz’da `[AllowAnonymous]` yoktu; endpoint’ler zaten kilitli değildi. O yazı 27 Temmuz’da geldi.

`RegisterCommand` (`Application/Authentication/Commands/RegisterCommand.cs`) Application’ın dilidir. Kullanıcı adı, mail, görünen ad, düz parola. Cevap `Guid`. Controller HTTP bilir, bu kayıt HTTP bilmez.

`RegisterCommandValidator` aynı klasörde, `Send`’den hemen sonra pipeline’da çalışır. Ad boş, mail bozuk, şifre altı karakterden kısa ise 400. Handler’a inilmez. “Bu mail alınmış mı” burada yok; o veritabanına bakar.

`RegisterCommandHandler` asıl kayıt işini yapar. Aynı `UserName` veya aynı email var mı diye `AnyAsync`. Varsa `ValidationException`, middleware 400. Yoksa hasher’ı çağırır. `User.Create`’e düz şifre gitmez. `SaveChanges`. Id döner. Bugün altında `Roles.Player` ve `UserRoles.Add` görürsen 16 Temmuz değil.

Handler hash’i kendi üretmez. `IPasswordHasher` (`Application/Abstractions/IPasswordHasher.cs`) sözleşmedir. `Hash` kayıtta düz parolayı saklanacak string’e çevirir. `Verify` girişte parolayı kayıttaki hash ile karşılaştırır. `Verify` 16 Temmuz’da yazıldı, çağıran 22 Temmuz.

`BCryptPasswordHasher` (`Infrastructure/Security/BCryptPasswordHasher.cs`) o sözleşmenin BCrypt hali. Application NuGet görmez. Bağlama `DependencyInjection.cs` içinde `AddSingleton<IPasswordHasher, BCryptPasswordHasher>()`. Aynı parolayı iki kez `Hash`’lesen sonuç farklı çıkabilir. Girişte `==` yok, `Verify` var.

`User` (`Domain/Users/User.cs`) satırın kendisi. `PasswordHash` alanı. `Password` kolonu yok. Hash geri açılmaz.

`UserConfiguration` kolonu zorunlu ve 500 karakter yapar. Tabloya kolon `AddUserPasswordHash` migration’ı ile girdi. `InitialCreate`’e elle yapıştırmadık.

Aynı gün ikinci bir yazma kapısı: `POST /api/users`, `CreateUserCommandHandler`. Register herkese açık üyelik. Bu endpoint admin’in elle kullanıcı açması. İkisinde de gövdeye düz şifre gelir. Tabloya düz gitmesin diye hasher ortak. Sonuç: kolona yalnız `PasswordHash`.

Kayıt bittiğinde üye tabloda durur, şifresi hash’lidir. Login yoktur. JWT yoktur. Yazmak için token şart değildir. Authentication’ın yarısı: “kim olduğunu kaydettik.”

---

## 2. Giriş — `POST /api/auth/login` (22 Temmuz)

Her istekte şifreyi tekrar yazmamak için üyenin elinde bir kanıt olması lazım. O kanıt JWT. Veritabanında token satırı yok. Sunucu imzalar, istemci taşır, sonraki istekte aynı anahtarla doğrular.

İstek yine JSON. `LoginRequest` (`Api/Contracts/LoginRequest.cs`) `userNameOrEmail` ve `password` taşır.

`AuthController` `POST login` bunu `LoginCommand` yapıp `Send` eder. Sonuç `null` ise 401, gövde yok. Doluysa 200 ve JSON’da token. `[AllowAnonymous]` 27 Temmuz.

`LoginCommand` ve `LoginResult` aynı dosyada (`Application/Authentication/Commands/LoginCommand.cs`). Komut: ad veya mail, parola. Sonuç: id, userName, email, token — ya da `null`. Kullanıcı yoksa da şifre yanlışsa da `null`. “Bu mail yok” demek üye listesini sızdırır. Bugün `RefreshToken` alanı var; 22 Temmuz’da yoktu.

`LoginCommandValidator` boş kimlik veya çok kısa şifreyi 400 yapar. Bu 401 değil.

`LoginCommandHandler` kullanıcıyı `UserName` veya `Email` ile arar. Yoksa `null`. Varsa `IPasswordHasher.Verify(parola, user.PasswordHash)`. Yanlışsa yine `null`. Doğruysa token ister. 22 Temmuz’da `SaveChanges` yok; JWT tabloya yazılmaz. Bugün alttaki refresh satırı 29 Ağustos.

Token’ı handler üretmez. `IJwtTokenService` (`Application/Abstractions/IJwtTokenService.cs`) `User` alır, string döner.

`JwtTokenService` (`Infrastructure/Security/JwtTokenService.cs`) `CreateToken` ile claim dizer: id, ad, email. 22 Temmuz’da rol claim’i yok. Key ile imzalar, `eyJ...` üretir. BCrypt parolayı saklar, JWT isteği imzalar.

`JwtOptions` (`Infrastructure/Security/JwtOptions.cs`) Key, Issuer, Audience, `ExpireMinutes` (60) okur. Anahtar `appsettings` `Jwt` bölümünde. Bugünkü `RefreshExpireDays` Ağustos.

`DependencyInjection.cs` hasher’ın yanına `Configure<JwtOptions>` ve `AddSingleton<IJwtTokenService, JwtTokenService>` ekler.

`Program.cs` gelen token’ı nasıl yoklayacağını bağlar. `AddAuthentication` + `AddJwtBearer`: imza, Issuer, Audience, süre. Key yoksa uygulama açılmaz. Pipeline: önce `UseAuthentication` (kimsin, `HttpContext.User`), sonra `UseAuthorization` (ne yapabilirsin). Ters olursa herkes 401. 22 Temmuz’da action’da `[Authorize]` yoktu; sıra yine doğru kuruldu.

Giriş bittiğinde elinde JWT vardır. Karakter POST hâlâ tokensız açılır. Kanıt var, kapı yok.

---

## 3. Sonraki istek — yazma, 401 (27 Temmuz)

İstemci `Authorization: Bearer eyJ...` koyar. `UseAuthentication` JwtBearer ile bakar. İmza ve süre tutmazsa veya header yoksa kullanıcı boştur.

`CharactersController` (ve kullanıcı yazma) action’larındaki `[Authorize]` boş kullanıcıyı 401 yapar. Karakter ekle, sil, güncelle token ister. Liste ve detay GET’te attribute yok; katalog bilerek açık bırakıldı. Herkes okusun, yazmak için giriş şart olsun diye.

`AuthController` login ve register `[AllowAnonymous]`. Yarın her yere `[Authorize]` konursa üyelik ve giriş yine çalışsın diye.

Aynı controller’da `GET /api/auth/me`, `[Authorize]`. Token’daki id’den kim olduğunu döner. 27 Temmuz’da id, userName, email. Bugünkü `permissions` 24 Ağustos. Sayfa yenilenince login JSON’u uçmuştur; token duruyorsa `/me` kim olduğunu söyler.

27 Temmuz’da rol yok. Giriş yapan herkes yazabilir. Authentication kapısı var (token şart). Authorization yok (Admin / Player yok).

---

## 4. Aynı istek, bir soru daha — 403 (28 Temmuz)

Kapı yalnız “token var mı” diyordu. Bunu ayırmak için `Users.Role` kolonu eklendi. Migration `AddUserRole`. `User` içinde `Role` ve `SetRole`. Eski satırlar ve yeni Register / CreateUser varsayılan `Player`. Çoğu kişi oyuncu olsun diye. İlk Admin koddan çıkmaz. SSMS’te `Role = Admin`, sonra yeniden login. Token login anında basılır; eski JWT’de yeni rol yoktur.

`Roles` (`Domain/Authorization/Roles.cs`) `Admin` ve `Player` const. Klasör `Authorization` (“ne yapabilirsin”). Register ve Login `Authentication`’da kaldı (“kimsin”).

`JwtTokenService.CreateToken` artık `ClaimTypes.Role` ve `user.Role` koyar.

`CharactersController` POST, PUT, DELETE o gün `[Authorize(Roles = Roles.Admin)]`. Bu claim’e bakar. Token yok: 401. Player token: 403, tanıdık, Admin değil. Admin token: 201. GET serbest.

Bugün o satır `[HasPermission(...)]`. 22 Ağustos. 28 Temmuz’u okurken Admin yazısını arama. Kapı aynı yerde, soru değişti: o gün “Admin misin”, sonra “bu fiil sende var mı”. `Users.Role` durur, `HasPermission` onu okumaz.

Scalar için `BearerSecuritySchemeTransformer` (`Api/OpenApi/BearerSecuritySchemeTransformer.cs`) ve `Program.cs` `AddPreferredSecuritySchemes("Bearer")`. API’yi kilitlemez. Send tuşu token’ı header’a koysun diye belgeye “Bearer JWT, yapıştır” yazar. Kilit boşsa Scalar tokensız atar, 401. Asıl 401/403 yine JwtBearer ve `[Authorize]`.

---

## Üç günü tek nefeste

16 Temmuz’da üye olmayı açtık. Şifre tabloya düz yazılmasın, aynı mail iki kez alınmasın diye hash koyduk. Sınıflar: `RegisterRequest` → `AuthController` register → `RegisterCommand` → `RegisterCommandValidator` → `RegisterCommandHandler` → `IPasswordHasher` / `BCryptPasswordHasher` → `User` → `AddUserPasswordHash`. Yan kapı: `CreateUserCommandHandler`.

22 Temmuz’da giriş açıldı. Her istekte şifre yazmamak için JWT. Sınıflar: `LoginRequest` → `AuthController` login → `LoginCommand` → `LoginCommandValidator` → `LoginCommandHandler` → `Verify` → `IJwtTokenService` / `JwtTokenService` + `JwtOptions` → `LoginResult`. Gelen istekte yoklama: `Program.cs` JwtBearer, `UseAuthentication`.

27 Temmuz’da yazmaya `[Authorize]`, login ile register `[AllowAnonymous]`, `/me`. Tokensız yazmak 401. Rol yok. Katalog GET bilerek açık.

28 Temmuz’da `Roles`, `Users.Role`, JWT’de rol claim’i, yazma yalnız Admin. Player POST 403. `BearerSecuritySchemeTransformer` denemeyi kolaylaştırır, kilidi koymaz.

Dosyada refresh, `UserRoles`, `[HasPermission]` görürsen 7–9’a katma. Omurga: hash, JWT, 401, sonra 403.


[Authorize] (yanında Roles = ... yokken) şunu sorar: geçerli bir JWT var mı? İmza tutuyor mu, süresi dolmamış mı. Tutuyorsa ASP.NET seni tanır, o metoda girersin. Tutmuyorsa 401. Burada “karakter ekleyebilir misin, silebilir misin” yok. Sadece giriş yapmışsın. 27 Temmuz bu.

JWT’ye “formdaki bilgiler dolduruldu” deme. Login’de şifre doğrulandı, token basıldı. Sonraki istekte o token header’da duruyor. [Authorize] o token’a bakıyor.

28 Temmuz’dan itibaren asıl “yapabilir mi” başlıyor. [Authorize(Roles = Admin)] Player’ı 403 yapar: tanıdık, bu işi yapamazsın. Ağustos’ta HasPermission / permission kodları aynı sorunun incelmiş hali: characters.create var mı.

Senin ayırdığın kelimelerle:

Yetki (bu projedeki [Authorize] kapısı): kapıdan geçtin, seni tanıyoruz. Token yok/ölü → 401.
İzin: şu fiili yapabilirsin. Yoksa 403. Rol string’i, sonra RBAC fiil listesi.
ASP.NET ikisine de “authorization” der; [Authorize] adı kafa karıştırır. Bizde 27 Temmuz authentication kapısı gibi çalışır, 28 Temmuz’dan sonra izin konuşulur. GET katalog hâlâ izinsiz açık; orada ne yetki ne izin sorulmuyordu.