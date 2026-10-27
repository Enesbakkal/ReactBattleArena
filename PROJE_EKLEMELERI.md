# ReactBattleArena — Proje Eklemeleri

Bu dosya **tüm projenin** baştan sona takip listesidir (best practice çizgisi):

- Backend (.NET / CQRS / API)
- Frontend (React / Vite)
- Veritabanı (EF migrations)
- İleride: Docker, Kubernetes, CI/CD, vb.

Öğrenim detayı → `REACT-OGRENIM.md` (başta backend Adım 1–15, sonra React, sonda RBAC) · Anlık durum → `CHECKPOINT.md` · Kalıcı checklist → **bu dosya**.

> **Not:** **Tüm Tamamlananlar** silinmez, kısaltılmaz; sadece alta genişletilir.  
> **14 Ağustos+:** Her anlamlı tamamlanan iş aynı oturumda buraya düzgün maddeyle işlenir.

---

## Tüm Tamamlananlar (Adım 1 → 29)

### Planlama & Ortam

- [x] `PROJE_MANTIGI.md`, `PROJE_EKLEMELERI.md`, `CHECKPOINT.md`
- [x] .NET 10 SDK kurulumu
- [x] Proje dizini: `D:\ReactBattleArena`
- [x] Adım adım, class class geliştirme yaklaşımı

### Adım 1 — Solution Yapısı

- [x] `ReactBattleArena.slnx`
- [x] Domain / Application / Infrastructure / Api projeleri

### Adım 2 — Proje Referansları

- [x] Application → Domain
- [x] Infrastructure → Application + Domain
- [x] Api → Application + Infrastructure



### Adım 3 — Domain (Character)

- [x] `Characters/Character.cs` (Create / Update, private set)



### Adım 4–6 — Character Create CQRS

- [x] MediatR, FluentValidation paketleri
- [x] `CreateCharacterCommand` / Handler / Validator
- [x] `IApplicationDbContext`



### Adım 7 — Infrastructure & DB

- [x] `ApplicationDbContext`, `CharacterConfiguration`
- [x] `AddInfrastructure`, connection string
- [x] Migration `InitialCreate` → `Characters` tablosu



### Adım 8 — DI Pipeline

- [x] `ValidationBehavior`, `AddApplication`
- [x] Program: `AddApplication` + `AddInfrastructure`



### Adım 9 — Characters API + Scalar

- [x] `CreateCharacterRequest`, `CharactersController` (POST)
- [x] `Scalar.AspNetCore`, `launchUrl: scalar/v1`
- [x] WeatherForecast şablonları silindi



### Adım 10 — Validation 400

- [x] `FluentValidationExceptionMiddleware`
- [x] `ApplicationBuilderExtensions`
- [x] Hatalı istek → 400 test edildi



### Adım 11 — Character Queries

- [x] `GetCharactersQuery` + Handler + DTO’lar
- [x] `GetCharacterByIdQuery` + Handler
- [x] Controller GET list / GET by id



### Adım 12 — Character Update / Delete

- [x] `UpdateCharacterCommand` + Validator + Handler
- [x] `DeleteCharacterCommand` + Handler
- [x] Controller PUT / DELETE
- [x] POST Create geri eklendi (yanlışlıkla silinmişti)
- [x] MediatR açıklama yorumları (`DependencyInjection`, Delete command/handler, Controller)



### Adım 13 — User Modülü

- [x] 13.1 `Users/User.cs` (Points, Create, Update, AddPoints)
- [x] 13.2 `CreateUserCommand` + Validator + Handler
- [x] 13.2 `IApplicationDbContext.Users`
- [x] 13.3 `UserConfiguration` (unique UserName/Email)
- [x] 13.3 `ApplicationDbContext.Users`
- [x] 13.3 Migration `AddUsers` → `Users` tablosu
- [x] 13.4 `GetUsersQuery` + Handler (paging tamam)
- [x] 13.4 `GetUserByIdQuery` + Handler
- [x] 13.4 `UpdateUserCommand` + Validator + Handler
- [x] 13.4 `DeleteUserCommand` + Handler
- [x] 13.4 `Contracts/CreateUserRequest.cs` (+ Password)
- [x] 13.4 `UsersController` (Class.cs kaldırıldı)



### Adım 14 — Authentication (devam)

- [x] 14.1 `User.PasswordHash` + `SetPasswordHash`
- [x] 14.1 Migration `AddUserPasswordHash`
- [x] 14.1 `CreateUserCommand` + Validator + Handler (password / hasher)
- [x] 14.2 `IPasswordHasher` (Application.Abstractions)
- [x] 14.2 `BCryptPasswordHasher` (Infrastructure.Security)
- [x] 14.2 DI: `AddSingleton<IPasswordHasher, BCryptPasswordHasher>`
- [x] 14.2 Klasör `Authentication/Commands` (Authorization ile karışmasın diye Auth değil)
- [x] 14.2 `RegisterCommand` + Validator + Handler
- [x] 14.2 Duplicate UserName/Email → `ValidationFailure` (errors dolu 400)
- [x] 14.2 `RegisterRequest` + `AuthController` (`POST /api/auth/register`)
- [x] Register Scalar test: başarılı / kısa şifre / duplicate username
- [x] 14.3 `IJwtTokenService` + `JwtTokenService` + `JwtOptions`
- [x] 14.3 `LoginCommand` + Validator + Handler (`LoginResult`)
- [x] 14.3 `LoginRequest` + `AuthController` `POST /api/auth/login`
- [x] 14.3 JwtBearer DI + `UseAuthentication` (Program.cs)
- [x] 14.3 Login Scalar test: 200 + JWT token
- [x] 14.4 `[Authorize]` korumalı yazma endpoint’leri
- [x] 14.4 Scalar Bearer OpenAPI (`BearerSecuritySchemeTransformer` + preferred scheme)
- [x] 15 `Roles.Admin` / `Roles.Player` sabitleri
- [x] 15 `User.Role` + migration `AddUserRole`
- [x] 15 Register/CreateUser varsayılan Player
- [x] 15 JWT role claim + Character CUD Admin-only
- [x] 15 Test: Admin 201, Player 403
- [x] CreateCharacterRequest Password kaldırıldı



