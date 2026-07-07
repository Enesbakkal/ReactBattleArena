# Geliştirme Checkpoint

Son güncelleme: 7 Temmuz 2026 — Adım 9 tamamlandı.

## Tamamlananlar

- [x] Adım 1 — Solution + 4 proje
- [x] Adım 2 — Proje referansları
- [x] Adım 3 — `Character.cs` (Domain)
- [x] Adım 4 — `CreateCharacterCommand.cs` + MediatR
- [x] Adım 5 — `IApplicationDbContext` + `CreateCharacterCommandHandler`
- [x] Adım 6 — `CreateCharacterCommandValidator` (FluentValidation)
- [x] Adım 7 — Infrastructure + EF Core + Migration
- [x] Adım 8 — `AddApplication` (MediatR + FluentValidation DI)
- [x] Adım 9 — `CharactersController` (POST create) + Scalar UI
  - `Contracts/CreateCharacterRequest`, `CharactersController`
  - Scalar.AspNetCore eklendi (`/scalar`), `launchSettings.json` auto-open
  - WeatherForecast şablon dosyaları silindi

## Sıradaki

- [ ] **Adım 10** — Validation 400 middleware (ValidationException → 400)
- [ ] Adım 11 — Character query'leri (GetById, GetList) + controller GET
- [ ] Adım 12 — Update / Delete command'ları
- [ ] Adım 13 — User modülü
- [ ] Adım 14 — Authentication & Authorization

## Not

Devam: Adım 10'dan (validation 400 middleware).
