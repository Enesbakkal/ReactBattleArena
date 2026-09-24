# Üye ol

Müşterinin henüz hesabı yoktur. Tarayıcıda `https://localhost:5173/register` adresini açar, formu doldurur ve gönderir. İstek `POST /api/auth/register` adresine gider. Header'da `Authorization: Bearer` yoktur. API kabul ederse `Users` tablosuna bir satır yazar ve `UserRoles` ile bu kullanıcıyı `Player` rolüne bağlar. Cevap `201` ve bir `Guid` olur. Access token ve refresh token bu cevapta yoktur. Sayfa `/login` adresine gider.

Aynı çağrıyı Scalar da atabilir. Scalar'da sayfa yoktur; gövde yine aynı dört alandır ve Bearer yine yoktur.

İstek şu sırayla yürür:

1. `RegisterPage` formu toplar, `apiFetch` çağırır (`auth: false`).
2. `apiFetch` JSON gövdeyi `https://localhost:7275/api/auth/register` adresine POST eder.
3. Pipeline: CORS, `UseAuthentication`, `UseAuthorization`. Metotta `[AllowAnonymous]` vardır.
4. `AuthController.Register` gövdeyi `RegisterCommand` yapar, MediatR'a verir.
5. `ValidationBehavior` kuralları çalıştırır. Kural bozuksa handler çalışmaz, cevap `400` olur.
6. `RegisterCommandHandler` parolayı BCrypt ile hash'ler, `Users` satırını yazar, `UserRoles` satırını yazar.
7. Controller `201` ve `Guid` döner. Sayfa bu `Guid` değerini saklamaz, giriş ekranına gider.

`Permissions`, `RolePermissions`, permission kodları, `GET /api/auth/me`, sayfa kapısı, buton gizleme ve `[HasPermission]` bu işte bitmez. Müşteri karakter eklerken durur. Üye olunca bilinen bağ şudur: yeni kullanıcı `UserRoles` üzerinden `Player` rolüne bağlıdır.

## Sayfa

`/register` adresi `App.tsx` içindedir. Kayıt ve giriş, karakter sayfalarının çerçevesi olan `AppLayout` dışında durur.

```12:14:web/src/App.tsx
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
```

Giriş ekranındaki bağlantı da aynı adrese gider.

```89:91:web/src/LoginPage.tsx
      <p>
        <Link to="/register">Kayıt ol</Link>
      </p>
```

`Link` tıklanınca tarayıcı belgeyi baştan yüklemez. `react-router` adresi değiştirir, eşleşen `Route` `RegisterPage` bileşenini çizer. Bu projede sayfa ASP.NET view değildir. API JSON döner, ekranı React basar.

Form dört alanı ve iki mesajı `useState` ile tutar. Başlangıç değerleri boş metindir.

```9:14:web/src/RegisterPage.tsx
  const [userName, setUserName] = useState('')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
```

`useState` bir değer ve onu değiştiren fonksiyon verir. `setUserName` çağrılınca React bu bileşeni yeniden çizer. ASP.NET tarafında buna yakın duran şey, form sınıfındaki bir `string` property'dir. Farkı şudur: atama olunca ekran da güncellenir.

Input, metni kendi içinde tutmaz. `value` state'i gösterir, `onChange` state'i yazar. Buna controlled input denir.

```65:69:web/src/RegisterPage.tsx
            <input
              type="text"
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
            />
```

Parola kutusunda `type="password"` vardır. Tarayıcı karakterleri gizler. `password` state'inde ve gidecek JSON'da parola düz metin durur. Hash tarayıcıda yapılmaz.

```92:99:web/src/RegisterPage.tsx
          <label>
            Şifre
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </label>
```

Gönder düğmesi formu gönderir. `e.preventDefault()` tarayıcının kendi GET isteğini keser. Bu satır olmazsa adres query string ile yenilenir ve `apiFetch` yarıda kalır.

```16:20:web/src/RegisterPage.tsx
  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccess('')
```

Gövde dört alandır. Görünen ad boş string ise `|| null` onu `null` yapar. Doluysa yazılan metin gider.

```35:44:web/src/RegisterPage.tsx
      const response = await apiFetch('/api/auth/register', {
        method: 'POST',
        auth: false,
        body: {
          userName,
          email,
          displayName: displayName || null,
          password,
        }
      })
```

`auth: false`, bu çağrıya Bearer koyma demektir. `HttpClient` ile aynı iş, JSON body yazıp `Authorization` header'ını eklememektir.

## apiFetch ve CORS

`apiFetch` varsayılanında `auth` true'dur ve `localStorage` içindeki access token'ı `Authorization: Bearer` diye yazar. Register bu varsayılanı kapatır.

```85:112:web/src/api.ts
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
```

`auth` false olduğu için `if (auth)` bloğuna girilmez. Giden header `Content-Type: application/json` olur. `Record<string, string>` burada C# `record` tipi değildir. Anahtar ve değer tutan bir sözlüktür.

Sayfa `localhost:5173` üzerindedir, API `https://localhost:7275` üzerindedir. Tarayıcı bunu başka origin sayar ve isteği CORS kontrolüne sokar. API şu origin'lere izin verir.

```28:40:ReactBattleArena/ReactBattleArena.Api/Program.cs
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
```

Scalar aynı POST'u API'nin kendi adresinden atar. O çağrı bu origin listesine takılmaz. Gövde ve Bearer'sız gitme kuralı aynıdır.

`apiFetch`, cevap `401` ise ve `auth` true ise `POST /api/auth/refresh` dener. Register'da `auth` false olduğu için bu dal çalışmaz. Kayıt cevabı `401` beklemez. Access token bitince yenileme, oturumu uzatma işidir.

```129:131:web/src/api.ts
  if (response.status !== 401 || auth === false) {
    return response
  }
```



## Pipeline ve controller

İstek API'ye girince middleware sırası şöyledir. Kimlik bakışı yetki bakışından önce gelir.

```90:100:ReactBattleArena/ReactBattleArena.Api/Program.cs
app.UseFluentValidationExceptionHandler();
app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

`UseAuthentication`, JwtBearer ile `Authorization: Bearer` arar. Bu istekte o header yoktur. `HttpContext.User` boş kalır. Hemen ardından `UseAuthorization` çalışır. Register metodu `[AllowAnonymous]` taşıdığı için boş kullanıcı burada `401` üretmez.

```29:41:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Register(
        [FromBody] RegisterRequest body,
        CancellationToken cancellationToken = default)
    {
        var id = await _mediator.Send(
            new RegisterCommand(body.UserName, body.Email, body.DisplayName, body.Password),
            cancellationToken);

        return Created($"/api/users/{id}", id);
    }
```

Sınıfın route'u `api/[controller]` olduğu için adres `api/auth/register` olur. `[FromBody]` JSON'u `RegisterRequest` alanlarına bağlar. ASP.NET Core alan adını büyük-küçük harf duyarsız okur. Frontend'in `userName` değeri `UserName` alanına düşer.

```3:9:ReactBattleArena/ReactBattleArena.Api/Contracts/RegisterRequest.cs
    public sealed class RegisterRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string Password { get; set; } = string.Empty;
    }
```

Controller kural yazmaz ve tabloya dokunmaz. `IMediator.Send` komutu Application katmanındaki handler'a taşır. `Created` HTTP `201` yazar, `Location` header'ına `/api/users/{id}` koyar, gövdeye de aynı `Guid` değerini basar.

## Komut ve 400

Komut dört alanı taşır. `IRequest<Guid>` handler'ın `Guid` döneceğini söyler.

```5:8:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommand.cs
public sealed record RegisterCommand(
    string UserName,
    string Email,
    string? DisplayName,
    string Password) : IRequest<Guid>;
```

`AddApplication` MediatR handler'larını, FluentValidation kurallarını ve her `Send` öncesinde çalışan `ValidationBehavior` tipini kaydeder.

```13:22:ReactBattleArena/ReactBattleArena.Application/Common/DependencyInjection.cs
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

Kayıt kuralları şunlardır. Kullanıcı adı ve e-posta boş olamaz. E-posta biçimi kontrol edilir. Görünen ad boş kalabilir. Parola boş olamaz, 6 ile 100 karakter arasındadır.

```10:14:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommandValidator.cs
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.DisplayName).MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
```

`ValidationBehavior` bu kuralları handler'dan önce çalıştırır. Hata varsa `ValidationException` atar ve `next()` çağrılmaz. `Users` tablosuna satır gitmez.

```16:36:ReactBattleArena/ReactBattleArena.Application/Common/ValidationBehavior.cs
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
```

`FluentValidationExceptionMiddleware` bu exception'ı yakalar. Status `400` olur, gövde `ValidationProblemDetails` olur. Hangi alanın kırıldığı `Errors` sözlüğündedir.

```16:42:ReactBattleArena/ReactBattleArena.Api/Middleware/FluentValidationExceptionMiddleware.cs
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => string.IsNullOrEmpty(g.Key) ? "_" : g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new ValidationProblemDetails
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Errors = errors
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
```

`Program.cs` bu middleware'i `UseFluentValidationExceptionHandler` ile pipeline'ın başına koyar. Exception controller'dan çıkınca HTTP cevabına burada döner.

## Handler, BCrypt, Users, UserRoles

Kurallar geçince `RegisterCommandHandler.Handle` çalışır. Önce aynı kullanıcı adı, sonra aynı e-posta var mı diye bakar. Varsa yine `ValidationException` atar. Middleware bunu da `400` yapar. Tabloya satır yazılmaz.

Dolu değilse parola `IPasswordHasher.Hash` ile gider. Düz metin `Users` satırına konmaz.

```22:49:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommandHandler.cs
    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var userNameTaken = await _db.Users.AnyAsync(u => u.UserName == request.UserName, cancellationToken);

        if (userNameTaken)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.UserName), "UserName is already taken.")
            });

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailTaken)
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
            DateTime.UtcNow);

        _db.Users.Add(entity);
```

Hasher sözleşmesi iki metot taşır. Kayıt `Hash` çağırır.

```3:8:ReactBattleArena/ReactBattleArena.Application/Abstractions/IPasswordHasher.cs
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password,string PasswordHash);
}
```

```7:12:ReactBattleArena/ReactBattleArena.Infrastructure/Security/BCryptPasswordHasher.cs
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
        // Aynı parola her Hash’te farklı string üretebilir (salt);
        // bu yüzden DB’de hash saklanır, login’de düz karşılaştırma yapılmaz.
    }
```

`BCrypt.Net.BCrypt.HashPassword` ürettiği string'in içine salt koyar. Aynı parola iki kayıtta aynı kolon değerini vermek zorunda değildir. Bu yüzden kayıt sırasında parola ile kolon `==` ile kıyaslanmaz. `Verify` girişi ilgilendirir. Orada müşterinin yazdığı parola, tablodaki `PasswordHash` ile kontrol edilir.

