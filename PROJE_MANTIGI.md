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

## Geliştirme Fazları

### Faz 1 — Temel CRUD
- User entity ve CRUD
- Character entity ve CRUD
- Basit React arayüzü

### Faz 2 — Kimlik Doğrulama
- Authentication (kayıt / giriş)
- Authorization (rol ve yetki kontrolü)

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
