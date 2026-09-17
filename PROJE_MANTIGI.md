# ReactBattleArena — Proje Mantığı

## Genel Konsept

- Anime, film ve çizgi roman karakterlerinden oluşan bir koleksiyon platformu
- Kullanıcılar takımlarına karakter seçer ve başka kullanıcılarla battle arena'da kapışır
- Kazanılan puanlarla sistemde tutulan görseller veya anime figürleri kazanılır
- Referans backend: `D:\BattleArenaAndFigures\BattleArena` mimarisi

## Kullanıcı Akışı

- Kayıt ol / giriş yap
- Karakter kataloğunu incele
- Takımına karakter ekle
- Arena'da rakip bul ve savaş
- Kazanınca puan kazan
- Puanları ödül kataloğunda (resim, figür vb.) harca

## Teknoloji Yığını

### Backend
- .NET 10
- Domain Driven Design (DDD)
- CQRS (MediatR)
- Fluent Validation
- CRUD API

### Frontend
- React

### Altyapı (ileride)
- SQL Server + EF Core
- Git ile versiyonlama (GitHub'a kod hazır olunca gönderilecek)

## Mimari Yaklaşım

- Katmanlı yapı: Domain → Application → Infrastructure → Api
- Her özellik için Commands / Queries ayrımı
- Controller'lar ince tutulur; iş mantığı handler'larda
- BattleArena'daki Character CRUD yapısı şablon olarak kullanılır

## Yetkilendirme Modeli (güncel — 17 Eylül 2026)

Başlangıçta (27–28 Temmuz) yetki tek bir string kolonla tutuluyordu: `Users.Role`
= `"Admin"` / `"Player"`. JWT'ye rol claim'i konuyor, endpoint'ler
`[Authorize(Roles = "Admin")]` ile korunuyordu.

20–24 Ağustos'ta bu model **değişti**. Bugünkü kurgu RBAC (Role-Based Access
Control) + izin kodu:

- Tablolar: `Roles`, `Permissions`, `RolePermissions` (composite PK),
  `UserRoles` (composite PK). Kullanıcının rolü artık `UserRoles` satırıdır.
- Endpoint'ler rol adıyla değil izin koduyla korunur:
  `[HasPermission(PermissionCodes.CharactersCreate)]`. Rol adı controller'da
  yazılı değildir; yeni bir rol izin alacaksa yalnızca `RolePermissions`'a satır
  eklenir, kod değişmez.
- İzinler **JWT'ye gömülmez**. Her istekte `IUserPermissionService` DB join'i
  yapar; yetki değişince kullanıcının yeniden giriş yapması gerekmez.
- Frontend `GET /api/auth/me` ile izin listesini alır, `PermissionContext` ile
  paylaşır. UI'da link gizlemek yetki değildir — API yine 403 döner.

### Eski modelden kalan artıklar (temizlenecek)

- `Users.Role` string kolonu hâlâ duruyor ve Register / CreateUser oraya
  `"Player"` yazıyor. Gerçek kaynak `UserRoles`; bu kolon ikinci bir doğruluk
  kaynağı olduğu için çelişki riski taşır.
- `JwtTokenService` hâlâ `ClaimTypes.Role` claim'i basıyor.
- `UsersController`'daki bir action hâlâ `[Authorize(Roles = Roles.Admin)]`
  kullanıyor; yani eski model tek noktada canlı.
- `AuthSeeder` içindeki `Users.Role` → `UserRoles` aktarımı tek seferlik taşıma
  işiydi, artık her açılışta boşa çalışıyor.

Temizlik sırası önemlidir: önce `UsersController` izin koduna geçmeli, sonra JWT
claim'i kaldırılmalı, en sonda kolon migration ile düşürülmeli. Ters sırada o
endpoint korumasız kalır.

## Oturum Modeli (güncel — 17 Eylül 2026)

- Access token: JWT, 60 dakika, sunucuda saklanmaz (imza ile doğrulanır).
- Refresh token: `RefreshTokens` tablosunda **hash'i** (SHA256) tutulur, ham
  değer yalnızca cevapta bir kez gider; ömür 7 gün.
- Rotation: her yenilemede kullanılan satır iptal edilir, yeni token verilir →
  refresh token tek kullanımlıktır.
- Reuse detection: iptal edilmiş bir refresh token tekrar gelirse hırsızlık
  varsayılır ve o kullanıcının tüm aktif satırları iptal edilir.
- Logout: `POST /api/auth/logout` satırı iptal eder; tarayıcı temizliği ayrıca
  yapılır.
- Saklama yeri şimdilik `localStorage`. `HttpOnly` cookie'ye geçiş kararı
  danışman görüşü bekliyor (gerekçeler `CHECKPOINT.md`'de).

## Geliştirme Fazları

### Faz 1 — Temel CRUD
- User entity ve CRUD
- Character entity ve CRUD
- Basit React arayüzü

### Faz 2 — Kimlik Doğrulama
- Authentication (kayıt / giriş) — **tamam**
- Authorization (rol ve yetki kontrolü) — **tamam**, RBAC + izin kodu
  (ayrıntı: "Yetkilendirme Modeli" bölümü)
- Oturum sürekliliği (refresh + rotation + logout) — **tamam**
  (ayrıntı: "Oturum Modeli" bölümü)

### Faz 3 — Battle Arena
- Takım oluşturma
- Eşleşme ve savaş mantığı
- Puan sistemi

### Faz 4 — Ödül Sistemi
- Puan ile ödül kazanma
- Görsel ve figür kataloğu
- Kullanıcı envanteri

## Proje Dizini

- `D:\ReactBattleArena`

## Notlar

- Kodlamaya başlamadan önce .NET 10 kurulumu yapılacak
- GitHub repo ayarı kod tamamlandıktan sonra yapılacak
- İlk hedef: basit ve çalışan User + Character CRUD + Auth
