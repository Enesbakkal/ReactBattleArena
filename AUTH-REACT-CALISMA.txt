# Authentication · Authorization · React — Çalışma Dosyası

**Amaç:** Çarşamba iş görüşmesi (10:00) öncesi Auth + React temellerini **bizim projeden** öğrenmek.  
**Kaynak proje:** `D:\ReactBattleArena` (.NET 10 API + Vite React).  
**Not:** Bu dosya öğretmek içindir. `REACT-OGRENIM.md` oturum günlüğü; bu dosya görüşme odaklı derstir.

---

# BÖLÜM 0 — Senin programın (Pazar → Çarşamba 10:00)

Kabaca günde ~4.5–6.5 saat bakma şansın var (işte sabah 2 + akşam 2.5; evde ~18:00 sonrası 2 saat). Ezber değil: **anlatabilmek**.

| Ne zaman | Ne oku / yap |
|----------|----------------|
| **Pazar akşam** | Bölüm 1 (AuthN/AuthZ kavram) + Bölüm 2 akışı (Register→Login→JWT→Authorize) bir kez baştan sona |
| **Pazartesi** | Bölüm 2 kodları: şifre hash, JWT claims, `Program.cs` pipeline, 401 vs 403. Sesli anlat: “Authentication nedir?” |
| **Salı** | Bölüm 3: Vite, component/state, `api.ts`, Login, Router, AppLayout. “SPA’da token nereye konur?” |
| **Çarşamba sabah (görüşmeden önce)** | Bölüm 4 mini soru bankası + “Node’da aynı şey ne olur?” tablosu. 20 dk tekrar |

**Görüşmede uzaktan konular (Node vs):** Aynı fikirler farklı isimle gelir. Sen .NET yaptın; “JWT Bearer + bcrypt hash + role claim” diyebilirsin — Express/Passport da aynı modeli kullanır.

---

# BÖLÜM 1 — Kavramlar (ezber değil, net ayrım)

## 1.1 Authentication (kimlik doğrulama) = “Kimsin?”

Sisteme “ben Enes’im, şifrem bu” dersin. Sistem doğrular → sana bir **kanıt** verir (bizde **JWT**).

- Login başarılı → 200 + token  
- Kullanıcı yok / şifre yanlış → **401 Unauthorized** (“kimlik kanıtlanamadı”)

Bizde: `POST /api/auth/login` → `LoginCommandHandler` şifreyi BCrypt ile verify eder → `JwtTokenService` token üretir.

## 1.2 Authorization (yetkilendirme) = “Ne yapabilirsin?”

Kimliğin belli olduktan sonra: bu endpoint’i **yapmaya hakkın var mı?**

- Token yok / geçersiz → yine çoğu zaman **401**  
- Token var ama rol yetmez → **403 Forbidden** (“seni tanıdım, ama bu işi yapamazsın”)

Bizde: Character **POST/PUT/DELETE** → `[Authorize(Roles = "Admin")]`. Player denerse **403**.

## 1.3 Üç kelime daha

| Terim | Anlam |
|-------|--------|
| **Claim** | Token içindeki küçük bilgi parçası: `sub` (user id), `role`, `email`… |
| **Bearer** | “Bu isteğin yetkisi şu token” — header: `Authorization: Bearer eyJ...` |
| **Hash** | Şifreyi **geri çevrilemez** şekilde saklamak. DB’de düz şifre **asla** olmaz. |

## 1.4 HTTP status — görüşmede sık sorulur

| Kod | Ne zaman (bizde) |
|-----|------------------|
| **200** | Login OK, GET liste/detay OK |
| **201** | Register / Create karakter oluştu |
| **204** | PUT/DELETE başarılı, body yok |
| **400** | Validation (kısa şifre, duplicate username…) |
| **401** | Login fail veya token yok/geçersiz |
| **403** | Token var, rol Admin değil (CUD) |
| **404** | Karakter/id yok |

## 1.5 Node.js / Express eşlemesi (görüşme için köprü)

Konu “Node” olsa da model aynı:

