# Geliştirme Checkpoint

Son güncelleme: 31 Temmuz 2026 — Faz 4 tamam (Register + Admin character create). Sonraki: Faz 5.

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
- Frontend: React + TypeScript (Vite) — `web/` adım adım
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

Plan özeti: React öğrenme fazları 0→5 (Login → Characters → Register/Admin → Router).

---

## Şu an neredeyiz?

- [x] Backend Adım 1–15 (Character/User CRUD, Auth JWT, roller, CORS 5173)
- [x] Hatalı “tüm React’i bir anda kurma” geri alındı (`web/` silindi)
- [x] **Faz 0 — Kavramlar** tamamlandı
  - Notlar: `REACT-OGRENIM.md`
- [x] **Faz 1 — Vite `web` projesi** kuruldu
  - Notlar: `REACT-OGRENIM.md` Faz 1
- [x] **Faz 2 — Login** tamam
  - Notlar: `REACT-OGRENIM.md` Faz 2 (+ devam)
- [x] **Faz 3 — Characters + Bearer** tamam
  - Notlar: `REACT-OGRENIM.md` Faz 3
- [x] **Faz 4 — Register + Admin character create** tamam
  - [x] Register: `RegisterPage` + `authView`
  - [x] Admin create: `CharactersPage` form + `POST /api/characters` + Bearer + 403; `load` dışarı
  - Notlar: `REACT-OGRENIM.md` Faz 4 (1/2) + (2/2)
- [ ] Faz 5 — react-router + Logout

## Karar notları (UI)

- Characters grid / ortak Grid component: **şimdilik ertele** (erken abstraction). Faz 5 sonrası kosmetik.

## Backend not

İlk Admin: SSMS’te `Role = Admin` + yeniden login.

## Yeni thread açılış cümlesi (kopyala)

```
ReactBattleArena — CHECKPOINT.md, PROJE_MANTIGI.md ve REACT-OGRENIM.md oku.
Cursor sadece yönlendirme; kod VS Code’da. Faz 5 react-router + Logout’tan devam.
```