### Adım 16 — React ortam (Vite + TypeScript)

- [x] `web/` — Vite + React + TypeScript (`npm create vite@latest web -- --template react-ts`)
- [x] `npm install` · `npm run dev` → `http://localhost:5173`
- [x] Öğrenim: component / props / state / JSX / interface (`REACT-OGRENIM.md` Faz 0–1)
- [x] Api CORS zaten 5173’e açık (`https://localhost:7275`)



### Adım 17 — Login (controlled form + JWT)

- [x] `LoginPage.tsx` — controlled input (`useState`)
- [x] `POST /api/auth/login` → JWT → `localStorage` (sonra `api.ts`)
- [x] Başarılı / yanlış şifre akışı
- [x] Notlar: `REACT-OGRENIM.md` Faz 2



### Adım 18 — Characters listesi + Bearer

- [x] `CharactersPage.tsx` — `GET /api/characters?page&pageSize`
- [x] Authorization Bearer ile (sonra ortak `apiFetch`)
- [x] Liste state + hata mesajı
- [x] Notlar: Faz 3



### Adım 19 — Register

- [x] `RegisterPage.tsx` — `POST /api/auth/register`
- [x] App route `/register`
- [x] Notlar: Faz 4 (1/2)



### Adım 20 — Admin character create (UI)

- [x] Create formu (önce liste sayfasında; sonra ayrı sayfa — Adım 22)
- [x] `POST /api/characters` — Admin JWT; Player → 403 beklenen
- [x] Notlar: Faz 4 (2/2)



### Adım 21 — react-router + Logout

- [x] `react-router-dom` kurulumu
- [x] `App.tsx` Routes: `/login`, `/register`, `/characters`, …
- [x] Logout: token sil + `/login`
- [x] Notlar: Faz 5



### Adım 22 — CharacterCard + CSS grid + create ayrımı

- [x] Karar: Mega Grid yok → `CharacterCard` + sayfa CSS grid
- [x] `CharacterCard.tsx` + `CharactersPage.css`
- [x] Liste `/characters` · Ekle `/characters/new` (`CharacterCreatePage.tsx`)
- [x] Notlar: Faz 6 + grid kararı



### Adım 23 — Character detail

- [x] Route `/characters/:id` → `CharacterDetailPage.tsx`
- [x] `GET /api/characters/{id}` (GetById) — liste state taşınmaz
- [x] Kart `Link` → detay URL (API çağrısı detay sayfasında)
- [x] Notlar: Faz 6 devam



### Adım 24 — Character edit (Admin)

- [x] Route `/characters/:id/edit` → `CharacterEditPage.tsx` (`:id/edit` sırası `:id`’den önce)
- [x] Açılışta yine GetById → form doldur
- [x] `PUT /api/characters/{id}` → 204 No Content
- [x] Notlar: Edit + useState/useEffect kalıbı



### Adım 25 — Character delete (Admin)

- [x] Detay sayfasında sil + `confirm`
- [x] `DELETE /api/characters/{id}` → 204
- [x] Characters frontend CRUD tamam (List / Create / Detail / Edit / Delete)



### Adım 26 — AppLayout + nested routes + Outlet

- [x] `AppLayout.tsx` — ortak header (marka, Karakterler, Çıkış)
- [x] Login/Register layout dışında; korumalı sayfalar `<Outlet />` içinde
- [x] Nested route öğrenim notu (`REACT-OGRENIM.md`)



### Adım 27 — UI palet kilidi (60-30-10)

- [x] Koyu mor palet kilit: bg `#1e1a24` · surface `#2e2838` · accent `#b39bc9`
- [x] `index.css` / layout / characters CSS hizası
- [x] Not: Happy Hues beyaz kart / teal denemeleri reddedildi



### Adım 28 — API/auth helper (`api.ts`)

- [x] `web/src/api.ts` — `API_BASE`, `getToken` / `setToken` / `clearToken`, `apiFetch`
- [x] Login/Register `auth: false`; diğerleri Bearer varsayılan
- [x] Sayfalar localhost `fetch`’ten taşındı (Login, Register, Characters, Create, Detail, Edit, AppLayout)
- [x] Öğrenim: TypeScript `type` / `Promise` / destructuring (`REACT-OGRENIM.md`)



### Adım 29 — RBAC (Role + Permission) — devam ediyor

Yapılış sırası (back → back → … → front):

