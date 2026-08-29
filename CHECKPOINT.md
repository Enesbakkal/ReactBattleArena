# Geliştirme Checkpoint

Son güncelleme: 28 Ağustos 2026 — `RefreshTokens` tablosu var (entity + EF + `AddRefreshTokens`). Login henüz refresh yazmıyor. Permission hâlâ DB.

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
  - [x] Register `UserRoles` + `GET /api/auth/me` permissions
  - [x] React `hasPermission` — liste Ekle, detay Düzenle/Sil (`/me` şart)
  - [x] Create URL kapısı — `meLoaded` + `/me` + `Navigate` (`CharacterCreatePage`)
  - [x] Edit URL kapısı — `characters.update` (`CharacterEditPage`); Sanji listeye düşer
  - [x] `PermissionContext` + AppLayout `/me`; liste + Create/Edit/Detail `usePermissions`
  - [x] RefreshToken Domain + EF + migration `AddRefreshTokens` (tablo boş; login yazmıyor)
  - [ ] Login cevabına refresh + `POST /api/auth/refresh`
  - [ ] React: 401’de sessiz yenileme (`api.ts`)
- [ ] Battle Arena backend

## Karar notları

- Mega Grid yok; `CharacterCard` + sayfa CSS grid.
- **Renk KİLİT:** `#1e1a24` / `#2e2838` / `#b39bc9`.
- **HTTP:** sayfalarda doğrudan `localhost` + `fetch` yok; sadece `api.ts`.
- Player CUD: permission yoksa 403; `RolePermissions` + `UserRoles` varsa 201 (yeniden login gerekmez).
- Scalar’da karakter JSON’u Register’a gitmesin — UserName/Email/Password 400’ü odur.
- Firefox CORS + status null → çoğu zaman Api kapalı / sertifika.
- Pedagoji: açıklamalar daha uzun (11 Ağustos+).
- UI gizleme ≠ yetki: link yok olsa da `/characters/new` API’de 403.
- Detayda `/me` yoksa `permissions` boş kalır; Admin’de de Düzenle/Sil görünmez.
- Create kapısı `useEffect`’ten **sonra**; `meLoaded` false iken erken `return` effect’i keser → sonsuz Yükleniyor.
- `me.permissions` (çoğul). `me.permission` → hep `[]` → herkes listeye atılır.
- Player `RolePermissions`’ta `characters.create` varsa **tüm** Player’lar new’de kalır (Sanji’ye özel satır yok). Seed default değil.
- Katalog `POST /characters` ≠ oyuncunun takımında karakter olması. New kapısı katalog yazma fiili.
- Edit URL `:id` **Guid**. `/characters/Franky/edit` 404; isim değil.
- `usePermissions` = diziyi Context’ten oku. `hasPermission` = dizide kod var mı. İkisi aynı şey değil.
- Layout unmount olmaz (`/characters` ↔ `/new`); listeye dönüşte layout `/me` tekrar atmaz.
- Dev’de Strict Mode `useEffect`’i iki kez çalıştırır → ilk açılışta 2× `me` normal. Production’da 1.
- **Konuşulacak (unutma):** refresh cookie vs `localStorage` — şimdilik tablo + hash; ham token DB’de yok.
- **`REACT-OGRENIM` düzeltme yöntemi (26 Ağu kilit):** Tüm dosyayı hikâyeleştir / “toparla” **isteme**. Bir başlık seç; eski metne dokunma; **alta** chat gibi okuma hali ekle. İlk parça hâlâ 26 Ağustos. Temmuz’a dokunma.
- **Yazım modeli + V2 kararı (29 Ağu):** Anlatım sırası **önce kod bloğu, sonra düz yazı açıklama**; metafor yalnızca görünmeyen mekanizmalar için (Context, ağaç, token akışı, MediatR pipeline, middleware sırası) ve kodda karşılığı gösterildikten sonra. Kurallar: `.cursor/rules/ogrenim-yazim.mdc`. Düzeltilmiş notlar **yeni** `REACT-OGRENIM-V2.md` dosyasına yazılır; eski `REACT-OGRENIM.md` arşiv.
- **V2 kapsam (29 Ağu):** Sadece React değil, **backend + React**. Bölüm sırası `git log` ile doğrulanmış gerçek kronoloji: Blok A backend temeli (2–28 Tem, 9 bölüm) → Blok B React (29 Tem–13 Ağu, 14 bölüm) → Blok C RBAC backend (20–24 Ağu) → Blok D frontend yetki (24–27 Ağu) → Blok E refresh token (28 Ağu→). Toplam 34 bölüm, turda tek bölüm. Backend notları unutulduğu için backend bölümleri React kadar ayrıntılı; “zaten biliyorsun” varsayımı yok. Blok geçişlerinde neden el değiştirdiğimiz yazılacak.

## Backend not

İlk Admin: `UserRoles` ile Admin rolü (Users.Role string tek başına HasPermission’a yetmez). 401 token; 403 permission yok; PUT/DELETE başarı 204.

## Yeni thread açılış cümlesi (kopyala)

```
ReactBattleArena — CHECKPOINT.md, PROJE_MANTIGI.md ve REACT-OGRENIM.md oku.
Cursor yönlendirme; kod VS Code’da. Sıradaki: Login refresh token üretimi (hash DB, ham cevapta). Yetki JWT’de değil. Refresh ≠ permission.
```

## Not düzeltme thread’i açılış cümlesi (kopyala)

```
ReactBattleArena — .cursor/rules/ogrenim-yazim.mdc kuralını uygula (34 bölümlük
backend + React planı orada, git log ile doğrulanmış).
REACT-OGRENIM.md’yi ve ilgili kaynak dosyaları oku, sonra REACT-OGRENIM-V2.md’ye
1. bölümü (02–03 Temmuz: solution + 4 katman + Character CQRS + EF InitialCreate)
yaz ve dur. Eski dosyaya dokunma.
```