| Bizde (.NET) | Tipik Node |
|--------------|------------|
| `BCryptPasswordHasher` | `bcrypt` / `bcryptjs` |
| `JwtTokenService` + JwtBearer | `jsonwebtoken` + middleware veya Passport JWT |
| `[Authorize(Roles = Admin)]` | middleware: `if (req.user.role !== 'Admin') return 403` |
| `localStorage` + `Authorization: Bearer` | aynı (frontend dil bağımsız) |
| MediatR LoginCommand | `authController.login` + service katmanı |

Cümle: “Ben bunu ASP.NET’te yaptım; Node’da bcrypt + JWT middleware ile aynı akış.”

---

# BÖLÜM 2 — Bizim projede Authentication & Authorization

## 2.1 Büyük resim (ezberle bu sırayı)

```
[Register]
  form → POST /api/auth/register
  → şifre Hash → User (Role=Player) DB’ye
  → 201 + id
  → (bizde otomatik login yok) → Login sayfasına git

[Login]
  form → POST /api/auth/login  (Bearer YOK)
  → kullanıcı bul + Verify(şifre, hash)
  → JWT üret (içinde Role claim)
  → 200 { token, ... }
  → React: setToken(token) → localStorage

[Sonraki istekler]
  React apiFetch → header Authorization: Bearer <token>
  → Api: UseAuthentication doğrular imza/issuer/audience/süre
  → UseAuthorization + [Authorize(Roles=...)] rol bakar
  → Handler çalışır veya 401/403
```

**Önemli:** Frontend’de token olması “güvenlik” değildir. Asıl kapı **backend**. Frontend sadece UX (login’e atma, buton gösterme). Player create denerse API 403 der.

## 2.2 Roller (Authorization sabitleri)

```csharp
// Domain/Authorization/Roles.cs
namespace ReactBattleArena.Domain.Authorization;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Player = "Player";
}

// Authentication = kimsin? (JWT)
// Authorization = ne yapabilirsin? (rol)
// Register → varsayılan Player. İlk Admin: SSMS’te Role = Admin + yeniden login (yeni JWT).
```

## 2.3 User entity — şifre hash + rol

```csharp
// Domain/Users/User.cs (özet + Create)
public sealed class User
{
    public Guid Id { get; private set; }
    public string UserName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public string PasswordHash { get; private set; } = null!;  // düz şifre YOK
    public int Points { get; private set; }
    public string Role { get; private set; } = null!;          // "Admin" | "Player"
    public DateTime CreatedAtUtc { get; private set; }

    public static User Create(
        string userName,
        string email,
        string? displayName,
        string passwordHash,
        string role,
        DateTime utcNow)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Role = role,
            Points = 0,
            CreatedAtUtc = utcNow
        };
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
    public void SetRole(string role) => Role = role;
}
```

## 2.4 Şifre hash arayüzü + BCrypt

Neden interface? Application katmanı “BCrypt.Net” bilmesin; Infrastructure uygular (test/değişim kolay).

```csharp
// Application/Abstractions/IPasswordHasher.cs
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string PasswordHash);
}
```

```csharp
// Infrastructure/Security/BCryptPasswordHasher.cs
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
```

**Görüşme cümlesi:** “Şifreyi one-way hash’liyoruz; login’de hash’i çözmüyoruz, aynı algoritmayla verify ediyoruz.”

## 2.5 Register — command + handler

```csharp
public sealed record RegisterCommand(
    string UserName,
    string Email,
    string? DisplayName,
    string Password) : IRequest<Guid>;
```

```csharp
// RegisterCommandHandler.cs
public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
{
    if (await _db.Users.AnyAsync(u => u.UserName == request.UserName, cancellationToken))
        throw new ValidationException(new[]
        {
            new ValidationFailure(nameof(request.UserName), "UserName is already taken.")
        });

    if (await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
        throw new ValidationException(new[]
        {
            new ValidationFailure(nameof(request.Email), "Email is already taken.")
        });

    var passwordHash = _passwordHasher.Hash(request.Password);

    var entity = User.Create(
        request.UserName,
        request.Email,
        request.DisplayName,
        passwordHash,
        Roles.Player,          // ← her kayıt Player
        DateTime.UtcNow);

    _db.Users.Add(entity);
    await _db.SaveChangesAsync(cancellationToken);
    return entity.Id;
}
```

