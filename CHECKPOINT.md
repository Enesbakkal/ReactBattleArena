# Geliştirme Checkpoint

Son güncelleme: 5 Ağustos 2026 — Karakter detay `/characters/:id` eklendi. Sonraki: edit → delete.

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
- Takılan **JSX/React syntax** oturumda kısa açıklanır (`REACT-OGRENIM.md` → JSX syntax).

## Öğrenme kuralları (HER OTURUM)

- **Cursor:** sadece soru / yönlendirme / kontrol. Agent tüm önyüzü tek seferde yazmasın.
- **VS Code:** kullanıcı React/TS kodunu burada yazar ve çalıştırır.
- Pedagoji (.NET’teki gibi): kısa kavram → kullanıcı yazar → C# / web eşlemesi → çalıştır → sonraki parça.
- Bir oturumda en fazla **1–2 yeni kavram**.
- Kopyala-yapıştır yığını yok; adım adım yaptır.
- Takılınca ASP.NET / HTML / HttpClient karşılığından anlat (WinForms değil).
- Syntax soruları normal; faz atlamadan açıkla.

Plan: React fazları 0→5 tamam. Ürün UI/CRUD devam (Faz 6+).

---

## Şu an neredeyiz?

- [x] Backend Adım 1–15
- [x] **Faz 0–5** React öğrenme
- [x] **Faz 6 — kart grid + Create ayrımı**
- [x] **Karakter detay** `/characters/:id` (`useParams`, kart Link, DetailPage)
  - Notlar: `REACT-OGRENIM.md` (detay + `&&` koşullu render)
- [ ] Characters CRUD: edit `/characters/:id/edit` → delete

## Karar notları (UI / Grid)

- Mega Grid yok — sayfaya özel grid + `CharacterCard`.
- Liste / create / detay ayrı route.
- Resim upload erken — Image URL string yeterli.

## Backend not

İlk Admin: SSMS’te `Role = Admin` + yeniden login. Create 401 → token yenile; 403 → Admin değil; Rarity 1–5; ImageUrl max 500.

## Yeni thread açılış cümlesi (kopyala)

```
ReactBattleArena — CHECKPOINT.md, PROJE_MANTIGI.md ve REACT-OGRENIM.md oku.
Cursor sadece yönlendirme; kod VS Code’da. Characters CRUD: edit sayfasından devam.
```