- [x] Karar: refresh token yok (ayrı faz — oturum süresi ≠ yetki modeli)
- [x] Kavram: Role vs Permission, many-to-many, UI gizleme ≠ API (`REACT-OGRENIM.md`)
- [x] Domain `Role` + `Permission` — çanta adı / fiil kodu; `Create` factory (`User` kalıbı)
- [x] Domain `UserRole` + `RolePermission` — ara tablo, kendi `Id` yok; satır = FK çifti
- [x] EF `RoleConfiguration` / `PermissionConfiguration` — unique `Name` / `Code`
- [x] EF `UserRoleConfiguration` / `RolePermissionConfiguration` — composite PK; User silinince UserRoles Cascade; Role/Permission Restrict
- [x] `IApplicationDbContext` + `ApplicationDbContext` DbSet’ler (`Roles`, `Permissions`, `UserRoles`, `RolePermissions`)
- [x] `dotnet build` OK
- [x] Migration `AddRbacTables` — `Roles`, `Permissions`, `UserRoles`, `RolePermissions` (Users.Role kolonu duruyor)
- [x] `PermissionCodes` + `Roles.ShopOwner` + `AuthSeeder` (idempotent; API açılışında)
- [x] Mevcut kullanıcılar `Users.Role` string → `UserRoles` satırı
- [x] Karar: yetki JWT’de değil; DB join (`IUserPermissionService` tablo değil)
- [x] `IUserPermissionService` + `UserPermissionService` + DI Scoped
- [x] `HasPermission` + policy provider/handler; Characters CUD `Roles.Admin` kalktı
- [x] Test: Admin POST `/api/characters` 201; Player + RolePermission + UserRoles ile 201 (JWT’ye permission gömülmedi)
- [x] Register `UserRoles` (Player) — Api restart gerekmez
- [x] `GET /api/auth/me` → `{ id, userName, email, permissions }`
- [x] React `hasPermission` (`web/src/permissions.ts`) — `can` değil; C# `HasPermission` ile aynı cümle
- [x] Liste `/me` + Ekle gizleme; detay `/me` + Düzenle/Sil; URL ile create hâlâ 403 (API kapısı)
- [x] Create sayfa kapısı: `/me` + `meLoaded` + `hasPermission(characters.create)` yoksa `Navigate` listeye
- [x] Edit sayfa kapısı: `characters.update`; Sanji URL ile listeye düşer; zoro Guid ile form
- [x] `PermissionContext` + AppLayout tek `/me`; `CharactersPage` `usePermissions`
- [x] Create / Edit / Detail `usePermissions` (sayfa `/me` kalktı; `meLoaded` Create’de layout’a bırakıldı)
- [x] Karar: refresh **var** (oturum); permission hâlâ DB join, JWT’ye gömülmez

### Adım 30 — Refresh token (oturum) — devam ediyor

- [x] Kavram: access JWT (kimsin, kısa) vs refresh (yeni access, hash DB’de)
- [x] Domain `RefreshToken` — `User` kalıbı; `TokenHash`; `Revoke`
- [x] EF `RefreshTokenConfiguration` — kendi `Id`; `TokenHash` unique 64; User Cascade
- [x] `IApplicationDbContext` + `ApplicationDbContext` `RefreshTokens`
- [x] Migration `AddRefreshTokens` + `database update`
- [x] Login: ham refresh cevapta, hash satırda
- [x] `IRefreshTokenGenerator.Hash` + `RefreshTokenGenerator.Hash` — login ve refresh aynı SHA256; `Create` artık aynı metodu çağırıyor (BCrypt değil: tuz değişir, unique index araması bozulur) — 14 Eyl
- [x] `RefreshCommand` (`LoginResult?`) + `RefreshCommandValidator` (`NotEmpty`, `MaximumLength(200)`; boş gövde 400, 401 değil) — 14 Eyl
- [x] `RefreshCommandHandler` — hamı hash’le, `TokenHash`’ten satırı bul; yok / `RevokedAtUtc` dolu / süresi bitmiş / user yok hepsi `null` (401 sızdırmaz); geçerliyse `Revoke` + yeni çift = **rotation**. `Revoke` 28 Ağustos’tan beri duruyordu, ilk kez burada çağrıldı — 14 Eyl
- [x] `RefreshRequest` + `AuthController` `POST /api/auth/refresh` — `[AllowAnonymous]` (access ölmüş olur; `[Authorize]` 401 döngüsü yapar), 200 / 401 / 400 — 15 Eyl
- [x] `api.ts` sessiz yenileme — `refreshSession` + `refreshInFlight` (paralel 401’ler tek yenileme bekler); `apiFetch` 401’de bir kez yeniler ve **yeni** Bearer ile tekrar atar; refresh’in kendisi düz `fetch` (döngü olmasın); `auth: false` ve 403 dokunulmaz — 15 Eyl
- [x] Test: `ExpireMinutes` geçici 1 → ölü access ile liste isteği 401 → refresh → 200; kullanıcı login formu görmedi (değer 60’a geri alınacak) — 15 Eyl
- [x] Temizlik: `RefreshCommand` namespace’i `Authentication.Commands`, 4 gereksiz `using`, `ExpireMinutes` 60 — 16 Eyl
- [x] `POST /api/auth/logout` — `LogoutCommand` + validator + handler (hash → satır → `Revoke`; süre kontrolü yok), `LogoutRequest`, `[AllowAnonymous]`, 204; handler `false` dönse de 204 (token varlığı sızmaz) — 16 Eyl
- [x] React Çıkış — `api.ts` `logout()` (refresh token yoksa istek atmaz, düz `fetch`, `clearToken` her hâlde çalışır); `AppLayout` `handleLogout` `async` + `await logout()` — 16 Eyl
- [x] `AppLayout` `/me` 401 kapısı — yenileme başarısızsa `clearToken` + `navigate('/login')`; önceki hâlde kullanıcı yetkisiz sayfada kalıyordu — 16 Eyl
- [x] Reuse detection — `RefreshCommandHandler`: iptal edilmiş refresh token tekrar gelirse o kullanıcının `RevokedAtUtc == null` satırlarının hepsi `Revoke`, dönüş yine 401 (`return null` if dışında). Frontend değişmedi; yanlış alarmı `refreshInFlight` engelliyor. A/B testi başarılı — 17 Eyl
- [ ] Karar bekliyor: refresh `localStorage` → `HttpOnly` cookie (danışmana soruldu; şimdilik `localStorage`)






### Adım 31 — Eski rol modelinin temizliği (18 Eylül, tamam)