Validator (özet): username/email zorunlu, password min 6.

## 2.6 Login — command + handler + sonuç

```csharp
public sealed record LoginCommand(string UserNameOrEmail, string Password)
    : IRequest<LoginResult?>;

public sealed record LoginResult(
    Guid UserId,
    string UserName,
    string Email,
    string Token);
// null → controller 401
```

```csharp
// LoginCommandHandler.cs
public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
{
    var user = await _db.Users.FirstOrDefaultAsync(
        u => u.UserName == request.UserNameOrEmail || u.Email == request.UserNameOrEmail,
        cancellationToken);

    if (user is null)
        return null;

    if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        return null;

    var token = _jwtTokenService.CreateToken(user);
    return new LoginResult(user.Id, user.UserName, user.Email, token);
}
```

**Güvenlik notu:** “Kullanıcı yok” ile “şifre yanlış” aynı cevap (null → 401). Saldırgana “bu email kayıtlı” sızdırma.

## 2.7 JWT üretimi

```csharp
// Application/Abstractions/IJwtTokenService.cs
public interface IJwtTokenService
{
    string CreateToken(User user);
}
```

```csharp
// Infrastructure/Security/JwtOptions.cs
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 60;
}
```

```csharp
// Infrastructure/Security/JwtTokenService.cs
public string CreateToken(User user)
{
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role)   // ← Authorization buradan okur
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _options.Issuer,
        audience: _options.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_options.ExpireMinutes),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

JWT üç parça (nokta ile): `header.payload.signature`  
Payload Base64’tür → **gizli değildir**; imza bozulmadan değiştirilemez. Bu yüzden secret key önemli.

`appsettings` yapısı (değerleri ezberleme; fikir önemli):

```json
"Jwt": {
  "Key": "(uzun gizli anahtar)",
  "Issuer": "ReactBattleArena",
  "Audience": "ReactBattleArena",
  "ExpireMinutes": 60
}
```

## 2.8 AuthController (HTTP kapısı)

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<Guid>> Register(
        [FromBody] RegisterRequest body,
        CancellationToken cancellationToken = default)
    {
        var id = await _mediator.Send(
            new RegisterCommand(body.UserName, body.Email, body.DisplayName, body.Password),
            cancellationToken);
        return Created($"/api/users/{id}", id);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login(
        [FromBody] LoginRequest body,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new LoginCommand(body.UserNameOrEmail, body.Password),
            cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        // Token geçerliyse User (ClaimsPrincipal) dolu
        return Ok(new
        {
            id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            userName = User.Identity?.Name,
            email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
        });
    }
}
```

- `[AllowAnonymous]` → login/register herkese açık (ileride global Authorize olsa bile).  
- `[Authorize]` on `me` → token şart; rol şart değil.

## 2.9 Program.cs — Authentication + Authorization + CORS

Sıra kritik: **Authentication → Authorization**.

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// ...
app.UseCors();
app.UseAuthentication(); // token’ı oku → User doldur
app.UseAuthorization();  // [Authorize] kurallarını uygula
app.MapControllers();
```

**CORS:** Tarayıcı, `localhost:5173` (Vite) → `localhost:7275` (API) cross-origin olduğu için API’nin 5173’e izin vermesi gerekir. CORS ≠ Authentication; ikisi farklı problem.

## 2.10 CharactersController — Authorization pratikte

```csharp
[HttpGet]
public async Task<ActionResult<PagedCharacterRowsResult>> GetPaged(...) { ... }
// Liste: şimdilik Authorize yok (anonim de okuyabilir tasarım)

[HttpGet("{id:guid}")]
public async Task<ActionResult<CharacterDetailDto>> GetById(...) { ... }

[Authorize(Roles = Roles.Admin)]
[HttpPost]
public async Task<ActionResult<Guid>> Create(...) { ... }

