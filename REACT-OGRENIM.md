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
→ **Faz 6 (Characters kart + CSS grid)** yapıldı / yapılıyor — aşağıdaki bölüme bak.

---

## Grid mantığı (proje kararı) — özet

### Ne yapmıyoruz

- Tüm sayfaların kullandığı **tek mega Grid** component’i yok.
- Erken “her listeyi bilen” abstraction yok (props cehennemi riski).

### Ne yapıyoruz (SPA önerisi)

1. **Layout paylaşılabilir** — ileride header/nav/çıkış (`App` layout). Bu kabuk.
2. **Liste/grid sayfaya özel** — Characters kartları ≠ Users tablosu ≠ ödül listesi.
3. **Tekrar edince çıkar** — 2–3 yerde aynı kart görünürse o zaman ortak `Card` / utility; şimdi değil.
4. **Characters için:** CSS Grid (`characters-grid`) + küçük `CharacterCard` (props).

### Yapı şeması (güncel — liste / ekle ayrıldı)

```
/characters  → CharactersPage
  ├── header (başlık + “Karakter ekle” + çıkış)
  └── characters-grid → CharacterCard[] (resim + ad + evren + rarity)

/characters/new → CharacterCreatePage
  ├── form (Admin create)
  └── characters-grid → CharacterCard[] önizleme
       (şimdilik mevcut liste; “sık kullanılan” backend sonra)
```

| Parça | Rol |
|--------|-----|
| `CharactersPage.css` `.characters-grid` | Yerleşim (kaç sütun, boşluk) |
| `CharacterCard.tsx` | Tek karakterin görünümü (liste + create önizleme) |
| `CharactersPage.tsx` | Sadece liste |
| `CharacterCreatePage.tsx` | Form + altta aynı kart grid |

### CSS Grid özü

```css
.characters-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 1rem;
}
```

Ekran genişleyince sütun artar; daralınca azalır. Ortak Grid kütüphanesi şart değil.

---

## Faz 6 — Characters kart + CSS grid (+ create ayrımı)

### Kavramlar

1. Props’lu `CharacterCard` component.
2. Sayfaya özel CSS Grid (mega Grid değil).
3. Liste ve ekleme **ayrı route** (CRUD best practice).

### Dosyalar

| Dosya | Ne |
|-------|-----|
| `web/src/CharacterCard.tsx` | Kart UI (imageUrl yoksa placeholder) |
| `web/src/CharactersPage.css` | Sayfa + grid + kart + form stilleri |
| `web/src/CharactersPage.tsx` | Sadece grid liste; Link → `/characters/new` |
| `web/src/CharacterCreatePage.tsx` | Admin form + önizleme grid; başarı → `/characters` |
| `web/src/App.tsx` | Route `/characters/new` |

### CRUD planı (Characters — sırayla)

| İş | Route |
|----|--------|
| Liste | `/characters` |
| Create | `/characters/new` ← yapıldı |
| Detay | `/characters/:id` ✓ |
| Edit | `/characters/:id/edit` (sonra) |
| Delete | detay/kart + onay (sonra) |

### API hataları (öğrenilen)

- **400** validation: Name/Universe zorunlu; Rarity **1–5**; ImageUrl max **500** karakter; response `errors` göster.
- **401** token yok/süresi dolmuş → yeniden login.
- **403** Admin değil.

### Not

Create sayfasındaki “Mevcut karakterler” grid’i kasıtlı önizleme; kullanıcıya hoş geldi. İleride gerçek “sık kullanılan” metrikleriyle değişebilir.

`CharacterCreatePage` map’inde de `id={c.id}` olmalı (`CharacterCard` artık `id` istiyor).

---

## JSX syntax — koşullu render (`&&`)

Öğrenci React’e yeni; takılan syntax’lar oturumda kısa açıklanır (faz atlamadan).

JSX içinde `{}` = JavaScript ifadesi.

`A && B`: **A truthy ise B’yi render et; değilse hiçbir şey.**

```tsx
{loading && <p>Yükleniyor…</p>}
{error && <p>{error}</p>}
{character && <article>…</article>}
```

| Kod | Anlamı |
|-----|--------|
| `loading && <p>…</p>` | `loading === true` → göster; `false` → boş |
| `error && <p>{error}</p>` | `error` dolu string → göster; `''` → boş |
| `{error}` | string’i ekrana yaz (Razor `@error`) |

Razor eşlemesi:

```cshtml
@if (loading) { <p>Yükleniyor…</p> }
@if (!string.IsNullOrEmpty(error)) { <p>@error</p> }
```

Sık bakılacaklar (ileride): `&&`, ternary `? :`, `map`, props, `useState` / `useEffect`, `useParams`.

Dosya upload (resim) erken — şimdilik Image URL string yeterli.