17 Eylül'de fark edildi: RBAC'a geçtik ama 27–28 Temmuz'un string rol modeli
birkaç noktada duruyor. Sıra önemli; ters sırada `UsersController` korumasız kalır.

- [x] `UsersController` `[Authorize(Roles = Roles.Admin)]` → `HasPermission(PermissionCodes.UsersDelete)` + seed Admin — 18 Eyl
- [x] `JwtTokenService`'ten `ClaimTypes.Role` claim’i kaldırıldı — 18 Eyl
- [x] `RegisterCommandHandler` / `CreateUserCommandHandler` `Users.Role`'e yazmayı bıraktı; `User.Role` property + `SetRole` + `UserConfiguration` eşlemesi kalktı — 18 Eyl
- [x] Migration `DropUserRoleColumn` + `database update` — 18 Eyl
- [x] `AuthSeeder`'daki `Users.Role` → `UserRoles` tek seferlik aktarım bloğu kalktı — 18 Eyl
- [x] Belge: `PROJE_MANTIGI.md`'ye "Yetkilendirme Modeli" ve "Oturum Modeli" bölümleri yazıldı — 17 Eyl

### Adım 32 — Light Blue kabuk (13 Ekim, karakter sayfaları)

Şablonu ikinci uygulama veya npm paketi yapmadık. `light-blue-react` klasörü referans. Ürün arayüzü `web/`, port 5173. API .NET’te kaldı. Şablonun sol menüsü ve üst çubuğu `web/src/shell` altına yazıldı. Hamburger her sayfaya ayrı konmadı. `App.tsx` içinde login ve register `AppLayout` dışında, eski mor palette. Karakter listesi, ekleme, detay ve düzenleme `AppLayout` altında `<Outlet />` ile açılıyor. Eski üst şerit (Karakterler linki ve Çıkış butonu) kalktı. Yerine `Sidebar` ve `Header` geldi. Menünün açık hali `sidebarOpen` state’i `AppLayout`’ta duruyor. Header’daki hamburger yalnız `onToggleSidebar` çağırır. Çıkış ikonu `onLogout` ile mevcut `handleLogout`’a gider: `logout()` sonra `/login`. Header kendi içinde `logout()` çağırmaz. Menü `NavLink` kullanır, tam sayfa yenilenmez.

- [x] `bootstrap` ve `sass` yalnız kabuk için kuruldu. Bootstrap CSS `main.tsx` içinde `index.css`’ten önce gelir. Böylece login’deki yazı rengi Bootstrap’in varsayılan koyu gri gövdesinin altında kalmaz — 13 Eki
- [x] `Sidebar` — Battle Arena ve Karakterler. `Header` — hamburger ve çıkış ikonu. İkisi de `AppLayout` içinde `Outlet`’in çevresine takıldı — 13 Eki
- [x] Kart genişliği kolonla sabit. Yükseklik resim dosyasının pikselinden değil, en-boy oranından gelir. Resimsiz kutu kare kalır. Izgara masonry değil: bir satırdaki en uzun kart, sonraki satırın başını belirler — 13 Eki
- [x] Kart, form ve butonlara Bootstrap sınıf adı (`btn`, `form-control`) yazılmadı. Hazır Bootstrap, input’u beyaz zemin yapardı. Mevcut sınıflar şablonun renk rollerine boyandı: iş butonu info, geri linki default, Sil danger — 13 Eki
- [x] Listeye dön ve Detaya dön önce `.characters-page__actions` dışında durduğu için yalnız genel `a` rengini alıyordu (mor, çerçevesiz). `characters-page__back` ile default hap oldular — 13 Eki
- [ ] Sil hâlâ şablonun canlı danger rengi. Soluk kırmızı (`$red-dark`) istenmişti, dosyaya yazılmadı — 13 Eki
- [x] Giriş ve kayıt kabuğun dışında kaldı; görünüm Adım 33’te Light Blue kartına çekildi — 22 Eki
- [ ] Harita, istatistik kartı ve takvim bu adıma girmedi

### Adım 33 — Login / Register Light Blue kartı (22 Ekim)

Kabuk dışında kalan giriş ve kayıt, şablonun auth kartına yaklaştırıldı. Sosyal login ve demo kullanıcı kutusu kopyalanmadı. Register gövdesi `userName`, `email`, `displayName`, `password` olarak kaldı.

- [x] `AuthLayout` + `AuthLayout.module.scss` — tam ekran radial gradient (`#333867` → `#17193b`), ortada kart, altta yıl — 22 Eki
- [x] `AuthForm.module.scss` — ortak form: koyu input, kırmızı submit, hata / başarı satırı — 22 Eki
- [x] `LoginPage` layout’a taşındı; kullanıcı/kilit SVG; kayıt sonrası `navigate('/login', { state: { registered: true } })` ile yeşil mesaj — 22 Eki
- [x] `RegisterPage` aynı kart; başarıda login’e state ile dönüş; “Giriş yap” linki state taşımaz — 22 Eki
- [x] Giriş sonrası rota hâlâ `/characters` (Dashboard sayfası yok) — 22 Eki

### Adım 34 — User profil API + iletişim gizliliği + parola (25–26 Ekim)

Sayfa modeli User ekranlarını tarif ederken yazma yetkisi eksikti. Kilit: hiç kimse (Admin dahil) başka kullanıcının profilini veya parolasını yazamaz. Listeden View salt okunur. E-posta/telefon varsayılan gizli (`IsContactPublic`).