[Authorize(Roles = Roles.Admin)]
[HttpPut("{id:guid}")]
public async Task<IActionResult> Update(...) { ... }

[Authorize(Roles = Roles.Admin)]
[HttpDelete("{id:guid}")]
public async Task<IActionResult> Delete(...) { ... }
```

| İstek | Player JWT | Admin JWT | Token yok |
|-------|------------|-----------|-----------|
| GET list/detail | 200 | 200 | 200 (şimdiki tasarım) |
| POST/PUT/DELETE | **403** | 201/204 | **401** |

Admin olmak: SSMS’te `Users.Role = 'Admin'` → **eski token’da eski rol vardır** → yeniden login.

## 2.11 Backend özet cümleleri (ezber)

1. Authentication: login + JWT.  
2. Authorization: claim’deki Role + `[Authorize(Roles=...)]`.  
3. Şifre: BCrypt hash; asla düz metin.  
4. 401 ≠ 403.  
5. Frontend token saklar; güvenlik sunucudadır.

---

# BÖLÜM 3 — React + Vite + Authentication (frontend)

## 3.1 Vite nedir? (görüşme 30 sn)

**Vite** = modern frontend geliştirme aracı / bundler.

- `npm run dev` → hızlı dev server (bizde **5173**)  
- React + TypeScript şablonu ile `web/` klasörünü kurduk  
- Production: `npm run build` → statik dosyalar

Bizim stack: **React 19 + TypeScript + react-router-dom + Vite 8**.

`main.tsx` girişi:

```tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>,
)
```

`BrowserRouter` URL’yi dinler; `App` içindeki `Routes` hangi sayfanın açılacağını seçer. **Sayfa yenilenmeden** ekran değişir → SPA.

## 3.2 React temelleri (Auth’u taşıyan parçalar)

| Kavram | Bizde ne işe yaradı |
|--------|---------------------|
| **Component** | `LoginPage`, `CharactersPage`… UI fonksiyonu |
| **useState** | form alanları, error, liste `items` |
| **controlled input** | `value={x}` + `onChange` → state kaynağı React |
| **useEffect** | sayfa açılınca API’den veri çek (liste/detay/edit) |
| **props** | `CharacterCard`’a `id`, `name`… |
| **JSX** | HTML benzeri; `className`, `{error && <p>}` |

Auth özelinde asıl üçlü: **token sakla → her istekte Bearer ekle → yoksa login’e at**.

## 3.3 `api.ts` — Auth’un React kalbi (tam kod)

```ts
export const API_BASE = 'https://localhost:7275'

export function getToken(): string | null {
  return localStorage.getItem('token')
}

export function setToken(token: string) {
  localStorage.setItem('token', token)
}

export function clearToken() {
  localStorage.removeItem('token')
}

type ApiFetchOptions = {
  method?: string
  body?: unknown
  /** false = login/register (Bearer yok). Varsayılan true. */
  auth?: boolean
}

export async function apiFetch(
  path: string,
  options: ApiFetchOptions = {},
): Promise<Response> {
  const { method = 'GET', body, auth = true } = options

  const headers: Record<string, string> = {}

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  if (auth) {
    const token = getToken()
    if (token) {
      headers.Authorization = `Bearer ${token}`
    }
  }

  return fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })
}
```

**Neden önemli?**

- Tek yerde base URL + Bearer → sayfalarda `localhost` tekrarı yok.  
- Login/Register: `auth: false` (henüz token yok / gönderme).  
- Create/Edit/Delete/Liste: varsayılan `auth: true` → otomatik Bearer.  
- Logout: `clearToken()`.

`localStorage`: tarayıcıda kalır (sekme kapanınca da). XSS riski tartışılır; görüşmede “biz öğrenme projesinde localStorage kullandık; httpOnly cookie alternatifi var” diyebilirsin.

## 3.4 LoginPage — controlled form + token

```tsx
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiFetch, setToken } from './api'