---

## Faz 6 devam — Karakter detay (`/characters/:id`)

### Kavramlar

1. `useParams` — URL’den `id`
2. `GET /api/characters/{id}` → `CharacterDetailDto`
3. Kart → `Link` ile detaya git

### Dosyalar

| Dosya | Ne |
|-------|-----|
| `CharacterDetailPage.tsx` | Detay; `{loading && …}` / `{error && …}` / character |
| `CharacterCard.tsx` | `id` prop + `Link to={/characters/${id}}` |
| `App.tsx` | `Route path="/characters/:id"` (`/new` önce) |
| Liste / create map | `id={c.id}` |

---

## useState başlangıç değerleri (Edit) — 10 / 1 “üzerine yazar mı?”

**Hayır.** `useState(10)` sadece **ilk render** iskeleti. `GET` bitince `setBaseAttack(data.baseAttack)` gerçek değeri koyar (örn. 100). Kaydet’te o state gider.

Form `{!loading && !loadError && (…)}` ile gizliyse kullanıcı 10’u görmez bile.

Create’te aynı defaults kalıcıdır çünkü orada GET ile doldurma yok.

---

## useState / setLoading — biz mi React mi?

```tsx
const [loading, setLoading] = useState(true)
```

| Parça | Kim |
|--------|-----|
| `useState` | **React**’ten gelir (`import { useState } from 'react'`) |
| `loading` | Bizim verdiğimiz **değişken adı** (istediğin isim: `isLoading` vb.) |
| `setLoading` | React’in ürettiği **setter**; isim = `set` + state adı |
| `true` | Bizim seçtiğimiz **başlangıç** |

React “loading” diye sabit bir şey dayatmaz; pattern biz yazıyoruz.

---

## useEffect — tamamı (Edit / Detail kalıbı)

```tsx
useEffect(() => {
  async function load() {
    // GET … setName(data.name) …
  }
  load()
}, [id, token])
```

### Ne işe yarar?

Component **ekrana geldikten sonra** (veya bağımlılıklar değişince) yan etki çalıştırır: API çağrısı, abonelik vb.  
Render’ın kendisi sadece UI üretmeli; “gidip server’dan veri çek” → `useEffect`.

ASP.NET benzeri: sayfa açılınca `OnGetAsync` / controller’da load — ama React’te UI çizildikten sonra effect çalışır.

### Parça parça

1. **`useEffect(…)`** — React hook (React’ten).
2. **`() => { … }`** — effect’e verdiğin fonksiyon (evet, arrow / “lambda” gibi). React bu fonksiyonu doğru zamanda çağırır.
3. **`async function load() { … }`** — effect’in **içinde** tanımlı yardımcı. Recursive değil: kendini çağırmıyor; sadece bir kez `load()` ile çalıştırıyoruz.
4. **`load()`** — az önce tanımladığın fonksiyonu çağır.
5. **`[id, token]`** — **dependency array** (bağımlılık listesi):
   - İlk mount’ta effect bir kez çalışır.
   - `id` veya `token` **değişirse** effect **yeniden** çalışır (başka karaktere geçince yeni GET).
   - `[]` boş olsaydı: sadece ilk açılışta bir kez.

### Recursive mi?

**Hayır.** `load` içinde `load()` yok. Sadece:

```
useEffect çalıştı → load fonksiyonunu tanımla → load() bir kez çağır → bitti
```

### `handleSubmit` useEffect içinde mi?

**Hayır.** Submit kullanıcı butona basınca çalışır → `onSubmit={handleSubmit}` ile formda.  
`useEffect` = sayfa açılınca / id değişince **otomatik load**.  
İkisi ayrı kapılar.

| Ne | Ne zaman |
|----|----------|
| `useEffect` → `load()` | Sayfa açıldı / `id` değişti |
| `handleSubmit` | Form submit (Kaydet) |

---

## Hata düzeltmesi (not)

`CharacterCard` `id` zorunlu → `CharacterCreatePage` map’ine `id={c.id}` eklendi (TS 2741).

---

## Characters CRUD — Edit (`/characters/:id/edit`)

### Kavramlar

1. `GET` ile formu doldur (`setName(data.name)` …).
2. `PUT` kaydet → **204 No Content** → `response.json()` yok → detaya `navigate`.

### Dosyalar

| Dosya | Ne |
|-------|-----|
| `CharacterEditPage.tsx` | Load + form + PUT |
| `App.tsx` | `Route /characters/:id/edit` |
| `CharacterDetailPage` | Link “Düzenle” |

`useState(10)` vb. sadece iskelet; GET sonrası gerçek değer. Form `{!loading && !loadError && …}` ile gizli.

---

## Characters CRUD — Delete (detay sayfası)

### Kavramlar

