# Geliştirme Checkpoint

Son güncelleme: 28 Temmuz 2026 — Adım 15 roller tamam (Admin/Player test edildi).

## Tamamlananlar

- [x] Adım 1–14 — Solution, Character/User CRUD, Auth Register/Login JWT, `[Authorize]`, Scalar Bearer
- [x] Adım 14.4 — Scalar OpenAPI Bearer kilidi (`BearerSecuritySchemeTransformer`)
- [x] Adım 15 — Authorization (string Role)
  - `Domain/Authorization/Roles.cs` (Admin, Player)
  - `User.Role` + `Create(..., role)` + `SetRole`
  - Register/CreateUser → `Roles.Player`
  - Migration `AddUserRole` (default Player)
  - JWT `ClaimTypes.Role`
  - Character CUD → `[Authorize(Roles = Admin)]`
  - User Delete → Admin
  - Test: Admin create **201**, Player create **403**
  - `CreateCharacterRequest` Password alanı silindi
  - IdentityModel paket sürümleri hizalandı (IDX00001)

## Sıradaki (seçim)

- [ ] React frontend (login, token, karakter listesi)
- [ ] Rol tabloları (`Roles` / `UserRoles`) — ileride
- [ ] Battle Arena (takım, savaş, puan)

## Not

İlk Admin SSMS ile `Role = Admin` + yeniden login.
Rol tablolarına geçiş mimariyi yıkmaz; Auth/User katmanını genişletir.

## Bugünkü commit mesajı

```
auth - roles admin player - User Role Roles constants Register CreateUser Player JWT role claim Character CUD Admin authorize Player 403 test Scalar Bearer OpenApi transformer CreateCharacterRequest password removed IdentityModel version align
```
