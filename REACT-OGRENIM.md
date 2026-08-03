# React Öğrenim Notları

Her faz bitince buraya **eklenir**. Döndüğünde hızlıca hatırlamak için oku.

**KURAL (asla bozma):** Eski fazlar silinmez, kısaltılmaz, üzerine yazılmaz. Faz 0, 1, 2… hepsi dosyada kalır; yeni faz sadece **alta eklenir**. 1500 faz olsa her birinin açıklaması burada olur.

**Öğrenci:** Web developer (.NET API). WinForms yok. Eşlemeler: Razor/MVC View, HTML form, HttpClient/`fetch`.

---

## Faz 0 — Temel Kavramlar

### Component nedir?

UI döndüren bir fonksiyon. C#'teki UserControl veya küçük bir View gibi düşünebilirsin. Dosya uzantısı `.tsx` olur çünkü içinde JSX (HTML benzeri sözdizimi) yazarsın.

```tsx
function LoginPage() {
  return <h1>Giriş</h1>;
}
```

### Props ile State farkı ne?

**Props:** Componente dışarıdan gelen parametreler. C#'teki method parametresi / constructor parametresi gibi. Component kendi props'unu değiştiremez.

```tsx
function Greeting(props: { name: string }) {
  return <p>Merhaba {props.name}</p>;
}
```

**State:** Component'in kendi tuttuğu veri. Değiştiğinde React o component'i yeniden çizer (UI refresh). C#'teki private field gibi ama değişince ekran otomatik güncellenir.

```tsx
const [count, setCount] = useState(0);
```

### interface C#'te neye benziyor?

C#'teki `interface`'e benzer — bir sözleşmedir. `class` veya `record` gibi somut bir tip değildir. TypeScript `interface` runtime'da nesne üretmez, sadece tip tarif eder.

```tsx
interface CharacterProps {
  name: string;
  rarity: number;
}
```

### JSX HTML midir?

Hayır. HTML'e benzer sözdizimi ama HTML değildir. `.tsx` dosyaları JSX barındırır; React bunları gerçek DOM'a çevirir. Tarayıcı JSX'i doğrudan anlamaz, önce JavaScript'e dönüştürülür.

```tsx
// Bu JSX:
<h1 className="title">Merhaba</h1>

// React bunu şuna çevirir:
React.createElement('h1', { className: 'title' }, 'Merhaba')
```

Not: HTML'de `class` yazarsın, JSX'te `className` yazarsın — çünkü `class` JavaScript'te rezerve kelime.

---

## Faz 1 — Vite + React projesi kurma

### Ne kurduk?

`D:\ReactBattleArena\web` — Vite + React + TypeScript şablonu.

```powershell
cd D:\ReactBattleArena
npm create vite@latest web -- --template react-ts
cd web
npm install
npm run dev
```

- **ESLint** linter olarak seçildi (Oxlint değil).
- Paket yöneticisi: **npm**.

### Vite nedir?

C#'teki `dotnet run` / hot reload’a benzer geliştirme aracı. Dev server + paketleyici (bundler). `npm run dev` → `http://localhost:5173/`.

### `npm run dev` bitmiyor mu?

Bitmemeli. Terminalde “ready” görüp URL yazması = server açık. Dosya kaydedince sayfa yenilenir. Durdurmak: `Ctrl+C`.

### PowerShell Execution Policy

`npm.ps1 is not digitally signed` hatası gelirse:

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

Geçici alternatif: `npm.cmd` kullan (`npm.cmd run dev`).

### npm 12 uyarısı

`New major version of npm available` sadece npm’in kendisi. Proje (Vite/React) güncel kuruldu; şimdilik `npm install -g npm@12` şart değil.

### Önemli dosyalar (şimdilik)

| Dosya | Ne işe yarar |
|-------|----------------|
| `index.html` | Sayfa kabuğu; `#root` buraya React bağlanır |
| `src/main.tsx` | Giriş noktası — React ağacını DOM’a mount eder |
| `src/App.tsx` | İlk component (şablon sayacı / logo) |
| `package.json` | Bağımlılıklar + `dev` / `build` script’leri |
| `vite.config.ts` | Vite ayarları |

### C# eşlemesi

