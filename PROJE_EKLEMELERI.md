# ReactBattleArena — Proje Eklemeleri

Bu dosya projeye eklenen özellikleri ve tamamlanan adımları takip eder.

> **Not:** **Tüm Tamamlananlar** listesi silinmez, sadece genişletilir.

---

## Tüm Tamamlananlar (Adım 1 → 14.3)

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
- [ ] 14.4 `[Authorize]` korumalı endpoint’ler

### Güvenlik & Git
- [x] `Microsoft.OpenApi` güvenlik güncellemesi
- [x] `.gitignore`
- [x] GitHub push (önceki commitler)
- [ ] Bugünkü login/JWT commit push (bekliyor)

### Sıradaki
- [ ] **Adım 14.4** — `[Authorize]` + Bearer token testi

### Hafta sonu notu
- [x] Kod akışı, validator/middleware, record, paging, IRequest açıklamaları yazıldı
- Detay: `CHECKPOINT.md` + sohbet özeti (12 Temmuz)

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
- Sırada: [Authorize] korumalı endpoint’ler

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
- [ ] Docker Compose (opsiyonel)
- [ ] React frontend

---

## Faz 2 — Character Modülü

- [x] Entity + full CRUD (Create/Read/Update/Delete)
- [x] Validator + API controller + Scalar
- [ ] React: karakter listesi ve form

---

## Faz 3 — User Modülü

- [x] User domain entity
- [x] User CRUD (Application: command + query)
- [x] FluentValidation kuralları
- [x] Migration `AddUsers`
- [x] Users API controller
- [x] PasswordHash (+ CreateUser password)
- [ ] React: kullanıcı yönetimi ekranları

---

## Faz 4 — Authentication & Authorization

- [x] Register endpoint (`POST /api/auth/register`)
- [x] Password hashing (`IPasswordHasher` / BCrypt)
- [x] Login endpoint (`POST /api/auth/login`)
- [x] JWT token üretimi + JwtBearer
- [ ] `[Authorize]` korumalı endpoint’ler
- [ ] Rol tabanlı yetkilendirme
- [ ] React login/register + korumalı route’lar

---

## Faz 5 — Battle Arena (ileride)

- [ ] Takım, eşleşme, savaş, puan

---

## Faz 6 — Ödül Sistemi (ileride)

- [ ] Ödül kataloğu, puan ile alma, envanter

---

## Commit mesajları (özet)

| Konu | Mesaj (kısa) |
|------|----------------|
| İlk foundation | `migration - InitialCreate - solution CQRS Character ... veritabani` |
| DI | `application - AddApplication - MediatR FluentValidation DI pipeline ...` |
| API + Scalar | `api - CharactersController - ... Scalar UI ...` |
| Validation 400 | `api - validation middleware - ... 400 ValidationProblemDetails ...` |
| Character GET | `character - query ve controller - GetCharacters... GET endpointleri` |
| Character PUT/DELETE | `character - update delete crud - ...` |
| **16 Temmuz** | `auth - register password - ...` |
| **22 Temmuz** | `auth - login jwt - IJwtTokenService JwtTokenService LoginCommand AuthController POST login JwtBearer UseAuthentication` |


---

## Proje Dosya Ağacı (güncel)

```
D:\ReactBattleArena\
├── CHECKPOINT.md
├── PROJE_EKLEMELERI.md
├── PROJE_MANTIGI.md
└── ReactBattleArena\
    ├── Domain\
    │   ├── Characters\Character.cs
    │   └── Users\User.cs (+ PasswordHash)
    ├── Application\
    │   ├── Abstractions\IApplicationDbContext.cs, IPasswordHasher.cs
    │   ├── Authentication\Commands\ (Register*)
    │   ├── Characters\ (Commands + Queries)
    │   └── Users\ (Commands + Queries)
    ├── Infrastructure\
    │   ├── Security\BCryptPasswordHasher.cs
    │   ├── Persistence\ (+ UserConfiguration PasswordHash)
    │   └── Migrations\ (...AddUsers, ...AddUserPasswordHash)
    └── Api\
        ├── Contracts\ (CreateCharacter, CreateUser+Password, Register)
        └── Controllers\ (Characters, Users, Auth)
```