function LoginPage() {
  const navigate = useNavigate()
  const [userNameOrEmail, setUserNameOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')

    try {
      const response = await apiFetch('/api/auth/login', {
        method: 'POST',
        auth: false,
        body: { userNameOrEmail, password },
      })

      if (!response.ok) {
        setError('Giriş başarısız')
        return
      }

      const data = await response.json()
      setToken(data.token)          // localStorage
      navigate('/characters')       // SPA yönlendirme
    } catch (err) {
      console.error(err)
      setError('API’ye ulaşılamadı (backend çalışıyor mu?)')
    }
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
      {error && <p>{error}</p>}
      <p>
        <Link to="/register">Kayıt ol</Link>
      </p>
    </form>
  )
}

export default LoginPage
```

Akış: submit → API login → `data.token` → `setToken` → characters.

## 3.5 RegisterPage — token yok, sonra login’e git

```tsx
const response = await apiFetch('/api/auth/register', {
  method: 'POST',
  auth: false,
  body: {
    userName,
    email,
    displayName: displayName || null,
    password,
  },
})

if (!response.ok) {
  setError('Kayıt başarısız (kullanıcı/email dolu veya validation)')
  return
}

setSuccess('Kayıt OK — şimdi giriş yap')
navigate('/login')
```

Register JWT vermez (bizim tasarım); kullanıcı login olur.

## 3.6 App.tsx — route haritası (API değil, UI)

```tsx
import { Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './AppLayout'
import LoginPage from './LoginPage'
import RegisterPage from './RegisterPage'
import CharactersPage from './CharactersPage'
import CharacterCreatePage from './CharacterCreatePage'
import CharacterDetailPage from './CharacterDetailPage'
import CharacterEditPage from './CharacterEditPage'

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<AppLayout />}>
        <Route path="/characters" element={<CharactersPage />} />
        <Route path="/characters/new" element={<CharacterCreatePage />} />
        <Route path="/characters/:id/edit" element={<CharacterEditPage />} />
        <Route path="/characters/:id" element={<CharacterDetailPage />} />
      </Route>

      <Route path="/" element={<Navigate to="/characters" replace />} />
      <Route path="*" element={<Navigate to="/characters" replace />} />
    </Routes>
  )
}

export default App
```

- Login/Register **layout dışında** (header/çıkış yok).  
- Karakter sayfaları `AppLayout` altında → ortak header + token kontrolü.  
- `:id/edit` **`:id`’den önce** — yoksa `edit` kelimesi id sanılır.

## 3.7 AppLayout — “giriş yoksa içeri alma” + Logout

```tsx
import { Link, Navigate, Outlet, useNavigate } from 'react-router-dom'
import './AppLayout.css'
import { clearToken, getToken } from './api'

