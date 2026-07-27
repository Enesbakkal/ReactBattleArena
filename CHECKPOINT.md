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

- [ ] **Adım 14.4 devam** — Scalar’da Bearer kilit (yarın)
- [ ] Authorization (roller / policy) — ileride
- [ ] React frontend

## Not (yarın — Scalar kilit)

Kilit ikonu yok çünkü OpenAPI’de Bearer şeması tanımlı değil. Yapılacaklar:

1. `Program.cs` — `AddOpenApi` içine Bearer `DocumentTransformer` ekle
2. `MapScalarApiReference(options => options.AddPreferredSecuritySchemes("Bearer"))`
3. Rebuild → Scalar’da Authentication / Bearer görünmeli

Şimdilik manuel header: `Authorization: Bearer <token>`

## Not

Login başarılı. Character POST test için geçerli JSON kullan (password alanı Character’da yok).

## Commit mesaj geçmişi (özet)

```
... önceki character/user/register commitleri ...
```

### Bugünkü commit için mesaj

```
auth - login jwt - IJwtTokenService JwtTokenService JwtOptions LoginCommand AuthController POST login JwtBearer Program UseAuthentication LoginResult token test edildi
```
