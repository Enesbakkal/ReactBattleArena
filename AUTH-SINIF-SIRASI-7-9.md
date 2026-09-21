# Authentication sırası — 7–9, 12–13, 22, 23, 24, 25, 26, 27, 28, 30, 32, 33, 34, 35, 36, 37, 38

Bu dosya `REACT-OGRENIM-V2.md` bölüm 7–9, 12–13, 22–28, 30, 32–38. İstek sırası auth omurgası: kimlik, yetki, oturum süresi, en sonda eski `Users.Role` string kolonunun kalkması. Create/Edit URL kapısı (29) ve Context kalan sayfalar (31) bu dosyada yok. Her adımda hangi sınıf ne yapıyor. VS Code’da o sınıfa git.

12–13’te asıl iş JWT’yi `localStorage`’a koymak ve listeye Bearer. Buton gizleme 28, Context 30. Auth üçlü özet 37. Kolon temizliği 38.

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

### Frontend izin sınıfları — kısa (asıl anlatım H / bölüm 28; Context 30)

30 Temmuz’da Ekle / Düzenle / Sil gizleme yoktu. Bugün listede duruyor. Backend `[HasPermission]` kapısı durur. Link yok diye POST açılmaz. Player URL’ye `/characters/new` yazarsa form açılabilir; asıl 403 API’dedir. UI yalnız rahatsız etmemek içindir.

`permissions.ts` iki şey export eder. `PERMISSIONS` sabitleri: `charactersCreate` değeri `'characters.create'`, aynı şekilde update ve delete. Yazım hatası olmasın diye. `hasPermission(permissions, code)` dizide kod var mı diye `includes` bakar. C# `codes.Contains("characters.create")` ile aynı fikir. React hook değildir. Düz fonksiyon.

Dizi nereden gelir. `AppLayout` `GET /api/auth/me` atar, `permissions` state’ine koyar. `PermissionContext.Provider` o diziyi alt sayfalara verir. `usePermissions` (`PermissionContext.tsx`) diziyi okur. Login layout dışında. Orada `usePermissions` çağırma. İnce ayrıntı bölüm 30.

`CharactersPage` `const permissions = usePermissions()` alır. JSX’te kapı şöyle:

`hasPermission(permissions, PERMISSIONS.charactersCreate) && <Link to="/characters/new">Karakter ekle</Link>`

`charactersCreate` yani `"characters.create"` listede varsa Karakter ekle çizilir. Aynı fikir: `characters.update` varsa Düzenle, `characters.delete` varsa Sil. `&&`’in solu koşul, sağı `Link` değil. JavaScript: `A && B`. A yanlışsa B’ye bakılmaz, sonuç false. A doğruysa sonuç B. React JSX’te false görünce hiçbir şey basmaz. True ise sağdaki `Link` veya `button` basılır. `hasPermission(...)` kapı. `Link` kapı açıksa çizilecek parça.

Detay, Create/Edit `Navigate` kapısı ve Context geçişi V2 bölüm 29–31.

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

Sonuçta yeni kullanıcı kaydolunca join’de görünür. `/me` o anki fiilleri söyler. UI gizleme bölüm 28.

---

## H. Link gizlemek yetki değildir — bölüm 28 (24 Ağustos)

27’de Api `/me` ile `permissions` dizisini döndürüyordu. 5173 hâlâ herkese “Karakter ekle” gösteriyordu. Player forma girip POST’ta 403 yiyordu. 24 Ağustos’un ikinci işi frontend’de o linki (ve detaydaki Düzenle / Sil’i) fiile göre gizlemek. Backend yeni endpoint açmadı. Asıl kapı yine `[HasPermission]` (bölüm 26).

### Sabit ve `hasPermission`

Önce `permissions.ts` geldi. `"characters.create"` üç sayfada elle yazılmasın diye sabit, kontrol için düz fonksiyon:

```1:9:web/src/permissions.ts
export const PERMISSIONS = {
  charactersCreate: 'characters.create',
  charactersUpdate: 'characters.update',
  charactersDelete: 'characters.delete',
} as const

export function hasPermission(permissions: string[], code: string): boolean {
  return permissions.includes(code)
}
```

Kodlar backend `PermissionCodes` ile aynı string’dir (bölüm 25). `as const` değerleri literal kilitler; yanlışlıkla `PERMISSIONS.charactersCreate = 'x'` derleme hatası olur. `hasPermission` React hook değildir. Dizi ve kod alır, `includes` ile true/false döner. C# tarafındaki `codes.Contains("characters.create")` ile aynı fikir. Dizinin nereden geldiği ayrı iştir; o gün sayfa `useState` ile tutuyordu, bugün `usePermissions` layout’tan okur (bölüm 30).

### Liste ve detayda `&&`

```95:97:web/src/CharactersPage.tsx
          {hasPermission(permissions, PERMISSIONS.charactersCreate) && (
            <Link to="/characters/new">Karakter ekle</Link>
          )}
```

`&&`’in solu koşul, sağı çizilecek parçadır. Koşul false ise React hiçbir şey basmaz. True ise `Link` basılır. Razor’da `@if (hasCreate) { <a>…</a> }` gibi. Create, update, delete ayrı fiillerdir: yalnız create varsa Ekle görünür, Düzenle/Sil görünmez.

```148:156:web/src/CharacterDetailPage.tsx
          {hasPermission(permissions, PERMISSIONS.charactersUpdate) && (
            <Link to={`/characters/${id}/edit`}>Düzenle</Link>
          )}
          {hasPermission(permissions, PERMISSIONS.charactersDelete) && (
            <button type="button" onClick={handleDelete}>
              Sil
            </button>
          )}
          <Link to="/characters">Listeye dön</Link>
```

24 Ağustos’ta liste `load` içinde `GET /api/auth/me` atıp `setPermissions(me.permissions ?? [])` diyordu. Bugün o blok yorumda; dizi Context’ten gelir:

```74:78:web/src/CharactersPage.tsx
      // const meResponse = await apiFetch('/api/auth/me')
      // if(meResponse.ok){
      //   const me = await meResponse.json()
      //   setPermissions(me.permissions ?? [])
      // }  
```

`?? []` şunu keser: API `permissions` göndermezse `undefined` kalmasın, `.includes` patlamasın. O gün detay kendi `/me`’sini unutursa dizi `[]` kalırdı; Admin dahil butonlar gizlenirdi. Liste `setPermissions` şarttı. Bugün ikisi de layout’tan okur; unutulan sayfa tuzağı 31’de kapanır.

### Gizleme ≠ yetki

Link yok diye Player’ın `/characters/new` yazamayacağını sanmak yanlış. Adres çubuğu durur; sayfa açılır. Asıl kapı `POST /api/characters` üzerindeki `[HasPermission]`’dır (bölüm 26): 403. Gizleme UX’tir; yetkisiz kişi formu doldurmasın diye. ASP.NET’te butonu Razor’da gizlesen action attribute yine durur. URL’den forma girişi kesmek ayrı adımdır (V2 bölüm 29); bu dosyada yok.

Kolayı şudur: `me.permission` (tekil) yazmak — API `permissions` çoğul; `undefined ?? []` herkesi yetkisiz gösterir. Detayda `/me` unutmak. Link gizleyince API’nin kapandığını sanmak. `hasPermission`’ı hook sanmak.

Sonuçta Player’da Ekle görünmez, Admin’de görünür. POST hâlâ 403 ile durur. Tek `/me` kutusu (Context) bölüm 30.

---

## I. Tek `/me`, `PermissionContext` — bölüm 30 (26 Ağustos)

28’de her sayfa kendi `GET /api/auth/me` isteğini atıyordu. Liste atıyordu, detay atıyordu, Create ve Edit de atıyordu. Backend’de `/me` her seferinde aynı `GetCodesAsync` join’ini çalıştırır: `UserRoles` → `RolePermissions` → `Permission.Code`. Dört sayfa, aynı kullanıcı, aynı dizi, dört HTTP, dört join. Yetki modeli değişmedi; israf değişti.

Bunu layout’a almak nested route yüzünden mümkün. Faz 5’te `AppLayout` parent route oldu, çocuk sayfa `Outlet` deliğinden çiziliyor. Liste’den detaya, detaydan edit’e giderken header ve layout unmount olmaz; değişen yalnız ortadaki çocuk. Layout ayaktayken `/me`’yi orada bir kez atarsan çocuklar aynı `permissions` dizisini okuyabilir. Her sayfayı yeniden mount eden bir yapıda bu işe yaramazdı; layout da her geçişte düşer, istek yine dört kez giderdi.