`AddInfrastructure` bu sınıfı `IPasswordHasher` olarak kaydeder. Handler somut BCrypt sınıfını tanımaz. Constructor `IPasswordHasher` ister, DI `BCryptPasswordHasher` verir.

> ```25:25:ReactBattleArena/ReactBattleArena.Infrastructure/DependencyInjection.cs
>         services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
> ```

`User.Create` yeni bir `Guid` üretir, puanı `0` yapar, `PasswordHash` alanına BCrypt çıktısını yazar. Entity'nin boş constructor'ı EF içindir. Uygulama kodu `Create` ile nesne kurar.

```16:40:ReactBattleArena/ReactBattleArena.Domain/Users/User.cs
    public string PasswordHash { get; private set; } = null!;

    // Arena / ödül için; Auth sonrası da kullanılacak
    public int Points { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static User Create(
        string userName,
        string email,
        string? displayName,
        string passwordHash,
        DateTime utcNow)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Points = 0,
            CreatedAtUtc = utcNow
        };
    }
```

EF eşlemesi tabloyu `Users` diye açar. Kullanıcı adı ve e-posta unique'dir. `PasswordHash` zorunludur ve en fazla 500 karakterdir. Validator'daki 100 karakterlik parola sınırı düz metin içindir. Kolondaki 500, BCrypt string'i içindir.

```11:21:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserConfiguration.cs
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DisplayName).HasMaxLength(100);

        builder.HasIndex(x => x.UserName).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
```

İlk `SaveChangesAsync` kullanıcıyı yazar. Ondan sonra handler `Roles` tablosunda adı `Player` olan satırı arar. Bulduğu rolün `Id` değeri ile yeni kullanıcının `Id` değerinden bir `UserRoles` satırı kurar. İkinci `SaveChangesAsync` bu bağı yazar. Dönüş, kullanıcının `Guid` değeridir.

```51:59:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommandHandler.cs
        await _db.SaveChangesAsync(cancellationToken);

        var playerRole = await _db.Roles.SingleAsync(
            r => r.Name == Roles.Player, cancellationToken);
        //Rol yoksa (seed çalışmamış) sessizce geçme, patlat ki fark edesin.

        _db.UserRoles.Add(UserRole.Create(entity.Id, playerRole.Id));
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;
```

Aranan ad sabittir. `"Player"` metni `Roles.Player` içindedir. Handler yeni bir `Role` satırı açmaz. Var olan satırın `Id` değerini kullanır.

```5:9:ReactBattleArena/ReactBattleArena.Domain/Authorization/Roles.cs
public static class Roles
{
    public const string Admin = "Admin";
    public const string Player = "Player";
    public const string ShopOwner = "ShopOwner";
```

`UserRole` iki kolon tutar.

```9:18:ReactBattleArena/ReactBattleArena.Domain/Authorization/UserRole.cs
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
    }
```

Birincil anahtar bu ikilidir. Aynı kullanıcıya aynı rol ikinci kez yazılamaz. Kullanıcı silinirse onun `UserRoles` satırları da silinir. Rol satırı, kendisine bağlı `UserRoles` varken silinemez.

```13:26:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserRoleConfiguration.cs
        builder.ToTable("UserRoles");
        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
```

Migration'ın `Up` metodu aynı tabloyu ve aynı yabancı anahtarları açar. `Down` bu tabloyu düşürür.

```62:83:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260821102302_AddRbacTables.cs
            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
```

`Role` satırının kendisi `Roles` tablosundadır. `Name` zorunludur ve unique'dir. Register bu satırı okur.

```11:14:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/RoleConfiguration.cs
        builder.ToTable("Roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Name).IsUnique();
```

`SingleAsync` `Player` satırını bulamazsa exception fırlatır. Bu `ValidationException` değildir. Middleware onu `400` yapmaz. `Roles` tablosunda `Player` yoksa kayıt `500` olur. Handler'daki yorum da bunu söyler: rol yoksa sessizce geçme.

Player'ın hangi fiile sahip olduğu, `RolePermissions` satırı ve permission kodu bu cevapta yoktur. O bağ karakter ekleme işindedir.

## Cevabın ekrana dönüşü

`201` için `response.ok` doğrudur. Sayfa gövdedeki `Guid` değerini okumaz ve `localStorage`'a bir şey yazmaz. Adres `/login` olur.

```46:55:web/src/RegisterPage.tsx
      if (!response.ok) {
        setError('Kayıt başarısız (kullanıcı/email dolu veya validation)')
        return
      }

      setSuccess('Kayıt OK — şimdi giriş yap')
      navigate('/login')
    } catch {
      setError('API’ye ulaşılamadı (backend çalışıyor mu?)')
    }
```

`400` ve `500` için `response.ok` yanlıştır. Sayfa `ValidationProblemDetails` gövdesini açmaz. Müşteri tek cümle görür: kayıt başarısız. Ağ yoksa, sertifika veya CORS yüzünden `fetch` reddederse `catch` çalışır. O dalda HTTP cevabı yoktur.

`{error && <p>{error}</p>}` boş string'te paragraf basmaz. `setError` dolu metin yazınca paragraf görünür. Aynı kalıp `success` için de vardır.

```104:108:web/src/RegisterPage.tsx
      {error && <p>{error}</p>}
      {success && <p>{success}</p>}
      <p>
        <Link to="/login">Girişe dön</Link>
      </p>
```



## Bu kod yanlış kullanılırsa

`e.preventDefault()` unutulursa tarayıcı formu kendi yönteminde gönderir. `apiFetch` çalışmaz, API'ye JSON gitmez.

`auth: true` bırakılırsa ve `localStorage`'da eski bir access token duruyorsa istek Bearer taşır. Register metodu `[AllowAnonymous]` olduğu için bu header'ı şart koşmaz. Kayıt yine de başka bir oturumun token'ı ile gider. Bu çağrı `auth: false` ile tokensız gider.

`UseAuthentication` ile `UseAuthorization` yer değiştirirse kimlik daha `HttpContext.User` üzerine yazılmadan yetki bakılır. Bearer taşıyan isteklerde `[Authorize]` herkesi boş kullanıcı sanıp `401` verir. Register'da `[AllowAnonymous]` bu hatayı gizleyebilir. Sıra yine önce kimlik, sonra yetkidir.

Parola 6 karakterden kısaysa `ValidationBehavior` handler'ı durdurur. BCrypt çalışmaz, tabloya satır gitmez. Cevap `400` olur. Ekran hangi alanın kırıldığını yazmaz, çünkü gövdeyi okumaz. Scalar aynı cevapta `Errors.Password` alanını gösterir.

İki `SaveChangesAsync` ayrı iştir. Birincisi `Users` satırını yazar, ikincisi `UserRoles` satırını. İkinci kayıt patlarsa kullanıcı tabloda kalır, `Player` bağı yazılmamış olur. Aynı adla yeniden denemek `400` döner, çünkü kullanıcı adı artık doludur.

`AnyAsync` kontrolü ile unique index yarışabilir. İki istek aynı anda "bu ad yok" görür. Biri insert eder, diğeri unique index'ten exception alır. Bu exception `ValidationException` olmadığı için middleware onu `400` yapmaz.

`PasswordHash` kolonuna düz parola yazılırsa girişteki `Verify` bu string'i BCrypt hash'i sanar ve parolayı kabul etmez. Kayıt `Hash` çıktısını yazar.

`setSuccess` ile `navigate('/login')` art arda durur. Adres hemen değişir. "Kayıt OK" cümlesi kayıt sayfasında kalmaz. Müşteri giriş formunu görür.

Üye olunca `Users` satırında BCrypt `PasswordHash` vardır, `UserRoles` satırı kullanıcıyı `Player` rolüne bağlar, tarayıcıda token yoktur. Sıradaki iş giriştir.

# Giriş yap

Müşteri kayıtlıdır. Tarayıcıda `https://localhost:5173/login` adresini açar, kullanıcı adını veya e-postayı ve parolayı yazar. İstek `POST /api/auth/login` adresine gider. Bu istekte `Authorization: Bearer` yoktur. API parolayı `Users.PasswordHash` ile doğrularsa tek cevapta iki string döner: access token ve refresh token. Access token JWT'dir, tabloda satırı yoktur. Refresh token'ın ham hali cevapta ve tarayıcıdadır. Tabloda `TokenHash` kolonu vardır. O kolon, ham token'ın SHA256 hash'idir.

Sayfa ikisini de `localStorage`'a yazar ve `/characters` adresine gider. O adres `AppLayout` içindedir. Layout, access token'ı `GET /api/auth/me` isteğinin `Authorization: Bearer` header'ına koyar. `UseAuthentication` JwtBearer ile bu header'ı okur, `UseAuthorization` `[Authorize]` ile kimliği şart koşar.

Aynı POST'u Scalar da atabilir. Gövde aynıdır, Bearer yine yoktur. `200` gelince access token Scalar'da Bearer alanına yapıştırılır. Sonraki korumalı çağrı onu header'da taşır.

İstek şu sırayla yürür:

1. `LoginPage` formu toplar, `apiFetch` çağırır (`auth: false`).
2. `apiFetch` JSON gövdeyi `https://localhost:7275/api/auth/login` adresine POST eder. `Authorization` header'ı yazılmaz.
3. Pipeline: CORS, `UseAuthentication`, `UseAuthorization`. Metotta `[AllowAnonymous]` vardır. Boş kullanıcı `401` üretmez.
4. `AuthController.Login` gövdeyi `LoginCommand` yapar, MediatR'a verir.
5. `ValidationBehavior` kuralları çalıştırır. Kural bozuksa handler çalışmaz, cevap `400` olur.
6. Handler kullanıcıyı bulur, BCrypt `Verify` çağırır. Kullanıcı yoksa veya parola uymazsa `null` döner, controller `401` yazar. Token üretilmez.
7. İkisi de tutarsa `JwtTokenService` access token basar. `RefreshTokenGenerator` 32 byte üretir, ham hali cevapta kalır, SHA256 hash'i `RefreshTokens.TokenHash` kolonuna yazılır.
8. Cevap `200` ve `LoginResult` olur. Sayfa `data.token` ve `data.refreshToken` değerlerini `localStorage`'a koyar.
9. Adres `/characters` olur. `AppLayout` token yoksa girişe döner. Token varsa `GET /api/auth/me` atar. Bu çağrıda Bearer vardır. JwtBearer imza, issuer, audience ve süreyi kontrol eder. `[Authorize]` geçerse `200` gelir.

`HasPermission`, sayfa kapısı ve buton gizleme bu cevapta bitmez. Müşteri karakter eklerken durur. Girişte bilinen şey şudur: access token kim olduğunu taşır, fiil listesi JWT'nin içinde değildir.

## Sayfa

