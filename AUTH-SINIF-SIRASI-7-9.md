# Authentication sırası — 7–9, 12–13, 22, 23, 24, 25, 26, 27

Bu dosya `REACT-OGRENIM-V2.md` bölüm 7–9, 12–13, 22, 23, 24, 25, 26 ve 27. İstek sırası: kayıt, giriş, token’lı yazma, tarayıcının token’ı saklaması, listeye Bearer, her sayfanın `api.ts` kapısı, hangi ekranın hangi controller metoduna gittiği, RBAC model, tabloların SQL’e dökülmesi ve seed, yazma kapısının fiil join’ine geçmesi, Register’ın `UserRoles` yazması ve `GET /api/auth/me` permissions listesi. Her adımda hangi sınıf ne yapıyor. VS Code’da o sınıfa git.

12–13’te asıl iş JWT’yi `localStorage`’a koymak ve `GET /api/characters` header’ına takmak. Buton gizleme (`permissions.ts`, `hasPermission`, `&&`) 24 Ağustos, bölüm 28. Burada kısa geçiyor. İnce ayrıntı 28–31.

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

CharactersController (ve kullanıcı yazma) action’larındaki `[Authorize]` boş kullanıcıyı 401 yapar. Karakter ekle, sil, güncelle token ister. Liste ve detay GET’te attribute yok; katalog bilerek açık bırakıldı. Herkes okusun, yazmak için giriş şart olsun diye.

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

28 Temmuz’da `Roles`, `Users.Role`, JWT’de rol claim’i, yazma yalnız Admin. Player POST 403. `BearerSecuritySchemeTransformer` denemeyi kolaylaştırır, kilidi koymaz. O gün şema tek kolon: kullanıcıya `Admin` veya `Player` yazılır. Bir kullanıcının birden fazla rolü, rolün fiil listesi yok. O tablo fikri bölüm 24.

Dosyada refresh, `UserRoles`, `[HasPermission]` görürsen 7–9’a katma. Omurga: hash, JWT, 401, sonra 403.

[Authorize] (yanında Roles = ... yokken) şunu sorar: geçerli bir JWT var mı? İmza tutuyor mu, süresi dolmamış mı. Tutuyorsa ASP.NET seni tanır, o metoda girersin. Tutmuyorsa 401. Burada “karakter ekleyebilir misin, silebilir misin” yok. Sadece giriş yapmışsın. 27 Temmuz bu.

JWT’ye “formdaki bilgiler dolduruldu” deme. Login’de şifre doğrulandı, token basıldı. Sonraki istekte o token header’da duruyor. [Authorize] o token’a bakıyor.

28 Temmuz’dan itibaren asıl “yapabilir mi” başlıyor. [Authorize(Roles = Admin)] Player’ı 403 yapar: tanıdık, bu işi yapamazsın. Ağustos’ta HasPermission / permission kodları aynı sorunun incelmiş hali: characters.create var mı.

Senin ayırdığın kelimelerle:

Yetki (bu projedeki [Authorize] kapısı): kapıdan geçtin, seni tanıyoruz. Token yok/ölü → 401.
İzin: şu fiili yapabilirsin. Yoksa 403. Rol string’i, sonra RBAC fiil listesi.
ASP.NET ikisine de “authorization” der; [Authorize] adı kafa karıştırır. Bizde 27 Temmuz authentication kapısı gibi çalışır, 28 Temmuz’dan sonra izin konuşulur. GET katalog hâlâ izinsiz açık; orada ne yetki ne izin sorulmuyordu.

---

## Takılan dört soru

`UserConfiguration` içinde `HasKey(x => x.Id)` birincil anahtardır. SQL Server’da PK varsayılan clustered index’tir. Satırlar `Id` sırasına göre durur. `HasIndex(x => x.UserName).IsUnique()` ve `HasIndex(x => x.Email).IsUnique()` ayrı unique index’lerdir. Clustered değiller. Tabloda clustered index bir tanedir; o zaten `Id`. UserName ve Email nonclustered unique index’tir. Aynı ad veya aynı mail ikinci kez yazılamasın diye dururlar. `IsClustered(false)` yazmadık. EF, SQL Server’da PK dışı unique index’i nonclustered basar.

`new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())` içindeki `sub` JWT’nin subject alanıdır. Token kimin hakkında. Bizde kullanıcı `Id`. `ClaimTypes.NameIdentifier` aynı id’yi ikinci isimle koyar. `/me` ikisinden birini arar. `sub` şifre değildir, rol de değildir.

`CharactersController`’dan `[Authorize]` kalkınca kapı kapanmadı. GET’te zaten yoktu. POST, PUT, DELETE’te 28 Temmuz’daki `[Authorize(Roles = Roles.Admin)]` gitti, yerine `[HasPermission(PermissionCodes.CharactersCreate)]` (update ve delete kodları da) geldi. `HasPermissionAttribute` `AuthorizeAttribute`’tan türer. Hâlâ authorization attribute’udur. Policy adı `Permission:` plus fiil. Token yoksa 401. Token var, fiil yoksa 403. Üstüne ayrıca `[Authorize]` yazmana gerek yok. HasPermission hem girişi hem fiili sorar. İnce ayrıntı bölüm 26.

Register’ın Player olması seed değildir. `RegisterCommandHandler` `User.Create(..., Roles.Player, ...)` ile `Users.Role` kolonuna Player yazar. Aynı handler `Roles` tablosundan Player satırını okur, `UserRoles`’a bağlar. O satırı `AuthSeeder` uygulama açılınca koyar. Seed olmasa `SingleAsync` patlar. `AddUserRole` migration’ındaki `defaultValue: "Player"` kolon eklenirken eski kullanıcılar boş kalmasın diyedir. Yeni üye her seferinde o default’a bağlanmaz. `CreateUserCommandHandler` de `Roles.Player` basar. Yeni kayıtta Player’ı kod yazar. Seed, Player rolünü tabloda bulundurur.

---

## A. Tarayıcı — bölüm 12 ve 13 (30 Temmuz)

Backend token basıyor. 30 Temmuz’da tarayıcı onu alıp saklar, sonraki GET’te header’a koyar. Scalar’daki kilit kutusunun React hali budur.

### 5. Login cevabındaki JWT — `LoginPage` ve `api.ts` (bölüm 12)

Form `LoginPage` (`web/src/LoginPage.tsx`). `handleSubmit` `POST /api/auth/login` atar. 30 Temmuz’da doğrudan `fetch('https://localhost:7275/api/auth/login', ...)` vardı. Bugün `apiFetch('/api/auth/login', { method: 'POST', auth: false, body: { userNameOrEmail, password } })`. `auth: false` şart. Login’de henüz token yok. Varsayılan `apiFetch` Bearer takar; boş veya eski token gitmesin diye kapatılır.

`apiFetch` (`web/src/api.ts`) tarayıcının `HttpClient`’ı gibi. `API_BASE` `https://localhost:7275`. Path birleşir. Body varsa `Content-Type: application/json` ve `JSON.stringify`. Bu dosya 11 Ağustos. Davranış 30 Temmuz `fetch`’i ile aynı, URL her sayfada tekrar etmez.

Başarıda `response.ok` (200–299). `data.token` gelir. `setToken` `localStorage` anahtarı `token` altına yazar. F5 atınca React state ölür, bu anahtar kalır. 30 Temmuz’da `LoginPage` `localStorage.setItem` yazıyordu. Bugün `setToken` / `getToken` / `clearToken` aynı çekmece. `setRefreshToken` 29 Ağustos; 30 Temmuz’da yoktu.

Yanlış şifre 401. `ok` false. “Giriş başarısız.” Mail sızmaz. `catch` ağ, CORS, sertifika. CORS 401 değildir.

Günün sonu: 5173 origin’inde `token` durur. Liste henüz yok. Kimse Bearer takmaz.

`apiFetch` içinde sıra iki ayrı istektir. `headers.Authorization = Bearer ...` yazılmadan önce **bu** fonksiyon backende gidip JWT almaz. JWT login’de gelmiş, çekmecede durur.