1. `DELETE /api/characters/{id}` + Bearer (Admin).
2. `window.confirm` → iptalde çık.
3. 204 → `navigate('/characters')`.

### `CharacterDetailPage`

- Tek header: Düzenle | Sil | Listeye dön
- `handleDelete` fonksiyon **içinde** (gövde dışarı taşmamalı)
- `deleteError` state; 401 / 403 / 404 ayrı mesaj

### Characters frontend CRUD durumu

| İş | Route / yer | Durum |
|----|-------------|--------|
| Liste | `/characters` | ✓ |
| Create | `/characters/new` | ✓ |
| Detay | `/characters/:id` | ✓ |
| Edit | `/characters/:id/edit` | ✓ |
| Delete | detayda Sil | ✓ |

---

## App layout kararı (9 Ağustos 2026)

- **Üst menü** (sol sidebar yok).
- Düz linkler; **dropdown yok** (şimdilik).
- Örnek: Brand · Karakterler · Çıkış — Ekle liste sayfasında.
- Login/Register layout’suz.
- Grid tam genişlik kalsın diye üst tercih edildi.

---

## Nested routes + `<Outlet />` (öğrenme — uzun not)

### Sorun neydi? (9 Ağustos)

`AppLayout.tsx` ve CSS yazılmıştı, `CharactersPage` sadeleştirilmişti; ama **`App.tsx` hâlâ eski düz route’lardı** — `AppLayout` import edilip parent route yapılmamıştı. Bu yüzden üst menü hiç görünmedi. Layout’un çalışması için child sayfaların **AppLayout’un altında** tanımlı olması gerekir.

### Nested (iç içe) route ne demek?

Eskiden her sayfa tek başına:

```tsx
<Route path="/characters" element={<CharactersPage />} />
<Route path="/characters/:id" element={<CharacterDetailPage />} />
```

Burada her URL sadece **kendi sayfasını** çizer. Ortak üst menü yok.

Yenisi: bir **ebeveyn (parent)** route var; onun `element`’i `AppLayout`. Altında **çocuk (child)** route’lar var:

```tsx
<Route element={<AppLayout />}>
  <Route path="/characters" element={<CharactersPage />} />
  <Route path="/characters/:id" element={<CharacterDetailPage />} />
  {/* new, edit… */}
</Route>
```

Kullanıcı `/characters` açınca React Router şunu yapar:

1. Parent’ı çalıştır → `AppLayout` çizilir (üst menü + kabuk).
2. Hangi child URL’ye uyuyorsa onu seç → örn. `CharactersPage`.
3. Child’ı parent’ın **içindeki boşluğa** yerleştir.

O boşluk = **`<Outlet />`**.

### `<Outlet />` nedir? (Razor eşlemesi)

`AppLayout` kabuktur:

```tsx
<header>…menü…</header>
<main>
  <Outlet />   {/* ← buraya o anki sayfa gelir */}
</main>
```

| URL | Outlet’in içine giren |
|-----|------------------------|
| `/characters` | `CharactersPage` |
| `/characters/new` | `CharacterCreatePage` |
| `/characters/abc-guid` | `CharacterDetailPage` |
| `/characters/abc-guid/edit` | `CharacterEditPage` |

ASP.NET Razor’da `_Layout.cshtml` + `@RenderBody()` gibidir: layout sabit, ortadaki içerik sayfaya göre değişir.  
`<Outlet />` = “buraya child route’un `element`’ini koy”.

“O anki child route” = adres çubuğundaki URL’ye uyan **alt** `Route`’un sayfası.

Login/Register parent’ın **dışında** kaldığı için onlarda üst menü yok.

### Neden `element={<AppLayout />}` path’siz parent?

```tsx
<Route element={<AppLayout />}>
```

Bu parent’ın kendi path’i yok; sadece “şu çocukları sarmala” der. Çocuklar kendi path’lerini taşır (`/characters`, …).

### Edit neden `:id`’den önce?

```tsx
<Route path="/characters/:id/edit" … />
<Route path="/characters/:id" … />
```

İkisi farklı segment sayısı olduğu için çoğu durumda ikisi de çalışır; yine de **daha spesifik** olanı (`…/edit`) önce yazmak alışkanlık / güvenlik. (`new` zaten ayrı path.)

### Login / token

`AppLayout` içinde token yoksa `<Navigate to="/login" />`. Böylece korumalı sayfalar layout’a girmeden login’e düşer. Login sayfası layout child’ı değil.

---

## Teknik borç (sonra bak)

Uygulama açılınca karakter listesinin **3–4 sn** gelmesi — henüz incelenmedi. Muhtemel: API soğuk start, HTTPS sertifika, StrictMode çift fetch, vs. Ayrı oturumda bakılacak.