Form iki alanı `useState` ile tutar.

```7:10:web/src/LoginPage.tsx
  const navigate = useNavigate()
  const [userNameOrEmail, setUserNameOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
```

Kullanıcı adı kutusu controlled input'tur. Metin state'te durur. Parola kutusunda `type="password"` vardır. Ekranda gizlenir, state'te ve JSON'da düz metin durur.

```70:84:web/src/LoginPage.tsx
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
```

`e.preventDefault()` tarayıcının kendi form GET'ini keser. Ardından hata metni temizlenir ve `apiFetch` çağrılır. `auth: false` bu isteğe Bearer koymaz. Gövde `userNameOrEmail` ve `password` alanlarıdır.

```12:15:web/src/LoginPage.tsx
  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
```

```40:47:web/src/LoginPage.tsx
      const response = await apiFetch('/api/auth/login',{
        method: 'POST',
        auth: false,
        body: {
          userNameOrEmail,
          password
        },
      })
```

`HttpClient` karşılığı, JSON body yazıp `Authorization` header'ı eklememektir. Üye ol ile aynı `apiFetch` kullanılır. Fark, adres ve gövdedir.

## Controller'a kadar

`apiFetch` içinde `auth` false olduğu için `if (auth)` bloğuna girilmez. Giden header `Content-Type: application/json` olur.

```104:111:web/src/api.ts
  if (auth) {// Login yaparken auth(false) olmadığı için buraya girmez
    // Backend'e henüz gidilmedi. JWT bu satırda Api'den gelmez.
    // Login'de gelmişti: POST /api/auth/login → data.token → setToken → localStorage 'token'.
    // Şimdi çekmeceden okuyoruz, header'a yazıyoruz, ONDAN SONRA alttaki fetch gider.
    const token = getToken()
    if (token) {
      headers.Authorization = `Bearer ${token}`
    }
  }
```

Yanlış parola da `401` döner. Bu, yukarıdaki `if (auth)` ile aynı `if` değildir. O blok isteği göndermeden önce Bearer yazar. Aşağıdaki `if` cevap geldikten sonra çalışır.

```129:134:web/src/api.ts
  if (response.status !== 401 || auth === false) {
    return response
  }

  //apiFetch, cevap 401 ise ve auth true ise POST /api/auth/refresh dener. Register'da auth false olduğu için bu dal çalışmaz. Kayıt cevabı 401 beklemez. Access token bitince yenileme, oturumu uzatma işidir.
  const refreshed = await refreshSession()
```

`||` iki taraftan biri true ise içeri girer ve `return response` çalışır. `refreshSession()` bir alt satırdadır. O satıra ancak bu `if` false olursa inilir.

Login'de yanlış parolanın değerleri şunlardır: `response.status` 401, `auth` false.

`401 !== 401` false olur. `auth === false` true olur. `false || true` true olur. `if` içeri girer, `401` cevabını sayfaya geri verir. `refreshSession()` çağrılmaz. Sayfa `Giriş başarısız` yazar.

`refreshSession()` için iki tarafın da false olması gerekir. Status 401 olacak, yani `status !== 401` false kalacak. `auth` true olacak, yani `auth === false` da false kalacak. `false || false` false olur, `return` çalışmaz, alttaki `refreshSession()` çalışır. Login `auth: false` gönderdiği için bu ikili oluşmaz.

Pipeline kayıt ile aynı sıradadır. Önce `UseAuthentication`, sonra `UseAuthorization`. Login metodu `[AllowAnonymous]` taşır. Header'da Bearer olmadığı için `HttpContext.User` boş kalır ve bu metot `401` vermez.

```45:58:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResult>> Login(
    [FromBody] LoginRequest body,
    CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new LoginCommand(body.UserNameOrEmail, body.Password),
            cancellationToken);

        return result is null ? Unauthorized() : Ok(result);
    }
```

Adres `api/auth/login` olur. `[FromBody]` JSON'u `LoginRequest` alanlarına bağlar. ASP.NET Core alan adını büyük-küçük harf duyarsız okur.

```3:7:ReactBattleArena/ReactBattleArena.Api/Contracts/LoginRequest.cs
public sealed class LoginRequest
{
    public string UserNameOrEmail { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
```

Controller kullanıcı aramaz ve token basmaz. `null` gelirse `Unauthorized()` HTTP `401` yazar, gövde boştur. Dolu gelirse `Ok(result)` HTTP `200` yazar.

Komut iki alan taşır. Dönüş `LoginResult?` olduğu için handler `null` dönebilir.

```5:13:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LoginCommand.cs
public sealed record LoginCommand(string UserNameOrEmail, string Password)
    : IRequest<LoginResult?>;

public sealed record LoginResult(
    Guid UserId,
    string UserName,
    string Email,
    string Token,
    string RefreshToken);
```

`LoginResult` içindeki `Token` access token'dır. `RefreshToken` ham refresh token'dır. İkisi de bu record'da string'dir. Ayrı cevap sınıfları yoktur. `Contracts/LoginResponse.cs` bu metotta kullanılmaz. `Ok(result)` bu record'u yazar.

ASP.NET Core JSON alan adlarını camelCase basar. Sayfanın okuduğu adlar `token` ve `refreshToken` olur.

## 400 ve 401

Handler'dan önce `LoginCommandValidator` çalışır. Kullanıcı adı veya e-posta boş olamaz. Parola boş olamaz, 6 ile 100 karakter arasındadır.

```8:11:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LoginCommandValidator.cs
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserNameOrEmail).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
```

Kural bozulursa `ValidationBehavior` `ValidationException` atar. Middleware bunu `400` ve `ValidationProblemDetails` yapar. Handler çalışmaz. Ne JWT ne `RefreshTokens` satırı oluşur.

Kurallar geçince handler `Users` tablosunda kullanıcı adı veya e-posta eşleşen satırı arar. Yoksa `null` döner. Varsa BCrypt `Verify`, yazılan parolayı satırdaki `PasswordHash` ile kontrol eder. Uymazsa yine `null` döner.

```28:39:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LoginCommandHandler.cs
    public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(
                u => u.UserName == request.UserNameOrEmail || u.Email == request.UserNameOrEmail,
                cancellationToken);

        if (user is null)
            return null;

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;
```

```18:21:ReactBattleArena/ReactBattleArena.Infrastructure/Security/BCryptPasswordHasher.cs
    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
```

İki başarısızlık da `null` olduğu için controller ikisine de `401` verir. Cevap, bu e-postanın kayıtlı olup olmadığını ayırmaz. BCrypt karşılaştırması `==` değildir. Kayıttaki `Hash` her çağrıda farklı string üretebilir, çünkü çıktının içinde salt vardır. `Verify` o salt'ı hash string'inin içinden okur.

## Access token

Parola tutunca handler iki token üretir. Access token `IJwtTokenService.CreateToken` ile gelir. Bu string veritabanına yazılmaz.

```47:57:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LoginCommandHandler.cs
        var token = _jwtTokenService.CreateToken(user);

        var utcNow = DateTime.UtcNow;
        var (rawRefresh, hash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(
            RefreshToken.Create(user.Id, hash, expires, utcNow));

        await _db.SaveChangesAsync(cancellationToken);

        return new LoginResult(user.Id, user.UserName, user.Email, token, rawRefresh);
```

`CreateToken` dört claim yazar: `sub`, `unique_name`, `email` ve `ClaimTypes.NameIdentifier`. `sub` ile `NameIdentifier` aynı kullanıcı `Guid` değeridir. Permission kodu ve rol adı bu diziye konmaz.

```20:40:ReactBattleArena/ReactBattleArena.Infrastructure/Security/JwtTokenService.cs
    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
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

İmza HMAC-SHA256'dır. Anahtar `Jwt:Key` değeridir. Sunucu token'ı bir tabloda aramaz. Gelen string'i aynı anahtar ile doğrular. Süre `ExpireMinutes` kadar sonradır. `JwtOptions` içinde bu değerin varsayılanı 60 dakikadır. Issuer ve audience da configuration'daki `Jwt` section'ından okunur. `SectionName` sabiti o section'ın adıdır: `"Jwt"`.

```3:14:ReactBattleArena/ReactBattleArena.Infrastructure/Security/JwtOptions.cs
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpireMinutes { get; set; } = 60;
    public int RefreshExpireDays { get; set; } = 7;
```

`AddInfrastructure`, configuration'daki `Jwt` section'ını `JwtOptions` sınıfına bağlar ve `JwtTokenService`'i kaydeder. `GetSection("Jwt")` appsettings içindeki `Jwt` nesnesini alır. `Configure<JwtOptions>` o nesnenin `Key`, `Issuer`, `Audience`, `ExpireMinutes` ve `RefreshExpireDays` alanlarını aynı adlı property'lere yazar. `Jwt:Key` boşsa API açılmaz. JwtBearer aynı anahtar, issuer, audience ve süreyi doğrular.

```27:29:ReactBattleArena/ReactBattleArena.Infrastructure/DependencyInjection.cs
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
```

```54:68:ReactBattleArena/ReactBattleArena.Api/Program.cs
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
```

`AddAuthentication` şemayı JwtBearer yapar. `AddJwtBearer` doğrulama kuralını kaydeder. Bu iki satır DI kaydıdır. İsteğin üstünden geçen middleware `UseAuthentication` ve ondan sonra `UseAuthorization` satırlarıdır. `ValidateLifetime` true olduğu için süresi dolmuş access token geçersiz kalır. O durumda `[Authorize]` `401` verir. Yenisini almak refresh token ile olur. O çağrı oturumu uzatma işidir.

BCrypt parola hash'ler. Bu işlem `BCryptPasswordHasher.Hash` içindedir. Sonuç `Users.PasswordHash` kolonuna yazılır. JWT imzası `JwtTokenService.CreateToken` içindeki `SigningCredentials` satırındadır. Algoritma HMAC-SHA256'dır, anahtar `Jwt:Key` değeridir. Bu imzalı string tabloya yazılmaz.

## Refresh token ve TokenHash

Access token basıldıktan hemen sonra refresh token üretilir. `Create` bir tuple döner. Tuple'daki adlar `Raw`, `Hash` ve `ExpiresAtUtc` olur. `Raw` ham refresh token'dır. Handler bu üç değeri şöyle karşılar: `rawRefresh`, `hash`, `expires`. `rawRefresh` yeni bir token değildir. `Raw` ile gelen aynı string'in handler içindeki adıdır.

```18:29:ReactBattleArena/ReactBattleArena.Infrastructure/Security/RefreshTokenGenerator.cs
    public string Hash(string raw)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    public (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes);
        var hash = Hash(raw); //Böylece iki yerde iki ayrı formül kalma riski kalkıyor.
        var expires = utcNow.AddDays(_options.RefreshExpireDays);
        return (raw, hash, expires);
    }
