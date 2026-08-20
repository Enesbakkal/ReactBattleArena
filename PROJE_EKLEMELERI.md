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

Yapılış sırası (bu adım backend; FE henüz yok):

- [x] Karar: refresh token yok (ayrı faz — oturum süresi ≠ yetki modeli)
- [x] Kavram: Role vs Permission, many-to-many, UI gizleme ≠ API (`REACT-OGRENIM.md`)
- [x] Domain `Role` + `Permission` — çanta adı / fiil kodu; `Create` factory (`User` kalıbı)
- [x] Domain `UserRole` + `RolePermission` — ara tablo, kendi `Id` yok; satır = FK çifti
- [x] EF `RoleConfiguration` / `PermissionConfiguration` — unique `Name` / `Code`
- [x] EF `UserRoleConfiguration` / `RolePermissionConfiguration` — composite PK; User silinince UserRoles Cascade; Role/Permission Restrict
- [x] `IApplicationDbContext` + `ApplicationDbContext` DbSet’ler (`Roles`, `Permissions`, `UserRoles`, `RolePermissions`)
- [x] `dotnet build` OK
- [ ] Migration (tablolar henüz SQL’de yok)
- [ ] Seed (Admin / Player / ShopOwner + permission bağları) + mevcut `User.Role` string taşıma
- [ ] JWT permission claim + `/me`
- [ ] Characters CUD `HasPermission`
- [ ] React `can()` + buton gizleme




### Güvenlik & Git

- [x] `Microsoft.OpenApi` güvenlik güncellemesi
- [x] `.gitignore`
- [x] GitHub push (önceki commitler)
- [ ] İsteğe bağlı: güncel FE commit push (kullanıcı kendi commit’liyor)



### Sıradaki

- [ ] Liste yükleme 3–4 sn gecikmesi (inceleme)
- [ ] Login/Register UI’yi kilit palete boyama
- [ ] Battle Arena backend
- [ ] (İleride) Docker / Kubernetes / CI
- [ ] Adım 29 RBAC devam: **migration + seed** → JWT/`me` → HasPermission → FE `can()`



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
- `Users.Role` string ve JWT hâlâ eski model; migration/seed yok
- Not: `REACT-OGRENIM.md` (role vs permission, FK kimde)

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
- [ ] React: kullanıcı yönetimi ekranları (Admin Users UI — henüz yok; Register/Login var)

---



## Faz 4 — Authentication & Authorization

- [x] Register endpoint (`POST /api/auth/register`)
- [x] Password hashing (`IPasswordHasher` / BCrypt)
- [x] Login endpoint (`POST /api/auth/login`)
- [x] JWT token üretimi + JwtBearer
- [x] `[Authorize]` korumalı endpoint’ler
- [x] Rol tabanlı yetkilendirme (string Role: Admin/Player)
- [x] React login/register + router + token (`api.ts`) — Adım 17, 19, 21, 28
- [ ] Rol tabloları (`Roles` / `UserRoles`) — Adım 29 başladı (entity + EF; migration yok)
- [ ] FE’de Admin linklerini role’e göre gizleme (şimdi link görünür; API 403)

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
│       ├── api.ts                ← apiFetch + token
│       ├── App.tsx / AppLayout.tsx
│       ├── LoginPage / RegisterPage
│       ├── CharactersPage / CharacterCard
│       ├── CharacterCreatePage / CharacterDetailPage / CharacterEditPage
│       └── *.css (palet kilidi)
└── ReactBattleArena\
    ├── Domain\
    │   ├── Characters\Character.cs
    │   ├── Authorization\Roles.cs (eski string sabitler — hâlâ JWT)
    │   ├── Authorization\Role.cs, Permission.cs, UserRole.cs, RolePermission.cs
    │   └── Users\User.cs (+ PasswordHash, Role string kolonu — henüz taşınmadı)
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