26 Ağustos’ta dosya sırası şöyle. Önce `PermissionContext.tsx`: `createContext` kutuyu açar, `usePermissions` altındaki sayfanın o kutuyu okumasını sağlar. Kutusu olmayan layout’un diziyi “çocuklara ver” demesinin yolu yok. Sonra `AppLayout`: `permissions` ve `meLoaded` state, `useEffect` içinde `/me`, cevap gelince dizi, `Provider` ile `Outlet`’i sarma. Liste o gün `usePermissions`’a geçti. Create, Edit ve Detail hâlâ kendi `/me`’lerini atıyordu; üç sayfayı aynı commit’te taşımak hangi 403’ün kimin isteği olduğunu karıştırırdı. Onların Context’e bağlanması bölüm 31. Layout `hasPermission` import etmez. Diziyi doldurur, Ekle / Düzenle kapısı sayfada kalır.

### Context ne işe yarar

```1:16:web/src/PermissionContext.tsx
import { createContext, useContext } from 'react'

type PermissionContextValue = {
    permissions: string[]
}

const PermissionContext = createContext<PermissionContextValue | null>(null)

export function usePermissions(): string[] {
    const ctx = useContext(PermissionContext)
    if(!ctx){
        throw new Error('usePermissions yalnızca AppLayout içinde')
    }
    return ctx.permissions
}
```

`createContext` bir kutu açar. `Provider` o kutuya değeri koyar. Kutunun altındaki bileşenler `usePermissions` ile aynı diziye uzanır; her sayfaya prop ile taşımana gerek kalmaz. Başlangıç değeri `null`’dır çünkü Login ve Register `AppLayout` dışındadır, orada Provider yoktur. O sayfalarda `usePermissions` çağırırsan throw eder. Sessizce boş dizi dönmek hatayı gizler: herkes yetkisiz görünür, asıl hata “Provider yok” kaybolur.

`usePermissions` “şu fiil var mı?” diye sormaz. Yalnız string dizisini verir. Fiil kontrolü hâlâ `hasPermission(permissions, PERMISSIONS.charactersCreate)` düz fonksiyonudur (bölüm 28). C# kabaca şöyle olurdu: istekte bir kez `GetCodesAsync` çalışır, sonuç bir yere konur, action’lar oradan okur. Liste yine veritabanındandır; JWT’ye permission yazılmaz.

### Layout doldurur, çocuk okur

```14:50:web/src/AppLayout.tsx
  const [permissions, setPermissions] = useState<string[]>([])
  const [meLoaded, setMeLoaded] = useState(false)

  useEffect(() => {
  if (!token) {
    return
  }

  async function loadMe() {
    try {
      const meResponse = await apiFetch('/api/auth/me')
      if (meResponse.ok) {
        const me = await meResponse.json()
        setPermissions(me.permissions ?? [])
      } else if (meResponse.status === 401) {
        // apiFetch buraya gelene kadar yenilemeyi denedi ve başaramadı: oturum bitti.
        clearToken()
        navigate('/login')
        return
      }
    } catch {
      // /me gelmese de meLoaded bitsin; yoksa sonsuz Yükleniyor
    }
    setMeLoaded(true)
  }

  loadMe()
}, [token])
  

  if (!token) {
    return <Navigate to="/login" replace />
  }

  if (!meLoaded) {
  return <p>Yükleniyor…</p>
}
```

`useState` ve `useEffect` `if (!token)` erken `return`’ünden **önce** durur. React hook sırası her render’da aynı olsun diye. Token yokken `return <Navigate …>` üstte olsaydı effect hiç kayıt olmazdı; sonra token gelse bile `/me` atılmaz, `meLoaded` true olmaz, ekran sonsuz “Yükleniyor…” kalırdı (V2 bölüm 29). Token yoksa effect içinden de hemen çıkılır, `/me` atılmaz. Token varken `loadMe` `GET /api/auth/me` atar, `me.permissions` diziyi doldurur.

`meLoaded` ayrı bayraktır. Dizi boş gelmek ile istek henüz bitmemek aynı şey değildir. Player’ın dizisi gerçekten boştur; Admin’in dizisi doludur ama cevap gelene kadar state `[]` durur. `meLoaded` false iken Provider ve `Outlet` çizilmez. Çizilseydi çocuk boş diziyi “yetkin yok” sanır, Create’e `Navigate` eder, istek bitince dizi dolardı. `setMeLoaded(true)` try/catch dışındadır: `/me` ağ atsa bile yükleme bitsin, “Yükleniyor…” sonsuza gitmesin. Bugünkü `401` dalı ve `logout()` sonradan geldi (bölüm 35). 26 Ağustos’un asıl işi dizi ve `meLoaded` idi.

```62:86:web/src/AppLayout.tsx
  return (
    <PermissionContext.Provider value={{ permissions }}>
      <div className="app-shell">
        <header className="app-header">
              <div className="app-header__left">
                  <Link to="/characters" className="app-header__brand">
                  ReactBattleArena
                  </Link>
                  <nav className="app-header__nav">
                  <Link to="/characters">Karakterler</Link>
                  </nav>
              </div>
              <button
                type="button"
                className="app-header__logout"
                onClick={handleLogout}
              >
                Çıkış
              </button>
          </header>
        <main className="app-main">
          <Outlet />
        </main>
    </div>
    </PermissionContext.Provider>
```

`Provider` `Outlet`’i sarar. Çocuk sayfa kutunun içindedir, `usePermissions` değeri görür. `Provider`’ı `Outlet`’in içine koysaydın çocuk context görmezdi; `usePermissions` throw ederdi. `value={{ permissions }}` her `setPermissions`’ta yeni obje verir, altındaki hook güncellenir. Listeye dönünce layout unmount olmaz, effect `[token]` yüzünden yeniden çalışmaz, `/me` tekrar atılmaz. Bu doğru.

Liste artık kendi `useState`’inden değil Context’ten okur:

```18:19:web/src/CharactersPage.tsx
function CharactersPage() {
  const permissions = usePermissions()
```

Ekle linki hâlâ bu sayfada `hasPermission(permissions, PERMISSIONS.charactersCreate) && …` ile gizlenir (bölüm 28). Layout o fonksiyonu import etmez; diziyi doldurur, kapı sayfada kalır. 26 Ağustos kanıtı: Admin’de Ekle görünür, Network’te listenin kendi `/me`’si yoktur, yalnız layout’unki vardır. Create’e basınca o gün hâlâ sayfa `/me`’si görünürdü. Create, Edit ve Detail ertesi gün bağlandı (bölüm 31).

### StrictMode’da iki `/me`

```7:12:web/src/main.tsx
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>,
)
```

Development’ta ilk açılışta Network’te iki `/me` görmen normaldir. StrictMode effect’i iki kez çalıştırır (bölüm 19). Production’da tek istek kalır. StrictMode kapatılmaz.

### Bu günün sınırı

Kolayı şudur: Login’de `usePermissions` çağırmak; `Provider`’ı `Outlet`’in **içine** koymak; `meLoaded` bitmeden Outlet çizmek; Context’i boş dizi default ile yaratıp throw’u kaldırmak; layout’ta `hasPermission` ile link gizleyip sayfanın diziyi boş sanması.

Sonuçta liste tek `/me` okuyor. Create, Edit ve Detail’in Context’e geçmesi bölüm 31. Refresh token ayrı bloktur (32+): permission “ne yapabilirim”, refresh “oturum ne kadar açık”.

---

## J. RefreshTokens tablosu — bölüm 32 (28 Ağustos)

30’da frontend yetki listesini tek `/me`’ye indirdi. Yetki modeli değişmedi: fiiller hâlâ DB join’de, JWT’de değil. Access JWT ise bölüm 8’den beri duruyor. Login imzalı bir string basar, sunucu o string’i tabloya yazmaz, `ExpireMinutes` (bizde 60) dolunca her korumalı istek 401 verir. Arena’da her saat şifre yazmak rahatsız. Yeni access token’ı şifresiz basmak için ikinci bir sır lazım. O sır veritabanında **ham** durmamalı; sızıntıda çalan kişi yeni access üretebilir.

28 Ağustos’ta yalnız o sır için tabloyu kurduk. Sıra: Domain `RefreshToken`, `RefreshTokenConfiguration`, `IApplicationDbContext` / `ApplicationDbContext` `DbSet`, migration `AddRefreshTokens`. Login henüz satır yazmıyor. Generator yok. React yok. SSMS’te tablo görünür, satır sayısı sıfırdır.