- [x] `User`: `FirstName`, `LastName`, `Phone`, `AvatarUrl`, `IsDisabled`, `IsContactPublic` + `UpdateProfile` — 25 Eki
- [x] Migration `AddUserProfileFields`; boş `AddUserContactVisibility` geçmişte kaldı; kolon `AddUserContactVisibilityColumn` ile eklendi — 25 Eki
- [x] Liste/detay DTO: profil alanları + `Roles` alt sorgu (`UserRoles` ⋈ `Roles`) — 25 Eki
- [x] Eski `PUT /api/users/{id}`: token id ≠ route id → `403` — 26 Eki
- [x] `PUT /api/users/profile`: id yalnız JWT `NameIdentifier`/`sub`; gövdede id yok; `IsContactPublic` yazılır — 26 Eki
- [x] `GET /api/users` ve `GET /api/users/{id}` `[Authorize]`; `CallerId` ile e-posta/telefon: sahip veya bayrak açık değilse `null`; `users.delete` bayrağı aşmaz — 26 Eki
- [x] `PUT /api/users/password`: mevcut parola `Verify`, yeni hash `SetPasswordHash`; id token’dan; yanlış mevcut → 400 — 26 Eki

### Adım 35 — Users menü + User Management tablosu (27 Ekim)

Backend yazma kapıları hazırdı; React menü ve tablo bağlandı. My Profile / Edit / Change Password içerikleri ve salt okunur View gövdesi sırada.

- [x] Sidebar `USERS`: User Management, My Profile, Edit Profile, Change Password — 27 Eki
- [x] Rotalar `AppLayout` içinde: `/users`, `/profile`, `/profile/edit`, `/profile/password`, `/users/:id` — 27 Eki
- [x] Stub: `MyProfilePage`, `EditProfilePage`, `ChangePasswordPage`, `UserDetailPage` — 27 Eki
- [x] `UsersPage` tablo: `GET /api/users`, sayfa boyutu, View + `users.delete` ile Delete; satırda Edit yok — 27 Eki
- [x] `PERMISSIONS.usersDelete`; aksiyonlar Light Blue tarzı mavi/kırmızı buton — 27 Eki
- [ ] My Profile içeriği (`/profile`)
- [ ] Salt okunur View içeriği (`/users/:id`)
- [ ] Edit Profile formu → `PUT /api/users/profile`
- [ ] Change Password formu → `PUT /api/users/password`
- [ ] Liste araması (API’de search yok)

### Güvenlik & Git

- [x] `Microsoft.OpenApi` güvenlik güncellemesi
- [x] `.gitignore`
- [x] GitHub push (önceki commitler)
- [ ] İsteğe bağlı: güncel FE commit push (kullanıcı kendi commit’liyor)



### Sıradaki

- [x] Login: refresh üret (hash DB, ham JSON)
- [x] `POST /api/auth/refresh` (eski revoke, yeni çift)
- [x] React: access bitince sessiz yenile (`api.ts`)
- [x] `ExpireMinutes` 60’a geri al (test için 1’e düşürülmüştü)
- [x] `POST /api/auth/logout`
- [x] Reuse detection (iptal edilmiş token tekrar gelirse tüm satırları iptal)
- [x] Login/Register Light Blue kartı (Adım 33)
- [x] User profil API + iletişim gizliliği + parola (Adım 34)
- [x] Users menü + User Management tablosu (Adım 35 — tablo)
- [ ] My Profile / View / Edit Profile / Change Password sayfa içerikleri
- [ ] Liste yükleme 3–4 sn gecikmesi (inceleme)
- [ ] Battle Arena backend (formül CHECKPOINT bölüm 2’de kilitli)
- [ ] (İleride) Docker / Kubernetes / CI



### Hafta sonu notu

- [x] Kod akışı, validator/middleware, record, paging, IRequest açıklamaları yazıldı

- Detay: `REACT-OGRENIM.md` başı (Backend Adım 1–15 + 12 Temmuz kod akışı). Eski “sadece CHECKPOINT özeti” buraya taşındı.

---



## Günlük Kayıt



### 2 Temmuz 2026

- Adım 1–5, plan dokümanları, `.gitignore`



### 3 Temmuz 2026

- Adım 6–7, InitialCreate migration



### 6 Temmuz 2026

- Adım 8, GitHub push



### 7 Temmuz 2026

- Adım 9, Scalar UI



### 8 Temmuz 2026

- Adım 10–11 başlangıç, hafta sonu teknik sorular not edildi



### 9 Temmuz 2026

- Adım 12 Character Update/Delete CRUD, MediatR yorumları



### 10 Temmuz 2026

- Adım 13 User Domain + Create + persistence (`AddUsers`)
- Adım 13.4 Application CRUD (query/update/delete) + CreateUserRequest
- GetUsersQueryHandler paging notu



### 12 Temmuz 2026

- Hafta sonu konularının açıklaması
- CHECKPOINT + PROJE_EKLEMELERI senkron güncelleme
- Commit mesaj geçmişi eklendi



### 16 Temmuz 2026

- UsersController tamamlandı
- PasswordHash + AddUserPasswordHash migration
- IPasswordHasher + BCryptPasswordHasher
- Register (Authentication klasörü) + AuthController
- Duplicate username 400 mesajı ValidationFailure ile düzeltildi



### 22 Temmuz 2026

- Login + JWT tamamlandı
- IJwtTokenService, JwtTokenService, JwtOptions
- LoginCommand + AuthController POST login
- JwtBearer + UseAuthentication
- Scalar: login 200 + token test edildi



### 28 Temmuz 2026

- Scalar Bearer kilidi (OpenAPI transformer)
- CreateCharacterRequest Password silindi
- Adım 15: string Role (Admin/Player)
- Character CUD Admin-only; Player 403 test edildi
- IdentityModel sürüm hizası (IDX00001)
- Sırada: React (başlandı) / rol tabloları / Arena



### Ağustos 2026 (React frontend — özet)

