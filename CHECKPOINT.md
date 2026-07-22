# Geliştirme Checkpoint

Son güncelleme: 22 Temmuz 2026 — Adım 14.3 Login + JWT tamam.

## Tamamlananlar

- [x] Adım 1–12 — Solution, Character full CRUD, MediatR, FluentValidation, Scalar, validation middleware
- [x] Adım 13 — User modülü (CRUD + UsersController)
- [x] Adım 14.1 — `PasswordHash` + `AddUserPasswordHash` migration
- [x] Adım 14.2 — Register (`IPasswordHasher`, `RegisterCommand`, `AuthController`)
- [x] Adım 14.3 — Login + JWT
  - `IJwtTokenService` + `JwtTokenService` + `JwtOptions`
  - `LoginCommand` / Validator / Handler → `LoginResult` (token)
  - `LoginRequest` + `AuthController` `POST /api/auth/login`
  - `Program.cs`: JwtBearer + `UseAuthentication` / `UseAuthorization`
  - Scalar login test: 200 + token (zoro)

## Sıradaki

- [ ] **Adım 14.4** — `[Authorize]` korumalı endpoint’ler (Bearer token ile)
- [ ] Authorization (roller / policy) — ileride
- [ ] React frontend

## Not

Login başarılı test edildi. Sonraki adım: token’sız 401, token’lı 200.

## Commit mesaj geçmişi (özet)

```
... önceki character/user/register commitleri ...
```

### Bugünkü commit için mesaj

```
auth - login jwt - IJwtTokenService JwtTokenService JwtOptions LoginCommand AuthController POST login JwtBearer Program UseAuthentication LoginResult token test edildi
```