```30:60:web/src/api.ts
export async function apiFetch(
  path: string,
  options: ApiFetchOptions = {},
): Promise<Response> {
  const { method = 'GET', body, auth = true } = options

  const headers: Record<string, string> = {}

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  if (auth) {
    // Backend'e henüz gidilmedi. JWT bu satırda Api'den gelmez.
    // Login'de gelmişti: POST /api/auth/login → data.token → setToken → localStorage 'token'.
    // Şimdi çekmeceden okuyoruz, header'a yazıyoruz, ONDAN SONRA alttaki fetch gider.
    const token = getToken()
    if (token) {
      headers.Authorization = `Bearer ${token}`
    }
  }

  // İstek burada backende çıkar (7275). Header'da Bearer varsa Api JWT'yi burada görür.
  // Cevap bu return: status + gövde. JSON'u sayfa response.json() ile açar.
  // Login/register auth: false olduğu için yukarıdaki if çalışmaz; o istek tokensız gider, JWT cevapta gelir.
  return fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })
}
```

Login: `auth: false`. Yukarıdaki `if (auth)` çalışmaz. `fetch` tokensız gider. Cevaptaki `data.token` `setToken` ile çekmeceye yazılır.

Liste: `auth` varsayılan `true`. `getToken` okur, Bearer yazılır, **ondan sonra** `fetch` 7275’e çıkar. Kapıya gitmeden önce evdeki kâğıdı header’a yapıştırırsın. Kâğıdı bu istekte kapıdan almazsın.

### 6. Liste ve Bearer — `CharactersPage` (bölüm 13)

Bölüm 12’nin sonu: `setToken` JWT’yi `localStorage` anahtarı `token` altına yazdı. `CharactersPage` o anahtarı **okumaz**. `getToken` da bu sayfada yok. Sayfa yalnız liste ister.

`load` içinde gördüğün satır şu: `apiFetch('/api/characters?page=1&pageSize=20')`. `auth` yazılmamış, varsayılan `true`. Bearer işi `web/src/api.ts` içindeki `apiFetch`’tedir. Orada `getToken()` çekmeceden okur, token varsa `Authorization: Bearer ...` koyar, `fetch` atar. Zincir: `CharactersPage` → `apiFetch` → `getToken` → header.

30 Temmuz’da `api.ts` yoktu. Aynı sayfada yorumda duran eski `load` hem `localStorage.getItem('token')` okuyordu hem `fetch` header’ına `Authorization: Bearer ${token}` yazıyordu. 11 Ağustos’ta o iki satır `apiFetch`’e taşındı. O yüzden bugün `CharactersPage`’de `getToken` arama.

`useEffect(..., [])` sayfa ilk çizilince bu `load`’u çalıştırır.

`GET /api/characters` controller’da `[Authorize]` yok. Tokensız da 200 gelir. Header’ı yine koyduk. Yazma (POST) token isteyecekti. Header alışkanlığı burada başladı.

Cevap `{ items, totalCount }`. `CharacterRow` TypeScript yüzü: `id` string (Guid JSON’da string), `name`, `universe`, `rarity`. Liste `data.items`.

Alttaki “izin sınıfları” 13 Temmuz’un parçası değil. Bearer’dan ayrı. Bugün aynı `CharactersPage` dosyasında Ekle linki durduğu için burada kısa duruyor. `usePermissions` token okumaz. Fiil listesini okur.

### Frontend izin sınıfları — kısa (asıl anlatım 28–31)

30 Temmuz’da Ekle / Düzenle / Sil gizleme yoktu. Bugün listede duruyor. Backend `[HasPermission]` kapısı durur. Link yok diye POST açılmaz. Player URL’ye `/characters/new` yazarsa form açılabilir; asıl 403 API’dedir. UI yalnız rahatsız etmemek içindir.

`permissions.ts` iki şey export eder. `PERMISSIONS` sabitleri: `charactersCreate` değeri `'characters.create'`, aynı şekilde update ve delete. Yazım hatası olmasın diye. `hasPermission(permissions, code)` dizide kod var mı diye `includes` bakar. C# `codes.Contains("characters.create")` ile aynı fikir. React hook değildir. Düz fonksiyon.

Dizi nereden gelir. `AppLayout` `GET /api/auth/me` atar, `permissions` state’ine koyar. `PermissionContext.Provider` o diziyi alt sayfalara verir. `usePermissions` (`PermissionContext.tsx`) diziyi okur. Login layout dışında. Orada `usePermissions` çağırma. İnce ayrıntı bölüm 30.

`CharactersPage` `const permissions = usePermissions()` alır. JSX’te kapı şöyle:

`hasPermission(permissions, PERMISSIONS.charactersCreate) && <Link to="/characters/new">Karakter ekle</Link>`

`charactersCreate` yani `"characters.create"` listede varsa Karakter ekle çizilir. Aynı fikir: `characters.update` varsa Düzenle, `characters.delete` varsa Sil. `&&`’in solu koşul, sağı `Link` değil. JavaScript: `A && B`. A yanlışsa B’ye bakılmaz, sonuç false. A doğruysa sonuç B. React JSX’te false görünce hiçbir şey basmaz. True ise sağdaki `Link` veya `button` basılır. `hasPermission(...)` kapı. `Link` kapı açıksa çizilecek parça.

Detay, Create/Edit `Navigate` kapısı, `meLoaded`, Context’e geçiş bölüm 28–31.

### 12–13 tek nefeste

12: `LoginPage` → `apiFetch` `auth: false` → `POST /api/auth/login` → `setToken` (`localStorage` `token`).

13: `CharactersPage` `load` → `apiFetch` → `api.ts` içinde `getToken` + Bearer → `GET /api/characters`.

Sonra (kısa): `permissions.ts` `PERMISSIONS` + `hasPermission`, dizi `AppLayout` `/me` + `PermissionContext` + `usePermissions`, listede `&&` ile Link.

---

## B. Tek kapı — bölüm 22 (11 Ağustos)

12–13’te login token yazar, liste `apiFetch` ile okur. O gün diğer sayfalar hâlâ kendi `https://localhost:7275` ve kendi `fetch`’lerini kopyalıyordu. 11 Ağustos’ta URL, JSON, Bearer tek dosyaya indi: `web/src/api.ts`. Backend’de yeni action yok. 5173 tarafında `HttpClient` tek oldu.

Yedi yerde aynı base URL ve aynı `Authorization` satırı vardı. Biri yanlış yazılırsa yedi dosyada ararsın. C#’ta `HttpClient` + `BaseAddress` aynı fikir. Eski `fetch` blokları sayfalarda yorumda durur. Canlı giden tek `fetch` `apiFetch` içindedir. Bearer’ın `getToken`’dan sonra `fetch`’e gittiğini 12–13’te gördün. 22 herkesin o helper’ı kullanmasıdır.

### `api.ts` parçaları

`API_BASE` Api’nin kökü. Path `/api/...` ile birleşir. Path’e tam URL yazarsan adres `https://localhost:7275https://...` olur.

```1:14:web/src/api.ts
export const API_BASE = 'https://localhost:7275'

export function getToken(): string | null {
  return localStorage.getItem('token')
}

export function setToken(token: string) {
  localStorage.setItem('token', token)
}

export function clearToken() {
  localStorage.removeItem('token')
  localStorage.removeItem('refreshToken')
}
```

`getToken` çekmeceden okur. `setToken` login cevabını yazar. `clearToken` çıkışta siler. Bugün yanında `refreshToken` de gider. 11 Ağustos’ta yalnız `token` siliniyordu. `getRefreshToken` 29 Ağustos. 22’nin işi değil.

```62:67:web/src/api.ts
type ApiFetchOptions = {
  method?: string
  body?: unknown
  /** false = login/register (Bearer yok). Varsayılan true. */
  auth?: boolean
}
```

Bu `type` derleme zamanı şekildir. `new ApiFetchOptions()` yok. C# DTO şekline yakın, runtime class değil. `?` alan gelmeyebilir. `unknown`: body bir şey olabilir, stringify aşağıda. `auth: false` Bearer takma. `auth = true` “auth geçildi” değil, “Bearer dene.”

```69:105:web/src/api.ts
export async function apiFetch(
  path: string,
  options: ApiFetchOptions = {},
): Promise<Response> {
  const { method = 'GET', body, auth = true } = options

  const headers: Record<string, string> = {}

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  if (auth) {// LOgin yaparken auth olmadığı için buraya girmez
    // Backend'e henüz gidilmedi. JWT bu satırda Api'den gelmez.
    // Login'de gelmişti: POST /api/auth/login → data.token → setToken → localStorage 'token'.
    // Şimdi çekmeceden okuyoruz, header'a yazıyoruz, ONDAN SONRA alttaki fetch gider.
    const token = getToken()
    if (token) {
      headers.Authorization = `Bearer ${token}`
    }
  }

  // İstek burada backende çıkar (7275). Header'da Bearer varsa Api JWT'yi burada görür.
  // Cevap bu return: status + gövde. JSON'u sayfa response.json() ile açar.
  // Login/register auth: false olduğu için yukarıdaki if çalışmaz; o istek tokensız gider, JWT cevapta gelir.
  
  //  return fetch(`${API_BASE}${path}`, {  // Burayı yorum yaptık çünkü refresh tokenın sessizce yenilenme mantığını eklemek istedik
  //    method,
  //   headers,
  //    body: body !== undefined ? JSON.stringify(body) : undefined,
  //  })

  const response = await fetch(`${API_BASE}${path}`, {
  method,
  headers,
  body: body !== undefined ? JSON.stringify(body) : undefined,
  })
```

