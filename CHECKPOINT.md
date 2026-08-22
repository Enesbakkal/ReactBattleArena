# Geliştirme Checkpoint

Son güncelleme: 22 Ağustos 2026 — HasPermission DB’den; Characters CUD Admin string Roles yok. Yarın: `/me` + Register UserRoles + React `can()`.

## Yeni chat’e geçerken oku

1. Bu dosya (`CHECKPOINT.md`)
2. `PROJE_MANTIGI.md` — ürün / mimari özet
3. `PROJE_EKLEMELERI.md` — adım checklist
4. İsteğe bağlı: `.cursor/rules/react-ogrenme.mdc`
5. **Çarşamba görüşme:** `AUTH-REACT-CALISMA.md` (Auth + React dersi)

---

## Proje mantığı (özet)

- Anime/film/comics karakter koleksiyonu + battle arena + puan → ödül
- Backend: .NET 10, DDD, CQRS (MediatR), FluentValidation, EF Core, SQL Server, JWT (Admin/Player)
- Katmanlar: Domain → Application → Infrastructure → Api
- Frontend: React + TypeScript (Vite) — `web/`
- Klasör: `D:\ReactBattleArena`
- Referans: `D:\BattleArenaAndFigures\BattleArena`
- Api: `https://localhost:7275` · Vite: `http://localhost:5173` (CORS hazır)

Detay: `PROJE_MANTIGI.md`

---

## Öğrenci profili

- Web developer (.NET API). WinForms yok — Razor/HTML/`fetch`.
- React sıfırdan; takılan JSX/syntax oturumda açıklanır (`REACT-OGRENIM.md`).

## Öğrenme kuralları (HER OTURUM)

- Cursor: yönlendirme; kod VS Code’da.
- 1–2 kavram / oturum; adım adım; WinForms örneği yok.
- **Açıklamalar:** biraz **daha uzun** (kavram + neden + C#/web eşlemesi). Not: `REACT-OGRENIM.md`.

---

## Şu an neredeyiz?

- [x] Backend Adım 1–15
- [x] React faz 0–5
- [x] Faz 6 kart grid + Create ayrı sayfa
- [x] Characters frontend CRUD
- [x] AppLayout + Outlet
- [x] UI renk KİLİT (koyu mor 60-30-10)
- [x] **API/auth helper** — `web/src/api.ts`
  - `apiFetch` + `getToken` / `setToken` / `clearToken`
  - Login/Register `auth: false`; diğerleri Bearer varsayılan
  - Taşınan: Login, Register, Characters, Create, Detail, Edit, AppLayout
  - Notlar: `REACT-OGRENIM.md` (`apiFetch` öğretimi + helper tamamlandı)
- [ ] Liste yükleme 3–4 sn gecikmesi
- [ ] UI ince ayar (login/register boyama)
- [x] **`PROJE_EKLEMELERI.md` ← React Adım 16–28** (14 Ağu; gerçek sırayla)
- [ ] **Adım 29 RBAC** — Role + Permission (refresh token yok)
  - [x] Domain entity’ler + EF configuration + DbSet (`dotnet build` OK)
  - [x] Migration `AddRbacTables` + `AuthSeeder` (Users.Role → UserRoles)
  - [x] `IUserPermissionService` (join; UserPermission tablosu yok)
  - [x] `HasPermission` Characters CUD; Player’a RolePermission + UserRoles ile 201
  - [ ] `/me` permissions; Register `UserRoles`; React `can()`
- [ ] Battle Arena backend

## Karar notları

- Mega Grid yok; `CharacterCard` + sayfa CSS grid.
- **Renk KİLİT:** `#1e1a24` / `#2e2838` / `#b39bc9`.
- **HTTP:** sayfalarda doğrudan `localhost` + `fetch` yok; sadece `api.ts`.
- Player CUD: permission yoksa 403; `RolePermissions` + `UserRoles` varsa 201 (yeniden login gerekmez).
- Scalar’da karakter JSON’u Register’a gitmesin — UserName/Email/Password 400’ü odur.
- Firefox CORS + status null → çoğu zaman Api kapalı / sertifika.
- Pedagoji: açıklamalar daha uzun (11 Ağustos+).
- RBAC: ara tabloda FK; `UserRoleId` yok — composite `(UserId, RoleId)`.

## Backend not

İlk Admin: `UserRoles` ile Admin rolü (Users.Role string tek başına HasPermission’a yetmez). 401 token; 403 permission yok; PUT/DELETE başarı 204.

## Yeni thread açılış cümlesi (kopyala)

```
ReactBattleArena — CHECKPOINT.md, PROJE_MANTIGI.md ve REACT-OGRENIM.md oku.
Cursor yönlendirme; kod VS Code’da. Sıradaki: GET /api/auth/me permissions, Register UserRoles, React can(). Refresh token yok. Yetki JWT’de değil.
```