- Adım 16–21: Vite, Login, Characters+Bearer, Register, Create UI, react-router+Logout
- Adım 22–25: Card+grid, Detail, Edit, Delete (Characters FE CRUD)
- Adım 26–28: AppLayout/Outlet, palet kilidi, `api.ts` helper
- 13–14 Ağu: test Q&A detay notları; `PROJE_EKLEMELERI` React sync



### 20 Ağustos 2026

- Adım 29 RBAC başladı (refresh token yok)
- Domain: `Role`, `Permission`, `UserRole`, `RolePermission`
- EF config + DbSet; composite PK (ayrı UserRoleId yok); build OK
- `Users.Role` string ve JWT hâlâ eski model; tablolar + seed henüz yoktu
- Not: `REACT-OGRENIM.md` (role vs permission, FK kimde)



### 21 Ağustos 2026

- Migration `AddRbacTables` (4 tablo; `Users.Role` drop edilmedi)
- Seed: `PermissionCodes`, `ShopOwner`, `AuthSeeder` + `Program` CreateScope
- Mevcut user’lar `UserRoles`’e taşındı. JWT hâlâ string Role claim
- Sıradaki o gün: JWT permission düşünülüyordu; 22 Ağu karar: yetki DB, JWT kimlik



### 22 Ağustos 2026

- Yetki JWT’ye gömülmez; `IUserPermissionService` join (UserPermission tablosu yok)
- `HasPermission` + policy provider/handler; Characters CUD `Roles.Admin` kalktı
- Test: `POST /api/characters` (Create). Register’a karakter JSON’u → UserName/Email 400
- Player 403: RolePermissions yetmez, `UserRoles` şart; Register seeder’a kadar satır yazmıyor
- Player + create permission + UserRoles → aynı token ile 201



### 24 Ağustos 2026

- Register artık `UserRoles` (Player) yazıyor; Api restart şart değil
- `GET /api/auth/me` permissions DB join; zoro Admin dört kod, robin’de elle eklenmiş create göründü
- React `permissions.ts` + `hasPermission`; liste/detay `load` içinde `/me`
- Detayda `/me` unutulunca `permissions=[]` → zoro’da da Düzenle/Sil yoktu
- JSX `koşul && <Link>`: sol kapı, sağ çizilen parça

### 25 Ağustos 2026

- `CharacterCreatePage`: token’dan sonra yetki kapısı; `/me` bitmeden form yok (`meLoaded`)
- Hook sırası: `useEffect` erken `return`’ün **üstünde** olmalı — aksi halde sonsuz Yükleniyor
- Yazım: `me.permissions`; preview `return` `setMeLoaded`’ı atlamasın
- UI: Sanji `/me` → `["characters.create"]` — new’de form açılır (doğru). Player rolüne elle create; kişiye değil
- Katalog ekleme ≠ takımda karakter. Create kapısı yanlış sayfa değil; kaçış testi Edit (`update` Player’da yok)

### 26 Ağustos 2026

- Edit kapısı: `PERMISSIONS.charactersUpdate`; `/me` karakter GET’ten önce; kapı `useEffect` sonrası
- URL `:id` Guid — `/characters/Franky/edit` 404 (isim değil). Sanji edit’te listeye düşer (doğru)
- Refresh token: bu fazda yok; permission değil, oturum süresi — ayrı konuşma (CHECKPOINT’te işaretli)
- `PermissionContext` + AppLayout `/me` + `meLoaded` sonra `Outlet`; liste `usePermissions`
- Create/Edit/Detail’e bu turda dokunulmadı: önce tek sayfada kanıt (liste Ekle + Network)
- `hasPermission` ≠ `usePermissions` (kontrol vs kaynak)
- Nested route: liste↔create layout unmount olmaz; dönüşte layout `/me` yok
- Plan kilit: yarın diğer sayfalar Context; öbür gün refresh konuşması; sonra 26 Ağu `REACT-OGRENIM` okuma hali (üzerine yazma yok)
- Dev Strict Mode: ilk açılışta 2× `me` normal; 3. Initiator/HMR/eski Network
- `web/src` düz klasör bu ölçekte doğru; Context ile big-bang `pages/` yok

### 27 Ağustos 2026

- Create / Edit / Detail `usePermissions`; sayfa `/me` ve Create `meLoaded` kalktı (layout bekliyor)
- Sanji `/characters/new` açık (Player’da create var); kaçış `/characters/{guid}/edit`

### 28 Ağustos 2026

- Refresh başı: Domain `RefreshToken` + EF + `AddRefreshTokens` → tablo `RefreshTokens`
- Ham token DB’de yok (`TokenHash`). Kendi `Id` (UserRole composite değil)
- Config: `Persistence` yazımı, `RefreshTokens` çoğul, `OnDelete` noktalı virgül — yoksa build/ef patlar
- `dotnet-ef` 10.0.5 vs runtime 10.0.9 uyarı; migration yine Done. Proje NuGet’i bu yüzden güncelleme
- Login henüz satır yazmıyordu (29 Ağu’da yazdı)

### 8 Eylül 2026

- `REACT-OGRENIM-V2` bölüm 32–34 yazıldı. `POST /api/auth/refresh` kodu sıradaki iş (VS Code)

### 14 Eylül 2026