Access JWT kısa ömürlü kimlik belgesidir; süresi token’ın kendi `exp` claim’indedir, sunucu onu satır satır tutmaz. Refresh token uzun ömürlü oturum belgesidir; satır DB’dedir, kolon `TokenHash`’tir, ham metin yoktur. İkisini tek JWT claim’ine sıkıştırmak yetki ile oturumu aynı kutuya koyar; o yüzden ayrı tablo.

### Domain — satır, ham token değil

```1:43:ReactBattleArena/ReactBattleArena.Domain/Authentication/RefreshToken.cs
namespace ReactBattleArena.Domain.Authentication;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime utcNow)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = utcNow
        };
    }

    public void Revoke(DateTime utcNow)
    {
        if (RevokedAtUtc is not null)
            return;

        RevokedAtUtc = utcNow;
    }
}
```

İskelet `User` ile aynıdır (bölüm 6): EF için private boş ctor, `private set`, factory `Create`. Klasör `Domain/Authentication/` — `Authorization` (Role / Permission) değil. Bu satır “oturumu uzatma sırrı”dır; fiil listesi değildir.

Kolon adı `TokenHash` bilinçlidir. `Create` ham string almaz; hash alır. Şifredeki `PasswordHash` ile aynı güvenlik fikri (bölüm 7): DB sızarsa ham refresh elinde olmasın. BCrypt burada kullanılmaz; algoritma 29 Ağustos’ta SHA256 hex olacak (bölüm 33). 28 Ağustos’ta yalnız kolon vardır.

Kendi `Id` Guid’i vardır çünkü bir kullanıcının zaman içinde birçok refresh satırı olur: yeniden login, ileride cihaz. `UserRole` composite PK `(UserId, RoleId)` idi; üyelik tektir. Refresh üyelik değil, oturum satırıdır.

`Revoke` satırı silmez. `RevokedAtUtc` yazar. Çıkış veya rotation (bölüm 34–35) eski satırı öldürür. `RevokedAtUtc` doluysa ikinci `Revoke` no-op’dur. 28 Ağustos’ta kimse `Revoke` çağırmaz.

`ExpiresAtUtc` refresh’in bitişidir. `CreatedAtUtc` ne zaman basıldığıdır. Access JWT’nin `exp`’i bu tabloda yoktur; access zaten kendi içinde ölür.

### EF configuration

```1:24:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/RefreshTokenConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactBattleArena.Domain.Authentication;
using ReactBattleArena.Domain.Users;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.TokenHash).IsUnique();

        builder.HasIndex(x => x.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

`ApplyConfigurationsFromAssembly` (bölüm 1) bu sınıfı kendiliğinden alır; `OnModelCreating`’e tek tek yazılmaz. Namespace `Persistence` olmalıdır; `Persistance` yazımı derlenmez, `dotnet ef` de bulamaz.

`HasMaxLength(64)` SHA256 hex içindir (32 byte → 64 karakter). Unique index aynı hash’in iki satır olmasını engeller; 33’te “bu ham token hangi satır?” araması bu index’ten gidecektir. `UserId` index’i “bu kullanıcının refresh satırları” sorgusu içindir (ileride hepsini kapatmak).

`HasOne<User>().WithMany()` — `User` üzerinde `ICollection<RefreshToken>` yoktur; Role tarafında da collection yazmamıştık (bölüm 24). FK yine durur. Cascade: kullanıcı silinince refresh satırları da silinir; yetim hash kalmaz.

Handler’ın tabloya uzanması `IApplicationDbContext` üzerindendir:

```8:17:ReactBattleArena/ReactBattleArena.Application/Abstractions/IApplicationDbContext.cs
public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

```24:24:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/ApplicationDbContext.cs
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

20 Ağustos RBAC gününde bu `DbSet` yoktu. 28 Ağustos’ta eklendi. Login handler ertesi gün `Add` edecek (bölüm 33); o gün kimse set’i kullanmıyordu.

### Migration — şema var, satır yok

```14:45:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260828163543_AddRefreshTokens.cs
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
```

`InitialCreate`’e kolon yapıştırmadık (bölüm 1). `Down` tabloyu düşürür. Designer / snapshot alıntılanmaz. `dotnet ef database update` sonrası SSMS’te `RefreshTokens` görünür; satır yoktur çünkü login henüz `Add` etmez.

### Bu gün HTTP yok

Uygulama içinden kimse bu tabloya yazmaz. `POST /api/auth/login` hâlâ yalnız access JWT basar (bölüm 8). Frontend aynı. Tetikleyen şey `dotnet ef migrations add AddRefreshTokens` ve `database update`’tir. `POST /api/auth/refresh` yoktur; o bölüm 34.

Kolayı şudur: namespace’i `Persistance` yazmak; refresh’i `Users`’a tek kolon yapmak (bir kişi bir satır sanmak; yeniden login kırılır); `UserRole` gibi composite PK koymak; ham token’ı `nvarchar` saklamak; `InitialCreate`’i elle düzenlemek; tabloyu görüp “login artık yeniliyor” sanmak.

Sonuçta uzun ömürlü oturum için tablo ve hash kolonu vardır. Kısa ömürlü kimlik hâlâ access JWT’dedir. Login’in hash yazıp hamı JSON’a koyması bölüm 33.

---

## K. Login refresh basar — bölüm 33 (29 Ağustos)

32’de `RefreshTokens` tablosu kurulmuştu ama boştu. Login hâlâ yalnız access JWT basıyordu. 29 Ağustos’ta generator geldi, login hash’i tabloya yazdı, ham refresh’i JSON’a koydu, React ikinci anahtarı `localStorage`’a aldı. Controller’a yeni action eklenmedi; `POST /api/auth/login` 200 body’si bir alan şişti. `POST /api/auth/refresh` ve `apiFetch` içinde 401’de sessiz yenileme bu gün yoktu (bölüm 34). Tarayıcı ikinci anahtarı tutar, henüz kullanmaz.

### Süre config’de, üretim Infrastructure’da

```13:14:ReactBattleArena/ReactBattleArena.Infrastructure/Security/JwtOptions.cs
    public int ExpireMinutes { get; set; } = 60;
    public int RefreshExpireDays { get; set; } = 7;
```

22 Temmuz’da yalnız `ExpireMinutes` vardı. Access dakika ile, refresh gün ile ölçülür; ikisi de `Jwt` section’dan bind olur. `Configure<JwtOptions>` zaten duruyordu; yeni property eklenince `appsettings`’teki `RefreshExpireDays` okunur.

```1:11:ReactBattleArena/ReactBattleArena.Application/Abstractions/IRefreshTokenGenerator.cs
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
```

Interface Application tarafındadır; handler Infrastructure class’ını görmesin. Namespace `ReactBattleArena.Abstractions` — `IApplicationDbContext` ise `ReactBattleArena.Application.Abstractions`; tutarsız ama gerçek. `Create` üç değeri bir tuple ile döner: ham string, hash, bitiş zamanı. Bugünkü `Hash` metodu 34’te ortak kullanım için ayrıldı; 29 Ağustos’ta SHA256 `Create` içindeydi. Bugünkü kod `Create` içinde `Hash(raw)` çağırır; formül tek yerde kalsın diye.

```9:31:ReactBattleArena/ReactBattleArena.Infrastructure/Security/RefreshTokenGenerator.cs
public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly JwtOptions _options;

    public RefreshTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string Hash(string raw)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    public (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes);
        var hash = Hash(raw); //Böylece iki yerde iki ayrı formül kalma riski kalkıyor.
        var expires = utcNow.AddDays(_options.RefreshExpireDays);
        return (raw, hash, expires);
        // Şifre hasher’ını (BCrypt) kullanma. BCrypt her seferinde farklı tuz üretir; TokenHash unique index ile arama bozulur. 
    }

}
```

32 rastgele byte Base64’e çevrilir; bu **ham** metindir, cevapta bir kez gider. SHA256(UTF8(ham)) hex’e çevrilir; `Convert.ToHexString` 64 karakter üretir, kolon `nvarchar(64)` ile örtüşür. `expires` `RefreshExpireDays` kadar ileridir.

BCrypt burada bilinçli kullanılmaz. Parolada amaç “aynı şifreyi doğrula”: her `Hash` farklı tuz üretir, `Verify` yeter. Refresh’te amaç “istemcinin gönderdiği hamı hash’le, unique index’ten satırı bul”. BCrypt her seferinde başka string üretir; `WHERE TokenHash = @x` tutmaz. Parola BCrypt (bölüm 7), refresh SHA256.

```27:29:ReactBattleArena/ReactBattleArena.Infrastructure/DependencyInjection.cs
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
```

Singleton: istek state’i yok; JWT servisi ile aynı ömür.

### Login artık satır yazar

```8:13:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LoginCommand.cs
public sealed record LoginResult(
    Guid UserId,
    string UserName,
    string Email,
    string Token,
    string RefreshToken);