| React / Vite | .NET tarafı |
|--------------|-------------|
| `npm run dev` | `dotnet run` (Api) |
| `localhost:5173` | `localhost:7275` (Api) |
| `web/` klasörü | ayrı frontend process; solution’daki `.csproj` değil |

---

## Faz 2 — Login (controlled form) — devam ediyor

Bu fazın **ilk parçası**: Login formu + controlled input. API (`fetch` + JWT) henüz yok; sonraki parça.

### Kavram: controlled input

Input’un `value`’su state’ten gelir; her tuşta `setState` → React yeniden çizer.

Düz HTML’de değer tarayıcıda durur. React’te değeri **biz state’te tutarız** — Razor’da ViewModel property’sine bağlamak gibi.

### Dosyalar

| Dosya | Ne yaptık |
|-------|-----------|
| `web/src/LoginPage.tsx` | Yeni — form + iki state + submit’te console.log |
| `web/src/App.tsx` | Vite şablonu silindi; sadece `<LoginPage />` |
| `web/src/main.tsx` | Dokunulmadı |

### Adım adım ne koyduk / neden

1. **`LoginPage.tsx` oluştur** — login UI’si ayrı component olsun.
2. **`import { useState } from 'react'`** — state kullanmak için (`using` gibi).
3. **`function LoginPage() { ... }`** — bu sayfanın UI’sini üreten component.
4. **İki `useState('')`:** `userNameOrEmail` / `password` (+ setter’lar). Backend `LoginRequest` alanlarıyla aynı isimler (camelCase). Başlangıç boş string.
5. **`handleSubmit(e: React.FormEvent)`** — form submit olayı.
   - `e.preventDefault()` → tarayıcı sayfayı yenilemesin (SPA).
   - `console.log(...)` → şimdilik API yok; değerlerin geldiğini görmek.
6. **`<form onSubmit={handleSubmit}>`** — submit → handler.
7. **Kullanıcı/email `<input>`** — `value={userNameOrEmail}` + `onChange={(e) => setUserNameOrEmail(e.target.value)}`.
8. **Şifre `<input type="password">`** — aynı mantık; karakterler gizli.
9. **`<button type="submit">Giriş</button>`** — form’un `onSubmit`’ini tetikler.
10. **`export default LoginPage`** — `App.tsx` import edebilsin.
11. **`App.tsx`:** `import LoginPage from './LoginPage'` + `return <LoginPage />` — uygulama açılınca login görünsün.

### `LoginPage.tsx` (bu parçanın sonucu)

```tsx
import { useState } from 'react'

function LoginPage() {
  const [userNameOrEmail, setUserNameOrEmail] = useState('')
  const [password, setPassword] = useState('')

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    console.log(userNameOrEmail, password)
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>
          Kullanıcı adı veya e-posta
          <input
            type="text"
            value={userNameOrEmail}
            onChange={(e) => setUserNameOrEmail(e.target.value)}
          />
        </label>
      </div>

      <div>
        <label>
          Şifre
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
      </div>

      <button type="submit">Giriş</button>
    </form>
  )
}

export default LoginPage
```

### `App.tsx` (bu parçanın sonucu)

```tsx
import LoginPage from './LoginPage'

function App() {
  return <LoginPage />
}

export default App
```

### Çalıştırma / kontrol

```powershell
cd D:\ReactBattleArena\web
npm run dev
```

`http://localhost:5173` → yaz → Giriş → F12 Console’da iki değer görünmeli.

### Web / .NET eşlemesi

| React | Bildiğin dünya |
|--------|----------------|
| `LoginPage.tsx` | Küçük Razor Partial / View |
| `useState` | ViewModel property |
| `value` + `onChange` | Input’u modele bağlamak |
| `e.preventDefault()` | Full page post yerine SPA (API sonra `fetch`) |
| `App` → `<LoginPage />` | Layout içine partial koymak |
| Backend alanlar | `LoginRequest`: `UserNameOrEmail`, `Password` → JSON camelCase |

### Sonraki (Faz 2 devam)

`POST https://localhost:7275/api/auth/login` + token (`LoginResult`).  
→ **Yapıldı:** aşağıdaki “Faz 2 devam — fetch + JWT” bölümüne bak.

---

## Faz 2 devam — fetch + JWT (tamam)

### Kavramlar

1. **`fetch`** — tarayıcıdan HTTP (`HttpClient` gibi).
2. **`localStorage`** — JWT’yi sakla; sonraki isteklerde Bearer için.