- Durum taraması: AuthN + AuthZ (RBAC) bitmiş; refresh yarım — login üretiyor, endpoint ve 401 yenilemesi yok
- RBAC açılımı tekrar: Role-Based Access Control; bizde rol → `RolePermissions` → izin kodu zinciri (rol adı controller’da yazılı değil)
- Karar: refresh saklama **`localStorage` kalıyor**; cookie kararı projenin danışmanına soruldu. Cookie’ye geçilirse yapılacaklar listesi `CHECKPOINT.md`’de duruyor
- Application katmanı yazıldı: `IRefreshTokenGenerator.Hash`, `RefreshTokenGenerator.Hash`, `RefreshCommand`, `RefreshCommandValidator`, `RefreshCommandHandler` (rotation)
- Yanında öğrenim yorumları: `Program.cs` middleware sırası, `JwtTokenService` imza mantığı, `BCryptPasswordHasher` tuz, `BearerSecuritySchemeTransformer` şema kontrolü

### 15 Eylül 2026

- `RefreshRequest` + `AuthController` `POST refresh`; `api.ts` `refreshSession` / `refreshInFlight` + `apiFetch` 401 tekrarı yazıldı
- `ExpireMinutes` 1 ile uçtan uca test: 401 → refresh → 200, kullanıcı formu görmedi
- Kod kontrolünde bulunanlar: `RefreshCommand.cs` namespace’i `Application.Commands` olmuş (klasörü `Authentication/Commands`) → üç dosyada fazladan `using`; validator’da gereksiz `using static System.Net.WebRequestMethods`; `Program.cs`’te kullanılmayan `System.IdentityModel.Tokens.Jwt`; `ExpireMinutes` 1 kalmış
- `dotnet build` hatası derleme değil dosya kilidi (Api çalışırken DLL kopyalanamıyor)

### 16 Eylül 2026

- Dünkü temizlik yapıldı: `RefreshCommand` namespace’i düzeldi, 4 gereksiz `using` gitti, `ExpireMinutes` 60
- `POST /api/auth/logout` yazıldı: `LogoutCommand` (`IRequest<bool>`) + validator + handler; `Revoke` ikinci kez kullanıldı, çıkışta süre kontrolü yok
- Frontend Çıkış sunucuya bağlandı: `api.ts` `logout()` + `AppLayout` `handleLogout` `async`
- `AppLayout` `loadMe` 401 kapısı: yenileme başarısızsa login’e yönlendirme (önce yetkisiz sayfada kalıyordu)
- Hata: `logout` import edildi ama `api.ts`’e eklenmemişti → sayfa açılmadı; export/import ismi birebir aynı olmalı
- Yazım kuralı değişti: takma adlı benzetme yok; terim doğrudan yazılır (“fiş” = refresh token, “çanta” = rol, “kâğıt” = permission dizisi)
- `REACT-OGRENIM-V2` bölüm 35 yazıldı

### 17 Eylül 2026

- Reuse detection yazıldı (`RefreshCommandHandler`): iptal edilmiş refresh token tekrar gelirse o kullanıcının tüm aktif satırları iptal, dönüş 401
- Test A/B ile doğrulandı: A → 200 + B; A ikinci kez → 401 **ve B de iptal**; B → 401; tüm satırlar `RevokedAtUtc` dolu
- Rotation tanımı nota yazıldı: her yenilemede eski satır iptal + yeni token = tek kullanımlık; ikinci kullanım anormal olduğu için tespit edilebilir
- Düzeltme: `activeTokens.Count > 0` kontrolü DB turunu engellemiyor — EF değişiklik yoksa `SaveChanges`’te veritabanına hiç gitmiyor; kontrol isteğe bağlı
- **Auth kod tarafı bitti.** Açık konu yalnız `HttpOnly` cookie kararı (danışman)
- `REACT-OGRENIM-V2` bölüm 36 yazıldı

### 18 Eylül 2026

- Eski string rol modeli koddan çıktı: `User.Role` / `SetRole` / EF eşlemesi, JWT `ClaimTypes.Role`, Register/CreateUser’ın kolona `"Player"` yazması
- `UsersController` Delete `HasPermission(users.delete)`; seed yalnız Admin (yorum satırındaki `[Authorize(Roles)]` korumasız bırakıyordu)
- `AuthSeeder` `Users.Role` → `UserRoles` aktarım bloğu kalktı
- Migration `DropUserRoleColumn` uygulandı (`Users.Role` kolonu düştü)
- `REACT-OGRENIM-V2` bölüm 37 (auth üçlüsünün son hâli) ve 38 (`Users.Role` temizliği) yazıldı

### 13 Ekim 2026

- Adım 32: Light Blue kabuk (`web/src/shell`), karakter sayfaları `AppLayout` altında

### 19 Ekim 2026

- Sayfa modeli + dövüş formülü CHECKPOINT’e kilitlendi; arena/dashboard kodu yok

### 22 Ekim 2026

- Adım 33: `AuthLayout` / `AuthForm`; Login ve Register Light Blue kart; kayıt sonrası login’de `location.state` mesajı

### 25–26 Ekim 2026

- Adım 34: User profil kolonları + `IsContactPublic`; self-only profile/password; GET’te iletişim gizleme; eski PUT IDOR kapatıldı
- Commit: `users - profil ve parola yalnız token'daki kullanıcıya yazılır`

### 27 Ekim 2026

- Adım 35: Users menü + `/users` tablosu (View / Delete); profil sayfaları stub; Edit satırda yok

---



## Hazırlık

- [x] .NET 10 SDK
- [x] `D:\ReactBattleArena`
- [x] Plan dokümanları
- [x] GitHub repo (önceki pushlar)

---



## Faz 1 — Solution & Altyapı

- [x] Solution + referanslar
- [x] MediatR + FluentValidation + EF Core
- [x] `AddApplication` + `ValidationBehavior`
- [x] React frontend (Vite `web/` — Adım 16+)
- [ ] Docker Compose (opsiyonel)
- [ ] Kubernetes / CI (ileride)

---



## Faz 2 — Character Modülü