```

`RandomNumberGenerator.GetBytes(32)` 32 byte üretir. Ham token bu byte'ların Base64 yazımıdır. `Hash` aynı byte dizisini UTF-8 diye değil, ham string'in UTF-8 byte'larını SHA256'dan geçirir. `SHA256.HashData` 32 byte döner. `Convert.ToHexString` bunu 64 karakterlik hex yapar. Kolon uzunluğu da 64'tür.

Bu hash BCrypt değildir. BCrypt her çağrıda yeni bir salt koyduğu için aynı ham token ikinci kez farklı string verirdi. Login'de yazılan `TokenHash` ile sonraki istekteki arama birbirini bulamazdı. SHA256 aynı girdiye aynı hex'i verir. Formül `Hash` metodunda tek yerde durur. Arayüz de bunu söyler: login ve refresh aynı SHA256'yı kullanır.

```3:9:ReactBattleArena/ReactBattleArena.Application/Abstractions/IRefreshTokenGenerator.cs
public interface IRefreshTokenGenerator
{
    string Hash(string Raw);
    // Login ve refresh aynı SHA256'yı kullansın. BCrypt değil — her seferinde farklı tuz üretir, unique index araması bozulur.
    // Refresh isteği ham token'ı gönderecek, sunucu onu hash'leyip TokenHash kolonundan arayacak.
    // Aramanın çalışması için hash formülünün login ile birebir aynı olması şart; o yüzden formülü tek metoda topluyoruz.
    (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow);
}
```

Handler ham değeri `LoginResult.RefreshToken` alanına koyar. Hash'i entity'ye verir. Ham token `RefreshTokens` tablosuna yazılmaz.

```9:35:ReactBattleArena/ReactBattleArena.Domain/Authentication/RefreshToken.cs
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime utcNow)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = utcNow
        };
    }
```

`Create` `RevokedAtUtc` atamaz. Yeni satırda bu alan boştur. `Revoke` doluysa ikinci kez yazmaz. Boşsa `utcNow` yazar. Çıkış bu metodu çağırır. Giriş çağırmaz.

```37:43:ReactBattleArena/ReactBattleArena.Domain/Authentication/RefreshToken.cs
    public void Revoke(DateTime utcNow)
    {
        if (RevokedAtUtc is not null)
            return;

        RevokedAtUtc = utcNow;
    }
```

EF tabloyu `RefreshTokens` diye açar. `TokenHash` zorunlu, en fazla 64 karakter ve unique'dir. Aynı hash iki satıra yazılamaz. `UserId` için ayrıca index vardır. Kullanıcı silinirse onun refresh token satırları da silinir.

```11:23:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/RefreshTokenConfiguration.cs
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.TokenHash).IsUnique();

        builder.HasIndex(x => x.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
```

Migration'ın `Up` metodu aynı kolonları açar. `Down` tabloyu düşürür.

```14:45:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260828163543_AddRefreshTokens.cs
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);
```

Süre `RefreshExpireDays` kadardır. Varsayılan 7 gündür. Access token 60 dakika, refresh token satırı 7 gün. İkisi aynı saat değildir. `SaveChangesAsync` yalnız refresh token satırını yazar. JWT bu `SaveChanges` ile diske gitmez. Kayıt başarılı olursa `LoginResult` hem JWT'yi (`Token`) hem ham refresh token'ı (`RefreshToken`) taşır. Bu ikinci alan handler'daki `rawRefresh` değişkenidir. `SaveChanges` patlarsa `return` satırına gelinmez, `200` gitmez, `rawRefresh` de cevapta yer almaz.

Her başarılı giriş yeni bir `RefreshTokens` satırı ekler. Eski satırı silmez. İki tarayıcı iki ham token tutabilir. Eski satırın iptali ve yenisiyle değişmesi oturumu uzatma işidir.

## Tarayıcının yazdığı yer

`200` için `response.ok` doğrudur. Sayfa JSON'u açar. `setToken` access token'ı `localStorage` anahtarı `token` ile yazar. `setRefreshToken` ham refresh token'ı `refreshToken` anahtarı ile yazar. Sonra adres `/characters` olur.

```49:57:web/src/LoginPage.tsx
      if (!response.ok) {
        setError('Giriş başarısız')
        return
      }

      const data = await response.json()
      setToken(data.token)
      setRefreshToken(data.refreshToken)
      navigate('/characters')
```

```3:20:web/src/api.ts
export function getToken(): string | null {
  return localStorage.getItem('token')
}

export function setToken(token: string) {
  localStorage.setItem('token', token)
}

export function clearToken() {
  localStorage.removeItem('token')
  localStorage.removeItem('refreshToken')
}

export function getRefreshToken(): string | null {
  return localStorage.getItem('refreshToken')
}
export function setRefreshToken(token: string) {
  localStorage.setItem('refreshToken', token)
}
```

`400` ve `401` ikisi de `response.ok` değildir. Sayfa gövdeyi ayırmaz. Müşteri `Giriş başarısız` görür. Scalar'da `400` alan hatalarını, `401` boş gövdeyi ayrı gösterir. Ağ, sertifika veya CORS yüzünden `fetch` reddederse `catch` çalışır.

`/characters` rotası `AppLayout` altındadır.

```16:17:web/src/App.tsx
      <Route element={<AppLayout />}>
        <Route path="/characters" element={<CharactersPage />} />
```

Layout önce `getToken()` okur. Access token yoksa bileşen `Navigate` ile `/login` adresine döner. Liste çizilmez.

```13:15:web/src/AppLayout.tsx
  const token = getToken()
  const [permissions, setPermissions] = useState<string[]>([])
  const [meLoaded, setMeLoaded] = useState(false)
```

```17:24:web/src/AppLayout.tsx
  useEffect(() => {
  if (!token) {
    return
  }

  async function loadMe() {
    try {
      const meResponse = await apiFetch('/api/auth/me')
```

```44:46:web/src/AppLayout.tsx
  if (!token) {
    return <Navigate to="/login" replace />
  }
```

Bu iki `if` aynı anda çalışmaz. `AppLayout` fonksiyonu yukarıdan aşağı iner. `useEffect` satırına gelince `loadMe` çağrılmaz. React, fonksiyonu saklar. Saklanan fonksiyon, `return` ile ekran çizildikten sonra çalışır.

Çizim sırasında sıra şöyledir. `getToken()` `localStorage` anahtarı `token` değerini okur. Anahtar yoksa 44. satırdaki `if` girer ve `Navigate` ile `/login` adresine döner. `/me` bu çizimde atılmaz. Anahtar varsa 44. satır geçilir. `meLoaded` hâlâ false olduğu için ekrana `Yükleniyor…` yazılır.

Çizim bitince saklanan `useEffect` fonksiyonu çalışır. Onun ilk satırı da `if (!token) return` olur. Anahtar yoksa `loadMe` hiç çağrılmaz. Anahtar duruyorsa `loadMe` çalışır ve `GET /api/auth/me` atılır. `apiFetch` varsayılanı `auth: true` olduğu için header `Authorization: Bearer` ve ardından access token olur. Arada bir boşluk vardır. `Bearer` kelimesi şemanın adıdır.

## Bearer ve [Authorize]

`/me` login'den farklıdır. Login `[AllowAnonymous]` idi. `/me` `[Authorize]` taşır. JwtBearer header'ı doğrulamadan bu metot çalışmaz.

Sıra aynıdır: `UseAuthentication` token'ı `HttpContext.User` yapar, `UseAuthorization` `[Authorize]` ile o kullanıcıya bakar. İmza, issuer, audience veya süre tutmazsa kullanıcı boş kalır ve cevap `401` olur. Header yoksa da `401` olur.

```98:105:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
```

```110:124:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
        if (!Guid.TryParse(idValue, out var userId))
            return Unauthorized();

        var codes = await _permissions.GetCodesAsync(userId, cancellationToken);

        return Ok(new
        {
            id = userId,
            userName = User.Identity?.Name,
            email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value,
            permissions = codes
        });
    }
```

`Me` kimliği JWT'den okur. Önce `ClaimTypes.NameIdentifier`, yoksa `sub`. İkisi de login'de aynı `Guid` ile yazılmıştı. Bu değer `Guid` olmazsa cevap yine `401` olur. Kullanıcı adı `User.Identity.Name` üzerinden, e-posta `email` claim'i üzerinden okunur.

`permissions` JWT'den okunmaz. `GetCodesAsync` veritabanına gider. Yol `UserRoles` satırından role, oradan `RolePermissions` satırına, oradan `Permission.Code` değerine çıkar. Aynı kod iki kez gelse `Distinct` onu teke indirir.

```15:27:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserPermissionService.cs
    public async Task<IReadOnlyList<string>> GetCodesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from ur in _db.UserRoles
            join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
            join p in _db.Permissions on rp.PermissionId equals p.Id
            where ur.UserId == userId
            select p.Code
        ).Distinct().ToListAsync(cancellationToken);
        // Distinct: iki rol aynı fiili verse bir kez.
    }
```

Bu dizi cevabın parçasıdır. `AppLayout` onu `permissions` state'ine yazar. Sayfa kapısı, buton gizleme ve `[HasPermission]` aynı diziyi kullanır. Onların ekran ve attribute kodu karakter ekleme işindedir. Burada duran sonuç şudur: fiil listesi access token'ın claim'i değildir. Müşteri tekrar login olmak zorunda değildir. Access token geçerliyken `AppLayout` her `GET /api/auth/me` isteğinde `GetCodesAsync` join'i yeniden çalışır. Sayfa yenilenince bu istek tekrar gider. Tablodaki `RolePermissions` satırı o arada değişmişse aynı access token ile dönen `permissions` dizisi değişir.

`401` kimliktir. Access token yok, bozuk veya süresi dolmuş demektir. `403` bu metotta üretilmez. `/me` fiil aramaz. `AppLayout` `401` görürse token'ları siler ve girişe döner. `apiFetch` bu cevabı layout'a bırakmadan önce, `auth` true olduğu için bir kez yenileme dener. O denemenin rotation kuralı oturumu uzatma işidir.

```28:32:web/src/AppLayout.tsx
      } else if (meResponse.status === 401) {
        clearToken()
        navigate('/login')
        return
      }