```

22 Temmuz’da dördüncü alan `Token` ile bitiyordu. ASP.NET JSON camelCase ile `refreshToken` basar. Controller imzası aynı `Ok(result)` — yeni endpoint değil, body’se bir property.

```47:57:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LoginCommandHandler.cs
        var token = _jwtTokenService.CreateToken(user);

        var utcNow = DateTime.UtcNow;
        var (rawRefresh, hash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(
            RefreshToken.Create(user.Id, hash, expires, utcNow));

        await _db.SaveChangesAsync(cancellationToken);


        return new LoginResult(user.Id, user.UserName, user.Email, token, rawRefresh);
```

22 Temmuz’da `CreateToken` sonrası doğrudan `LoginResult` dönülüyordu; JWT için `SaveChanges` yoktu çünkü access tabloda durmaz. Bugün tek `SaveChanges` refresh satırını basar. Access yine yalnız cevapta gider.

Tuple açılımı nettir: `rawRefresh` JSON’a, `hash` `TokenHash` kolonuna. SSMS’teki hash ile F12’deki `refreshToken` **aynı string değildir**. Aynıysa hamı DB’ye yazmışsındır.

`Revoke` hâlâ çağrılmaz. Her login yeni satır ekler; eski satırlar `RevokedAtUtc` null kalır. Rotation bölüm 34’te gelir. Kullanıcı yok veya şifre yanlış yine `null` → 401; satır yazılmaz.

```44:58:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResult>> Login(
    [FromBody] LoginRequest body,
    CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new LoginCommand(body.UserNameOrEmail, body.Password),
            cancellationToken);

        return result is null ? Unauthorized() : Ok(result);
    }
```

Action 22 Temmuz’dan beri budur. 200’de artık `refreshToken` de vardır. Bugünkü dosyada altında `POST refresh` ve `logout` görürsen onlar 34–35; 29 Ağustos’un parçası değildir.

### React — ikinci anahtar, henüz kullanılmıyor

```3:21:web/src/api.ts
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

export function getRefreshToken(): string | null {
  return localStorage.getItem('refreshToken')
}
export function setRefreshToken(token: string) {
  localStorage.setItem('refreshToken', token)
}
```

11 Ağustos’ta `clearToken` yalnız `token` siliyordu. Anahtar adı JSON’daki `refreshToken` (camelCase). 29 Ağustos’ta `apiFetch` 401 görünce login’e atmıyordu, sessiz yenileme de yoktu; sayfa `response.ok`’a bakıyordu. Bugün altında `refreshSession` görürsen bölüm 34’tür. `getRefreshToken` / `setRefreshToken` bu günde kayıt içindir.

```54:57:web/src/LoginPage.tsx
      const data = await response.json()
      setToken(data.token)
      setRefreshToken(data.refreshToken)
      navigate('/characters')
```

30 Temmuz / 11 Ağustos’ta yalnız `setToken` vardı. `auth: false` aynıdır; login’de Bearer yoktur. Çıkışta `clearToken` ikisini birden silmelidir. Yalnız `token` silinirse refresh `localStorage`’da kalır; 34 gelince yanlışlıkla kullanılır.

### Bu günün sınırı

`LoginPage` → `POST /api/auth/login` → handler access JWT + refresh satırı + 200 `{ token, refreshToken, ... }`. Karakter GET hâlâ Bearer access kullanır. Access 401 olursa 29 Ağustos’ta sessiz yenileme yoktur; kullanıcı tekrar login olur. 403 permission’dır; refresh 403’ü düzeltmez.

Kolayı şudur: refresh hash’ini BCrypt yapmak; hamı `TokenHash` kolonuna yazmak (SSMS = localStorage); refresh’i JWT claim’ine gömmek; `data.RefreshToken` (Pascal) okumak — JSON camelCase, `undefined` → `setItem("undefined")`; `clearToken`’dan `refreshToken`’ı unutmak; 403’te yenileme beklemek; `apiFetch`’in 401’de zaten yenilediğini sanmak.

Sonuçta login refresh basıyor: hash DB’de, ham tarayıcıda. Kullanılacak yer `POST /api/auth/refresh` + `api.ts` 401 (bölüm 34).

---

## L. Refresh kullanılır — bölüm 34 (8 Eylül / kod 14–15 Eylül)

33’te login refresh basıyordu ama kimse kullanmıyordu. Access 60 dakika bitince karakter GET 401 oluyor, kullanıcı şifreyi yeniden yazıyordu. `localStorage`’daki `refreshToken` duruyordu. Bu adımda sırayla şunlar geldi: `Hash` metodunun ortaklaşması, `RefreshCommand` + validator + handler (rotation), `RefreshRequest` + `AuthController` `POST refresh`, `api.ts` içinde 401’de bir kez yenile + tekrar. Sayfa kodları değişmedi; `apiFetch` içeride halleder. Permission hâlâ DB join’dedir; refresh yetki listesini JWT’ye yazmaz.

V2’de bu bölüm önce hedef not olarak yazılmıştı; kod sonradan yazılıp test edildi. Aşağıdaki alıntılar bugünkü working tree’dendir.

### Aynı hash, ayrı metot

Login hash üretirken kullanılan formül ile refresh ararken kullanılan formül birebir aynı olmalıdır. 33’te SHA256 `Create` içindeydi. Bugün ayrı `Hash` metodu var; `Create` onu çağırır.

```1:11:ReactBattleArena/ReactBattleArena.Application/Abstractions/IRefreshTokenGenerator.cs
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
```

```18:29:ReactBattleArena/ReactBattleArena.Infrastructure/Security/RefreshTokenGenerator.cs
    public string Hash(string raw)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    public (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes);
        var hash = Hash(raw); //Böylece iki yerde iki ayrı formül kalma riski kalkıyor.
        var expires = utcNow.AddDays(_options.RefreshExpireDays);
        return (raw, hash, expires);
```

İstemci ham refresh token’ı gönderir. Sunucu `Hash` ile `TokenHash` unique index’ten satırı bulur. BCrypt `Verify` burada yoktur: amaç “bu string’in satırı hangisi?”, “şifre doğru mu?” değildir.

### Rotation — eski satır ölür, yeni çift doğar

```6:8:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommand.cs
public sealed record RefreshCommand(string RefreshToken) : IRequest<LoginResult?>;
// null → fiş yok / iptal edilmiş / süresi bitmiş → controller 401 döner.
// Dönüş tipi login ile aynı LoginResult?, çünkü cevap yine yeni access + yeni refresh çifti olacak.
```

Dönüş tipi login ile aynı `LoginResult?`’dır: yeni access + yeni refresh. Şifre yoktur. Boş gövde validator’da 400 olur (`NotEmpty`); 401 değildir.

```5:13:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandValidator.cs
public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
        //Bu sınıfı Program.cs'e kaydetmeyeceksin; AddValidatorsFromAssembly zaten assembly'yi tarıyor.
        //Boş gövde gelirse 400 döner, 401 değil — "fiş yanlış" ile "fiş hiç yok" ayrı şeyler.
    }
}
```

Handler hamı hash’ler, satırı bulur, geçerliyse eskiyi iptal eder, yeni çift basar:

```25:76:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandHandler.cs
    public async Task<LoginResult?> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var hash = _refreshTokens.Hash(request.RefreshToken);

        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null)
            return null;

        if (existing.RevokedAtUtc is not null)
        {
            // Rotation yüzünden her refresh token tek kullanımlık. İptal edilmiş bir token
            // ikinci kez geldiyse aynı zinciri iki taraf tutuyor demektir; çalınmış varsayıyoruz.

            var activeTokens = await _db.RefreshTokens
                .Where(t => t.UserId == existing.UserId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            
            if (activeTokens.Count > 0)
            {
                foreach (var activeToken in activeTokens)
                    activeToken.Revoke(utcNow);

                await _db.SaveChangesAsync(cancellationToken);
            }
                
            return null;
        }


        if (existing.ExpiresAtUtc <= utcNow)
            return null;

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken);
        if (user is null)
            return null;
        existing.Revoke(utcNow);
        //Dört ayrı başarısızlık durumunun hepsi aynı null'u döndürüyor; login'deki "email sızdırmama" mantığının aynısı.
        //existing.Revoke(utcNow) satırından sonra ayrıca bir Update çağırmıyoruz, çünkü satırı sorguyla çektiğimiz an EF onu takibe alıyor;
        //SaveChangesAsync değişikliği kendisi UPDATE'e çeviriyor. Aynı SaveChanges hem eski satırın RevokedAtUtc'sini hem yeni satırın INSERT'ünü tek transaction'da yazıyor — rotation tam olarak bu.

        var (rawRefresh, newHash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(RefreshToken.Create(user.Id, newHash, expires, utcNow));

        await _db.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.CreateToken(user);
        return new LoginResult(user.Id, user.UserName, user.Email, token, rawRefresh);
    }
