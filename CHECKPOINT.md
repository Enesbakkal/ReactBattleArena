# Geliştirme Checkpoint

Son güncelleme: 18 Eylül 2026 — Auth kod tarafı + eski string rol temizliği bitti (`DropUserRoleColumn` uygulandı). Cookie kararı danışmanda. Permission hâlâ DB. Sıradaki: Battle Arena backend.

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
  - [x] RefreshToken Domain + EF + migration `AddRefreshTokens`
  - [x] Login cevabına refresh (hash DB, ham JSON)
  - [x] `POST /api/auth/refresh` + rotation (14–15 Eyl) — `Hash` ortak SHA256, `RefreshCommand` + validator + handler, `RefreshRequest`, `[AllowAnonymous]` action
  - [x] React: 401’de sessiz yenileme (`api.ts`) — `refreshSession` + `refreshInFlight`, yeni Bearer ile tek tekrar, 403 dokunulmaz
  - [x] `POST /api/auth/logout` (16 Eyl) — `LogoutCommand` + validator + handler, `LogoutRequest`, 204; `Revoke`’un ikinci kullanımı, süre kontrolü yok (çıkışta gereksiz)
  - [x] React Çıkış (16 Eyl) — `api.ts` `logout()` düz `fetch` + `clearToken`; `AppLayout` `handleLogout` `async`
  - [x] `/me` 401 kapısı (16 Eyl) — `loadMe` 401’de `clearToken` + `navigate('/login')`; önce yetkisiz sayfada kalıyordu
  - [x] `ExpireMinutes` 60’a geri alındı
  - [x] Temizlik: `RefreshCommand` namespace’i `Authentication.Commands`, 4 gereksiz `using` silindi
  - [x] Reuse detection (17 Eyl) — `RefreshCommandHandler`: iptal edilmiş token gelirse o kullanıcının `RevokedAtUtc == null` satırlarının hepsi iptal, dönüş yine 401. Test A/B ile doğrulandı
  - [ ] `LogoutCommandHandler`’daki kullanılmayan `Domain.Authentication` using’i (küçük)
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
- **Refresh saklama kararı (14 Eyl):** Şimdilik `localStorage` **kalıyor**; cookie’ye geçilmiyor. Nihai karar projenin danışmanına (Abi) sorulacak — cevap gelmeden mimari değiştirilmeyecek. Not 34 bu hâliyle geçerli: refresh ham token istek gövdesinde, `localStorage`’da saklanıyor.
- **Cookie’ye geçilirse yapılacaklar (hazır liste, bugün yapılmıyor):** `HttpOnly` + `Secure` + `SameSite=Strict` + `Path=/api/auth/refresh` cookie; access token `localStorage` değil **bellekte** (açılışta bir kez refresh); `Program.cs` CORS’a `AllowCredentials()`; şema farkı (`http:5173` → `https:7275`) cross-site sayıldığı için Vite proxy veya aynı alan adı; `credentials: 'include'`. Gerekçe: XSS’te 7 günlük refresh sızarsa oturum süresiz kaçırılır; `HttpOnly` cookie JS’e okunmaz.
- **Refresh’in cookie’den bağımsız eksikleri:** `RefreshToken.Revoke` metodu var ama **hiçbir yer çağırmıyor** → rotation yok; gerçek `POST /api/auth/logout` yok (Çıkış sadece `localStorage` siliyor, DB satırı 7 gün geçerli); iptal edilmiş token tekrar kullanılırsa kullanıcının tüm satırlarını geçersiz kılma yok.
- **Rotation çalışıyor (15 Eyl):** Yukarıdaki maddenin ilk kısmı kapandı — `RefreshCommandHandler` `Revoke` çağırıyor, aynı `SaveChanges` eski satırı iptal edip yeni satırı basıyor. Aynı ham token ikinci kez gelirse 401. Logout revoke ve reuse detection **hâlâ açık**.
- **Benzetme kuralı (16 Eyl):** Notlarda ve chat’te takma ad yok. Terim doğrudan yazılır; benzetme şartsa her kullanımda terimle birlikte: “refresh token (fiş)”. Tek başına “fiş / çanta / kâğıt” yazmak yasak — eski notlarda “fiş” = refresh token, “çanta” = rol, “kâğıt” = permission dizisi. Kural dosyası: `.cursor/rules/ogrenim-yazim.mdc` madde 2a.
- **Eski rol modeli (18 Eyl, kapandı):** `Users.Role` kolonu, JWT rol claim’i ve `[Authorize(Roles = Admin)]` kalktı. Delete artık `users.delete` izni (`HasPermission`); seed yalnız Admin. Migration `DropUserRoleColumn` veritabanına uygulandı.
- **Belge (17 Eyl):** `PROJE_MANTIGI.md`'de yetki/oturum modeli hiç yazılmamıştı (eskimiş değil, eksikti). "Yetkilendirme Modeli" ve "Oturum Modeli" bölümleri eklendi: RBAC + izin kodu, izinlerin JWT'ye gömülmemesi, refresh/rotation/reuse/logout, `localStorage` kararı.
- **Süre vs iptal (17 Eyl):** Süresi dolan refresh token kendi kendine iptal olmaz; arka planda iş yok. `RefreshExpireDays` yalnız `ExpiresAtUtc`’yi hesaplar, ret istek anında `ExpiresAtUtc <= utcNow` ile olur. “İptal edilmiş” ve “süresi dolmuş” ayrı sebepler, ikisi de 401. Çalınan token en fazla 7 gün (rotation varsa genelde çok daha az) işe yarar. Eski satırlar birikiyor → ileride temizlik işi.
- **Tuple deconstruction (17 Eyl):** `var (rawRefresh, newHash, expires) = _refreshTokens.Create(utcNow);` tek değerin üç parçasını dağıtır. Eşleşme **sırayla**, isimle değil: sıra karışırsa ham token `TokenHash` kolonuna yazılır ve hash kullanıcıya gider — derlenen, sessiz güvenlik hatası.
- **Rotation (tanım):** Her yenilemede kullanılan refresh token’ın satırı iptal edilir ve yeni bir refresh token verilir → refresh token tek kullanımlıktır. Kazanç: sızan token’ın ömrü kısalır, ve ikinci kullanım **anormal** olduğu için tespit edilebilir.
- **Reuse detection (17 Eyl):** İptal edilmiş refresh token tekrar gelirse hırsızlık varsayılır; o kullanıcının tüm aktif satırları iptal edilir, istek yine 401. `return null` `if` içine alınmamalı — aktif satır olmasa da 401 dönmeli. `activeTokens.Count > 0` kontrolü isteğe bağlı: EF değişiklik yoksa `SaveChanges`’te DB’ye hiç gitmez (önceki “DB turu” açıklaması yanlıştı). Yanlış alarmı `api.ts` `refreshInFlight` engelliyor.
- **Logout (16 Eyl):** Çıkış artık sunucuda da gerçek; `RefreshTokens` satırı iptal edilir. `[AllowAnonymous]` (access ölmüş olabilir), dönüş 204, handler `false` dönse de 204 — token’ın varlığı sızmaz. `logout()` düz `fetch` (apiFetch olsa çıkışta yenileme denerdi); `clearToken` `try/catch` dışında.
- **Bugünün hatası (16 Eyl):** `AppLayout`’a `logout` import edildi ama `api.ts`’e fonksiyon eklenmemişti → sayfa açılmadı (“does not provide an export named 'logout'” = dosya var, isim yok).
- **Refresh akışı (15 Eyl, ezber):** Ölü access → JwtBearer 401 → `refreshSession` → `POST /api/auth/refresh` `{ refreshToken }` → handler hash + rotation → 200 yeni çift → aynı istek **yeni** Bearer ile bir kez tekrar. Refresh düz `fetch` (yoksa döngü); `refreshInFlight` paralel 401’lerde tek yenileme; 403’e dokunulmaz (yetki, süre değil); fiş de dolmuşsa refresh 401 → `clearToken` → sayfa gerçek 401 görür.
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