```

Karakter listesinin GET'i ayrıdır. `GetPaged` üzerinde `[Authorize]` yoktur. Scalar'dan Bearer'sız `GET /api/characters` bu koda göre `401` beklemez. Ekranda listeyi görmek için layout yine `token` anahtarını şart koşar. O şart sayfanın kendisindedir, bu GET metodunda değildir.

```24:32:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HttpGet]
    [ProducesResponseType(typeof(PagedCharacterRowsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedCharacterRowsResult>> GetPaged(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCharactersQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }
```



## Bu kod yanlış kullanılırsa

`auth: true` ile login atılırsa ve tarayıcıda eski bir access token duruyorsa istek Bearer taşır. Login `[AllowAnonymous]` olduğu için bunu şart koşmaz. Asıl karışma `401` dalındadır. Yanlış parola `401` döner. `auth` true ise `apiFetch` bunu bitmiş oturum sanıp `POST /api/auth/refresh` dener. Login `auth: false` ile gider, yanlış parola düz `401` kalır.

Kullanıcı yokken `404`, yanlış parolada `401` dönmek, o e-postanın kayıtlı olduğunu söyler. Handler ikisinde de `null` döner. Controller ikisine de `401` yazar.

`UseAuthorization` `UseAuthentication`'dan önce yazılırsa Bearer gelen istekte `[Authorize]` kullanıcıyı boş görür. `/me` her seferinde `401` verir. Sıra önce kimlik, sonra yetkidir.

Access token `localStorage` yerine adres çubuğuna konursa sunucu ve tarayıcı log'una düşer. Bu proje onu `Authorization` header'ında taşır.

`Jwt:Key` kısa kalırsa HMAC zayıf kalır. Anahtar sızarsa aynı issuer ve süreyle access token basılır. API o string'i kendi bastığı token'dan ayırmaz, çünkü token tabloda aranmaz, imza ile doğrulanır.

JWT'nin içine permission kodu yazılırsa kod tablodan alınmış gibi görünür. Bu projede claim dizisinde o kod yoktur. `POST /api/auth/login` bu join'i çalıştırmaz. Join, access token ile giden her `GET /api/auth/me` isteğinde çalışır.

Refresh token BCrypt ile hash'lenirse `TokenHash` unique araması bozulur. Aynı ham token ikinci istekte başka hex üretirdi. Kolonda SHA256 hex durur.

Ham refresh token kolona da yazılırsa veritabanını okuyan kişi oturumu uzatabilir. `Create` kolona hash verir, ham değeri yalnız `LoginResult` ile tarayıcıya bırakır.

`data.token` yazılır da `data.refreshToken` unutulursa access token `localStorage`'da kalır, yenileme için ham token yoktur. Login ikisini birden yazar.

`setToken` sonrası `navigate` unutulursa token durur, müşteri login formunda kalır. Layout'a geçilmediği için `/me` de atılmaz.

Giriş tutunca tarayıcıda access token ve ham refresh token vardır, `RefreshTokens` satırında SHA256 `TokenHash` vardır, `/me` bu access token'ı Bearer ile gönderir ve `[Authorize]` kimliği kabul eder. Fiil listesi JWT'de değildir, join'den gelir. Sıradaki iş karakter eklemektir.

# Karakter ekle

Müşteri yeni bir karakter yazmak ister. İstek `POST /api/characters` olur. Header'da `Authorization: Bearer` ve access token vardır. API önce kim olduğuna, sonra `characters.create` kodunun onda olup olmadığına bakar. Kod varsa karakter yazılır ve cevap `201` olur. Kod yoksa cevap `403` olur. Ekran aynı kararı iki yerde uygular: listedeki Ekle linki ve `/characters/new` sayfasının kendisi.

İstek şu sırayla yürür:

1. Liste sayfası `hasPermission` ile `characters.create` koduna bakar. Kod dizide yoksa Karakter ekle linki çizilmez. Bu çizim `POST` atmaz.
2. Adres elle `/characters/new` yazılırsa `CharacterCreatePage` aynı diziye bakar ve `/characters` adresine döner. Bu `if` de `POST` atmaz.
3. Form `apiFetch` ile `POST /api/characters` atar. `auth` yazılmaz. Varsayılan true olduğu için Bearer gider.
4. Access token bitmişse `apiFetch` bu `401` üzerinde `POST /api/auth/refresh` dener. `403` bu dala girmez.
5. Pipeline önce `UseAuthentication`, sonra `UseAuthorization` çalışır. `[HasPermission(PermissionCodes.CharactersCreate)]` policy adını `Permission:characters.create` yapar. Policy `RequireAuthenticatedUser` ile kimliği de şart koşar.
6. Aynı `PermissionAuthorizationHandler` `GetCodesAsync` ile `UserRoles`, `RolePermissions` ve `Permissions` tablolarını okur. Listede `characters.create` varsa `Succeed` çağrılır.
7. `ValidationBehavior` kuralları bozulursa cevap `400` olur. Handler çalışmaz.
8. Handler `Character.Create` ile satır kurar, `SaveChangesAsync` yazar, controller `201` ve `Guid` döner.
9. Sayfa `201` görünce `/characters` adresine gider. `403` gelirse `Yetkin yok` yazar.

Karakter eklemek için join'in `characters.create` kodunu bulması gerekir. Join iki tablodan geçer. `UserRoles` kullanıcının hangi role bağlı olduğunu söyler. `RolePermissions` o rolün hangi koda bağlı olduğunu söyler. Bu iki satırı yazan metotlar ayrıdır.

`UserRoles` satırını yazan metot `RegisterCommandHandler.Handle` olur. Yeni kullanıcının `Id` değeri ile `Player` rolünün `Id` değerini alır.

```54:61:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommandHandler.cs
        var playerRole = await _db.Roles.SingleAsync(
            r => r.Name == Roles.Player, cancellationToken);

        _db.UserRoles.Add(UserRole.Create(entity.Id, playerRole.Id));
        await _db.SaveChangesAsync(cancellationToken);
```

`RolePermissions` satırını yazan metot `AuthSeeder.EnsureRolePermissionAsync` olur. `SeedAsync` API açılırken onu çağırır. `Admin` rolüne `characters.create` bağlanır. `Player` rolü için bu çağrı yoktur.

```22:28:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersCreate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersUpdate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersDelete, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.ShopItemsCreate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.UsersDelete, cancellationToken);

        await EnsureRolePermissionAsync(db, Roles.ShopOwner, PermissionCodes.ShopItemsCreate, cancellationToken);
```

Register kullanıcının `UserRoles.RoleId` kolonuna `Player` yazar. Seed `Player` için `RolePermissions` satırı açmadığı için join `Player` rolüne gelir ve `characters.create` bulamaz. Yeni kayıt olan müşteri karakter ekleyemez. Kullanıcının `UserRoles` satırındaki `RoleId` değeri `Admin` rolünün `Id` değeri olursa join seed'in açtığı `RolePermissions` satırına gelir, kodu bulur ve o müşteri ekler. `Admin` için `UserRoles` satırı yazan bir metot yoktur. O satır tabloda ayrıca durur.

Bu işte HTTP isteğinden önce duran kayıtlar şunlardır. API açılırken `CreateScope` ile `AuthSeeder.SeedAsync` çalışır. Bu bir müşteri isteği değildir. `Roles`, `Permissions` ve `RolePermissions` satırlarını yoksa yazar.

## Tablolar ve seed

Dört tablo vardır. `Roles` rolün adını tutar. `Permissions` fiilin kodunu tutar. `UserRoles` bir kullanıcıyı bir role bağlar. `RolePermissions` bir role bir fiil bağlar.

```14:32:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260821102302_AddRbacTables.cs
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
```

`RolePermissions` birincil anahtarı `RoleId` ve `PermissionId` ikilisidir. Aynı role aynı fiil ikinci kez yazılamaz.

```39:47:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260821102302_AddRbacTables.cs
            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
```

Kodlar `PermissionCodes` içindedir. Karakter eklemenin kodu `characters.create` metnidir.

```3:9:ReactBattleArena/ReactBattleArena.Domain/Authorization/PermissionCodes.cs
public static class PermissionCodes
{
    public const string CharactersCreate = "characters.create";
    public const string CharactersUpdate = "characters.update";
    public const string CharactersDelete = "characters.delete";
    public const string ShopItemsCreate = "shop.items.create";
    public const string UsersDelete = "users.delete";
```

`Permission` satırında bu metin `Code` kolonuna yazılır. `RolePermission` yalnız iki `Guid` tutar: rolün `Id` değeri ve permission'ın `Id` değeri.

```13:19:ReactBattleArena/ReactBattleArena.Domain/Authorization/RolePermission.cs
    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        };
    }
```

API ayağa kalkınca seed çalışır.

```82:86:ReactBattleArena/ReactBattleArena.Api/Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await AuthSeeder.SeedAsync(db);
}
```

Seed üç rol yazar: `Admin`, `Player`, `ShopOwner`. Sonra permission kodlarını yazar. İlk `SaveChangesAsync` bu satırları kaydeder. Ardından bağları yazar. `Admin` rolüne `characters.create`, `characters.update`, `characters.delete`, `shop.items.create` ve `users.delete` bağlanır. `ShopOwner` rolüne yalnız `shop.items.create` bağlanır. `Player` için `EnsureRolePermissionAsync` çağrısı yoktur.

```10:28:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
        await EnsureRoleAsync(db, Roles.Admin, cancellationToken);
        await EnsureRoleAsync(db, Roles.Player, cancellationToken);
        await EnsureRoleAsync(db, Roles.ShopOwner, cancellationToken);

        await EnsurePermissionAsync(db, PermissionCodes.CharactersCreate, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.CharactersUpdate, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.CharactersDelete, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.ShopItemsCreate, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.UsersDelete, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersCreate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersUpdate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersDelete, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.ShopItemsCreate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.UsersDelete, cancellationToken);

        await EnsureRolePermissionAsync(db, Roles.ShopOwner, PermissionCodes.ShopItemsCreate, cancellationToken);
```

Bağ, rol adı ve kod metniyle aranır. İkisi de tabloda varsa ve bu ikili daha önce yazılmamışsa `RolePermissions` satırı eklenir.

```51:65:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
    private static async Task EnsureRolePermissionAsync(
        ApplicationDbContext db,
        string roleName,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleAsync(r => r.Name == roleName, cancellationToken);
        var permission = await db.Permissions.SingleAsync(p => p.Code == permissionCode, cancellationToken);

        var exists = await db.RolePermissions.AnyAsync(
            x => x.RoleId == role.Id && x.PermissionId == permission.Id,
            cancellationToken);

        if (!exists)
            db.RolePermissions.Add(RolePermission.Create(role.Id, permission.Id));
    }
```

Bir kullanıcıya fiil vermek için bu projede ayrı bir HTTP endpoint yoktur. İki satır vardır.

`UserRoles` satırını bu solution içinde yazan tek metot `RegisterCommandHandler.Handle` olur. Kod şudur.

```54:61:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommandHandler.cs
        var playerRole = await _db.Roles.SingleAsync(
            r => r.Name == Roles.Player, cancellationToken);

        _db.UserRoles.Add(UserRole.Create(entity.Id, playerRole.Id));
        await _db.SaveChangesAsync(cancellationToken);