function AppLayout() {
  const navigate = useNavigate()
  const token = getToken()

  if (!token) {
    return <Navigate to="/login" replace />
  }

  function handleLogout() {
    clearToken()
    navigate('/login')
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="app-header__left">
          <Link to="/characters" className="app-header__brand">
            ReactBattleArena
          </Link>
          <nav className="app-header__nav">
            <Link to="/characters">Karakterler</Link>
          </nav>
        </div>
        <button type="button" className="app-header__logout" onClick={handleLogout}>
          Çıkış
        </button>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}

export default AppLayout
```

- `getToken()` yok → Login’e.  
- `<Outlet />` = bu layout’un içine child route (CharactersPage vs.) yerleşir.  
- Logout = token sil + navigate. Sunucuda “session invalidate” yok (stateless JWT); süre dolunca veya key değişince ölür.

## 3.8 Korunan API çağrısı örneği (Create)

Create sayfasında (özet):

```tsx
const token = getToken()
if (!token) {
  return <Navigate to="/login" replace />
}

// Kayıt:
const response = await apiFetch('/api/characters', {
  method: 'POST',
  body: {
    name,
    universe,
    biography: biography || null,
    rarity,
    baseAttack,
    baseDefense,
    baseSpeed,
    imageUrl: imageUrl || null,
  },
})
// auth varsayılan true → Bearer otomatik
// Player → 403; Admin → 201
```

Liste:

```tsx
const response = await apiFetch('/api/characters?page=1&pageSize=20')
const data = await response.json()
setItems(data.items)
```

## 3.9 React’te tekrarlayan kalıp (Detail/Edit — bir kez anla)

Aşırı benzer olduğu için üç kopya kod yok; kalıp:

1. `useParams()` ile `id`  
2. `useEffect` → `apiFetch(/api/characters/${id})`  
3. JSON → state (detay objesi veya form alanları)  
4. Edit kaydet → `PUT` + 204 (body parse etme)  
5. Delete → `confirm` + `DELETE` + 204  

**SPA gerçeği:** Liste state’i detaya taşınmaz; detay/edit **yeniden GetById** yapar.

## 3.10 CharacterCard + router Link (Auth değil ama sık sorulur)

Kart tıklanınca API çağırmaz; URL değiştirir:

```tsx
<Link to={`/characters/${id}`}>...</Link>
```

Sonra DetailPage GetById yapar.

## 3.11 Frontend Auth — net özet

```
Vite (5173) ──CORS──► Api (7275)
     │
     ├─ Login/Register: apiFetch(..., auth:false)
     ├─ setToken → localStorage
     ├─ AppLayout: token yoksa Navigate /login
     ├─ apiFetch varsayılan: Authorization Bearer
     └─ Logout: clearToken
```

React **Authentication yapmaz**; sadece token taşır ve UX uygular. Asıl AuthN/AuthZ API’dedir.

---

# BÖLÜM 4 — Görüşme mini soru bankası

Kendine sesli cevap ver (1–2 dk):

1. Authentication ile Authorization farkı?  
2. JWT içinde ne var? Neden payload’a sır koyulmaz?  
3. Şifreyi neden hash’lersin? Verify nasıl çalışır?  
4. 401 ile 403 farkı? Bizde örnek?  
5. Bearer header nasıl görünür?  
6. Vite ne işe yarar? Port?  
7. Controlled component nedir?  
8. `apiFetch` neden yazdık? `auth: false` ne zaman?  
9. SPA’da logout ne yapar / yapmaz?  
10. Node’da aynı sistemi nasıl kurarsın? (bcrypt + jwt + middleware)

**Kısa örnek cevap (1):**  
“Authentication kimliğini kanıtlamak — bizde login + JWT. Authorization o kimlikle ne yapabileceğin — bizde token’daki Role claim ve Admin-only character CUD.”

**Kısa örnek cevap (4):**  
“Yanlış şifre 401. Player karakter silmeye çalışırsa token geçerli olduğu için 403.”

---

# BÖLÜM 5 — Bilinçli sınırlar (dürüstlük puanı)

Görüşmede abartma; şunları “henüz yok / basit” de:

- Refresh token / token revoke listesi yok  
- FE’de Admin butonlarını role’e göre gizlemiyoruz (API yine 403)  
- Rol tablosu (Roles/UserRoles) yok — string kolon  
- HttpOnly cookie session yok — localStorage JWT  
- Node production deneyimim yok; kavramları bu projeden biliyorum  

Dürüst + net > ezber yalan.

---

# Dosya haritası (hızlı bul)

| Konu | Dosya |
|------|--------|
| Roller | `Domain/Authorization/Roles.cs` |
| User | `Domain/Users/User.cs` |
| Register/Login handlers | `Application/Authentication/Commands/*` |
| BCrypt / JWT | `Infrastructure/Security/*` |
| Auth HTTP | `Api/Controllers/AuthController.cs` |
| Admin CUD | `Api/Controllers/CharactersController.cs` |
| Pipeline | `Api/Program.cs` |
| FE helper | `web/src/api.ts` |
| Login/Register | `web/src/LoginPage.tsx`, `RegisterPage.tsx` |
| Guard + Outlet | `web/src/AppLayout.tsx` |
| Routes | `web/src/App.tsx`, `main.tsx` |

Detaylı React oturum notları: `REACT-OGRENIM.md`  
Proje checklist: `PROJE_EKLEMELERI.md` (Adım 14–15 backend auth, 16–28 React)

---

*Oluşturma: 16 Ağustos 2026 — Çarşamba 10:00 görüşme hazırlığı.*
