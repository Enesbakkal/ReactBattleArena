# Geliştirme Checkpoint

Son güncelleme: 16 Temmuz 2026 — Adım 14.2 Register tamam, JWT sırada.

## Tamamlananlar

- [x] Adım 1–12 — Solution, Character full CRUD, MediatR, FluentValidation, Scalar, validation middleware
- [x] Adım 13 — User modülü
  - Domain `User`, Application CRUD, `AddUsers` migration
  - `UsersController` + `CreateUserRequest`
- [x] Adım 14.1 — `PasswordHash` (Domain + UserConfiguration + `AddUserPasswordHash` migration)
- [x] Adım 14.2 — Authentication Register
  - `IPasswordHasher` + `BCryptPasswordHasher`
  - `RegisterCommand` / Validator / Handler (`Authentication` klasörü)
  - `AuthController` → `POST /api/auth/register`
  - `RegisterRequest`
  - CreateUser handler `IPasswordHasher` kullanacak şekilde güncellendi
  - Duplicate UserName/Email → `ValidationFailure` ile düzgün 400

## Sıradaki

- [ ] **Adım 14.3** — Login + JWT
- [ ] Adım 14.4 — `[Authorize]` korumalı endpoint’ler
- [ ] Authorization (roller / policy) — ileride

## Not

Register Scalar’da test edildi (başarılı kayıt, kısa şifre 400, duplicate userName 400).
JWT henüz yok.

## Commit mesaj geçmişi (referans)

```
migration - InitialCreate - solution CQRS Character ...
application - AddApplication - MediatR FluentValidation DI ...
api - CharactersController - POST + Scalar ...
api - validation middleware - 400 ValidationProblemDetails ...
character - query ve controller - GET endpoints ...
character - update delete crud - PUT DELETE POST restore ...
```

### Bugünkü commit için mesaj

```
auth - register password - User PasswordHash AddUserPasswordHash migration IPasswordHasher BCryptPasswordHasher RegisterCommand AuthController POST register UsersController CreateUser password hash duplicate username email ValidationFailure 400
```

## Hafta sonu — kod akışı tekrarı

- [x] Akış, validator/middleware, record, paging, IRequest notları yazıldı