- [x] Entity + full CRUD (Create/Read/Update/Delete)
- [x] Validator + API controller + Scalar
- [x] React: liste, kart grid, create/detail/edit/delete (Adım 18, 20, 22–25)

---



## Faz 3 — User Modülü

- [x] User domain entity
- [x] User CRUD (Application: command + query)
- [x] FluentValidation kuralları
- [x] Migration `AddUsers`
- [x] Users API controller
- [x] PasswordHash (+ CreateUser password)
- [x] Profil alanları + `IsContactPublic` + self-only `PUT profile` / `PUT password` (Adım 34)
- [x] React: Users menü + User Management tablosu (Adım 35); My Profile / Edit / parola içerikleri sırada

---



## Faz 4 — Authentication & Authorization

- [x] Register endpoint (`POST /api/auth/register`)
- [x] Password hashing (`IPasswordHasher` / BCrypt)
- [x] Login endpoint (`POST /api/auth/login`)
- [x] JWT token üretimi + JwtBearer
- [x] `[Authorize]` korumalı endpoint’ler
- [x] Rol tabanlı yetkilendirme → RBAC izin kodları (`HasPermission`, Adım 29–31)
- [x] React login/register + router + token (`api.ts`) — Adım 17, 19, 21, 28, 33
- [x] Rol tabloları + seed + FE `hasPermission` / Context — Adım 29–31
- [x] Refresh / logout / reuse — Adım 30
- [x] User self-only profil + parola + iletişim gizliliği — Adım 34

---



## Faz 5 — Battle Arena (ileride)

- [ ] Takım, eşleşme, savaş, puan

---



## Faz 6 — Ödül Sistemi (ileride)

- [ ] Ödül kataloğu, puan ile alma, envanter

---



## Commit mesajları (özet)


| Konu                 | Mesaj (kısa)                                                                 |
| -------------------- | ---------------------------------------------------------------------------- |
| İlk foundation       | `migration - InitialCreate - solution CQRS Character ... veritabani`         |
| DI                   | `application - AddApplication - MediatR FluentValidation DI pipeline ...`    |
| API + Scalar         | `api - CharactersController - ... Scalar UI ...`                             |
| Validation 400       | `api - validation middleware - ... 400 ValidationProblemDetails ...`         |
| Character GET        | `character - query ve controller - GetCharacters... GET endpointleri`        |
| Character PUT/DELETE | `character - update delete crud - ...`                                       |
| **16 Temmuz**        | `auth - register password - ...`                                             |
| **22 Temmuz**        | `auth - login jwt - ...`                                                     |
| **28 Temmuz**        | `auth - roles admin player - User Role ... Player 403 ... Scalar Bearer ...` |
| **Ağustos React**    | Vite · login · characters · register · router · card CRUD · layout · api.ts  |
| **20 Ağu RBAC**      | `auth - rbac role permission domain - Role Permission UserRole RolePermission EF composite PK DbSet ...` |
| **22 Ağu HasPermission** | `auth - rbac HasPermission DB - policy handler Characters CUD IUserPermissionService me later` |
| **24 Ağu /me**       | `auth - me permissions register UserRoles - GET auth/me codes Register writes Player UserRole` |
| **24 Ağu FE gizle**  | `react - hasPermission hide crud links - /me permissions Karakter ekle Duzenle Sil` |
| **25 Ağu Create kapı** | `react - create page permission guard - /me meLoaded Navigate characters.create` |
| **26 Ağu Edit+Context** | `react - edit guard PermissionContext - AppLayout me usePermissions list only` |
| **27 Ağu Context sayfalar** | `react - create edit detail usePermissions - page me removed layout context` |
| **28 Ağu Refresh tablo** | `auth - refresh token table - RefreshToken entity EF AddRefreshTokens` |
| **22 Eki Auth UI** | `login and register pages` |
| **26 Eki User profil** | `users - profil ve parola yalnız token'daki kullanıcıya yazılır` |


---



## Proje Dosya Ağacı (güncel)

```
D:\ReactBattleArena\
├── CHECKPOINT.md
├── PROJE_EKLEMELERI.md
├── PROJE_MANTIGI.md
├── REACT-OGRENIM.md
├── web\                          ← React + Vite + TypeScript
│   └── src\
│       ├── api.ts                ← apiFetch + token + refresh + logout
│       ├── App.tsx / AppLayout.tsx
│       ├── AuthLayout + AuthForm  ← giriş/kayıt kartı (22 Eki)
│       ├── LoginPage / RegisterPage
│       ├── shell\                 ← Sidebar (USERS menü), Header
│       ├── CharactersPage / CharacterCard / Create / Detail / Edit
│       ├── UsersPage (+ module scss) / UserDetailPage (stub)
│       ├── MyProfilePage / EditProfilePage / ChangePasswordPage (stub)
│       └── index.css              ← kabuk dışı token’lar
└── ReactBattleArena\
    ├── Domain\
    │   ├── Characters\Character.cs
    │   ├── Authorization\Role.cs, Permission.cs, UserRole.cs, RolePermission.cs
    │   └── Users\User.cs (+ profil alanları, IsContactPublic; Role kolonu düştü)
    ├── Application\
    │   ├── Abstractions\... (DbSet: Roles / Permissions / UserRoles / RolePermissions)
    │   ├── Authentication\Commands\ (Register*, Login*)
    │   ├── Characters\ (Commands + Queries)
    │   └── Users\ (Commands + Queries)
    ├── Infrastructure\
    │   ├── Security\ (BCrypt, JwtTokenService)
    │   ├── Persistence\ (+ Role/Permission/UserRole/RolePermission Configuration)
    │   └── Migrations\
    └── Api\
        ├── Contracts\
        ├── Controllers\ (Characters, Users, Auth)
        └── Program.cs (JWT + CORS 5173)
```