```

`Roles` tablosunda adı `Player` olan satırı bulur. Yeni kullanıcının `Id` değeri ile o rolün `Id` değerinden bir satır kurar. `SaveChangesAsync` bu satırı `UserRoles` tablosuna yazar. `Admin` rolünün `Id` değerini yazan bir metot yoktur. O satır tabloda ayrıca durursa join `Admin` rolünün kodlarına gider.

`RolePermissions` satırını bu solution içinde yazan tek metot `AuthSeeder.EnsureRolePermissionAsync` olur. `SeedAsync` bunu API açılırken şu çağrılarla çalıştırır.

```22:30:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersCreate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersUpdate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersDelete, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.ShopItemsCreate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.UsersDelete, cancellationToken);

        await EnsureRolePermissionAsync(db, Roles.ShopOwner, PermissionCodes.ShopItemsCreate, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
```

`Player` rolü için bu metot çağrılmaz. Metot rolün `Id` değeri ile permission'ın `Id` değerinden satır kurar. Satır yoksa bir önceki bloktaki `RolePermissions.Add` satırı ekler. `SaveChangesAsync` tabloya yazar.

JWT'nin claim dizisinde bu kod yoktur. `GetCodesAsync` her kontrolde tabloları yeniden okur. `RolePermissions` veya `UserRoles` değişince aynı access token ile sonuç değişir. Yeni login gerekmez.

Var olan kodlarla yeni bir rol açmak da tablo işidir. `Roles` satırına yeni bir `Name` yazılır. `RolePermissions` satırları o rolün `Id` değerini, tabloda duran permission `Id` değerlerine bağlar. `PermissionCodes` içine yeni sabit eklenmez.

Yeni bir permission kodu kodla birlikte gelir. `PermissionCodes` içine sabit yazılır. Korunacak action'a `[HasPermission]` o sabitle konur. Ekrandaki `PERMISSIONS` nesnesine aynı metin yazılır. Seed `EnsurePermissionAsync` ile `Permissions` satırını, `EnsureRolePermissionAsync` ile rol bağını basar. Veritabanında kod durup action'da attribute durmuyorsa o action bu koda bakmaz.

Bu publish, yeni bir action içindir. `characters.create` zaten `[HasPermission]` ile `POST /api/characters` üzerinde durur. Bu kodu `Player` rolüne vermek veya bir kullanıcının `UserRoles` satırını `Admin` rolüne çevirmek tablo satırıdır. O satır değişince aynı publish ile sonraki istek join'i yeniden okur. Yeni bir HTTP metodu yoksa veritabanındaki yeni kodun bakacağı bir action da yoktur.

## Ekranda aynı dizi

`AppLayout`, access token varken `GET /api/auth/me` atar. Cevaptaki `permissions` dizisini state'e yazar. `meLoaded` true olunca `Outlet` çizilir. Karakter sayfaları bu diziyi kendi isteğiyle yeniden almaz. `PermissionContext.Provider` diziyi alta verir.

```48:50:web/src/AppLayout.tsx
  if (!meLoaded) {
  return <p>Yükleniyor…</p>
  }
```

```62:63:web/src/AppLayout.tsx
  return (
    <PermissionContext.Provider value={{ permissions }}>
```

```82:86:web/src/AppLayout.tsx
        <main className="app-main">
          <Outlet />
        </main>
    </div>
    </PermissionContext.Provider>
```

`usePermissions` bu diziyi okur. Çağrı `AppLayout` dışında yapılırsa hata verir, çünkü context orada doldurulur.

```9:14:web/src/PermissionContext.tsx
export function usePermissions(): string[] {
    const ctx = useContext(PermissionContext)
    if(!ctx){
        throw new Error('usePermissions yalnızca AppLayout içinde')
    }
    return ctx.permissions
}
```

Ekrandaki metinler backend sabitleriyle aynıdır.

```1:8:web/src/permissions.ts
export const PERMISSIONS = {
  charactersCreate: 'characters.create',
  charactersUpdate: 'characters.update',
  charactersDelete: 'characters.delete',
} as const

export function hasPermission(permissions: string[], code: string): boolean {
  return permissions.includes(code)
}
```

`hasPermission` dizide `characters.create` var mı diye bakar. Liste sayfası linki buna bağlar. Dizi bu kodu içermiyorsa link çizilmez.

```95:97:web/src/CharactersPage.tsx
          {hasPermission(permissions, PERMISSIONS.charactersCreate) && (
            <Link to="/characters/new">Karakter ekle</Link>
          )}
```

`&&` sol taraf false ise sağdaki `Link` basılmaz. Bu gizleme yetki kararı değildir. Müşteri adresi elle `/characters/new` yazabilir. Sayfa o zaman kendi kapısını çalıştırır.

`/characters/new` rotası `CharacterCreatePage` açar. Sayfa `usePermissions` ile aynı diziyi alır. Access token yoksa `/login` adresine döner. Dizi `characters.create` içermiyorsa `/characters` adresine döner. Form çizilmez.

```139:149:web/src/CharacterCreatePage.tsx
   if (!token) {
    return <Navigate to="/login" replace />
  }

  if (!hasPermission(permissions, PERMISSIONS.charactersCreate)){
    return <Navigate to="/characters" replace />
  }
```

Düzenle aynı dizinin ikinci kapısıdır. Detay sayfasında `characters.update` yoksa Düzenle linki çizilmez. Adres elle `/characters/{id}/edit` yazılırsa edit sayfası `/characters` adresine döner. PUT gövdesinin kendisi düzenleme işidir. Kapı bu işte durur, çünkü link ve sayfa aynı diziyi kullanır.

```148:154:web/src/CharacterDetailPage.tsx
          {hasPermission(permissions, PERMISSIONS.charactersUpdate) && (
            <Link to={`/characters/${id}/edit`}>Düzenle</Link>
          )}
          {hasPermission(permissions, PERMISSIONS.charactersDelete) && (
            <button type="button" onClick={handleDelete}>
              Sil
            </button>
          )}
```

```167:169:web/src/CharacterEditPage.tsx
  if (!hasPermission(permissions, PERMISSIONS.charactersUpdate)) {
    return <Navigate to="/characters" replace />
  }
```

Sil düğmesi de `hasPermission` ile `characters.delete` koduna bakar. DELETE isteğinin gövdesi silme işidir.

Üç yer aynı kod adına bakar ve üçü de aynı anda çalışmaz. Liste sayfasındaki `hasPermission` yalnız `Karakter ekle` linkini çizer. Bu çizim `POST` atmaz. `/characters/new` sayfasındaki `hasPermission` formun çizilip çizilmeyeceğine bakar. O `if` de `POST` atmaz. İkisi de `AppLayout`'un `/me` cevabından bellekte tuttuğu `permissions` dizisini okur. `Ekle` düğmesi basılınca `apiFetch` `POST /api/characters` atar. `HasPermission` o anda `GetCodesAsync` ile üç tabloyu okur: `UserRoles`, `RolePermissions`, `Permissions`. Kod metni `Permissions.Code` kolonundadır. Kullanıcıya ulaşması için `UserRoles` satırı bir role, `RolePermissions` satırı o rolü bu koda bağlamış olmalıdır. `Permissions` satırı dururken bu bağ silinirse dizi hâlâ `characters.create` içerir, join kodu döndürmez, API `403` verir. Sayfa bu status için `Yetkin yok` yazar.

## POST ve 403

Form `apiFetch` ile gider. `auth` yazılmaz. Varsayılan true olduğu için header'a `Authorization: Bearer` ve `localStorage` içindeki access token konur.

```99:116:web/src/CharacterCreatePage.tsx
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

      if (response.status === 403) {
        setFormError('Yetkin yok')
        return
      }
```

Access token bitmişse `apiFetch` bu `401` üzerinde `POST /api/auth/refresh` dener(Access token'ın süresi dolunca API `401` döner. `403` bu durumda gelmez.). O denemenin rotation kuralı oturumu uzatma işidir. `403` bu dala girmez. `403` kimlik vardır, fiil yoktur demektir.

İstek API'de önce `UseAuthentication`, sonra `UseAuthorization` içinden geçer. `POST` metodunun üstünde `[HasPermission(PermissionCodes.CharactersCreate)]` vardır.  
  
`[HasPermission]` bir `AuthorizeAttribute` türüdür. Ürettiği policy'nin ilk şartı oturum açmış kullanıcıdır. Permission kodu ondan sonra aranır.

Attribute sınıfı JWT'yi kendisi okumaz. Policy adını `Permission:characters.create` diye kurar.

```
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = "Permission:" + permission;
    }
}
```

`UseAuthentication` bu addan önce çalışır. JwtBearer access token'ı doğrular ve `HttpContext.User`'ı doldurur. Süre dolmuşsa kullanıcı boş kalır.

`UseAuthorization` bu policy'yi açar. Policy iki şey ister. Birincisi `RequireAuthenticatedUser`. İkincisi `PermissionRequirement`.

```
var policy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .AddRequirements(new PermissionRequirement(code))
    .Build();
```

`RequireAuthenticatedUser` geçmezse cevap `401` olur. `GetCodesAsync` çalışmaz. Geçerse handler `characters.create` kodunu join'de arar. Kod yoksa cevap `403` olur.

```46:66:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersCreate)]  // POST
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateCharacterRequest body,
        CancellationToken cancellationToken = default)
    {
        var id = await _mediator.Send(
            new CreateCharacterCommand(
                body.Name,
                body.Universe,
                body.Biography,
                body.Rarity,
                body.BaseAttack,
                body.BaseDefense,
                body.BaseSpeed,
                body.ImageUrl),
            cancellationToken);

        return Created($"/api/characters/{id}", id);
    }
```

`HasPermissionAttribute` bir `AuthorizeAttribute` türüdür. Policy adını `Permission:` artı kod diye kurar. Karakter eklemede bu ad `Permission:characters.create` olur.

```5:10:ReactBattleArena/ReactBattleArena.Api/Authorization/HasPermissionAttribute.cs
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = "Permission:" + permission;
    }
}
```

`Program.cs` bu policy adını çözen provider'ı ve kontrolü yapan handler'ı kaydeder. Provider tek örnektir. Handler istek ömründedir, çünkü veritabanına gider.

```23:25:ReactBattleArena/ReactBattleArena.Api/Program.cs
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

Provider ad `Permission:` ile başlıyorsa policy kurar. Policy iki şey ister: oturum açmış kullanıcı ve `PermissionRequirement`. Requirement'ın `Code` alanı `characters.create` olur.

```22:30:ReactBattleArena/ReactBattleArena.Api/Authorization/PermissionPolicyProvider.cs
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var code = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(code))
                .Build();
```

Access token yoksa, imzası bozuksa veya süresi dolmuşsa `RequireAuthenticatedUser` geçmez. Cevap `401` olur. `Create` action'ına ve `CreateCharacterCommandHandler`'a gelinmez.