### Ne değişti

- `handleSubmit` → `async`; `POST https://localhost:7275/api/auth/login`
- Body: `{ userNameOrEmail, password }` (`LoginRequest` camelCase)
- Başarı: `localStorage.setItem('token', data.token)` + `onLogin()` (Faz 3’te eklendi)
- Hata: `response.ok` değilse “Giriş başarısız”; `catch` → “API’ye ulaşılamadı”
- Başarılı girişte ekranda mesaj yoktu (sadece token + sonra Characters’a geçiş) — normaldi

### Kontrol

- Yanlış şifre → “Giriş başarısız”
- API kapalı → “API’ye ulaşılamadı…”
- Doğru login → Application → Local Storage → `http://localhost:5173` → `token` dolu  
  (Manifest değil; PWA manifest bu projede yok, gerekmez)

### Eşleme

| React | Bildiğin |
|--------|----------|
| `fetch` POST + JSON | `HttpClient` + `PostAsJsonAsync` |
| `localStorage` token | Cookie / session yerine client’ta JWT |
| `LoginResult` | `userId`, `userName`, `email`, `token` |

---

## Faz 3 — Characters + Bearer (tamam)

### Kavramlar

1. **`useEffect(..., [])`** — sayfa açılınca bir kez API çağır (mount).
2. **Bearer** — `Authorization: Bearer <token>` (Scalar’daki gibi).

### Dosyalar

| Dosya | Ne yaptık |
|-------|-----------|
| `web/src/CharactersPage.tsx` | Liste: `GET /api/characters` + Bearer |
| `web/src/LoginPage.tsx` | `onLogin` prop; başarıda `onLogin()` |
| `web/src/App.tsx` | Token yoksa Login, varsa Characters |

### `App` akışı

```tsx
const [isLoggedIn, setIsLoggedIn] = useState(
  () => !!localStorage.getItem('token'),
)
// !isLoggedIn → <LoginPage onLogin={() => setIsLoggedIn(true)} />
// isLoggedIn → <CharactersPage />
```

Sayfa yenilense bile token duruyorsa direkt karakter listesi açılır.

### CharactersPage özeti

- `interface CharacterRow` — `id`, `name`, `universe`, `rarity` (`CharacterRowDto` camelCase)
- `useEffect` içinde `load()`: token oku → `fetch(.../api/characters?page=1&pageSize=20)` + Bearer
- `setItems(data.items)` — `PagedCharacterRowsResult.Items`
- `items.map` → `<li key={c.id}>`

### Görülen sonuç (örnek)

Karakterler listesi (Robin, Franky, Luffy…). İki aynı Luffy satırı DB’de iki kayıt olabilir — frontend hatası değil.

### Not

`GET /api/characters` backend’de anonim de çalışır; Bearer’ı **öğrenmek** için koyduk (sonraki Admin create’te zorunlu olacak).

### Eşleme

| React | Bildiğin |
|--------|----------|
| `useEffect` + fetch | Sayfa load’da API çağrısı |
| `Authorization: Bearer` | Scalar Authorize / `HttpClient` DefaultRequestHeaders |
| `items.map` | Razor `@foreach` |

### Sonraki

Faz 4 — Register + Admin character create.  
→ **Register yapıldı:** aşağıdaki “Faz 4 (1/2) — Register” bölümüne bak. Admin create sonraki oturum.

---

## Faz 4 (1/2) — Register (tamam)

Admin character create **henüz yok** — sonraki oturum.

### Kavram

Login ile aynı form + `fetch` kalıbı. Router yok: `App` içinde `authView: 'login' | 'register'` ile ekran değişimi (Faz 5’te gerçek URL).

### Dosyalar

| Dosya | Ne yaptık |
|-------|-----------|
| `web/src/RegisterPage.tsx` | Yeni — `POST /api/auth/register` |
| `web/src/LoginPage.tsx` | `onGoRegister` + “Kayıt ol” butonu |
| `web/src/App.tsx` | `authView` ile Login ↔ Register |

### RegisterRequest (JSON camelCase)

`userName`, `email`, `displayName` (opsiyonel → boşsa `null`), `password`.