`Promise<Response>` bitince `.ok`, `.status`, `.json()` elindedir. C# kabaca `Task<HttpResponseMessage>`. İkinci argüman yoksa `{}` → GET ve `auth: true`. `Record<string, string>` C# `record` değil, `Dictionary<string, string>` gibi sözlük.

`body` varsa `JSON.stringify` burada. Sayfa `[object Object]` gönderemez. Ağ koparsa `fetch` fırlatır, sayfa `catch` der. 401/403 fırlatmaz, `ok` false kalır. 11 Ağustos bu `fetch`’te biterdi. Bugün altında 401’de `refreshSession` var. O bölüm 34. 22’yi okurken o kuyruğu 11 Ağustos sanma.

### Sayfalar

Login Bearer takmaz. Token henüz yok. 200 olunca `setToken` yazar.

```40:56:web/src/LoginPage.tsx
      const response = await apiFetch('/api/auth/login',{
        method: 'POST',
        auth: false,
        body: {
          userNameOrEmail,
          password
        },
      })

      if (!response.ok) {
        setError('Giriş başarısız')
        return
      }

      const data = await response.json()
      setToken(data.token)
      setRefreshToken(data.refreshToken)
```

`setRefreshToken` 29 Ağustos. 11 Ağustos’ta yalnız `setToken` vardı.

Register da tokensız gider. Başarıda token yazmaz, login sayfasına atar.

```35:44:web/src/RegisterPage.tsx
      const response = await apiFetch('/api/auth/register', {
        method: 'POST',
        auth: false,
        body: {
          userName,
          email,
          displayName: displayName || null,
          password,
        }
      })  
```

Liste `auth` yazmaz, varsayılan `true`. Bearer `api.ts` içinde takılır.

```62:72:web/src/CharactersPage.tsx
  async function load() {
    try{
      const response = await apiFetch('/api/characters?page=1&pageSize=20')

      if(!response.ok) {
        setError('Karakterler Alınmadı')
        return
      }

      const data = await response.json()
      setItems(data.items)
```

Create önce kısa liste önizler, sonra POST atar. İkisinde de `auth` yok, Bearer gider. 403 olursa “Yetkin yok.”

```51:51:web/src/CharacterCreatePage.tsx
      const response = await apiFetch('/api/characters?page=1&pageSize=8')
```

```99:111:web/src/CharacterCreatePage.tsx
      const response = await apiFetch('/api/characters', {
        method: 'POST',
        body: {
          name,
          universe,
          biography: biography || null,
          rarity,
          baseAttack,
          baseDefense,
          baseSpeed,
          imageUrl: imageUrl || null,
        },
      })
```

Detay yükleme GET, silme aynı path DELETE. 401 ve 403’e sayfa bakar.

```56:56:web/src/CharacterDetailPage.tsx
        const response = await apiFetch(`/api/characters/${id}`)
```

```113:115:web/src/CharacterDetailPage.tsx
      const response = await apiFetch(`/api/characters/${id}`, {
        method: 'DELETE',
      })
```

Edit formu doldurmak için GET, kaydetmek için PUT.

```53:53:web/src/CharacterEditPage.tsx
        const response = await apiFetch(`/api/characters/${id}`)
```

```116:127:web/src/CharacterEditPage.tsx
      const response = await apiFetch(`/api/characters/${id}`, {
        method: 'PUT',
        body: {
          name,
          universe,
          biography: biography || null,
          rarity,
          baseAttack,
          baseDefense,
          baseSpeed,
          imageUrl: imageUrl || null,
        },
      })
```

Layout oturum var mı diye `getToken` bakar. Bu, `apiFetch`’in Bearer takmasından ayrı. Yoksa `/login`. Varsa `/me` (24 Ağustos). Çıkışta `clearToken`.

```13:13:web/src/AppLayout.tsx
  const token = getToken()
```

```24:24:web/src/AppLayout.tsx
      const meResponse = await apiFetch('/api/auth/me')
```

```52:55:web/src/AppLayout.tsx
  function handleLogout() {
    clearToken()
    navigate('/login')
  }
```

Create, Edit, Detail kendi `getToken()` ile “token yoksa login” diye de bakabilir. Bearer yine `apiFetch` içindedir.

### `Link` ile `apiFetch`

```95:96:web/src/CharactersPage.tsx
          {hasPermission(permissions, PERMISSIONS.charactersCreate) && (
            <Link to="/characters/new">Karakter ekle</Link>
```

`Link` yalnız adresi değiştirir. 7275’e gitmez. Api’ye gitmek `apiFetch` ister.

### 22 tek nefeste

`api.ts` kök, çekmece, `apiFetch`. Login ve register `auth: false`. Liste, create, detail, edit, `/me` Bearer’lı. Tek canlı `fetch` `api.ts`’te. Hangi path’in hangi metoda gittiği bölüm 23.

---

## C. Hangi sayfa hangi metoda gider — bölüm 23 (13 Ağustos)

13 Ağustos’ta yeni sınıf yazılmadı. Amaç karışıklığı kesmekti. Adres çubuğunda da `characters` var, `apiFetch` yolunda da `characters` var. İkisi aynı kapı gibi durur. Değiller.

React Router path hangi **ekranın** açılacağını seçer. `apiFetch` hangi **controller action**’ın çalışacağını seçer. `<Link to={...}>` neredeyse her zaman router’dır. Backend’e gitmek için sayfanın içinde `apiFetch` gerekir. Düzenle yazdık diye C#’ta `Edit` arama. Bizde metot `Update`, HTTP `PUT`.

### Login örneği: biri Api, biri yalnız sayfa

```40:47:web/src/LoginPage.tsx
      const response = await apiFetch('/api/auth/login',{
        method: 'POST',
        auth: false,
        body: {
          userNameOrEmail,
          password
        },
      })
```

Bu çağrı `POST /api/auth/login` atar. `AuthController.Login` alır, `LoginCommand` çalışır (bölüm 8). Aynı sayfadaki `Link to="/register"` ise sadece `RegisterPage`’i açar. Kayıt POST’u ayrı bir `apiFetch`’tir. Link tıklanınca register endpoint’i çalışmaz.

### Liste: URL’de GetPaged yazılmaz

Karakter listesini açınca `CharactersPage` `load` içinde şunu atar:

```62:64:web/src/CharactersPage.tsx
  async function load() {
    try{
      const response = await apiFetch('/api/characters?page=1&pageSize=20')
```

Karşıda metot adı `GetPaged`’dir. Bu isim tarayıcıya yazılmaz. ASP.NET HTTP yöntemi ve route şablonu ile eşler. `[Route("api/[controller]")]` + `[HttpGet]` → `/api/characters`. Query’deki `page` ve `pageSize` metoda parametre olur. Controller `GetCharactersQuery` gönderir, JSON `{ items, totalCount }` döner. React `setItems(data.items)` der.

```24:32:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HttpGet]
    [ProducesResponseType(typeof(PagedCharacterRowsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedCharacterRowsResult>> GetPaged(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCharactersQuery(page, pageSize), cancellationToken);
        return Ok(result);
```

Handler’da `pageSize` 200 tavanına sıkıştırılır. 200 varsayılan sayfa boyutu değil. Biri `pageSize=5000` yazarsa veritabanını kilitlemesin diye. Liste 20 ister, 20 gelir. `Skip((page - 1) * pageSize)` çünkü ekranda 1. sayfa deriz, SQL 0’dan sayar. Sayfa 1 → Skip 0. Sayfa 2 → Skip 20.

Aynı parametre adlarıyla başka iş yapan bir metot yazabilir misin? Yazabilirsin. Adları değiştirmen gerekmez. ASP.NET C# metot adına bakmaz. Eşleme HTTP yöntemi ve route şablonudur.

`GetPaged` `[HttpGet]` ile `/api/characters` olur. `GetById` `[HttpGet("{id:guid}")]` ile `/api/characters/{guid}` olur. İkisinde de `CancellationToken` var; karışmaz. `Update` da `Guid id` alır; adı `GetById` ile aynı, farkı `PUT`:

```69:77:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersUpdate)]  // PUT
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
    Guid id,
    [FromBody] CreateCharacterRequest body,
```

Aynı `page` / `pageSize` adlarını başka bir action’da da kullanabilirsin. Örnek: `[HttpGet("search")]` yine `[FromQuery] int page` alabilir. Query adı `?page=` ile bağlanır; başka metoddaki `page` ile çakışmaz.

Çakışma parametre adından değil, aynı kapıdan gelir. İkinci bir `[HttpGet]` de çıplak `/api/characters` olursa ASP.NET hangisini seçeceğini bilemez. `page`’i `sayfa` yapsan da olmaz.

### Karta tıklayınca Api gitmez; detay sayfası gider

```11:13:web/src/CharacterCard.tsx
function CharacterCard({ id, name, universe, rarity, imageUrl }: CharacterCardProps) {
  return (
      <Link to={`/characters/${id}`} className="character-card-link">
```

Bu `Link` 7275’e istek atmaz. Adresi `/characters/{guid}` yapar. Router `CharacterDetailPage`’i açar.

Karta basınca `id` detay sayfasına prop olarak gitmiyor. Kart kapanıyor, `CharacterDetailPage` yeni açılıyor. `id` adres çubuğunda gidiyor. `App.tsx`’te path `/characters/:id`. Router URL’deki o parçayı `:id` diye alıyor. Yeni sayfa `useParams()` deyince router o Guid’i veriyor. Yani aynı Guid önce kartta, sonra adreste, sonra `useParams`’ta. (React tarafını sonra ayrıntılı işleriz.)

`useParams` path’e Guid yazmaz. Adresteki `:id` parçasını okur. Yazan karttaki `Link`: `to={`/characters/${id}`}`. Router adresi değiştirir. Kart kapanır.

```22:23:web/src/CharacterDetailPage.tsx
function CharacterDetailPage() {
  const { id } = useParams<{ id: string }>()
```

Bu `id` URL’deki Guid’dir. Karttan prop gelmez. Kodun kalanı bunu kullanır: `apiFetch(\`/api/characters/${id}\`)`. Karttaki Guid ile aynı değer, çünkü URL’ye o yazılmıştı. Kaynak artık kart değil, adres çubuğu. Edit sayfasında da aynı satır var (`CharacterEditPage` satır 9); oradaki `id` de URL’den gelir.

Asıl GET orada başlar. `apiFetch(\`/api/characters/${id}\`)` atılır. Controller `[HttpGet("{id:guid}")] GetById` bunu `GetCharacterByIdQuery`’ye çevirir.

```35:44:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CharacterDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CharacterDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCharacterByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
```

Liste DTO’sunda `BaseAttack` gibi istatistikler vardır (`CharacterRowDto`). Kart onları çizmez. API unutulmuş değil. Liste sade dursun diye UI tercihi. Arena’da ATK satırı ayrı ürün kararı.

### SPA cache değildir

SPA deyince bazen şöyle sanılır: uygulama bir kez bütün JSON’u çeker, gerisi hafızadan okunur, sunucuya bir daha gitmez. Bizde öyle değil.

SPA’nın verdiği şey tam HTML sayfa yenilememektir. Adres değişir, kabuk (`AppLayout`) kalır, ortadaki ekran değişir. Veri hâlâ HTTP’dir. Liste kendi `GetPaged`’ini atar. Detay kendi `GetById`’sini atar. Edit yine kendi `GetById`’sini atar. Listedeki `items` state’i detaya props ile taşınmaz.

Bunu bilinçli yaptık. Başka biri aynı karakteri güncellediyse sen taze satır görürsün. Bedeli ekstra istek. Listeyi “cache” gibi detaya versen istek azalır, ekran bayat kalabilir.

Edit sayfası açılınca zaten kendi `GetById`’sini atıyor. Form o anki satırla dolar. F5 şart değil. Form açıkken başka biri kaydederse senin input’lar eski kalır. Kaydete basarsan senin eski değerler üzerine yazar. O durumda sayfayı yenilemek (veya edit’e tekrar girmek) taze satırı çeker.

Bizde “kim son yazdı kazandı”; çakışma kilidi yok. İleride iki kişi aynı karakteri aynı anda düzenlerse `rowversion` (veya benzeri concurrency token) eklemek gerekir. Şimdiki öğrenme ve tek admin kaydı için şart değil. Auth bitmeden bunu yazmıyoruz.

### `/characters` yazınca ne olur, sırayla

Adres çubuğuna `/characters` yazarsın. `/` yazarsan `App.tsx` seni `/characters`’a çevirir. `AppLayout` kabuğu açılır, `Outlet` içine `CharactersPage` oturur.

Sayfa ilk çizildiğinde `items` boş dizidir. Grid boş görünür. Çünkü veri henüz gelmemiştir. `useEffect` bağımlılık dizisi `[]` olduğu için bu mount’ta bir kez `load` çalışır. `load` `GetPaged`’e gider, cevap gelince `setItems` olur, `map` `CharacterCard` çizer. Effect’in neden `[]` olduğu bölüm 19’da durur. Burada bilmen gereken: boş grid hata değil, isteğin henüz bitmemiş olmasıdır.

### Detaydan düzenle: yine Link, kaydetince PUT

Detaydaki Düzenle bir `Link`’tir. `/characters/{id}/edit` açılır. 13 Ağustos’ta bu link herkese görünürdü. Asıl kapı yine API. Edit sayfası detayın state’ini almaz. Formu doldurmak için aynı karakteri `GetById` ile bir daha ister. Kaydete basınca:

```116:118:web/src/CharacterEditPage.tsx
      const response = await apiFetch(`/api/characters/${id}`, {
        method: 'PUT',
        body: {
```

Bu `PUT` `CharactersController.Update`’e gider. 13 Ağustos’ta `[Authorize(Roles = Admin)]` idi. Bugün `[HasPermission(CharactersUpdate)]`. 204 gelir, gövde yoktur, `json()` çağırma. Sayfa detaya döner. Player PUT denerse 403. Çakışma kilidi yok; form açıkken son kayıt kazanır (yukarıda SPA paragrafı).

### `App.tsx` veri taşımaz

```16:21:web/src/App.tsx
      <Route element={<AppLayout />}>
        <Route path="/characters" element={<CharactersPage />} />
        <Route path="/characters/new" element={<CharacterCreatePage />} />
        <Route path="/characters/:id/edit" element={<CharacterEditPage />} />
        <Route path="/characters/:id" element={<CharacterDetailPage />} />
      </Route>
```

Bu harita URL’yi ekrana bağlar. JSON her sayfanın `apiFetch` ve `useState` işidir. `Outlet` çocuğu layout’un ortasına koyar (bölüm 20). ASP.NET’te route’un action seçmesi gibi fikir; burada action değil, hangi React sayfası.

### 23 tek nefeste

5173 sayfa seçer. 7275 action çalıştırır.

Login `POST /api/auth/login` → `Login`. Register `POST /api/auth/register` → `Register`. Liste `GET /api/characters` → `GetPaged`. Kart tıklanınca yalnız router. Detay `GET /api/characters/{id}` → `GetById`. Create `POST /api/characters` → `Create`. Edit `PUT` → `Update`. Sil `DELETE` → `Delete`.

13 Ağustos’ta yazma hâlâ Admin string’iydi. Fiil tabloları bölüm 24.

---

## D. Rol ve fiil tabloları — bölüm 24 (20 Ağustos)

23’ün sonunda karakter yazmak hâlâ `[Authorize(Roles = Admin)]` idi. JWT içindeki rol claim’ine bakıyorduk. 20 Ağustos’ta React’e dokunmadık. Yeni endpoint de yok. Domain’e `Role`, `Permission` ve iki ara tabloyu koyduk. SQL henüz yok; migration 21 Ağustos (bölüm 25). `[HasPermission]` 22 Ağustos (bölüm 26). Login o gün de `Users.Role` string’ini token’a basıyor.

### `Users.Role` neden yetmedi

28 Temmuz’da kullanıcıya tek etiket verdik. Kolon hâlâ duruyor:

```22:24:ReactBattleArena/ReactBattleArena.Domain/Users/User.cs
    public string Role { get; private set; } = null!;
    //= null!; = “derleyiciye: başlangıçta null görünebilir ama runtime’da asla null kalmayacak” demek.
    //null-forgiving (!) işareti
```

Sabitler `Roles` sınıfında:

```5:9:ReactBattleArena/ReactBattleArena.Domain/Authorization/Roles.cs
public static class Roles
{
    public const string Admin = "Admin";
    public const string Player = "Player";
    public const string ShopOwner = "ShopOwner";
```

`CharactersController` yazma action’ları o gün `[Authorize(Roles = Roles.Admin)]` diyordu. Soru “Admin misin?” idi. ShopOwner eklemek her Admin kontrolünü tek tek düşünmek demek. Player yarın karakter eklesin dersen controller’ı yeniden yazarsın. Bundan sonra sormak istediğimiz şey “Admin misin” değil, “`characters.create` sende var mı”.

`User` üzerine `RoleId` tek yabancı anahtar koymak da yetmez. O zaman da bir kişinin tek rolü olur. Aynı anda Player ve ShopOwner olmaz. Virgülle `"Player,ShopOwner"` yazmak kırılır.

Authentication hâlâ kim olduğunu sorar: login, BCrypt, JWT. Yoksa 401. Authorization ne yapabileceğini sorar. Yoksa 403. 20 Ağustos yalnızca ikinci sorunun tablosunu modelledi. Kapı henüz bu tablolara bakmıyor.

### `Role` ve `Permission`

```3:21:ReactBattleArena/ReactBattleArena.Domain/Authorization/Role.cs
public sealed class Role
{
    private Role()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public static Role Create(string name)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name
        };
    }
}
```

`Role` bir isim tutar: `Admin`, `Player`. Bu sınıf tek başına action’ı kilitlemez.

```3:21:ReactBattleArena/ReactBattleArena.Domain/Authorization/Permission.cs
public sealed class Permission
{
    private Permission()
    {
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public static Permission Create(string code)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Code = code
        };
    }
}
```

`Permission` somut fiili tutar. `Code` örneği `characters.create`, `characters.update`, `characters.delete`. `shop.items.create` o gün shop ekranı yokken kayıt için duruyor. Private constructor ve `Create`, `Character` / `User` ile aynı kalıp (bölüm 1). `Roles.ShopOwner` sabiti yorumdan kalma; satır seed’de gelecek (bölüm 25).

### Ara tablolar

Bir kullanıcının birden fazla rolü olsun, bir rolün birden fazla fiili olsun diye iki join entity yazdık. `User` sınıfına `ICollection<UserRole>` koymadık.

```1:20:ReactBattleArena/ReactBattleArena.Domain/Authorization/UserRole.cs
namespace ReactBattleArena .Domain.Authorization;

public sealed class UserRole
{
    private UserRole()
    {

    }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
    }
}
```

Namespace’te boşluk var (`ReactBattleArena .Domain`). 20 Ağustos’tan yazım hatası; derleyici aynı assembly içinde tipi yine görür. `UserRole` kim hangi role üye olduğunu söyler. İki satır iki rol demek: Mehmet hem Player hem ShopOwner olabilir.

```3:21:ReactBattleArena/ReactBattleArena.Domain/Authorization/RolePermission.cs
public sealed class RolePermission
{
    private RolePermission()
    {
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        };
    }
}
```

`RolePermission` bu rolün bu fiili yapabileceğini söyler. Player’a create vermek buraya satır eklemektir. `Create` action aynı kalır. Login’de iki rolün fiilleri birleşir. Mehmet’in shop fiili varsa figür ekler; `characters.create` hiçbir rolünde yoksa karakter ekleyemez.

SQL’de `Users`, `UserRoles` ve `Roles` join olur. Identity’deki `AspNetUserRoles` aynı iş. Frontend o gün hâlâ `apiFetch` kullanıyor; bu tabloları okumuyor.

### Şema: Mehmet hangi fiilleri alır

`User` satırında permission listesi yok. Mehmet `Users`’ta bir kişidir. 28 Temmuz’daki `Role` kolonu hâlâ orada durabilir; 24’ün bağları o kolonu kullanmaz. Fiil, üç tablodan geçerek gelir.

Birinci tablo `UserRoles`. Satırın iki Guid’i var: `UserId` ve `RoleId`. Bir kullanıcıya birden fazla satır yazılırsa birden fazla rolü vardır. Mehmet’e Player ve ShopOwner bağlamak iki satırdır. Tersi de olur: aynı `RoleId` birçok `UserRoles` satırında durur. Player rolüne hem Mehmet hem Ayşe üye olabilir.

İkinci tablo `Roles`. Bu tablo yalnız isim tutar: Admin, Player. Fiil burada yok. Rol, insanları fiillere bağlayan etiket.

Sıfırdan yeni bir rol (örneğin Moderator) eklemek, 26’dan sonra publish istemez. Kapı rol adına bakmaz; `characters.create` gibi fiilin o rolün `RolePermissions` satırında olup olmadığına bakar. `Roles`’a satır, `RolePermissions`’a mevcut fiillerin bağları, `UserRoles`’a üyelik yazman yeter. `Roles.cs` içindeki `Admin` / `Player` sabitleri seeder ve Register içindir; tabloda olmasa da Moderator çalışır. `AuthSeeder` “yoksa ekle” der, senin SSMS’te açtığın rolü silmez.

Publish ve kod, yeni bir **fiil** gerektiğinde çıkar. `arena.join` diye bir kod yoksa controller’da `[HasPermission]` yazamazsın, frontend `PERMISSIONS` sabiti de olmaz. O zaman `PermissionCodes`, attribute, seed ve bir deploy gerekir. Rol yönetim ekranımız yok; bugün yeni rol SSMS (veya ileride bir admin API). 20 Ağustos’ta bu henüz işlemez: kapı hâlâ JWT’deki `Admin` string’ine bakıyor.

Üçüncü tablo `RolePermissions`. Satırın iki Guid’i var: `RoleId` ve `PermissionId`. Bir rolün birden fazla fiili varsa o kadar satır vardır. Admin’e `characters.create`, `characters.update`, `characters.delete` vermek üç satırdır. Tersi de olur: bir permission’ın birden fazla `RolePermission` satırı olabilir. `characters.create` hem Admin’e hem yarın Moderator’e bağlıysa aynı `PermissionId` iki satırda durur. Fiil bir kez `Permissions` tablosundadır; çoğalan şey bağdır.

`Permissions` somut fiildir. `Code` `characters.create` gibi bir string. Kullanıcıya doğrudan bağlanmaz.

Mehmet karakter ekleyebilir mi, diye bakınca zincir şöyle yürür. Önce `UserRoles`’ta Mehmet’in `RoleId`’leri bulunur. Sonra o rollerin `RolePermissions` satırları okunur. Sonra o satırlardaki `PermissionId`’lerin `Code`’ları birleşir (union). Mehmet’te Player ve ShopOwner varsa iki çantanın fiilleri toplanır. `characters.create` bu birleşik listede yoksa POST 403 olur. Listede varsa kapı açılır.

Doğrudan `UserPermission` tablosu yok. Kullanıcıya fiil yazmıyoruz. Fiili role yazıyoruz, kullanıcıyı role yazıyoruz. Player yarın karakter eklesin dersen `RolePermissions`’a bir satır eklersin. Mehmet’in `Users` satırına dokunmazsın. Admin’den create’i almak da Mehmet’i tek tek gezmek değil, Admin–`characters.create` bağını silmektir. O bağ gidince Admin olan herkes create kaybeder.

20 Ağustos’ta bu join henüz çalışmıyor. Tablolar SQL’de yok, kapı hâlâ JWT’deki rol string’ine bakıyor. 25 tabloları basacak, 26 her istekte bu zinciri okuyacak. Kafadaki resim yine bu: User → UserRoles → Role → RolePermissions → Permission.

### Composite PK ve silme

```13:26:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserRoleConfiguration.cs
        builder.ToTable("UserRoles");
        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        //Cascade (User): kullanıcı silinince o kullanıcının UserRoles satırları da silinsin.

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        // Restrict(Role): rol hâlâ birine bağlıysa rolü silemezsin.
```

`HasKey` iki kolonu birlikte birincil anahtar yapıyor. Aynı kullanıcı-rol çifti iki kez yazılamaz. Ayrı bir `Id` Guid’i yok. `WithMany()` boş bırakıldı çünkü `User`’da collection yok. FK yine var. Kullanıcı silinince üyelikler gitsin diye User tarafı Cascade. Rol hâlâ birine bağlıyken silinmesin diye Role tarafı Restrict.