Token geçerliyse handler çalışır. Kullanıcı `Guid` değerini `ClaimTypes.NameIdentifier` claim'inden okur. `GetCodesAsync` aynı join'i yapar: `UserRoles` satırından `RoleId`, oradan `RolePermissions`, oradan `Permission.Code`. Dönen listede requirement'ın kodu varsa `Succeed` çağrılır.

```16:27:ReactBattleArena/ReactBattleArena.Api/Authorization/PermissionAuthorizationHandler.cs
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var idValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idValue, out var userId))
            return;

        var codes = await _permissions.GetCodesAsync(userId);
        if (codes.Contains(requirement.Code))
            context.Succeed(requirement);
    }
```

```19:25:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserPermissionService.cs
        return await (
            from ur in _db.UserRoles
            join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
            join p in _db.Permissions on rp.PermissionId equals p.Id
            where ur.UserId == userId
            select p.Code
        ).Distinct().ToListAsync(cancellationToken);
```

`Player` kullanıcısında join boş kalır. `characters.create` listede yoktur. `Succeed` çağrılmaz. Kimlik geçerlidir, fiil yoktur. Cevap `403` olur. MediatR'a inilmez. `Characters` tablosuna satır yazılmaz.

`Admin` kullanıcısında join `characters.create` döner. `Succeed` çağrılır. Ondan sonra `ValidationBehavior` komut kurallarını çalıştırır. Ad veya evren boşsa, rarity 1 ile 5 arasında değilse cevap `400` olur. Kurallar geçince handler karakteri yazar ve `Guid` döner. Controller `201` ve `Location: /api/characters/{id}` yazar.

```19:35:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/CreateCharacterCommandHandler.cs
    public async Task<Guid> Handle(CreateCharacterCommand request, CancellationToken cancellationToken)
    {
        var entity = Character.Create(
            request.Name,
            request.Universe,
            request.Biography,
            request.Rarity,
            request.BaseAttack,
            request.BaseDefense,
            request.BaseSpeed,
            request.ImageUrl,
            DateTime.UtcNow);

        _db.Characters.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
```

Sayfa `201` görünce forma `Karakter eklendi` yazar ve `/characters` adresine gider.

Karakter eklenince bilinen ayrım şudur. `401` kimlik yoktur veya access token geçersizdir. `403` kimlik vardır, `characters.create` join'de yoktur. Linki gizlemek bu `403` kararının yerine geçmez. Aynı access token, `UserRoles` veya `RolePermissions` değişince bir sonraki POST'ta başka sonuç verebilir.

# Karakteri düzenle

Müşteri var olan bir karakterin alanlarını değiştirmek ister. Detay sayfasında Düzenle linkine basar. Adres `/characters/{id}/edit` olur. Sayfa önce `GET /api/characters/{id}` ile karakteri okur, formu doldurur. Kaydet düğmesi `PUT /api/characters/{id}` atar. Header'da `Authorization: Bearer` ve access token vardır. API `characters.update` kodunu join'de arar. Kod varsa satır güncellenir ve cevap `204` olur. Gövde boştur. Kod yoksa cevap `403` olur.

Bu kod `characters.create` ile aynı join'den gelir. Seed `Admin` rolüne `PermissionCodes.CharactersUpdate` bağını da yazar. O sabit `characters.update` metnidir. `Player` rolü için bu çağrı yoktur. Register kullanıcının `UserRoles` satırını `Player` rolüne bağladığı için yeni kayıt olan müşteri düzenleyemez. `UserRoles` satırı `Admin` rolüne bakıyorsa join kodu bulur ve o müşteri düzenler.

İstek şu sırayla yürür:

1. Detay sayfası `hasPermission` ile `characters.update` koduna bakar. Kod dizide yoksa Düzenle linki çizilmez.
2. Adres elle yazılırsa `CharacterEditPage` aynı diziye bakar ve `/characters` adresine döner.
3. Sayfa `useParams` ile URL'deki `id` değerini alır. `apiFetch` `GET /api/characters/{id}` atar. Bu GET'in üstünde `[HasPermission]` yoktur.
4. Form alanları cevapla dolar. Kaydet `PUT` atar. `auth` yazılmaz. Varsayılan true olduğu için Bearer gider.
5. Pipeline önce `UseAuthentication`, sonra `UseAuthorization` çalışır. `[HasPermission(PermissionCodes.CharactersUpdate)]` policy adını `Permission:characters.update` yapar.
6. Aynı `PermissionAuthorizationHandler` `GetCodesAsync` ile `UserRoles`, `RolePermissions` ve `Permissions` tablolarını okur. Listede `characters.update` varsa `Succeed` çağrılır.
7. `ValidationBehavior` kuralları bozulursa cevap `400` olur. Handler çalışmaz.
8. Handler `Characters` tablosunda `Id` arar. Yoksa `false` döner, controller `404` yazar. Varsa `Character.Update` alanları değiştirir, `SaveChangesAsync` yazar, controller `204` döner.
9. Sayfa `204` gövdesini `json()` ile açmaz. Adres `/characters/{id}` olur.



## Düzenle linki ve sayfa kapısı

Rota `App.tsx` içindedir. `/characters/:id/edit`, `/characters/:id` rotasından önce durur. Daha genel rota önce gelseydi `edit` kelimesi bir `id` sanılırdı.

```16:20:web/src/App.tsx
      <Route element={<AppLayout />}>
        <Route path="/characters" element={<CharactersPage />} />
        <Route path="/characters/new" element={<CharacterCreatePage />} />
        <Route path="/characters/:id/edit" element={<CharacterEditPage />} />
        <Route path="/characters/:id" element={<CharacterDetailPage />} />
```

Detay sayfası `AppLayout`'un tuttuğu `permissions` dizisini okur. `characters.update` dizide varsa Düzenle linki çizilir. Bu çizim `PUT` atmaz.

```148:150:web/src/CharacterDetailPage.tsx
          {hasPermission(permissions, PERMISSIONS.charactersUpdate) && (
            <Link to={`/characters/${id}/edit`}>Düzenle</Link>
          )}
```

`CharacterEditPage` URL'deki `id` değerini `useParams` ile alır. Access token'ı `getToken` ile, dizi `usePermissions` ile okur. Diziyi gönderen istek `GET /api/auth/me` olur. Bu isteği `CharacterEditPage` atmaz. `AppLayout` içindeki `loadMe` atar, cevaptaki `permissions` alanını state'e yazar ve `PermissionContext` ile alta verir. Düzenleme sayfası o state'i okur.

```8:13:web/src/CharacterEditPage.tsx
function CharacterEditPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const token = getToken()
  const permissions = usePermissions()
```

Sayfa açılınca `useEffect` karakteri ister. `id` veya token yoksa istek atılmaz. Varsa `apiFetch` `GET /api/characters/{id}` atar. `404` karakter yok demektir. `200` gelince alanlar state'e yazılır.

```29:35:web/src/CharacterEditPage.tsx
  useEffect(() => {
    async function load() {
      if (!token || !id) {
        setLoadError('Id veya token yok')
        setLoading(false)
        return
      }
```

```53:75:web/src/CharacterEditPage.tsx
        const response = await apiFetch(`/api/characters/${id}`)

        if (response.status === 404) {
          setLoadError('Karakter bulunamadı')
          setLoading(false)
          return
        }

        if (!response.ok) {
          setLoadError('Karakter alınamadı')
          setLoading(false)
          return
        }

        const data = await response.json()
        setName(data.name)
        setUniverse(data.universe)
        setBiography(data.biography ?? '')
        setRarity(data.rarity)
        setBaseAttack(data.baseAttack)
        setBaseDefense(data.baseDefense)
        setBaseSpeed(data.baseSpeed)
        setImageUrl(data.imageUrl ?? '')
```

Bu GET'in controller metodunda `[HasPermission]` yoktur. Karakteri okumak `characters.update` koduna bakmaz. Düzenleme kodu, formu çizmeden önce sayfada ve `PUT` sırasında API'de aranır.

```35:44:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CharacterDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CharacterDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCharacterByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
```

Çizim sırası şöyledir. Token yoksa `/login` adresine dönülür. Karakter henüz gelmediyse `Yükleniyor…` yazılır. Dizi `characters.update` içermiyorsa `/characters` adresine dönülür. Form bu üçünden biri tutunca çizilir.

```161:169:web/src/CharacterEditPage.tsx
  if (!token) {
  return <Navigate to="/login" replace />
  }
  if (loading) {
    return <p>Yükleniyor…</p>
  }
  if (!hasPermission(permissions, PERMISSIONS.charactersUpdate)) {
    return <Navigate to="/characters" replace />
  }
```

`hasPermission` bellekteki diziyi okur. `PUT` atmaz. Link gizlense de adres elle yazılabilir. Sayfa kapısı da `PUT` atmaz. İkisi de `/me` cevabındaki diziyi kullanır. `PUT` kararı API'dedir.

## PUT

Kaydet `handleSubmit` içinden gider. `e.preventDefault()` tarayıcının kendi form isteğini keser. `apiFetch` yolu ``/api/characters/${id}`` olur. Metot `PUT` olur. Bearer, `apiFetch` varsayılanı ile eklenir.

```116:138:web/src/CharacterEditPage.tsx
      const response = await apiFetch(`/api/characters/${id}`, {
        method: 'PUT',
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

      if (response.status === 401) {
        setFormError('Oturum yok — tekrar giriş yap')
        return
      }

      if (response.status === 403) {
        setFormError('Yetkin yok')
        return
      }
```

Access token bitmişse `apiFetch` bu `401` üzerinde önce `POST /api/auth/refresh` dener. O denemenin rotation kuralı oturumu uzatma işidir. Yenileme tutmazsa sayfa `401` görür ve `Oturum yok` yazar. `403` o dala girmez. `403` kimlik vardır, `characters.update` join'de yoktur demektir. Sayfa bunu `Yetkin yok` diye yazar.

Controller metodu `id` değerini URL'den, gövdeyi `CreateCharacterRequest` ile alır. Üstündeki attribute `CharactersUpdate` sabitini taşır. Policy adı `Permission:characters.update` olur. Aynı `PermissionPolicyProvider` bu adı `PermissionRequirement` yapar. Aynı `PermissionAuthorizationHandler` `ClaimTypes.NameIdentifier` içinden kullanıcı `Guid` değerini okur ve `GetCodesAsync` çağırır.

```69:92:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersUpdate)]  // PUT
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
    Guid id,
    [FromBody] CreateCharacterRequest body,
    CancellationToken cancellationToken = default)
    {
        var updated = await _mediator.Send(
            new UpdateCharacterCommand(
                id,
                body.Name,
                body.Universe,
                body.Biography,
                body.Rarity,
                body.BaseAttack,
                body.BaseDefense,
                body.BaseSpeed,
                body.ImageUrl),
            cancellationToken);

        return updated ? NoContent() : NotFound();
    }
```