- Başarı: 201 + Guid; token yok → login’e dön (`onRegistered`).
- Hata: “Kayıt başarısız…” / API kapalı mesajı.
- `type="button"` “Girişe dön” / “Kayıt ol” → form submit tetiklemez.

### App akışı (özet)

```tsx
const [authView, setAuthView] = useState<'login' | 'register'>('login')
// !isLoggedIn + register → <RegisterPage onRegistered/onBack → login />
// !isLoggedIn + login → <LoginPage onGoRegister → register />
```

### Eşleme

| React | Bildiğin |
|--------|----------|
| `POST /api/auth/register` | AuthController Register |
| `authView` state | Geçici “hangi form” (router değil) |

### Sonraki (Faz 4 — 2/2)

Admin ile `POST /api/characters` + Bearer; Player’da 403.  
→ **Yapıldı:** aşağıdaki “Faz 4 (2/2) — Admin character create” bölümüne bak.

---

## Faz 4 (2/2) — Admin character create (tamam)

### Kavramlar

1. Korumalı `POST` + Bearer (`[Authorize(Roles = Admin)]`).
2. `load`’u `useEffect` dışına çıkarmak → create sonrası listeyi yenilemek.

### Ne değişti (`CharactersPage.tsx`)

- Form state’leri: `name`, `universe`, `biography`, `rarity`, `baseAttack`, `baseDefense`, `baseSpeed`, `imageUrl` + `formError` / `formSuccess`
- `async function load()` component gövdesinde; `useEffect(() => { load() }, [])`
- `handleCreate`: `POST /api/characters` + JSON (`CreateCharacterRequest` camelCase)
  - **403** → “Yetkin yok (Admin gerekli)”
  - Başarı → form temizle + `await load()`
- JSX: form liste üstünde; `type="number"` alanlarda `Number(e.target.value)`

### Dosya iskeleti (hatırlatma)

```
state’ler → load() → useEffect → handleCreate → return (form + ul)
```

### Test

- Admin login → Ekle → listede yeni satır
- Player → 403 mesajı
- Rol değişince: Local Storage `token` sil + yeniden login

### Not (UI)

Grid / ortak Grid component **şimdilik yok** — erken; Faz 5 sonrası kosmetik. Ortak component 2–3 tekrar görünce çıkarılır.

### Eşleme

| React | Bildiğin |
|--------|----------|
| `POST` + Bearer + 403 | Admin-only endpoint / Scalar Authorize |
| `load` yeniden çağrı | Create sonrası liste refresh |

### Sonraki

Faz 5 — react-router + Logout.  
→ **Yapıldı:** aşağıdaki “Faz 5 — react-router + Logout” bölümüne bak.

---

## Faz 5 — react-router + Logout (tamam)

### Kavramlar

1. **react-router** — URL ile sayfa (`/login`, `/register`, `/characters`); `authView` state kalktı.
2. **Logout** — `localStorage.removeItem('token')` + `navigate('/login')`.

### Kurulum

```powershell
cd D:\ReactBattleArena\web
npm install react-router-dom
```

`npm fund` / `npm audit` uyarıları şimdilik yok sayıldı.

### Dosyalar

| Dosya | Ne yaptık |
|-------|-----------|
| `main.tsx` | `BrowserRouter` ile `App` sarıldı |
| `App.tsx` | `Routes` / `Route` / `Navigate`; `isLoggedIn` + `authView` silindi |
| `LoginPage.tsx` | `useNavigate` → `/characters`; `Link` → `/register`; props yok |
| `RegisterPage.tsx` | `navigate('/login')`; `Link` → `/login`; props yok |
| `CharactersPage.tsx` | Token yoksa `<Navigate to="/login" />`; Çıkış butonu |

### Route’lar

| Path | Sayfa |
|------|--------|
| `/login` | LoginPage |
| `/register` | RegisterPage |
| `/characters` | CharactersPage |
| `/` ve `*` | → `/characters` |

### Eşleme

| React | Bildiğin |
|--------|----------|
| `BrowserRouter` + `Routes` | ASP.NET endpoint routing (ama client-side) |
| `Link` / `navigate` | `<a>` full reload yerine SPA geçiş |
| Logout token sil | Session/cookie clear benzeri |

### Planlanan React fazları (0→5)

Hepsi tamam. Sonraki işler ürün/UI (grid, polish, vs.) — ayrı karar.

