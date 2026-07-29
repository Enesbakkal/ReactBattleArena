# Geliştirme Checkpoint

Son güncelleme: 29 Temmuz 2026 — React öğrenme sıfırdan; Cursor = soru, VS Code = kod.

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
- Frontend: React + TypeScript (Vite) — henüz yok; adım adım kurulacak
- Klasör: `D:\ReactBattleArena` (solution + ileride `web/`)
- Referans: `D:\BattleArenaAndFigures\BattleArena`
- Api: `https://localhost:7275` · Vite gelecek: `http://localhost:5173` (CORS hazır)

Detay: `PROJE_MANTIGI.md`

---

## Öğrenme kuralları (HER OTURUM)

- **Cursor:** sadece soru / yönlendirme / kontrol. Agent tüm önyüzü tek seferde yazmasın.
- **VS Code:** kullanıcı React/TS kodunu burada yazar ve çalıştırır.
- Pedagoji (.NET’teki gibi): kısa kavram → kullanıcı yazar → C# eşlemesi → çalıştır → sonraki parça.
- Bir oturumda en fazla **1–2 yeni kavram**.
- Kopyala-yapıştır yığını yok; adım adım yaptır.
- Takılınca C# karşılığından anlat.

Plan özeti: React öğrenme fazları 0→5 (Login → Characters → Register/Admin → Router).

---

## Şu an neredeyiz?

- [x] Backend Adım 1–15 (Character/User CRUD, Auth JWT, roller, CORS 5173)
- [x] Hatalı “tüm React’i bir anda kurma” geri alındı (`web/` silindi)
- [x] **Faz 0 — Kavramlar** tamamlandı (component, props, state, interface, JSX)
  - Notlar: `REACT-OGRENIM.md`
- [x] **Faz 1 — Vite `web` projesi** kuruldu (`D:\ReactBattleArena\web`, React 19 + Vite 8)
  - ExecutionPolicy CurrentUser RemoteSigned
  - `npm run dev` → http://localhost:5173/ React logosu görüldü
  - Notlar: `REACT-OGRENIM.md` Faz 1
- [ ] Faz 2 — Login
- [ ] Faz 3 — API client + Characters
- [ ] Faz 4 — Register + Admin character create
- [ ] Faz 5 — react-router + Logout

## Backend not

İlk Admin: SSMS’te `Role = Admin` + yeniden login.

## Yeni thread açılış cümlesi (kopyala)

```
ReactBattleArena — CHECKPOINT.md, PROJE_MANTIGI.md ve REACT-OGRENIM.md oku.
Cursor sadece yönlendirme; kod VS Code’da. Faz 2 Login’den devam.
```