```

Satır yok, süresi dolmuş veya kullanıcı yok → `null` → 401. Ayrıntı sızmaz (login’deki email sızdırmazlık, bölüm 8).

`Revoke` 32’de yazılmıştı; ilk anlamlı çağrı burada. Eski satır silinmez; `RevokedAtUtc` dolar. Yeni satır yeni hash alır. Aynı ham ikinci kez gelirse zaten iptaldir → yine 401. Çalınan refresh bir kez işe yarar; asıl tarayıcı bir sonraki 401’de düşer.

Bugünkü dosyada `RevokedAtUtc is not null` dalının içinde kullanıcının **tüm aktif** refresh satırlarını iptal eden blok vardır. Bu reuse detection’dır; ince ayrıntı bölüm 36. 34’ün çekirdeği şudur: geçerli satırı `Revoke` et, yeni çift bas, access’i `CreateToken` ile imzala.

JWT yine tabloda yoktur. Permission hâlâ `/me` join’idir.

```1:6:ReactBattleArena/ReactBattleArena.Api/Contracts/RefreshRequest.cs
namespace ReactBattleArena.Api.Contracts
{
    public sealed class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
```

```62:78:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]
    //[AllowAnonymous] şart: bu endpoint'e gelindiğinde access token çoktan ölmüş olacak,
    //[Authorize] koyarsak 401 döngüsüne gireriz. [HasPermission] de yok, çünkü bu bir oturum kapısı, bir fiil kapısı değil.
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResult>> Refresh(
    [FromBody] RefreshRequest body,
    CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RefreshCommand(body.RefreshToken),
            cancellationToken);

        return result is null ? Unauthorized() : Ok(result);
    }
```

`[AllowAnonymous]` şarttır: access zaten ölmüştür; Bearer şartı 401 döngüsü olur. Login gibi 200 / 401 / 400. `[HasPermission]` yoktur; bu oturum kapısıdır, fiil kapısı değildir. Bugünkü dosyada altındaki `logout` bölüm 35’tir.

### `apiFetch` 401 görünce yeniler

İki korumalı istek aynı anda 401 alırsa ikisi de aynı ham refresh’i harcamasın diye `refreshInFlight` tek Promise tutar. İkinci çağrı yeni yenileme başlatmaz; birincinin sonucunu bekler.

```23:60:web/src/api.ts
let refreshInFlight: Promise<boolean> | null = null