Cascade şunu der: Mehmet’i `Users`’tan silince `UserRoles`’taki Mehmet–Player satırı da silinsin. O satırın UserId’si artık kimseyi göstermez. Restrict ters yöndedir. Player rolünü silmeye kalkınca SQL bakar: bu RoleId hâlâ bir `UserRoles` satırında var mı. Varsa silmeyi reddeder. Yoksa Mehmet’in üyeliği havada kalır, ya da bir `Roles` satırını silince yanlışlıkla herkesten etiket gider. Kullanıcı gidebilir, üyelik onunla gider. Rol katalogdur; biri bağlıyken katalog satırını koparma.

```11:22:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/RolePermissionConfiguration.cs
        builder.ToTable("RolePermissions");
        builder.HasKey(x => new { x.RoleId, x.PermissionId });

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
```

Burada iki FK de Restrict. Fiil veya rol hâlâ bağlıyken satır silinmesin. `characters.create` fiilini silmek, Admin hâlâ ona bağlıyken olmasın. `Roles.Name` ve `Permissions.Code` unique:

```11:14:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/RoleConfiguration.cs
        builder.ToTable("Roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Name).IsUnique();
```

Handler bu tablolara `IApplicationDbContext` üzerinden uzanır. Application, Infrastructure class’ını görmez (bölüm 1). Bugünkü arayüzde `RefreshTokens` de var; o 28 Ağustos (bölüm 32). 20 Ağustos’ta dört RBAC `DbSet` vardı:

```12:16:ReactBattleArena/ReactBattleArena.Application/Abstractions/IApplicationDbContext.cs
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
```

### Bu gün HTTP yok

20 Ağustos’ta bu dört sınıfı henüz hiçbir HTTP isteği kullanmıyor. Login yine `Users.Role` kolonunu okuyup JWT’ye basıyor. React tarafı aynı `apiFetch`. Tablolar SQL’e dökülmedi; `dotnet ef` ve seed ertesi gün gelecek (bölüm 25). Yani bugünün kazancı model. Kapı değişmedi. Karakter yazmak hâlâ token’daki Admin string’ine bakıyor.

Kolayı şudur: `Users.Role` kolonunu silip JWT’nin hâlâ o claim’i beklediğini unutmak. Join tablosuna ayrı bir `Id` Guid koyup aynı kullanıcı-rol çiftini iki kez yazabilmek de ikinci tuzak. `User`’da `ICollection<UserRole>` yok diye ilişkinin hiç kurulmadığını sanmak üçüncü. `ICollection` olmayınca tablo yok olmaz. EF bağı `UserRoleConfiguration` içindeki `HasOne` / `HasForeignKey` ile kurar. Handler `_db.UserRoles` ile satır yazar. Navigation property kolaylıktır; asıl bağ yabancı anahtardır.

Fiil listesini token’a koymadık. Rol string’ini 28 Temmuz’da koyduk:

```22:28:ReactBattleArena/ReactBattleArena.Infrastructure/Security/JwtTokenService.cs
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };
```

`user.Role` `Users.Role` kolonudur ve login anında basılır. SSMS’te kolonu Admin yapsan eski JWT hâlâ Player der. `[Authorize(Roles = Admin)]` o claim’e bakar; yeniden login şart (bölüm 9).

22 Ağustos’tan sonra yazma kapısı başka yerden bakar. `[HasPermission]` her istekte `UserRoles` ile `RolePermissions`’ı join eder (bölüm 26). Token’ın içindeki Role claim’ini okumaz. `UserRoles` satırını değiştirirsen bir sonraki POST yeni tabloya bakar; JWT’nin süresinin dolmasını beklemezsin. `Users.Role` kolonu ve token’daki claim bayat kalabilir. Login hâlâ kolondan basar. Kolon ile claim’i birlikte tutmak ayrı bir iş.

Sonuçta modelde rol ve fiil nesneleri var. SQL ve seed yok. Endpoint hâlâ Admin string’ine bakıyor. 21 Ağustos’ta tablo ve seeder (bölüm 25).

---

## E. Tablolar SQL’de, katalog seed — bölüm 25 (21 Ağustos)

24’te entity ve configuration vardı; SQL yoktu. 21 Ağustos’ta sırayla şunlar geldi: `PermissionCodes` sabitleri, `AddRbacTables` migration’ı, `AuthSeeder`, `Program.cs` içinde `CreateScope` ile seed. `[HasPermission]` henüz yok (bölüm 26). Login hâlâ JWT’ye `Users.Role` yazar. Register hâlâ yalnız string `Player` yazar; `UserRoles` satırı bir sonraki Api açılışında seed’den gelir (bölüm 27’de Register doğrudan yazacak).

### `PermissionCodes` — fiil adı tek yerde

```1:9:ReactBattleArena/ReactBattleArena.Domain/Authorization/PermissionCodes.cs
namespace ReactBattleArena.Domain.Authorization;

public static class PermissionCodes
{
    public const string CharactersCreate = "characters.create";
    public const string CharactersUpdate = "characters.update";
    public const string CharactersDelete = "characters.delete";
    public const string ShopItemsCreate = "shop.items.create";
}
```

Bu string’ler DB’deki `Permissions.Code` ile aynıdır. Attribute ve seeder bu sabitleri kullanır. `"characters.create"`’i üç dosyada elle yazmak typo üretir. `ShopItemsCreate` shop ekranı yokken kayıt içindir; seed’de ShopOwner’a bağlanır. `Roles.Admin` rol adı sabitiydi (bölüm 9); burada fiil kodu sabiti.

### Migration — dört tablo, `Users.Role` duruyor

`dotnet ef migrations add AddRbacTables` dünün configuration’ını SQL’e döker. `Users.Role` kolonuna dokunulmaz. Designer / snapshot alıntılanmaz; `Up` yeter.

```14:24:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260821102302_AddRbacTables.cs
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });
```

`Roles` aynı fikir, `Name` max 50. Ara tabloda composite PK ve 24’teki silme kuralları:

```70:83:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260821102302_AddRbacTables.cs
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
```

FK bağ tablosundadır; `Users`’ta `RoleId` kolonu yoktur. Unique index’ler `IX_Permissions_Code` ve `IX_Roles_Name`. `Down` dört tabloyu düşürür; `Users`’a dokunmaz.

### `AuthSeeder` — yoksa ekle

Her `dotnet run`’da katalog dolsun diye seed çalışır. Bu geçici test datası değildir.

```9:29:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(db, Roles.Admin, cancellationToken);
        await EnsureRoleAsync(db, Roles.Player, cancellationToken);
        await EnsureRoleAsync(db, Roles.ShopOwner, cancellationToken);

        await EnsurePermissionAsync(db, PermissionCodes.CharactersCreate, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.CharactersUpdate, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.CharactersDelete, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.ShopItemsCreate, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersCreate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersUpdate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersDelete, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.ShopItemsCreate, cancellationToken);

        await EnsureRolePermissionAsync(db, Roles.ShopOwner, PermissionCodes.ShopItemsCreate, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
```

İlk `SaveChanges` rol ve fiil Guid’lerinin oluşması içindir. `RolePermission` o Guid’leri ister; insert olmamış satırda Id yok. Admin dört fiili alır. ShopOwner yalnız shop alır. Player’a `RolePermission` satırı yok: katalog GET zaten açık, yazma 403 kalır. O gün kapı hâlâ `[Authorize(Roles = Admin)]`; yarın fiil join’ine geçecek.

```59:75:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
    private static async Task EnsureRoleAsync(
        ApplicationDbContext db,
        string name,
        CancellationToken cancellationToken)
    {
        if (!await db.Roles.AnyAsync(r => r.Name == name, cancellationToken))
            db.Roles.Add(Role.Create(name));
    }

    private static async Task EnsurePermissionAsync(
        ApplicationDbContext db,
        string code,
        CancellationToken cancellationToken)
    {
        if (!await db.Permissions.AnyAsync(p => p.Code == code, cancellationToken))
            db.Permissions.Add(Permission.Create(code));
    }
```

Idempotent: aynı işlemi ikinci kez çalıştırınca sonuç değişmez. İkinci `dotnet run` ikinci bir `Admin` satırı eklemez; `Name` unique index de patlardı. `EnsureRolePermissionAsync` `(RoleId, PermissionId)` var mı diye bakar.

Sonra eski `Users.Role` string’i join’e kopyalanır:

```31:56:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
        var rolesByName = await db.Roles.ToDictionaryAsync(r => r.Name, cancellationToken);
        var users = await db.Users.ToListAsync(cancellationToken);

        // Composite PK çiftleri — "bu kullanıcıya bu rol zaten verilmiş mi?"
        var existingPairs = (await db.UserRoles.ToListAsync(cancellationToken))
            .Select(x => (x.UserId, x.RoleId))
            .ToHashSet();

        foreach (var user in users)
        {
            // Eski kolon Users.Role (string) → yeni UserRoles satırı
            var roleName = string.IsNullOrWhiteSpace(user.Role) ? Roles.Player : user.Role;
            if (!rolesByName.TryGetValue(roleName, out var role))
                role = rolesByName[Roles.Player];

            if (existingPairs.Contains((user.Id, role.Id)))
                continue;

            db.UserRoles.Add(UserRole.Create(user.Id, role.Id));
            existingPairs.Add((user.Id, role.Id)); // aynı kullanıcı döngüde iki kez eklenmesin
        }

        await db.SaveChangesAsync(cancellationToken);
```

`ToDictionaryAsync` her rol adı için Guid’i hazır tutar; döngüde tekrar `Roles` sorgusu atılmaz. SSMS’te `Role = Admin` yapılmış kullanıcı `UserRoles`’a Admin Guid’i alır. Bilinmeyen string Player’a düşer. HashSet aynı çifti ikinci kez eklemesin diye durur.

### `CreateScope` — kökten scoped alınmaz

```69:75:ReactBattleArena/ReactBattleArena.Api/Program.cs
// DbContext scoped (istek ömrü). Program kökü request değil → CreateScope ile kısa ömürlü kapsül;
// using bitince context Dispose. Yoksa root provider'dan scoped alınamaz.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await AuthSeeder.SeedAsync(db);
}
```

`ApplicationDbContext` DI’da scoped’dur: bir HTTP isteği boyunca bir instance. `Program.cs` bir istek değildir; `app.Services` kök provider’dır. Kökten scoped çekmek runtime hatası verir. `CreateScope()` kısa ömürlü bir kapsül açar; seed bitince `using` Dispose eder. Bugünkü `Program.cs` üstte provider kayıtları da vardır (bölüm 26); 21 Ağustos’ta eklenen parça bu `using` bloğudur.

### Bu günün sınırı

`dotnet ef database update` tabloları kurar. Her `dotnet run` seed’i çalıştırır. Frontend yeni endpoint görmez. Karakter POST hâlâ `[Authorize(Roles = Admin)]`. Scalar’da Roles / Permissions dolu görünür; JWT claim hâlâ string `role`.

Kolayı şudur: `RolePermission` yazmadan önce `SaveChanges` unutmak (FK Guid boş); migration’sız seed (tablo yok); seed’i controller’a gömmek; Api açıkken kaydolup `UserRoles` beklemek — 21 Ağustos’ta Register join yazmaz, satır bir sonraki restart’ta gelir (bölüm 27 kapatır); `Users.Role` kolonunu drop etmek — JWT claim bozulur, kolon kasıtlı durdu.

Sonuçta dört tablo SQL’de, fiil kodları sabit, her açılışta idempotent katalog, eski string roller join’e kopyalanır. Endpoint hâlâ “Admin misin?”. Ertesi gün her istekte DB join (bölüm 26).

---

## F. Yazma kapısı fiile bakıyor — bölüm 26 (22 Ağustos)

24’te tabloları modelledik. 25’te `AddRbacTables` dört tabloyu SQL’e döktü, `AuthSeeder` Admin / Player / fiil satırlarını bastı. 22 Ağustos’ta kapı değişti. Karakter POST / PUT / DELETE artık `[Authorize(Roles = Admin)]` değil; üç ayrı `[HasPermission(...)]`. GET liste ve detay attribute’siz kaldı. React aynı `apiFetch` URL’lerini kullanıyor; değişen şey Api’nin sorusu.

### Fiil listesi nereden geliyor

Handler’ın “bu kullanıcının kodları neler” diye sorduğu yer Application sözleşmesi:

```3:6:ReactBattleArena/ReactBattleArena.Application/Abstractions/IUserPermissionService.cs
public interface IUserPermissionService
{
    Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

Infrastructure’daki uygulama o join’i çalıştırır. `Users.Role` string’i bu sorguda yoktur. Kullanıcıya doğrudan fiil tablosu da yoktur. Yol, 24’teki şema: `UserRoles` → `RolePermissions` → `Permissions.Code`. `Roles` satırına uğramaz çünkü iki ara tabloda zaten aynı `RoleId` Guid’i durur.

```15:26:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserPermissionService.cs
    public async Task<IReadOnlyList<string>> GetCodesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from ur in _db.UserRoles
            join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
            join p in _db.Permissions on rp.PermissionId equals p.Id
            where ur.UserId == userId
            select p.Code
        ).Distinct().ToListAsync(cancellationToken);
    }
```

`Distinct` şunu keser: Mehmet hem Admin hem başka bir role üye olsa ve ikisi de `characters.create` verse listeye kod bir kez girer. Servis istek başına Scoped kaydolur (`DependencyInjection.cs`), çünkü altında `DbContext` vardır.

Burada bilinçli bir tercih var. Fiilleri JWT claim’lerine gömmek (stateless access token) ayrı bir model olurdu: login anında kodları claim yapar, her istekte DB’ye bakmazsın, token süresi dolana kadar liste bayat kalır. Bizde token yalnız kimlik taşır (`sub` / `NameIdentifier`). Fiil her yazma isteğinde tablodan okunur. SSMS’te Player’a `characters.create` satırı eklersen aynı Bearer ile bir sonraki POST 201 olabilir; yeniden login şart değildir. Satırı silersen bir sonraki POST 403 olur.

### `[HasPermission]` ile `[Authorize(Policy = ...)]` aynı kapı mı

Evet, aynı yerde aynı iş. `HasPermissionAttribute` `AuthorizeAttribute`’tan türer. Constructor’da framework’ün beklediği `Policy` string’ini doldurur:

```5:11:ReactBattleArena/ReactBattleArena.Api/Authorization/HasPermissionAttribute.cs
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = "Permission:" + permission;
    }
}
```

`[HasPermission(PermissionCodes.CharactersCreate)]` yazmak, elle `[Authorize(Policy = "Permission:characters.create")]` yazmakla aynı anlama gelir. İkisini yan yana koymana gerek yok; biri yeter. Kısa attribute typo’yu ve `"Permission:"` önekini unutmayı azaltır. `PermissionCodes.CharactersCreate` sabiti `"characters.create"` string’idir (`PermissionCodes.cs`).

Framework bu policy adını DI’daki tek `IAuthorizationPolicyProvider`’a sorar. Bizim provider `Permission:` ile başlayan adları kendi çevirir; diğer adları ASP.NET’in default provider’ına bırakır.

```6:37:ReactBattleArena/ReactBattleArena.Api/Authorization/PermissionPolicyProvider.cs
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string Prefix = "Permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var code = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(code))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
```

`Permission:characters.create` gelince iki kural üretilir: önce giriş yapmış ol (`RequireAuthenticatedUser`), sonra `PermissionRequirement` içindeki kodu kanıtla. `Permission:` değilse — örneğin boş `[Authorize]` veya eski Roles policy — `_fallback` cevaplar. Böylece `/me` gibi yalnız token isteyen action’lar bozulmaz.

Requirement bir zarftır; içinde yalnız fiil kodu durur:

```5:12:ReactBattleArena/ReactBattleArena.Api/Authorization/PermissionRequirement.cs
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Code { get; }
    public PermissionRequirement(string code)
    {
        Code = code;
    }
}
```

Handler o zarfı açar. Token’dan `NameIdentifier` ile kullanıcı id’sini alır. `GetCodesAsync` ile DB listesini çeker. Listede `requirement.Code` varsa `Succeed` der. Yoksa veya id parse olmazsa `Succeed` demeden çıkar; framework bunu başarısız sayar. Token yoksa policy’deki `RequireAuthenticatedUser` yüzünden 401. Token var, fiil yoksa 403. Token içinde permission claim aranmaz.

```7:28:ReactBattleArena/ReactBattleArena.Api/Authorization/PermissionAuthorizationHandler.cs
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserPermissionService _permissions;

    public PermissionAuthorizationHandler(IUserPermissionService permissions)
    {
        _permissions = permissions;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var idValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idValue, out var userId))
            return;

        var codes = await _permissions.GetCodesAsync(userId);
        if (codes.Contains(requirement.Code))
            context.Succeed(requirement);
    }
}
```

Zincir tek cümlede şöyle: attribute bir isim yazar → provider o ismi “login + şu kod” politikasına çevirir → handler kodu DB listesinde arar.

### `AddAuthorization` ile `UseAuthorization`

```21:23:ReactBattleArena/ReactBattleArena.Api/Program.cs
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

