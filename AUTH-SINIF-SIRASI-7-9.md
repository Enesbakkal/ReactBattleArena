# Authentication sırası — 7–9, 12–13, 22, 23, 24

Bu dosya `REACT-OGRENIM-V2.md` bölüm 7–9, 12–13, 22, 23 ve 24. İstek sırası: kayıt, giriş, token’lı yazma, tarayıcının token’ı saklaması, listeye Bearer, her sayfanın `api.ts` kapısı, hangi ekranın hangi controller metoduna gittiği, sonra `Users.Role` string’inin yetmediği RBAC tabloları. Her adımda hangi sınıf ne yapıyor. VS Code’da o sınıfa git.

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

28 Temmuz’da `Roles`, `Users.Role`, JWT’de rol claim’i, yazma yalnız Admin. Player POST 403. `BearerSecuritySchemeTransformer` denemeyi kolaylaştırır, kilidi koymaz.

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

### Karta tıklayınca Api gitmez; detay sayfası gider

```11:13:web/src/CharacterCard.tsx
function CharacterCard({ id, name, universe, rarity, imageUrl }: CharacterCardProps) {
  return (
      <Link to={`/characters/${id}`} className="character-card-link">
```

Bu `Link` 7275’e istek atmaz. Adresi `/characters/{guid}` yapar. Router `CharacterDetailPage`’i açar.

Karta basınca `id` detay sayfasına prop olarak gitmiyor. Kart kapanıyor, `CharacterDetailPage` yeni açılıyor. `id` adres çubuğunda gidiyor. `App.tsx`’te path `/characters/:id`. Router URL’deki o parçayı `:id` diye alıyor. Yeni sayfa `useParams()` deyince router o Guid’i veriyor. Yani aynı Guid önce kartta, sonra adreste, sonra `useParams`’ta. (React tarafını sonra ayrıntılı işleriz.)

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

Bu `PUT` `CharactersController.Update`’e gider. 13 Ağustos’ta `[Authorize(Roles = Admin)]` idi. Bugün `[HasPermission(CharactersUpdate)]`. 204 gelir, gövde yoktur, `json()` çağırma. Sayfa detaya döner. Player PUT denerse 403.

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

Burada iki FK de Restrict. Fiil veya rol hâlâ bağlıyken satır silinmesin. `Roles.Name` ve `Permissions.Code` unique:

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

20 Ağustos’ta bu sınıfları hiçbir endpoint çağırmıyor. Tablolar henüz migration almadı. Login `Users.Role` yazıyor, JWT o string’i taşıyor. Frontend aynı `apiFetch`’i kullanıyor. Seed ve `dotnet ef` ertesi gün (bölüm 25).

Kolonu silip JWT’yi unutmak kolay bir hata. `Users.Role` duruyor, claim hâlâ oradan geliyor. Join’e ayrı `Id` koyup aynı çifti iki kez eklemek de ikinci hata. `ICollection` yok diye ilişkinin yok sanılması üçüncü. Permission’ı JWT’ye gömmedik; istekte DB’den bakılacak (bölüm 26).

Sonuçta modelde rol ve fiil nesneleri var. SQL ve seed yok. Endpoint hâlâ Admin string’ine bakıyor. 21 Ağustos’ta tablo ve seeder (bölüm 25).