async function refreshSession(): Promise<boolean> {
  if (refreshInFlight) {
    return refreshInFlight
  }

  refreshInFlight = (async () => {
    const refreshToken = getRefreshToken()
    if (!refreshToken) {
      return false
    }

    const response = await fetch(`${API_BASE}/api/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    })

    if (!response.ok) {
      clearToken()
      return false
    }

    const data = await response.json()
    setToken(data.token)
    setRefreshToken(data.refreshToken)
    return true
  })()

  try {
    return await refreshInFlight
  } finally {
    refreshInFlight = null
  }
}
```

Refresh isteği **`apiFetch` değil düz `fetch`** olmalıdır. `apiFetch` kullanırsan 401 gelince yine `refreshSession` çağrılır; kendi kuyruğunda kilitlenirsin. Başarısızda `clearToken` hem access’i hem refresh’i siler. Başarıda `data.refreshToken` yeni hamdır; eski localStorage değeri çöptür çünkü sunucu eski satırı revoke etmiştir.

```124:145:web/src/api.ts
  const response = await fetch(`${API_BASE}${path}`, {
  method,
  headers,
  body: body !== undefined ? JSON.stringify(body) : undefined,
  })
  if (response.status !== 401 || auth === false) {
    return response
  }
  const refreshed = await refreshSession()
  if (!refreshed) {
    return response
  }
  const retryHeaders: Record<string, string> = { ...headers }
  const newToken = getToken()
  if (newToken) {
    retryHeaders.Authorization = `Bearer ${newToken}`
  }
  return fetch(`${API_BASE}${path}`, {
    method,
    headers: retryHeaders,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })
```

11 Ağustos’ta `apiFetch` 401’i olduğu gibi dönerdi. Bugün `auth: true` ve 401 ise bir kez yenile, sonra aynı path’i yeni Bearer ile bir kez tekrarla. Login/register `auth: false` olduğu için yanlış şifre 401’inde refresh denenmez. **403’e dokunulmaz**: yetki yoktur, süre dolmamış olabilir; refresh 403’ü 201 yapmaz.

Tekrar istekte **yeni** `Authorization` şarttır. Eski `headers` nesnesini olduğu gibi göndermek en sık hatadır; içinde ölü JWT kalır, ikinci kez 401 alırsın. Body aynı JSON’dır; PUT yarım kalmaz.

Sayfa kodu (`CharactersPage`, layout `/me`) değişmedi. StrictMode çift `/me` hâlâ iki GET atabilir; ikisi 401 olursa `refreshInFlight` tek yenileme yapar.

### Bu günün sınırı

Ölü access ile `GET /api/characters` (veya `/me`) → JwtBearer 401 → `refreshSession` → `POST /api/auth/refresh` → handler hash + rotation → 200 yeni çift → aynı path ikinci kez yeni Bearer ile. Kullanıcı form görmez. Refresh de 7 gün dolmuşsa refresh 401, `clearToken`, sayfa gerçek 401 görür.

Kolayı şudur: refresh’i `apiFetch` ile atmak (döngü); 403’te yenilemek; login 401’inde yenilemek; rotation’suz eski refresh’i canlı bırakmak; BCrypt ile aramak; refresh’e `Authorization: Bearer` koymak; `refreshInFlight` olmadan paralel 401 (ikinci istek revoke edilmiş hamı yollar); yeni `refreshToken`’ı `setRefreshToken` etmemek.

Sonuçta access bitince şifresiz yeni çift gelir. Permission hâlâ DB’dedir. Logout (DB’de satırı öldürmek) bölüm 35. Reuse detection’ın ince testi bölüm 36.

---

## M. Çıkış sunucuda da biter — bölüm 35 (16 Eylül)

34’te access bitince sessiz yenileme çalışıyordu. Çıkış düğmesi ise hâlâ yalnız `clearToken()` çağırıyordu. Tarayıcıdaki iki anahtar siliniyor, veritabanındaki `RefreshTokens` satırı `RevokedAtUtc = null` kalıyordu; yedi gün daha geçerli sayılıyordu. Ham refresh token bir yere kopyalanmışsa “çıkış yaptım” onu geçersiz kılmıyordu. `RefreshToken.Revoke` 32’den beri duruyordu ama yalnız rotation’da kullanılıyordu.

16 Eylül’de sıra şöyleydi: önce `LogoutCommand` + validator + handler, sonra `LogoutRequest` + `AuthController` action, en sonda `api.ts` `logout` ve `AppLayout` `handleLogout`. Backend önce yazıldı; endpoint olmadan frontend’in çağıracağı adres yoktu, Scalar’dan da tek başına test edilebiliyordu. Aynı gün ikinci düzeltme: `/me` gerçek 401 dönünce login’e yönlendirme.

### Command ve validator

```1:5:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LogoutCommand.cs
using MediatR;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest<bool>;
```

Girdi `RefreshCommand` ile aynıdır: ham refresh token. Fark dönüş tipindedir. Burada `LoginResult?` yoktur; çıkışta yeni çift üretilmez. `true` “satır bulundu ve iptal edildi” demektir, ama controller bu bilgiyi dışarı vermez; her hâlükârda 204 döner.

```5:10:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LogoutCommandValidator.cs
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}
```

Kural `RefreshCommandValidator` ile aynıdır. `Program.cs`’e kayıt satırı yazılmaz; `AddValidatorsFromAssembly` bulur (bölüm 2). Boş gövde 400 üretir.

### Handler — `Revoke`’un ikinci kullanımı

```22:36:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LogoutCommandHandler.cs
    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = _refreshTokens.Hash(request.RefreshToken);

        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null || existing.RevokedAtUtc is not null)
            return false;

        existing.Revoke(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
```

İlk adımlar refresh handler ile aynıdır: hamı `Hash` ile SHA256’ya çevir, `TokenHash` unique index’ten satırı bul. Fark, bulunan satırdan sonra yeni satır üretilmemesidir. Oturumu uzatmıyoruz; kapatıyoruz.

Süre kontrolü bilinçli yoktur. Refresh’te `ExpiresAtUtc` kontrolü şarttır; süresi dolmuş token ile yeni access vermek yanlış olur. Çıkışta süresi dolmuş satırı iptal etmek zararsızdır; fazladan `if` getirisi yoktur.

`Revoke` sonrası `Update` çağrılmaz. Satır sorguyla çekildiği anda EF onu takip eder; `SaveChangesAsync` UPDATE üretir. Bu, refresh handler’daki davranışın aynısıdır.

Burada iptal edilen yalnızca o refresh satırıdır (o cihazın oturumu). Kullanıcı başka cihazdan da girmişse o satır ayrı durur. “Tüm cihazlardan çık” `UserId` ile tüm satırları dolaşmak ister; o ayrı özelliktir.

### Api katmanı

```1:5:ReactBattleArena/ReactBattleArena.Api/Contracts/LogoutRequest.cs
namespace ReactBattleArena.Api.Contracts;

public sealed class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
```

```80:91:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Logout(
        [FromBody] LogoutRequest body,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new LogoutCommand(body.RefreshToken), cancellationToken);

        return NoContent();
    }
```

`[AllowAnonymous]` refresh ile aynı sebeptendir: access çoktan ölmüş olabilir. `[Authorize]` koysaydık “çıkış yapamıyorum” durumu çıkardı. Kimlik kanıtı gövdedeki refresh token’ın satırla eşleşmesidir.

Dönüş `NoContent` (204). Handler `false` dönse bile 204 verilir; “bu token sistemde yoktu” sızmaz. Login’deki sızdırmazlık (bölüm 8) ile aynı mantık. `_mediator.Send` dönüş değeri kullanılmaz; `bool` ileride log için durur.

Scalar testi: login ol, `refreshToken`’ı logout gövdesine koy, 204 al, SSMS’te `RevokedAtUtc` dolsun. Aynı ham ile `POST /api/auth/refresh` 401 verir.

### Frontend — `logout` ve Çıkış düğmesi

```62:83:web/src/api.ts
export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken()

  if (refreshToken) {
    try {
      await fetch(`${API_BASE}/api/auth/logout`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ refreshToken }),
      })
    } catch {
      // API kapalıysa bile yerel temizlik yapılmalı; kullanıcı ekranda kalmasın
      //Üç ayrıntı var burada. Token yoksa isteği hiç atmıyoruz, çünkü validator boş değere 400 döner ve çıkış yaparken hata görmek anlamsız. 
      // İstek apiFetch değil düz fetch; apiFetch kullanırsak 401 ihtimalinde refresh denemesi yapar, oysa biz tam tersini istiyoruz. 
      // clearToken() de try/catch'in dışında, yani sunucuya ulaşılamasa bile tarayıcı temizlenir.
    }
  }

  clearToken()
}
```

Üç karar vardır. Refresh yoksa istek atılmaz; validator boş değere 400 döner, çıkışta hata anlamsızdır. İstek düz `fetch` ile gider; `apiFetch` kullanılsa 401’de `refreshSession` çalışır, yani kapatırken yenilemeye çalışırdın. `clearToken()` `try/catch` dışındadır; API kapalı olsa bile tarayıcı temizlenir.

```57:60:web/src/AppLayout.tsx
  async function handleLogout() {
    await logout()
    navigate('/login')
  }
```

Eski hâllerde önce `removeItem('token')`, sonra `clearToken()` vardı. Bugün `logout()` çağrılır ve `async`’tir. `await` olmadan `navigate` istek yarıda kesilebilir. `onClick={handleLogout}` aynı kalır; React async handler’ı kabul eder.

### `/me` 401’inde login’e dönüş

Testte ölü access ve hiç refresh token olmayan durum oluştu. `/me` 401 verdi; `refreshSession` ağa çıkmadan `if (!refreshToken) return false` dedi. Sayfa login’e gitmedi; `loadMe` yalnız `ok` bakıyordu.

```22:38:web/src/AppLayout.tsx
  async function loadMe() {
    try {
      const meResponse = await apiFetch('/api/auth/me')
      if (meResponse.ok) {
        const me = await meResponse.json()
        setPermissions(me.permissions ?? [])
      } else if (meResponse.status === 401) {
        // apiFetch buraya gelene kadar yenilemeyi denedi ve başaramadı: oturum bitti.
        clearToken()
        navigate('/login')
        return
      }
    } catch {
      // /me gelmese de meLoaded bitsin; yoksa sonsuz Yükleniyor
    }
    setMeLoaded(true)
  }
```

`apiFetch` buraya 401 ile geldiyse yenilemeyi zaten denemiş ve başaramamıştır. `clearToken` + `navigate('/login')` gerekir. `return` önemlidir; `setMeLoaded(true)` çalışmasın ki yönlendirme sırasında yetkisiz sayfa bir an görünmesin. ASP.NET MVC’de `[Authorize]` başarısız olunca `LoginPath`’e redirect benzeri bir şey vardır; React’te bunu sen yazarsın.

### Bu günün sınırı

Kolayı şudur: `logout` import edip `api.ts`’e yazmayı unutmak (“does not provide an export named 'logout'”); logout’u `apiFetch` ile atmak; 204’ü hata sanmak; handler `false`’u 404 yapmak (token varlığını sızdırır); `localStorage`’ı elle bozup “kod bozuldu” sanmak.

Sonuçta çıkış sunucuda da gerçektir: satır iptal edilir, o ham bir daha access basamaz. Oturumu bitmiş kullanıcı yetkisiz sayfada kalmaz; login’e gider. Reuse detection (iptal token tekrar gelirse tüm aktif satırlar) bölüm 36.

---

## N. Reuse detection — bölüm 36 (17 Eylül)

34’te rotation şunu yaptı: her başarılı yenilemede kullanılan refresh token satırı `Revoke` edilir ve kullanıcıya yeni bir refresh token verilir. Yani refresh token tek kullanımlıktır. Normal istemci iptal edilmiş hamı bir daha göndermez. Gönderildiyse iki açıklama vardır: token sızmış ve hem saldırgan hem gerçek kullanıcı aynı zinciri kullanmaya çalışıyordur, ya da istemci aynı hamı iki kez yollayan bir hata yapıyordur. (Buradaki her “token” refresh token’dır; access JWT bu tabloda tutulmaz.)

16 Eylül’e kadar iptal satır ikinci kez gelince handler yalnız `null` dönüyordu: istek 401 alıyor, saldırganın elindeki **yeni** zincir (rotation’da basılan B) yaşamaya devam ediyordu. 17 Eylül’de tek dosya değişti: `RefreshCommandHandler`. Frontend’e dokunulmadı; 34’teki `refreshInFlight` bu özelliği yanlış alarmdan korur.

### Rotation’ı bir kez daha

```64:72:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandHandler.cs
        existing.Revoke(utcNow);
        //Dört ayrı başarısızlık durumunun hepsi aynı null'u döndürüyor; login'deki "email sızdırmama" mantığının aynısı.
        //existing.Revoke(utcNow) satırından sonra ayrıca bir Update çağırmıyoruz, çünkü satırı sorguyla çektiğimiz an EF onu takibe alıyor;
        //SaveChangesAsync değişikliği kendisi UPDATE'e çeviriyor. Aynı SaveChanges hem eski satırın RevokedAtUtc'sini hem yeni satırın INSERT'ünü tek transaction'da yazıyor — rotation tam olarak bu.

        var (rawRefresh, newHash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(RefreshToken.Create(user.Id, newHash, expires, utcNow));

        await _db.SaveChangesAsync(cancellationToken);
```

Sızmış refresh’in ömrü bir sonraki yenilemeye kadar kısalır. Daha önemlisi: iptal edilmiş ham ikinci kez gelince bunun anormal olduğunu anlayabilirsin. Bu bölüm o bilgiyi kullanır.

### Kod — iptal satır tekrar gelirse tüm aktifler

```35:54:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandHandler.cs
        if (existing.RevokedAtUtc is not null)
        {
            // Rotation yüzünden her refresh token tek kullanımlık. İptal edilmiş bir token
            // ikinci kez geldiyse aynı zinciri iki taraf tutuyor demektir; çalınmış varsayıyoruz.

            var activeTokens = await _db.RefreshTokens
                .Where(t => t.UserId == existing.UserId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            
            if (activeTokens.Count > 0)
            {
                foreach (var activeToken in activeTokens)
                    activeToken.Revoke(utcNow);

                await _db.SaveChangesAsync(cancellationToken);
            }
                
            return null;
        }
```

Sorgu, gelen satırın `UserId`’si üzerinden o kullanıcının **iptal edilmemiş** tüm refresh satırlarını çeker. `RevokedAtUtc == null` olmasa eski iptalli satırlar da gelirdi; `Revoke` içinde zaten no-op vardır ama boşuna satır çekilir.

Çekilen satırlar EF change tracking’e girer; `foreach` içinde `Revoke` yeter, `Update` gerekmez. Tek `SaveChangesAsync` hepsini UPDATE yazar. SQL kabaca `WHERE UserId = @p AND RevokedAtUtc IS NULL`.

`return null` her durumda çalışır: aktif satır bulunsa da bulunmasa da istek 401’dir. Neden başarısız olduğu sızmaz (bölüm 8).

`if (activeTokens.Count > 0)` isteğe bağlıdır. EF takipte değişiklik yoksa `SaveChangesAsync` veritabanına gitmez. Asıl niyet `foreach` + `SaveChanges`’tir.

### Yanlış alarm ve `refreshInFlight`

İki istek aynı anda 401 alıp ikisi de aynı ham refresh’i gönderse, birincisi rotation yapar, ikincisi iptal satırla gelir. Reuse detection ikinciyi “çalınma” sanır ve kullanıcının tüm aktif satırlarını kapatır. Sebepsiz her yerden çıkış.

```23:28:web/src/api.ts
let refreshInFlight: Promise<boolean> | null = null

async function refreshSession(): Promise<boolean> {
  if (refreshInFlight) {
    return refreshInFlight
  }
```

34’te yazılan bu üç satır tam bunu engeller: paralel 401’lerde ikinci çağrı yeni yenileme başlatmaz, birincinin sonucunu bekler. StrictMode’un `/me`’yi iki kez atması (bölüm 19) bu yüzden sorun çıkarmaz.

### Kim tetikler, nasıl test edilir

Gerçek kullanıcı akışında bu isteği kimse atmaz. Scalar’dan elle veya sızmış token ile gelir. Frontend sonucu: refresh 401 → `refreshSession` `clearToken` → layout `/me` kapısı (35) login’e gider. Kullanıcı şifreyle yeniden girer; saldırganda şifre yoktur, zincir kopar.

17 Eylül testi: (1) Uygulamadan giriş, `localStorage`’daki refresh’i kopyala — **A**. (2) Scalar `POST /refresh` gövdesine A → **200**, cevapta yeni refresh — **B**; SSMS’te A’nın `RevokedAtUtc` dolar (rotation). (3) Aynı istek A ile ikinci kez → **401**; SSMS’te B de iptal (reuse detection). (4) B gönder → **401**. (5) O kullanıcının tüm satırlarında `RevokedAtUtc` dolu. Üçüncü adım özelliğin kanıtıdır: eskiden orada yalnız 401 olurdu, B yaşardı.

### İptal ile süre dolması ayrıdır

Süresi dolan refresh kendi kendine `Revoke` olmaz. Arka plan işi yoktur; `RefreshExpireDays` yalnız `ExpiresAtUtc` hesaplar. Kontrol istek anındadır:

```57:58:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandHandler.cs
        if (existing.ExpiresAtUtc <= utcNow)
            return null;
```

İkisi de 401 üretir ama SSMS’te farklı görünür: birinde `RevokedAtUtc` dolu, öbüründe boş ama `ExpiresAtUtc` geçmişte. Çalınan token en fazla yedi gün işe yarar; rotation varsa genelde daha az — gerçek kullanıcı bir kez yenileyince çalınan satır iptal olur, sonraki kullanım reuse detection’a düşer. Eski satırlar tabloda birikir; üretimde temizlik ayrı iştir, şimdilik yok.

### Bu günün sınırı

Kolayı şudur: `return null`’u atlamak; yalnız gelen satırı iptal edip diğerlerini bırakmak (saldırganın B’si yaşar); `RevokedAtUtc == null` filtresini yazmamak; `refreshInFlight` olmadan reuse açmak (paralel 401 kullanıcıyı atar).

Sonuçta çalınmış refresh ikinci kullanımda o kullanıcının bütün aktif oturumlarını kapatır. Auth kod tarafında açık madde kalmaz. Özet: kimlik JWT, yetki DB, oturum refresh — bölüm 37.

---

## O. Üç kelimeyi yerleştir — bölüm 37 (18 Eylül)

Bu bölüm yeni endpoint yazmaz. 16 Temmuz Register’dan 17 Eylül reuse detection’a kadar olan işin bugünkü hâlidir. Amaç üç kelimeyi kodda nereye denk geldiğini unutmamaktır: authentication (kimsin), authorization (ne yapabilirsin), refresh token (oturum ne kadar sürer).

Kronoloji tek cümle: önce kimlik (Register, Login, JWT, `[Authorize]`), sonra arayüz (React login / Bearer / `/me`), sonra yetki tabloları (RBAC + `HasPermission`), sonra oturum süresi (refresh + rotation + logout + reuse detection). JWT login bitince arayüzsüz test zorlaştığı için React’e geçmiştik. Yetki JWT’ye gömülmesin diye RBAC’te backend’e dönmüştük. Access 60 dakikada ölünce yine backend’e refresh için dönmüştük.

### Authentication — kimsin

Kimlik `AuthController`’daki action’lardadır. Register ve Login şifreyle kim olduğunu kanıtlar. Refresh ve Logout elindeki ham refresh token ile oturumu uzatır veya kapatır. `/me` access JWT olmadan çalışmaz.

```29:32:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
```

```44:46:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
```

```62:65:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]
    //[AllowAnonymous] şart: bu endpoint'e gelindiğinde access token çoktan ölmüş olacak,
    //[Authorize] koyarsak 401 döngüsüne gireriz. [HasPermission] de yok, çünkü bu bir oturum kapısı, bir fiil kapısı değil.
    [HttpPost("refresh")]
```

```80:83:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
```

Refresh ve Logout’ta `[AllowAnonymous]` bilinçlidir: access ölmüşken de yenilemek ve çıkış yapmak gerekir. `[Authorize]` koysan 401 döngüsü olur.

```98:101:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
```

`/me` tam tersidir: token’daki `NameIdentifier` (veya `sub`) olmadan kullanıcı id’si yoktur.

Access JWT’nin içinde bugün rol yoktur. Claim listesi kimlik bilgisidir:

```22:28:ReactBattleArena/ReactBattleArena.Infrastructure/Security/JwtTokenService.cs
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };
```

22 Temmuz’da burada `ClaimTypes.Role` de vardı; 18 Eylül’de kalktı (bölüm 38). Sunucu access token’ı tabloda saklamaz; imza `Jwt:Key` ile doğrulanır. Süre `ExpireMinutes` (60). Parola BCrypt ile `PasswordHash` kolonunda durur; JWT imzası ile karıştırılmaz.

Frontend: `LoginPage` → `POST /api/auth/login` → `setToken` + `setRefreshToken`; korumalı sayfalar `apiFetch` ile `Authorization: Bearer`. Cookie auth yok; Bearer header var.

### Authorization — ne yapabilirsin

Yetki access JWT’de değildir. Zincir `UserRoles` → `RolePermissions` → `Permissions.Code`. Her istekte join:

```15:25:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserPermissionService.cs
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
```

`Distinct` aynı izni iki rol verse bir kez döner. Controller’da rol adı yazılmaz; izin kodu yazılır:

```46:47:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersCreate)]  // POST
    [HttpPost]
```

```4:10:ReactBattleArena/ReactBattleArena.Api/Authorization/HasPermissionAttribute.cs
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = "Permission:" + permission;
    }
}
```

`HasPermission` bir `AuthorizeAttribute` türevidir; policy adı `"Permission:" + kod`. Provider requirement üretir, handler `IUserPermissionService`’e sorar. JWT’de izin claim’i olmadığı için yetki değişince yeniden login gerekmez.

Frontend gizleme yetki değildir. `hasPermission` dizide kod var mı diye bakar; API yine 403 dönebilir. Liste `/me`’nin `permissions` alanından gelir, `AppLayout` `PermissionContext` ile paylaşır. `&&` ile link gizlemek, URL’yi elle yazanın POST’unu durdurmaz.

### Refresh token — oturum ne kadar sürer

Access 60 dakikada ölür. Refresh ham hâli `localStorage`’da, hash’i `RefreshTokens.TokenHash`’te (SHA256, unique). Login hash’i yazar, hamı JSON’a koyar. `POST /api/auth/refresh` rotation yapar: eski satır `Revoke`, yeni çift cevapta. İptal edilmiş refresh tekrar gelirse reuse detection o kullanıcının tüm aktif satırlarını iptal eder (36). `POST /api/auth/logout` yalnız o cihazın satırını iptal eder.

`apiFetch` 401’de `refreshSession` çağırır; refresh’in kendisi düz `fetch` (döngü olmasın). `refreshInFlight` paralel 401’lerde tek yenileme. 403’e dokunulmaz — o yetki yok demektir, süre değil. Yenileme de başarısızsa `AppLayout` `/me` 401’inde `clearToken` + `/login` (35).

### 401 / 403 / refresh

Authentication 401’dir: token yok, bozuk veya süresi dolmuş. Authorization 403’tür: token geçerli, izin kodu yok. Refresh üçüncü şeydir: yeni access üretir, izin listesini değiştirmez. Refresh’i `HttpOnly` cookie’ye taşımak ayrı karar; bugün saklama yeri `localStorage`.

### Sonuç

Kimlik JWT, yetki DB join, oturum süresi refresh token. 27 Temmuz’daki `Users.Role` string modeli Characters yazmada Ağustos’ta `HasPermission` ile, kolon olarak 18 Eylül’de (bölüm 38) kapandı.

---

## P. `Users.Role` kolonu kalkar — bölüm 38 (18 Eylül)

37 özetledi: kimlik JWT, yetki join, oturum refresh. Ama 27–28 Temmuz’un string rolü üç yerde hâlâ duruyordu: `Users.Role` kolonu, JWT’deki `ClaimTypes.Role`, ve `UsersController` Delete’teki `[Authorize(Roles = Roles.Admin)]` (veya yorumlanmış hâli). 18 Eylül’de o artıklar silindi.

Sıra bilinçliydi. Önce Delete’i `users.delete` izin koduna bağladık, sonra claim’i ve kolonu kaldırdık. Tersi olsaydı: `[Authorize(Roles)]` kalkar, JWT’de rol kalmaz, Delete attribute’suz herkese açık kalırdı.

Dosya sırası: `PermissionCodes.UsersDelete` + seed Admin’e o izni verdi → `UsersController` `HasPermission` → `JwtTokenService`’ten rol claim’i çıktı → `User.Role` / `SetRole` / `Create`’teki `role` / configuration kalktı → Register ve CreateUser kolona `"Player"` yazmayı bıraktı → seed’deki `Users.Role` → `UserRoles` aktarım döngüsü silindi → `DropUserRoleColumn` migration + `database update`.

### Neden kolon yetmiyordu

Tek string kolon bir kullanıcının tek rolü olduğunu varsayar. RBAC’te kullanıcı birden fazla role girebilir (`UserRoles`) ve yetki rol adına değil izin koduna bağlıdır. Kolon durduğu sürece ikinci doğruluk kaynağı vardı: `Users.Role = "Player"` iken `UserRoles`’ta Admin satırı olabilirdi. JWT claim’i o kolonu taşıdığı için `[Authorize(Roles)]` kolondaki eski değere bakardı; `HasPermission` tabloya bakardı.

### Entity’den kolonun çıkması

```24:40:ReactBattleArena/ReactBattleArena.Domain/Users/User.cs
    public static User Create(
        string userName,
        string email,
        string? displayName,
        string passwordHash,
        DateTime utcNow)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Points = 0,
            CreatedAtUtc = utcNow
        };
    }
```

28 Temmuz’da beşinci parametre `string role` idi ve `Role = role` atanıyordu. `SetRole` da vardı. İkisi gitti. Register hâlâ Player’ı **ayrı tabloya** yazar; kolonla işi kalmadı:

```42:57:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommandHandler.cs
        var entity = User.Create(
            request.UserName,
            request.Email,
            request.DisplayName,
            passwordHash,
            DateTime.UtcNow);

        _db.Users.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var playerRole = await _db.Roles.SingleAsync(
            r => r.Name == Roles.Player, cancellationToken);
        //Rol yoksa (seed çalışmamış) sessizce geçme, patlat ki fark edesin.

        _db.UserRoles.Add(UserRole.Create(entity.Id, playerRole.Id));
        await _db.SaveChangesAsync(cancellationToken);
```

İki `SaveChanges`: önce `Users.Id` üretilsin, sonra `UserRoles.UserId` o Guid’i alsın. Rol yoksa `SingleAsync` fırlatır.

`CreateUserCommandHandler` aynı `User.Create` imzasını kullanır ama `UserRoles` satırı yazmaz. Bu bugün açılan boşluk değildir; Admin `POST /api/users` ile eklediği kullanıcıda izin listesi boş kalır. Register akışı etkilenmez. CreateUser’a `UserRoles` eklemek ayrı iştir.

```21:22:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserConfiguration.cs
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
        //UserName ve Email unique — aynı kullanıcı / mail iki kez eklenemez.
```

Burada `builder.Property(x => x.Role)...` vardı. Property entity’den düşünce mapping de düşmezse build, mapping durup kolon düşünce runtime patlardı. İkisi birlikte gitti.

### Delete: rol adı yerine izin kodu

`[Authorize(Roles = Roles.Admin)]` yorumlanmış veya kalkmışken Delete attribute’suz kalabilirdi. Temizlik onu açık bırakmak değildir; Characters CUD ile aynı kapıya bağlamaktır:

```77:78:ReactBattleArena/ReactBattleArena.Api/Controllers/UsersController.cs
    [HasPermission(PermissionCodes.UsersDelete)]
    [HttpDelete("{id:guid}")]
```

```8:9:ReactBattleArena/ReactBattleArena.Domain/Authorization/PermissionCodes.cs
    public const string ShopItemsCreate = "shop.items.create";
    public const string UsersDelete = "users.delete";
```

```18:18:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
        await EnsurePermissionAsync(db, PermissionCodes.UsersDelete, cancellationToken);
```

```26:26:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.UsersDelete, cancellationToken);
```

Seed idempotent’tir. Player ve ShopOwner’a `users.delete` verilmedi; eski “yalnız Admin silebilir” davranışı korundu, ama controller’da `Roles.Admin` string’i yoktur. Api açılınca seed çalışır; eski token’da rol claim’i olsa bile handler DB’ye bakar.

Create ve Update hâlâ yalnız `[Authorize]` (giriş yapmış herkes). Bu 18 Eylül’ün kapsamı değildi; eski model yalnız Delete’te Admin string’i kullanıyordu.

### JWT’den rol claim’i

```22:28:ReactBattleArena/ReactBattleArena.Infrastructure/Security/JwtTokenService.cs
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };
```

`CreateToken` artık `user.Role` okumaz; property yoktur. `[Authorize(Roles = ...)]` hiçbir Characters/Users yazma kapısında kalmadığı için claim’i bırakmanın işlevi yoktu. `/me` id’yi `NameIdentifier` / `sub` ile, izinleri `GetCodesAsync` ile alır.

### Seed aktarım döngüsü ve migration

21 Ağustos’ta `AuthSeeder` her açılışta `Users.Role` string’ini okuyup `UserRoles` yoksa ekliyordu. Kolon gidince o kod derlenmez. Döngü silindi; mevcut kullanıcıların `UserRoles` satırları o seed’den beri duruyor. Yeni kayıt Register handler’dan gelir.

```13:16:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260918112656_DropUserRoleColumn.cs
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
```

`Up` kolonu düşürür. `Down` `nvarchar(50) NOT NULL default ""` ile geri ekler; geri alınırsa eski string boş gelir, `UserRoles` etkilenmez. Komut: `dotnet ef migrations add DropUserRoleColumn` sonra `database update`.

### Bu günün sınırı

Delete: Bearer + `users.delete` yoksa 403, token yoksa 401. Register kolon yazmaz, `UserRoles` Player yazar. Login JWT’de rol claim’i taşımaz; `/me` yine join’den izin doldurur.

Kolayı şudur: `[Authorize(Roles)]`’i “kaldırdım” sanıp Delete’i açık bırakmak; claim’i silip Roles attribute’unu unutmak; entity’den `Role`’ü silip configuration’da property bırakmak; migration’sız çalıştırıp kolon hatası almak; CreateUser’ın `UserRoles` yazmamasını bu temizlikle karıştırmak.

Kalan (bilinçli): refresh `localStorage`; süresi dolmuş `RefreshTokens` birikir; CreateUser `UserRoles` yazmaz.

### Sonuç

Tek doğruluk kaynağı `UserRoles` + `RolePermissions`. JWT kimlik taşır, yetki taşımaz. Authentication / authorization / refresh üçlüsü kodda da bu dosyada da aynı modeli anlatır. 7–9 auth dosyası burada biter.