Bunlar iki farklı katmandır. `UseAuthorization()` boru hattındaki middleware’dir; istek action’a gelmeden önce authorization çalışsın diye durur. JWT login gününden beri vardı ve silinmez. `AddAuthorization()` ise DI kaydıdır. Provider constructor’ı `IOptions<AuthorizationOptions>` ister; o options’ı bu çağrı üretir. Kendi `IAuthorizationPolicyProvider`’ını kaydedince framework’ün dolaylı varsayılanına güvenmek yetmez; `AddAuthorization()` açık yazılır.

Provider Singleton’dır çünkü yalnız string çevirir, DbContext tutmaz. Handler Scoped’dur çünkü `IUserPermissionService` ve onun `DbContext`’i istek ömründedir. Singleton handler Scoped DbContext çekse yaşam süresi çatışırdı.

### Üç action, üç kapı

```46:47:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersCreate)]  // POST
    [HttpPost]
```

```69:70:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersUpdate)]  // PUT
    [HttpPut("{id:guid}")]
```

```95:96:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersDelete)]  // DELETE
    [HttpDelete("{id:guid}")]
```

Sınıfın tepesine tek `[HasPermission(CharactersCreate)]` koymuyoruz. Koysaydık class’taki her action o fiili isterdi; GET liste ve detay da kapanırdı, ya da her GET’e `[AllowAnonymous]` yağardı. Create, Update ve Delete zaten farklı kodlar. Birinin düzenleyip silememesi ürün kararıdır; üç kapı bunu mümkün kılar. Üç attribute’u aynı metoda üst üste yazarsan ASP.NET hepsini ister (AND). Player’a yalnız create verirsen PUT yine 403 kalır — bu istenen davranış olabilir, ama yanlışlıkla class’a yığınca GET’leri de kilitlersin.

`GetPaged` ve `GetById` attribute’siz. Tokensız 200 gelir; katalog bilerek açık (bölüm 4, 13).

Scalar veya React ile test: Admin Bearer POST → 201. Player aynı POST → 403. SSMS’te Player + `characters.create` `RolePermissions` satırı, **aynı token** ile tekrar POST → 201. Satırı sil → 403. ShopOwner’da `shop.items.create` varken `characters.create` yoksa karakter POST yine 403. JWT decode’da permission arama; yoktur.

### Bu günün sınırı

22 Ağustos’ta Register hâlâ yalnız `Users.Role = Player` yazıyordu; `UserRoles` satırı Api açılışındaki seed’e kalıyordu. RolePermissions dolu, UserRoles boşsa join boş döner, POST 403. O boşluğu Register’ın ikinci `SaveChanges` ile doldurması 24 Ağustos (bölüm 27). Frontend buton gizleme de yok; o 28.

Kolayı şudur: karakter JSON’unu register’a göndermek; join’in `Users.Role` string’ine bakacağını sanmak; provider yazıp `AddAuthorization()` unutmak; `UseAuthorization` silmek; class-level `HasPermission`; fiilleri JWT’ye gömüp SSMS değişince eski token’ın yetkisini taşımak.

Sonuçta yazma fiilleri DB join’den gelir. JWT kimlik taşır. `/me` permissions listesi ve Register’ın `UserRoles` yazması bölüm 27.

---

## G. Register join’e girer, `/me` fiilleri söyler — bölüm 27 (24 Ağustos)

26’da kapı `UserRoles` → `RolePermissions` join’ine bakıyordu. Testte Player’a fiil yazılmış olsa bile yeni kayıtlı kullanıcı POST create’de 403 alıyordu. Sebep fiil satırının yokluğu değildi. Register yalnız `Users.Role = Player` string’ini yazıyordu. `UserRoles` satırı Api açılışındaki seed’e kalıyordu. Join `UserRoles`’tan başlar; o satır yoksa liste boş döner, `HasPermission` 403 der. 24 Ağustos’ta Register hemen `UserRoles` yazar. Aynı gün `GET /api/auth/me` cevaba `permissions` dizisini ekler. React o gün bu endpoint’i henüz çağırmaz; Scalar ve ertesi gün UI kullanır (bölüm 28).

### Register neden iki kez `SaveChanges` yapar

```42:60:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommandHandler.cs
        var entity = User.Create(
            request.UserName,
            request.Email,
            request.DisplayName,
            passwordHash,
            Roles.Player,
            DateTime.UtcNow);

        _db.Users.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var playerRole = await _db.Roles.SingleAsync(
            r => r.Name == Roles.Player, cancellationToken);
        //Rol yoksa (seed çalışmamış) sessizce geçme, patlat ki fark edesin.

        _db.UserRoles.Add(UserRole.Create(entity.Id, playerRole.Id));
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;
```

İlk `SaveChanges` kullanıcıyı `Users`’a basar. Böylece `entity.Id` veritabanında gerçek bir satır olur. `UserRoles.UserId` yabancı anahtardır; kullanıcı satırı yokken üyelik satırı yazılamaz. İkinci `SaveChanges` Player rolünün Guid’ini alır (`Roles` tablosundan `SingleAsync`) ve `UserRoles`’a Mehmet–Player bağını yazar. `Roles`’ta Player yoksa (seed hiç çalışmamışsa) `SingleAsync` patlar. Sessizce yutmuyoruz; eksik katalog fark edilsin diye.

`Users.Role` kolonu hâlâ `"Player"` yazılır. Login JWT’deki `ClaimTypes.Role` claim’ini o kolondan basar (bölüm 8). `[HasPermission]` o claim’e bakmaz; join’e bakar. Yani yeni kullanıcı kaydolur olmaz create yapamaz — seed Player’a CUD vermez, bu ürün kararıdır. Ama join artık çalışır: `UserRoles` doludur, `GetCodesAsync` boş dizi döner, 403 “üyelik yok” değil “fiil yok” anlamına gelir. Api’yi yeniden başlatmana gerek kalmaz. SSMS’te Player’a `characters.create` eklersen aynı oturumda POST 201 olabilir.

`RegisterPage` hâlâ `apiFetch('/api/auth/register', { auth: false, ... })` atar (bölüm 14, 22). Değişen taraf backend’dir.

### `/me` ne döner, ne hata verir

Yeni kullanıcıyla login olduktan sonra `GET /api/auth/me` Bearer ile çalışır. Action `[Authorize]` ister; token yoksa veya bozuksa 401. Id claim’i parse olmazsa yine 401. Token sağlamsa controller aynı `GetCodesAsync`’i çağırır ve JSON döner.

```98:123:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        //Aynı controller’da GET / api / auth / me, [Authorize].Token’daki id’den kim olduğunu döner.
        //27 Temmuz’da id, userName, email. Bugünkü permissions 24 Ağustos.Sayfa yenilenince login JSON’u uçmuştur; token duruyorsa / me kim olduğunu söyler.

        if (!Guid.TryParse(idValue, out var userId))
            return Unauthorized();

        var codes = await _permissions.GetCodesAsync(userId, cancellationToken);

        return Ok(new
        {
            id = userId,// out var daki userId
            userName = User.Identity?.Name,
            email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value,
            permissions = codes
        });
    }
```

`permissions` string dizisidir. Seed’deki düz Player için dizi boştur (`[]`). Admin için `characters.create` / `update` / `delete` ve `shop.items.create` gelir. Bu liste JWT’nin içinden okunmaz; her `/me` anında join yeniden çalışır. SSMS’te `RolePermissions` değişince bir sonraki `/me` yeni listeyi getirir; yeniden login şart değildir.

Boş dizi hata değildir. “Bu kullanıcının şu an hiç fiili yok” demektir. Katalog GET attribute’siz kaldığı için liste hâlâ 200 döner. Karakter POST hâlâ `[HasPermission]`; Player’da create yoksa 403. 24 Ağustos’ta React `/me`’yi bağlamadığı için UI buton gizlemez; formdan 403 görürsün. Link gizleme bölüm 28.

### Bu günün sınırı

Kolayı şudur: `/me`’nin JWT’deki eski permission listesini döndüğünü sanmak; Register’da `UserRoles`’u yine seed’e bırakmak; Player yokken `SingleAsync`’i try/catch ile yutmak; `/me`’ye `[AllowAnonymous]` koymak.

Sonuçta yeni kullanıcı kaydolunca join’de görünür. `/me` o anki fiilleri söyler. UI gizleme ve sayfa kapıları Blok D (bölüm 28).