Join karakter eklemedeki join'dir. `UserRoles` kullanıcıyı role, `RolePermissions` rolü permission'a, `Permissions.Code` kolonu `characters.update` metnine bağlar. `Succeed` çağrılmazsa controller'a gelinmez. Cevap `403` olur. `Characters` satırı değişmez.

`Succeed` çağrılırsa MediatR `UpdateCharacterCommand` taşır. Dönüş `bool` olur. `true` satır güncellendi demektir. `false` bu `Id` ile satır yok demektir.

```5:14:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/UpdateCharacterCommand.cs
public sealed record UpdateCharacterCommand(
    Guid Id,
    string Name,
    string Universe,
    string? Biography,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    string? ImageUrl) : IRequest<bool>;
```

Handler'dan önce `UpdateCharacterCommandValidator` çalışır. `Id` boş olamaz. Ad ve evren boş olamaz. Rarity 1 ile 5 arasındadır. Attack, defense ve speed 0 ile 9999 arasındadır. Kural bozulursa `ValidationException` middleware'de `400` olur. Handler çalışmaz.

```8:17:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/UpdateCharacterCommandValidator.cs
    public UpdateCharacterCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Universe).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Biography).MaximumLength(2000);
        RuleFor(x => x.Rarity).InclusiveBetween(1, 5);
        RuleFor(x => x.BaseAttack).InclusiveBetween(0, 9999);
        RuleFor(x => x.BaseDefense).InclusiveBetween(0, 9999);
        RuleFor(x => x.BaseSpeed).InclusiveBetween(0, 9999);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
```

Kurallar geçince handler `Characters` tablosunda `Id` arar. Satır yoksa `false` döner. Controller `NotFound()` ile `404` yazar. Satır varsa `Character.Update` aynı nesnenin alanlarını yeni değerlerle değiştirir. Yeni bir `Guid` üretilmez. `SaveChangesAsync` değişen kolonları yazar. Handler `true` döner. Controller `NoContent()` ile `204` yazar.

```16:35:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/UpdateCharacterCommandHandler.cs
    public async Task<bool> Handle(UpdateCharacterCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Characters
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity is null)
            return false;

        entity.Update(
            request.Name,
            request.Universe,
            request.Biography,
            request.Rarity,
            request.BaseAttack,
            request.BaseDefense,
            request.BaseSpeed,
            request.ImageUrl);

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
```

```50:68:ReactBattleArena/ReactBattleArena.Domain/Characters/Character.cs
    public void Update(
        string name,
        string universe,
        string? biography,
        int rarity,
        int baseAttack,
        int baseDefense,
        int baseSpeed,
        string? imageUrl)
    {
        Name = name;
        Universe = universe;
        Biography = biography;
        Rarity = rarity;
        BaseAttack = baseAttack;
        BaseDefense = baseDefense;
        BaseSpeed = baseSpeed;
        ImageUrl = imageUrl;
    }
```

`204` gövdesi boştur. Sayfa `response.json()` çağırmaz. `404` için `Karakter bulunamadı` yazar. `400` için `ValidationProblemDetails` içindeki alan mesajlarını birleştirir. `204` gelince adres detay sayfasına döner.

```140:155:web/src/CharacterEditPage.tsx
      if (response.status === 404) {
        setFormError('Karakter bulunamadı')
        return
      }

      if (!response.ok) {
        const problem = await response.json().catch(() => null)
        const messages = problem?.errors
          ? Object.values(problem.errors).flat().join(' | ')
          : problem?.title ?? `Hata ${response.status}`
        setFormError(String(messages))
        return
      }

      // 204 No Content — body yok; json() çağırma
      navigate(`/characters/${id}`)
```

Düzenleme tutunca aynı karakter satırının alanları değişmiştir. `Id` aynıdır. `401` kimliktir. `403` kimlik vardır, `characters.update` join'de yoktur. Düzenle linkini gizlemek bu `403` kararının yerine geçmez.

# Karakteri sil

Müşteri detay sayfasında Sil düğmesine basar. Tarayıcı `window.confirm` ile sorar. Müşteri vazgeçerse istek gitmez. Onaylarsa `DELETE /api/characters/{id}` gider. Header'da `Authorization: Bearer` ve access token vardır. API `characters.delete` kodunu aynı join'de arar. Kod varsa satır silinir ve cevap `204` olur. Gövde boştur. Kod yoksa cevap `403` olur. Ayrı bir silme sayfası yoktur. Ekrandaki kapı Sil düğmesinin çizilip çizilmemesidir.

Seed `Admin` rolüne bu kodu da bağlar. Sabit `PermissionCodes.CharactersDelete` olur. Metin `characters.delete` olur. `Player` rolü için çağrı yoktur. Register kullanıcının `UserRoles` satırını `Player` rolüne yazdığı için yeni kayıt olan müşteri silemez. `UserRoles` satırı `Admin` rolüne bakıyorsa join kodu bulur.

```24:24:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersDelete, cancellationToken);
```

İstek şu sırayla yürür:

1. Detay sayfası `hasPermission` ile `characters.delete` koduna bakar. Kod dizide yoksa Sil düğmesi çizilmez. Bu çizim `DELETE` atmaz.
2. Düğme `handleDelete` çağırır. `window.confirm` false dönerse fonksiyon `return` eder.
3. `apiFetch` `DELETE /api/characters/{id}` atar. `auth` yazılmaz. Varsayılan true olduğu için Bearer gider.
4. Pipeline önce `UseAuthentication`, sonra `UseAuthorization` çalışır. `[HasPermission(PermissionCodes.CharactersDelete)]` policy adını `Permission:characters.delete` yapar.
5. Aynı `PermissionAuthorizationHandler` `GetCodesAsync` ile `UserRoles`, `RolePermissions` ve `Permissions` tablolarını okur. Listede `characters.delete` varsa `Succeed` çağrılır.
6. `DeleteCharacterCommand` için ayrı bir validator sınıfı yoktur. `ValidationBehavior` bu komutta kural çalıştırmaz.
7. Handler `Characters` tablosunda `Id` arar. Yoksa `false` döner, controller `404` yazar. Varsa `Remove` ile satırı siler, `SaveChangesAsync` yazar, controller `204` döner.
8. Sayfa `204` gövdesini `json()` ile açmaz. Adres `/characters` olur.



## Sil düğmesi

Detay sayfası `useParams` ile `id` değerini, `usePermissions` ile `AppLayout`'un tuttuğu diziyi okur. Sil düğmesi `characters.delete` dizide varsa çizilir.

```22:27:web/src/CharacterDetailPage.tsx
function CharacterDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const token = getToken()
  const permissions = usePermissions()
```

```151:154:web/src/CharacterDetailPage.tsx
          {hasPermission(permissions, PERMISSIONS.charactersDelete) && (
            <button type="button" onClick={handleDelete}>
              Sil
            </button>
          )}
```

`hasPermission` bellekteki diziyi okur. `DELETE` atmaz. Düğme gizlense de Scalar'dan aynı URL'ye `DELETE` atılabilir. O çağrıda düğme yoktur. Kararı API verir.

Düğme basılınca `window.confirm` açılır. Müşteri iptal ederse `ok` false olur ve `apiFetch` çağrılmaz.

```94:98:web/src/CharacterDetailPage.tsx
  async function handleDelete() {
    if (!id || !token) return

    const ok = window.confirm('Bu karakteri silmek istediğine emin misin?')
    if (!ok) return
```



## DELETE

Onaydan sonra istek gider. Gövde yoktur. `id` URL'dedir.

```113:125:web/src/CharacterDetailPage.tsx
      const response = await apiFetch(`/api/characters/${id}`, {
        method: 'DELETE',
      })

      if (response.status === 401) {
        setDeleteError('Oturum yok — tekrar giriş yap')
        return
      }

      if (response.status === 403) {
        setDeleteError('Yetkin yok (Admin gerekli)')
        return
      }
```

Access token bitmişse `apiFetch` bu `401` üzerinde önce `POST /api/auth/refresh` dener. O denemenin rotation kuralı oturumu uzatma işidir. Yenileme tutmazsa sayfa `401` görür ve `Oturum yok` yazar. `403` o dala girmez. Sayfadaki metin `Admin gerekli` der. API rol adına bakmaz. `characters.delete` join'de yoksa `403` döner. `Admin` rolü bu kodu seed ile taşıdığı için metin o role işaret eder. Karar `Permission.Code` kolonundadır.

Controller `id` değerini URL'den alır. Üstündeki attribute `CharactersDelete` sabitini taşır. Policy adı `Permission:characters.delete` olur. Aynı provider ve aynı handler çalışır. `GetCodesAsync` üç tabloyu okur. `Succeed` çağrılmazsa `Delete` metoduna gelinmez. Cevap `403` olur. Satır durur.

```95:106:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersDelete)]  // DELETE
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _mediator.Send(new DeleteCharacterCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
```

`Succeed` çağrılırsa MediatR `DeleteCharacterCommand` taşır. Komut yalnız `Id` tutar. Dönüş `bool` olur.

```5:5:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/DeleteCharacterCommand.cs
public sealed record DeleteCharacterCommand(Guid Id) : IRequest<bool>;
```

Bu komutun validator sınıfı yoktur. `ValidationBehavior` validator bulamazsa `next()` ile handler'a geçer. Boş `Guid` için `400` üreten bir kural bu komutta durmaz. `{id:guid}` route kısıtı URL'deki metin `Guid` değilse action'a girmez.

Handler `Characters` tablosunda `Id` arar. Satır yoksa `false` döner. Controller `NotFound()` ile `404` yazar. Satır varsa `Remove` onu silinecek diye işaretler. `SaveChangesAsync` `DELETE` SQL'ini yazar. Handler `true` döner. Controller `NoContent()` ile `204` yazar.

```18:29:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/DeleteCharacterCommandHandler.cs
    public async Task<bool> Handle(DeleteCharacterCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Characters
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity is null)
            return false;

        _db.Characters.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
```

`204` gövdesi boştur. Sayfa `response.json()` çağırmaz. `404` için `Karakter bulunamadı` yazar. Başka bir hata status'ü `Silinemedi` yazar. `204` gelince adres karakter listesine döner. `deleteError` doluysa paragraf basılır.

```127:137:web/src/CharacterDetailPage.tsx
      if (response.status === 404) {
        setDeleteError('Karakter bulunamadı')
        return
      }

      if (!response.ok) {
        setDeleteError(`Silinemedi (${response.status})`)
        return
      }

      navigate('/characters')
```

```162:162:web/src/CharacterDetailPage.tsx
      {deleteError && <p>{deleteError}</p>}
```

Silme tutunca o `Id` ile `Characters` satırı kalkmıştır. `401` kimliktir. `403` kimlik vardır, `characters.delete` join'de yoktur. Sil düğmesini gizlemek bu `403` kararının yerine geçmez.