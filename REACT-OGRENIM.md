# React Öğrenim Notları

Her faz bitince buraya **eklenir**. Döndüğünde hızlıca hatırlamak için oku.

**KURAL (asla bozma):** Eski fazlar silinmez, kısaltılmaz, üzerine yazılmaz. Faz 0, 1, 2… hepsi dosyada kalır; yeni faz sadece **alta eklenir**. 1500 faz olsa her birinin açıklaması burada olur.

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
