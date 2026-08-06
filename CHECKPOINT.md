# Geliştirme Checkpoint

Son güncelleme: 6 Ağustos 2026 — Characters frontend CRUD tamam (liste/create/detay/edit/delete).

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

- Web developer (.NET API). WinForms yok — Razor/HTML/`fetch`.
- React sıfırdan; takılan JSX/syntax oturumda açıklanır (`REACT-OGRENIM.md`).

## Öğrenme kuralları (HER OTURUM)

- Cursor: yönlendirme; kod VS Code’da.
- 1–2 kavram / oturum; adım adım; WinForms örneği yok.

---

## Şu an neredeyiz?

- [x] Backend Adım 1–15
- [x] React faz 0–5 (Login → Characters Bearer → Register → Router/Logout)
- [x] Faz 6 kart grid + Create ayrı sayfa
- [x] **Characters frontend CRUD tamam**
  - Liste `/characters`
  - Create `/characters/new`
  - Detay `/characters/:id`
  - Edit `/characters/:id/edit`
  - Delete detayda (confirm + DELETE 204)
  - Notlar: `REACT-OGRENIM.md` (useEffect, `&&`, useState iskelet, CRUD)

## Karar notları

- Mega Grid yok; `CharacterCard` + sayfa CSS grid.
- Resim upload erken — Image URL string.
- Sonraki ürün adımı birlikte seçilir (aşağıya bak).

## Backend not

Admin: SSMS `Role = Admin` + yeniden login. 401 token; 403 rol; PUT/DELETE 204 body yok.

## Yeni thread açılış cümlesi (kopyala)

```
ReactBattleArena — CHECKPOINT.md, PROJE_MANTIGI.md ve REACT-OGRENIM.md oku.
Cursor sadece yönlendirme; kod VS Code’da. Characters CRUD bitti; sıradaki adımı seç / devam.
```
