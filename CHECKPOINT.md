# Geliştirme Checkpoint

Son güncelleme: 4 Ağustos 2026 — Faz 6: Characters kart grid + Create ayrı sayfa (`/characters/new`). Sonraki: detay → edit → delete.

## Yeni chat’e geçerken oku

1. Bu dosya (`CHECKPOINT.md`)
2. `PROJE_MANTIGI.md` — ürün / mimari özet
3. `PROJE_EKLEMELERI.md` — adım checklist
4. İsteğe bağlı: `.cursor/rules/react-ogrenme.mdc`

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

- Web developer (.NET backend / API biliniyor). **WinForms yok** — eşleme: Razor/MVC View, HTML form, `fetch`/HttpClient.
- React sıfırdan; Cursor tarif eder, kod VS Code’da yazılır.

## Öğrenme kuralları (HER OTURUM)

- **Cursor:** sadece soru / yönlendirme / kontrol. Agent tüm önyüzü tek seferde yazmasın.
- **VS Code:** kullanıcı React/TS kodunu burada yazar ve çalıştırır.
- Pedagoji (.NET’teki gibi): kısa kavram → kullanıcı yazar → C# / web eşlemesi → çalıştır → sonraki parça.
- Bir oturumda en fazla **1–2 yeni kavram**.
- Kopyala-yapıştır yığını yok; adım adım yaptır.
- Takılınca ASP.NET / HTML / HttpClient karşılığından anlat (WinForms değil).

Plan: React fazları 0→5 tamam. Ürün UI/CRUD devam (Faz 6+).

---

## Şu an neredeyiz?

- [x] Backend Adım 1–15
- [x] **Faz 0–5** React öğrenme (Login → Characters → Register/Admin create → Router/Logout)
- [x] **Faz 6 — Characters kart + CSS grid + Create ayrımı**
  - Mega Grid yok; `CharacterCard` + sayfa CSS grid
  - `/characters` liste; `/characters/new` Admin form + önizleme grid
  - Grid mantığı + CRUD planı: `REACT-OGRENIM.md`
- [ ] Characters CRUD devam: detay `/characters/:id` → edit → delete

## Karar notları (UI / Grid)

- Mega Grid yok — sayfaya özel grid + `CharacterCard`.
- Liste ve create ayrı route (CRUD best practice).
- Create altındaki mevcut-karakter grid’i kasıtlı önizleme (sık kullanılanlar sonra).

## Backend not

İlk Admin: SSMS’te `Role = Admin` + yeniden login. Create 401 → token yenile; 403 → Admin değil; Rarity 1–5; ImageUrl max 500.

## Yeni thread açılış cümlesi (kopyala)

```
ReactBattleArena — CHECKPOINT.md, PROJE_MANTIGI.md ve REACT-OGRENIM.md oku.
Cursor sadece yönlendirme; kod VS Code’da. Characters CRUD: detay sayfasından devam.
```
