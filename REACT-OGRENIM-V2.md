# Öğrenim Notları V2 — Backend + React

Bu dosya `REACT-OGRENIM.md` arşivinin yerine, baştan ileriye doğru yeniden yazılan öğrenim notudur. Eski dosyaya dokunulmaz. Bölümler `git log` sırasıyla gider: önce backend (2 Temmuz), sonra React, sonra RBAC, sonra refresh token.

**İlerleme:** 34 / 34 yazıldı · **bitti.**

---

## Blok A — Backend temeli (2–28 Temmuz)

Bu blokta frontend yok. Amaç: Character kataloğunu katmanlı CQRS + EF ile ayağa kaldırmak. HTTP endpoint 7 Temmuz’da, React 29 Temmuz’da gelecek.

---

### 1. 02–03 Temmuz — Solution, dört katman, Character, ilk command, EF InitialCreate

**Commit’ler:** `cf1cd48` (2 Temmuz, “Character CQRS foundation”), `1b5ef32` (3 Temmuz, “InitialCreate”).

Bu adımda dosyaları şu sırayla ekledik. Önce `ReactBattleArena.slnx` ve dört `.csproj`, çünkü katman sınırını kod yazmadan önce kilitlemek istedik: kim kime referans verebilir, kim veremez. Sonra Domain’de `Character`, çünkü henüz tablo yokken iş nesnesini tanımlamak istedik — “karakter nedir” SQL’den bağımsız dursun. Sonra Application’da `CreateCharacterCommand`, `CreateCharacterCommandHandler` ve `IApplicationDbContext`: HTTP yoktu ama “karakter ekleme işi” controller’da değil handler’da dursun diye. 3 Temmuz’da Infrastructure’a `ApplicationDbContext`, `CharacterConfiguration`, `AddInfrastructure` ve `InitialCreate` migration geldi; validator dosyası da aynı gün yazıldı ama pipeline’a henüz bağlanmadı. O gün `CharactersController` yoktu — bu yüzden bu handler’ı tarayıcıdan çağırmak mümkün değildi.

#### Solution ve dört proje

```1:7:ReactBattleArena/ReactBattleArena.slnx
<Solution>
  <Project Path="ReactBattleArena.Api/ReactBattleArena.Api.csproj" />
  <Project Path="ReactBattleArena.Application/ReactBattleArena.Application.csproj" />
  <Project Path="ReactBattleArena.Domain/ReactBattleArena.Domain.csproj" />
  <Project Path="ReactBattleArena.Infrastructure/ReactBattleArena.Infrastructure.csproj" />
</Solution>
```

`.slnx` Visual Studio / `dotnet` solution dosyasıdır; içinde dört proje yolu var, başka bir şey yok. Klasör de aynı isimle `ReactBattleArena/` altında. Hedef framework `.NET 10`. Api şablondan geldiği için 2 Temmuz’da hâlâ `WeatherForecastController` duruyordu; onu 7 Temmuz’da sileceğiz.

Dört projenin iş bölümü şöyle: Domain entity ve iş kuralı tutar, Application komut/sorgu ve handler tutar, Infrastructure EF ve dış dünya (SQL, hash, JWT) tutar, Api yalnızca HTTP’dir. Bağımlılık dışarıdan içeriye akar; Domain başka katmanı **bilmez**.

Bunu csproj referansları sağlar, yorum satırı değil:

```1:9:ReactBattleArena/ReactBattleArena.Domain/ReactBattleArena.Domain.csproj
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

Domain’de `<PackageReference>` yok, `<ProjectReference>` de yok. NuGet’siz class library. O gün de bugün de böyle: Character sınıfı EF attribute’u taşımaz, MediatR bilmez, ASP.NET bilmez. Bu yüzden Domain’i tek başına derlersin; SQL Server ayakta olmasa da entity derlenir.

```17:19:ReactBattleArena/ReactBattleArena.Application/ReactBattleArena.Application.csproj
  <ItemGroup>
    <ProjectReference Include="..\ReactBattleArena.Domain\ReactBattleArena.Domain.csproj" />
  </ItemGroup>
```

Application yalnızca Domain’e bakar. 2 Temmuz’da paketler `MediatR` + `Microsoft.EntityFrameworkCore` idi (handler `IRequestHandler` kullansın, arayüz `DbSet` kullansın diye). 3 Temmuz’da FluentValidation eklendi. Bugünkü dosyada ayrıca `BCrypt.Net-Next` var; o 16 Temmuz’da Register ile geldi, bu bölümün işi değil.

```21:24:ReactBattleArena/ReactBattleArena.Infrastructure/ReactBattleArena.Infrastructure.csproj
  <ItemGroup>
    <ProjectReference Include="..\ReactBattleArena.Application\ReactBattleArena.Application.csproj" />
    <ProjectReference Include="..\ReactBattleArena.Domain\ReactBattleArena.Domain.csproj" />
  </ItemGroup>
```

Infrastructure hem Application’a hem Domain’e bakar. EF’in somut `ApplicationDbContext`’i burada yaşayacak; Application sadece arayüzü görecek. 2 Temmuz’da Infrastructure’da henüz EF paketi yoktu, sadece referanslar vardı. SQL paketleri 3 Temmuz’da geldi.

```27:30:ReactBattleArena/ReactBattleArena.Api/ReactBattleArena.Api.csproj
  <ItemGroup>
    <ProjectReference Include="..\ReactBattleArena.Application\ReactBattleArena.Application.csproj" />
    <ProjectReference Include="..\ReactBattleArena.Infrastructure\ReactBattleArena.Infrastructure.csproj" />
  </ItemGroup>
```

Api, Application + Infrastructure’a bakar; Domain satırı `Api.csproj`’ta yok. SDK-style projede `ProjectReference` zinciri derlemeye de akar: Api → Application → Domain olduğu için Api, Domain’in public tiplerini yine `using` edebilir (`CharactersController` sonradan `PermissionCodes` için `ReactBattleArena.Domain.Authorization` yazar). Bu “Application üzerinden Domain’e ulaşmak”tır — ekstra Domain referansı yazmana gerek kalmaz.

Yine de controller’ın işi `Character.Create` çağırmak değildir; `CreateCharacterCommand` gönderir, entity’yi handler üretir. Derleyicinin Domain’i görmesi ile katmanın Domain’i sahiplenmesi aynı şey değil.

Eski bir ASP.NET MVC projesinde çoğu zaman tek proje olurdu: controller, SQL, iş kuralı aynı yerde. Burada o karışıklığı baştan kestik.

Eşleme: `HttpClient` ile başka bir API’ye istek atan bir .NET client, karşı tarafın `DbContext`’ini referans almaz; sözleşmeye bakar. Application katmanı da Infrastructure’ın somut EF sınıfını değil `IApplicationDbContext` sözleşmesini görür.

#### Character entity

Tablo henüz yoktu. Önce iş nesnesi:

```1:70:ReactBattleArena/ReactBattleArena.Domain/Characters/Character.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace ReactBattleArena.Domain.Characters;
public sealed class Character
{
    private Character()
    {
        
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Universe { get; private set; } = null!;
    public string? Biography { get; private set; }
    public int Rarity { get; private set; }
    public int BaseAttack { get; private set; }
    public int BaseDefense { get; private set; }
    public int BaseSpeed { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Character Create( // factory
        string name,
        string universe,
        string? biography,
        int rarity,
        int baseAttack,
        int baseDefense,
        int baseSpeed,
        string? imageUrl,
        DateTime utcNow)
    {
        return new Character
        {
            Id = Guid.NewGuid(),
            Name = name,
            Universe = universe,
            Biography = biography,
            Rarity = rarity,
            BaseAttack = baseAttack,
            BaseDefense = baseDefense,
            BaseSpeed = baseSpeed,
            ImageUrl = imageUrl,
            CreatedAtUtc = utcNow
        };
    }

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
}
```

`sealed` — bu sınıftan türetme yok. Alanlar `private set`: Application’daki handler `entity.Name = "x"` yazamaz; derleyici reddeder. Yeni karakter yalnızca `Character.Create(...)` ile üretilir. `Create` sınıfın **içinde** olduğu için object initializer `private set`’e erişir; `Guid.NewGuid()` ve `CreatedAtUtc` orada doldurulur. `Update` de aynı sınıfta; 9 Temmuz’daki PUT bu metodu çağıracak, ama o gün henüz PUT yoktu. Metodu şimdiden yazmamızın nedeni: “karakter nasıl değişir” kuralı entity’de dursun, handler alanları tek tek set etmesin.

`private Character()` boş constructor EF içindir. Handler `new Character()` diyemez (private). EF, tablo satırını nesneye çevirirken parametresiz constructor’a ihtiyaç duyar; onu reflection ile çağırır. Sen çağıramazsın, EF çağırır. `Name = null!` nullable uyarıyı bastırır: C# “bu string boş olmasın” der, EF doldurana kadar gerçekten null’dır.

Dosyanın tepesindeki `using System.Collections.Generic` ve `using System.Text` kullanılmıyor. `dotnet new classlib` şablonundan kaldı. Zararsız, ama “entity’yi şablondan açıp üzerine yazdık” izi. Temizlesek de davranış değişmez.

Bu sınıf o günden bugüne **değişmedi**. User, Role, RefreshToken ayrı entity’ler olarak sonra eklendi; Character’a kolon yapıştırmadık.

#### İlk CQRS command

HTTP body henüz yok. Yine de “karakter ekle” isteğini bir mesaj tipi olarak yazdık:

```1:16:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/CreateCharacterCommand.cs
using MediatR;

namespace ReactBattleArena.Application.Characters.Commands;

public sealed record CreateCharacterCommand(
    string Name,
    string Universe,
    string? Biography,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    string? ImageUrl) : IRequest<Guid>;
//IRequest<Guid>
//“Bu istek işlenince Guid döner” (yeni karakterin Id'si)
//Character.Create() ile aynı alanlar(Id ve CreatedAt handler'da eklenir)
```

`record` immutable bir veri taşıyıcısıdır: oluşturulunca alanlar değişmez. Entity `class`’tır çünkü kimliği (`Id`) vardır ve `Update` ile değişir. Command’da `Id` ve `CreatedAtUtc` yok; onları handler `Character.Create` içinde üretir. İstemci “şu Guid ile kaydet” diyemesin diye.

`IRequest<Guid>` MediatR sözleşmesi: “bu mesaj işlenince `Guid` döner.” Handler’daki `Task<Guid> Handle(...)` imzası bunu karşılar. Eski MVC’de karşılığı `ActionResult<Guid>` gibi bir dönüş tipi olurdu; fark şu ki command HTTP bilmez. Aynı command’ı testte, ileride bir background job’da da `Send` edebilirsin.

7 Temmuz’da `CharactersController.Create` bu record’u `body` alanlarından doldurup `IMediator.Send` edecek. 4 Ağustos’ta `CharacterCreatePage` `apiFetch('/api/characters', { method: 'POST', ... })` ile o controller’a gidecek. 2 Temmuz’da bu zincirin hiçbiri yoktu; elimizde sadece mesaj tipi vardı.

#### Handler

```1:34:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/CreateCharacterCommandHandler.cs
using MediatR;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Characters;

namespace ReactBattleArena.Application.Characters.Commands;

public sealed class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateCharacterCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

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
}
```

Handler `IRequestHandler<CreateCharacterCommand, Guid>` uygular: “şu mesaj gelince bu işi yap, `Guid` dön.” Constructor’da `IApplicationDbContext` ister; `ApplicationDbContext` sınıf adını yazmaz. Böylece Application, SQL Server’a nasıl bağlandığını bilmez.

`Handle` içinde üç iş var. Bir: `Character.Create(...)` ile entity üret — `DateTime.UtcNow` burada verilir, command’da saat taşımayız. İki: `_db.Characters.Add(entity)` — bu henüz SQL çalıştırmaz, EF’e “bu nesne yeni, INSERT olacak” der. Üç: `SaveChangesAsync` gerçek INSERT’i gönderir. Dönüş değeri `entity.Id`; 7 Temmuz’daki controller bunu `201 Created` body’sinde kullanacak.

`CancellationToken` isteğin iptal edildiğini handler’a taşır (istemci bağlantıyı kesti, vs.). `SaveChangesAsync`’e iletmezsen iptal SQL’in ortasında kalır. Alışkanlık: token’ı aşağı ver.

Bu handler o günden bugüne **değişmedi**. 2 Temmuz’da derlenirdi ama DI’da MediatR taraması yoktu (`AddApplication` 6 Temmuz, bölüm 2). HTTP de yoktu. Yani “kod var, henüz kimse çağırmıyor” hali.

#### IApplicationDbContext — Application’ın gördüğü arayüz

```1:18:ReactBattleArena/ReactBattleArena.Application/Abstractions/IApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Domain.Authorization;
using ReactBattleArena.Domain.Characters;
using ReactBattleArena.Domain.Users;
using ReactBattleArena.Domain.Authentication;
namespace ReactBattleArena.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

2 Temmuz’da bu arayüzde yalnızca `DbSet<Character> Characters` ve `SaveChangesAsync` vardı. `Users` 12 Temmuz’da, RBAC set’leri 20–21 Ağustos’ta, `RefreshTokens` 28 Ağustos’ta eklendi. Handler hâlâ sadece `Characters` kullanıyor; ekstra set’ler sonraki bölümlerin işi.

Neden interface? Handler’ın `ApplicationDbContext`’e (Infrastructure sınıfı) bağlı kalmasını istemedik. `.csproj` içindeki `<ProjectReference>` şu anlama gelir: **bu proje, işaret ettiği projenin public tiplerini kullanabilir.** Application.csproj yalnızca Domain’e bakıyor; Infrastructure satırı yok. Infrastructure.csproj ise Application’a bakıyor — bu yüzden Infrastructure, Application’ın `IApplicationDbContext`’ini görür ve `class ApplicationDbContext : IApplicationDbContext` yazabilir.

Tersini karıştırma: Application, Infrastructure’ın koduna erişemez. Handler `new ApplicationDbContext(...)` veya `using ReactBattleArena.Infrastructure.Persistence` yazamaz; derleyici projeyi görmez. İkisi birbirine referans verse döngü olur, `dotnet build` reddeder. Çalışma anında birbirine bağlayan Api’dir: `AddInfrastructure` somut sınıfı `IApplicationDbContext` olarak kaydeder, handler constructor’da arayüzü ister, DI aynı nesneyi verir.

Bunun bedeli: arayüz `DbSet` kullandığı için Application `Microsoft.EntityFrameworkCore` paketini alır. Tam bir repository arayüzü (`ICharacterRepository`) EF tipini Application’dan çıkarırdı; biz “DbContext’in ince arayüzü”nü seçtik. BattleArena referansındaki kalıp da buydu.

`DbSet<Character>` hem sorgu (`Where`, `ToListAsync`) hem `Add`/`Remove` için. `SaveChangesAsync` değişiklikleri SQL’e basar, etkilenen satır sayısını döner; Create handler o sayıyı kullanmaz, `entity.Id` döner.

#### Validator dosyası (3 Temmuz) — henüz pipeline yok

```1:18:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/CreateCharacterCommandValidator.cs
using FluentValidation;

namespace ReactBattleArena.Application.Characters.Commands;

public sealed class CreateCharacterCommandValidator : AbstractValidator<CreateCharacterCommand>
{
    public CreateCharacterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Universe).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Biography).MaximumLength(2000);
        RuleFor(x => x.Rarity).InclusiveBetween(1, 5);
        RuleFor(x => x.BaseAttack).InclusiveBetween(0, 9999);
        RuleFor(x => x.BaseDefense).InclusiveBetween(0, 9999);
        RuleFor(x => x.BaseSpeed).InclusiveBetween(0, 9999);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}
```

Bu dosya 3 Temmuz commit’inde (`1b5ef32`) geldi. Kurallar `CharacterConfiguration`’daki `HasMaxLength` sayılarıyla aynı: Name 200, Universe 120, Biography 2000, ImageUrl 500. Amaç: boş isim veya 6 rarity handler’a, dolayısıyla SQL’e düşmesin.

Ama 3 Temmuz’da `ValidationBehavior` yoktu, `AddApplication` yoktu. Yani validator sınıfı duruyordu, kimse onu command gönderilmeden önce çalıştırmıyordu. MVC’deki `ModelState.IsValid` karşılığı pipeline’da 6 Temmuz’da kurulacak (bölüm 2); 400 cevabı 8 Temmuz’da (bölüm 4). Şimdilik kurallar yazıldı, henüz otomatik çalışmıyor.

#### ApplicationDbContext

```1:32:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Authentication;
using ReactBattleArena.Domain.Authorization;
using ReactBattleArena.Domain.Characters;
using ReactBattleArena.Domain.Users;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Character> Characters => Set<Character>();

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

3 Temmuz’da bu sınıfta yalnızca `Characters` vardı; `Users` ve sonrası aynı arayüzdeki gibi sonradan eklendi. `DbContext` EF’in somut bağlamı, `IApplicationDbContext` Application’ın gördüğü yüz. İkisini birden uyguladığı için DI’da **aynı nesneyi** iki tip olarak verebiliriz (birkaç paragraf aşağıda).

`DbSet<Character> Characters => Set<Character>()` her çağrıda `Set` döner; alan değil property. EF bunu “Characters tablosuna giden giriş” olarak tanır.

`OnModelCreating` içinde `ApplyConfigurationsFromAssembly` aynı assembly’deki (`Infrastructure`) tüm `IEntityTypeConfiguration<>` sınıflarını tarar. `CharacterConfiguration`’ı tek tek `modelBuilder.ApplyConfiguration(new CharacterConfiguration())` diye yazmayız. Yeni tablo ekleyince configuration sınıfını aynı klasöre koymak yeter; bu satır değişmez.

Constructor `DbContextOptions<ApplicationDbContext>` alır. Connection string burada hard-code edilmez; `AddDbContext` options’ı doldurur. Testte başka provider (ör. InMemory) takılabilir — o gün yapmadık, kapı açık duruyor.

#### CharacterConfiguration — tablo eşlemesi Domain’de değil

```1:23:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/CharacterConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactBattleArena.Domain.Characters;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("Characters");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Universe).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Biography).HasMaxLength(2000);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);

        builder.HasIndex(x => x.Universe);
        builder.HasIndex(x => new { x.Universe, x.Name });
    }
}
```

`ToTable("Characters")` tablo adı. `HasKey` PK. `IsRequired` + `HasMaxLength` kolon kısıtları: Name/Universe boş olamaz, string uzunlukları SQL’de `nvarchar(n)` olur. `Biography` ve `ImageUrl` required değil — entity’de `string?` ile uyumlu.

İki index: Universe tek başına (filtre: “şu evrendekiler”), `(Universe, Name)` birlikte. Unique index değil; aynı isim aynı evrende iki kez eklenebilir. Unique yapsaydık `HasIndex(...).IsUnique()` gerekirdi, yapmadık.

Bu mapping’i entity üzerine `[MaxLength(200)]` attribute’u olarak da yazabilirdik. O zaman Domain EF’i “bilir”di. Configuration’ı Infrastructure’da tutmamızın nedeni: Domain NuGet’siz kalsın, SQL ayrıntısı iş nesnesine bulaşmasın.

Bu dosya o günden bugüne **değişmedi**.

#### DI: aynı istekte tek DbContext

```13:23:ReactBattleArena/ReactBattleArena.Infrastructure/DependencyInjection.cs
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());
```

3 Temmuz’da `AddInfrastructure` yalnızca bu kadardı. Bugünkü dosyanın devamında hasher, JWT, refresh generator, permission service var; onlar sonraki bölümler.

`GetConnectionString("DefaultConnection")` değeri `appsettings.Development.json` içinde. Şifreyi buraya kopyalamıyorum; Development json commit’te duruyor, notlara sır taşınmaz.

`AddDbContext` EF’i SQL Server ile kaydeder. `AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())` şu anlama gelir: biri `IApplicationDbContext` isterse, **yeni bir kutu açma**; bu istek için zaten üretilmiş `ApplicationDbContext`’i ver.

Görünmeyen kısım DI yaşam süresidir. `Scoped`, bir HTTP isteği boyunca tek DbContext demektir. Handler `IApplicationDbContext` ister, ileride başka bir servis `ApplicationDbContext` isterse, ikisi de aynı örneği alır. İki ayrı örnek olsaydı biri `Add` eder öteki `SaveChanges` çağırırdı; INSERT gitmezdi. `GetRequiredService` ile “aynı kutuyu iki etiketten ver” bunu kilitler.

3 Temmuz’da `Program.cs`’e yalnızca `builder.Services.AddInfrastructure(builder.Configuration)` eklendi. `AddApplication` yoktu — o 6 Temmuz. Bugünkü `Program.cs` JWT, CORS, `AddApplication` ile dolu; o satırlar sonraki bölümler.

#### InitialCreate migration

Komut (Api startup proje, Infrastructure hedef) kabaca: `dotnet ef migrations add InitialCreate`. EF, o anki modele bakıp `Up`/`Down` üretir. Elle `CREATE TABLE` yazmadık.

```12:50:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260703153026_InitialCreate.cs
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Universe = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Biography = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Rarity = table.Column<int>(type: "int", nullable: false),
                    BaseAttack = table.Column<int>(type: "int", nullable: false),
                    BaseDefense = table.Column<int>(type: "int", nullable: false),
                    BaseSpeed = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Universe",
                table: "Characters",
                column: "Universe");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Universe_Name",
                table: "Characters",
                columns: new[] { "Universe", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Characters");
        }
```

`Up` ileri: `Characters` tablosu + iki index. Kolon tipleri configuration ile birebir: `uniqueidentifier`, `nvarchar(200)` required, Biography/ImageUrl nullable. `Down` geri: tabloyu siler. `dotnet ef database update` `Up`’ı çalıştırır.

`*.Designer.cs` ve `ApplicationDbContextModelSnapshot.cs` EF’in otomatik ürettiği kopyadır; onları elle düzenlemez, notlara da alıntılamayız. Snapshot “model şimdi böyle” kaydıdır; bir sonraki migration farkı buradan hesaplar.

Migration bir şema **versiyonudur**. SSMS’te tabloyu elle değiştirirsen bir sonraki `migrations add` ile saparsın. Bundan sonra Users, PasswordHash, Role, RBAC, RefreshTokens hep yeni migration dosyası olarak eklenecek; `InitialCreate`’e kolon yapıştırmayacağız.

#### Bu kodu kim tetikliyor? (o gün: kimse)

2–3 Temmuz’da HTTP endpoint yok. `CreateCharacterCommandHandler` derlenir, DbContext DI’da durur, tablo SQL Server’da oluşur; ama `POST /api/characters` henüz yok. Test yolu Scalar da 7 Temmuz.

Sonradan bağlanan zincir: tarayıcıda `CharacterCreatePage` → `apiFetch('/api/characters', { method: 'POST' })` → `CharactersController.Create` → `Send(CreateCharacterCommand)` → bu handler → `IApplicationDbContext` → `Characters` INSERT. Controller’daki `HasPermission` 22 Ağustos; 2 Temmuz’da yetki yoktu, endpoint de yoktu.

#### Bu adımda yapılan / kalan iz

Character’daki kullanılmayan `using`’ler classlib şablonundan kaldı. Connection string Development json’a yazıldı ve commit’lendi — notların uyarısı “secret’i buraya yapıştırma”; dosyada durduğu ayrı konu.

Sık düşülen hata: handler’da `new Character { Name = ... }` (private ctor + private set bunu engeller) veya Domain’e `Microsoft.EntityFrameworkCore` ekleyip `[Table]` basmak (katman sınırını ilk günden deler). Başka biri: `Add` deyip `SaveChangesAsync` unutmak — bellekte entity vardır, tabloda satır yoktur.

#### Sonuçta ne kazandık

Çalışan bir HTTP API değil; çalışan bir **iskelet**: dört katman kilitli, Character iş nesnesi, “ekle” komutu handler’da, SQL’de `Characters` tablosu. Kapıyı 7 Temmuz’da controller açacak.

---

### 2. 06 Temmuz — MediatR taraması, ValidationBehavior, `AddApplication`

**Commit:** `fa09ee4` (6 Temmuz, “AddApplication - MediatR FluentValidation DI pipeline”).

Bu adımda dosyaları şu sırayla ekledik. Önce `ValidationBehavior.cs`, çünkü 3 Temmuz’daki `CreateCharacterCommandValidator` duruyordu ama kimse onu `Send`’den önce çalıştırmıyordu. Sonra Application’da `DependencyInjection.AddApplication`: handler’ları ve validator’ları assembly taramasıyla DI’ya yazmak, behavior’ı pipeline’a takmak. En sonda `Program.cs`’e `AddApplication()` — bu çağrı olmasa diğer iki dosya derlenir, uygulama ayağa kalkınca hiç kayıt olmazdı. Hâlâ `CharactersController` yok; pipeline kuruldu, HTTP kapısı yarın (bölüm 3).

#### `AddApplication` — üç kayıt, tek metot

```1:25:ReactBattleArena/ReactBattleArena.Application/Common/DependencyInjection.cs
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ReactBattleArena.Application.Common;
using System.Reflection;

namespace ReactBattleArena.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application assembly'sindeki tüm IRequestHandler<,> implementasyonlarını tarar ve DI'a ekler.
        // DeleteCharacterCommandHandler da burada kayıt olur — elle AddScoped yazmana gerek yok.
        // İlgili handler yoksa Send çağrısında "handler bulunamadı" hatası alırsın.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // FluentValidation → Validator'ları bulur (CreateCharacterCommandValidator vb.)
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Pipeline → Her istekte validation çalışır (ValidationBehavior)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
```

Dosya klasörü `Common/`, namespace ise `ReactBattleArena.Application`. Api `using ReactBattleArena.Application;` deyince `AddApplication()` görünür. `this IServiceCollection` extension olduğu için `builder.Services.AddApplication()` yazılır; Infrastructure’daki `AddInfrastructure` ile aynı kalıp.

`Assembly.GetExecutingAssembly()` bu kodun derlendiği assembly’dir: Application. MediatR oradaki tüm `IRequestHandler<,>` sınıflarını bulur (`CreateCharacterCommandHandler` 2 Temmuz’dan beri oradaydı). FluentValidation `AbstractValidator<>` türeyenleri bulur. Yeni handler ekleyince bu metoda satır yazmazsın; tarama alır. Handler’ı unutup `Send` edersen çalışma anında “handler bulunamadı” dersin — derleme uyarısı yok.

`typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)` açık generic kayıttır: her `TRequest` / `TResponse` çifti için aynı behavior sınıfı. `CreateCharacterCommand` → `Guid` için de, sonra gelecek `GetCharactersQuery` için de bu tek satır yeter. `AddTransient` her `Send`’de yeni behavior örneği; hafif bir sınıf, scoped DbContext gibi istek boyu state tutmaz.

6 Temmuz’daki dosyada bu üç çağrı vardı, yorumlar daha kısaydı. `DeleteCharacterCommandHandler` cümlesi 9 Temmuz’da eklendi (o handler henüz yoktu). O gün ayrıca kullanılmayan üç `using` vardı: `Characters.Commands`, `System.Runtime.ConstrainedExecution`, `static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model` — IntelliSense’in yanlış tamamladığı izler; sonra silindi.

#### Program.cs — kaydı gerçekten açmak

```17:19:ReactBattleArena/ReactBattleArena.Api/Program.cs
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

3 Temmuz’da yalnızca `AddInfrastructure` vardı. 6 Temmuz’da `using ReactBattleArena.Application;` ve `AddApplication()` eklendi. Bugünkü `Program.cs` JWT, CORS, Scalar, seeder ile dolu; o satırlar sonraki bölümler. Bu iki çağrının sırası bu gün için önemli değil: ikisi de aynı `IServiceCollection`’a yazar. Eksik olan `AddApplication` olsaydı controller yarın `IMediator` isteyince DI patlardı; `AddInfrastructure` eksik olsaydı handler `IApplicationDbContext` isteyince patlardı.

Eşleme: `Program.cs`’teki `builder.Services.AddXxx()` klasik ASP.NET Core DI kaydıdır. MVC’de `AddControllers` + belki `AddDbContext` tek projede dururdu. Burada Application kendi kaydını kendi assembly’sinde toplar; Api sadece “aç” der. Filter’ı her controller’a `[Validate]` yazmak yerine bir kez pipeline’a takmak gibi — ama HTTP’ye özel değil, her `IMediator.Send` için.

#### ValidationBehavior — handler’dan önce

```1:39:ReactBattleArena/ReactBattleArena.Application/Common/ValidationBehavior.cs
using FluentValidation;
using MediatR;

namespace ReactBattleArena.Application.Common;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

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
}

//Ne yapar? Handler çalışmadan önce validator kurallarını çalıştırır; hata varsa ValidationException fırlatır.
```

Bu sınıf o günden bugüne **değişmedi**. `IPipelineBehavior<TRequest, TResponse>` MediatR’ın “`Send` ile handler arasına gir” sözleşmesi. `where TRequest : notnull` MediatR 12+ kısıtı; null command yok.

Constructor `IEnumerable<IValidator<TRequest>>` ister. DI, o anki command tipi için kayıtlı validator’ları verir. Liste boş olabilir — `GetCharactersQuery` gibi validator’ı olmayan isteklerde `Any()` false olur, doğrudan `next()` çalışır. Tek `IValidator<TRequest>` isteseydin, validator yokken DI “servis bulunamadı” derdi. `IEnumerable` bu yüzden: sıfır, bir veya birden fazla.

Görünmeyen mekanizma pipeline’dır. `IMediator.Send` bir koridordur; `ValidationBehavior` koridorun ilk odası, `next` sonraki oda (başka behavior varsa o, yoksa handler). `await next()` demezsen handler hiç çalışmaz — `Add` / `SaveChanges` da olmaz. Hata varsa `ValidationException` fırlatılır, `next()` çağrılmaz; boş isimle karakter INSERT edilmez.

`Validate` burada senkron. `SelectMany` birden fazla validator’ın `Errors` listesini tek listeye indirir. `failures.Count != 0` ise exception. Bu exception henüz HTTP 400 değildir. 8 Temmuz’daki middleware (`FluentValidationExceptionMiddleware`) onu `ValidationProblemDetails` yapacak. 6 Temmuz’da endpoint de yok; exception tipi hazır, status kodu yok.

#### Validator zaten vardı — şimdi bulunuyor

```5:18:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/CreateCharacterCommandValidator.cs
public sealed class CreateCharacterCommandValidator : AbstractValidator<CreateCharacterCommand>
{
    public CreateCharacterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Universe).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Biography).MaximumLength(2000);
        RuleFor(x => x.Rarity).InclusiveBetween(1, 5);
        RuleFor(x => x.BaseAttack).InclusiveBetween(0, 9999);
        RuleFor(x => x.BaseDefense).InclusiveBetween(0, 9999);
        RuleFor(x => x.BaseSpeed).InclusiveBetween(0, 9999);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}
```

3 Temmuz’da bu sınıf vardı; 6 Temmuz’da `AddValidatorsFromAssembly` onu `IValidator<CreateCharacterCommand>` olarak kaydeder. `Send(new CreateCharacterCommand(...))` olunca behavior `_validators` içinde bunu görür, `Name` boşsa `ValidationException` atar, handler’a düşmez.

Eşleme: MVC’de `[Required]` + `ModelState.IsValid` action’ın başında durur, HTTP model binding’e bağlıdır. Burada kural command tipine bağlıdır. Aynı command’ı testte veya ileride bir background `Send` ile de çalıştırsan validator yine çalışır. Controller `if (!ModelState.IsValid) return BadRequest()` yazmaz; pipeline ortak.

#### Bu kodu kim tetikliyor? (o gün: henüz HTTP yok)

6 Temmuz’da `IMediator.Send` çağıran controller yok. Pipeline DI’da duruyor. 7 Temmuz’da `CharactersController.Create` `Send(CreateCharacterCommand)` deyince sıra şöyle olacak: controller → `Send` → `ValidationBehavior` → (geçerse) `CreateCharacterCommandHandler` → `SaveChanges`. 4 Ağustos’ta `CharacterCreatePage` `apiFetch('/api/characters', { method: 'POST' })` ile o controller’a gidecek; boş isim bu behavior’da takılacak, 8 Temmuz’dan sonra cevap 400 olacak.

#### Bu adımda yapılan / kalan iz

İlk `AddApplication` dosyasına yanlış `using`’ler yapışmıştı (`ConstrainedExecution`, `DbLoggerCategory`). Zararsız, derlemeyi bozmaz; “otomatik import’a güvenme” izi.

Sık düşülen hata: `RegisterServicesFromAssembly`’ye Api assembly’sini vermek — handler’lar Application’dadır, tarama boş kalır, `Send` “handler yok” der. Bir diğeri: behavior’ı kaydedip `next()`’i unutmak — handler hiç çalışmaz. Bir diğeri: validator var sanıp `IValidator<T>` (tekil) inject etmek; query’lerde validator olmayınca uygulama ayağa kalkmaz.

#### Sonuçta ne kazandık

Handler ve validator artık “dosya olarak var” değil, uygulama açılınca bulunuyor ve her `Send` validator’dan geçiyor. HTTP cevabı ve endpoint hâlâ yok; iskelete **otomatik doğrulama koridoru** eklendi.

---

### 3. 07 Temmuz — İlk endpoint: POST /api/characters, Scalar

**Commit:** `65f3c84` (7 Temmuz, “CharactersController POST + Scalar UI”).

Bu adımda dosyaları şu sırayla ekledik. Önce `CreateCharacterRequest`, çünkü JSON body’sinin şekli Application’daki `record` ile aynı olmasın diye ayrı bir HTTP DTO istedik. Sonra `CharactersController` — iki gündür bekleyen `Send(CreateCharacterCommand)` çağrısı nihayet bir action’dan çıkacak. En sonda Scalar: `Scalar.AspNetCore` paketi, `Program.cs`’te `MapScalarApiReference`, `launchSettings.json`’da `launchUrl: scalar/v1`. WeatherForecast şablon controller’ı ve `WeatherForecast.cs` bu commit’te silindi; deneme yüzeyi sahte hava durumu değil, karakter POST’u olsun diye.

#### CreateCharacterRequest — HTTP’nin gördüğü gövde

```1:14:ReactBattleArena/ReactBattleArena.Api/Contracts/CreateCharacterRequest.cs
namespace ReactBattleArena.Api.Contracts;

public sealed class CreateCharacterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Universe { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public int Rarity { get; set; }
    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    public int BaseSpeed { get; set; }
    public string? ImageUrl { get; set; }
}
//Controller command'ı doğrudan almak yerine, dışarıya bir request DTO açıyoruz.
```

Bu sınıf o günden bugüne **alan olarak** aynı (28 Temmuz’da bir ara `Password` eklenip aynı gün kaldırıldı; bugün yok). `class` + `{ get; set; }`: JSON deserializer ve model binder özelliklere değer yazabilsin diye. Command `record`’dur, oluşturulunca değişmez. Request binder’ın doldurduğu kova, command handler’ın aldığı mesaj.

Neden controller `CreateCharacterCommand`’ı `[FromBody]` almaz? HTTP sözleşmesi ile Application mesajı birbirine yapışmasın diye. Dışarıdaki JSON yarın ekstra alan isterse (veya Password gibi yanlış bir alan sızarsa) command’ı kirletmeden request’i değiştirirsin. Yorum satırı tam bunu söylüyor.

Eşleme: ASP.NET MVC’de `CreateCharacterViewModel` veya `[FromBody] SomeDto`. `fetch` tarafında bu, `JSON.stringify({ name, universe, ... })` ile giden gövdedir. 4 Ağustos’ta `CharacterCreatePage` aynı alanları `apiFetch('/api/characters', { method: 'POST', body: JSON.stringify(...) })` ile gönderecek.

#### CharactersController — ince HTTP, kalın handler

7 Temmuz’da bu sınıfta yalnızca `Create` vardı. Bugün GET / PUT / DELETE ve `[HasPermission]` duruyor; onlar 8–9 Temmuz ve 22 Ağustos. Create’in gövdesi aynı `Send` çağrısı:

```12:22:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
[ApiController]//Otomatik model binding + 400 davranışı
[Route("api/[controller]")]
public sealed class CharactersController : ControllerBase
{
    private readonly IMediator _mediator;
    //HTTP → MediatR köprüsü; controller iş mantığı bilmez

    public CharactersController(IMediator mediator)
    {
        _mediator = mediator;
    }
```

`[ApiController]` model binding + otomatik 400: JSON parse edilemezse veya required binding patlarsa MVC `ModelState` üzerinden 400 döner. Bu, FluentValidation’ın `ValidationException`’ı değildir; o hâlâ 8 Temmuz’da middleware ile 400 olacak. `[Route("api/[controller]")]` içinde `[controller]` token’ı sınıf adından `Controller` soneki silinmiş halidir → `/api/characters`.

Constructor `IMediator` ister. Dün `AddApplication` bunu kaydetti; kaydetmeseydin uygulama ayağa kalkınca DI “IMediator yok” derdi. Controller SQL, `Character.Create`, validator kuralı bilmez. Yorumun dediği köprü: HTTP → `Send`.

```46:67:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
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

7 Temmuz’da `[HasPermission]` yoktu; POST açıktı, token da yoktu. Satır 46 22 Ağustos’ta geldi. `[HttpPost]` + `[FromBody]`: istek `POST /api/characters`, gövde JSON. Action `body` alanlarını `CreateCharacterCommand`’a kopyalar, `Send` eder, dönen `Guid` ile `Created(...)` der.

`Created(url, id)` HTTP **201** + `Location` başlığı (`/api/characters/{id}`) + body’de id. 200 “elimde vardı”, 201 “yeni oluştu”. Henüz GET by id yok (yarın / bölüm 4); Location yine de o adresi işaret eder.

`[ProducesResponseType(400)]` Scalar / OpenAPI belgesine “bu action 400 dönebilir” yazar. 7 Temmuz’da middleware yoktu: boş `Name` `ValidationBehavior`’da `ValidationException` olur, işlenmezse cevap **500**’dü. Belge 400 diyordu, gerçek 500’dü — 8 Temmuz bunu kapatacak.

9 Temmuz’da POST bir ara silinip aynı commit’te geri eklendi; bugün duruyor.

#### Scalar — tarayıcıdan deneme

```76:84:ReactBattleArena/ReactBattleArena.Api/Program.cs
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddPreferredSecuritySchemes("Bearer");
    });
}
```

7 Temmuz’da satır `app.MapScalarApiReference();` idi; `Bearer` kilidi JWT ile (27–28 Temmuz) geldi. `MapOpenApi()` JSON şemayı üretir, `MapScalarApiReference()` onu HTML UI olarak sunar. Swagger UI’nin muadili; paketi `Scalar.AspNetCore`. Yalnızca Development’ta açılır — production’da bu blok çalışmaz.

```13:21:ReactBattleArena/ReactBattleArena.Api/Properties/launchSettings.json
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "scalar/v1",
      "applicationUrl": "https://localhost:7275;http://localhost:5189",
```

F5 / `dotnet run` (https profili) tarayıcıyı `https://localhost:7275/scalar/v1` açar. Api adresi o günden beri 7275; Vite 5173 henüz yok, CORS da yok.

Eşleme: Postman veya `.http` dosyası yerine solution içi deneme UI. MVC’de bazen Swagger. React yok; ilk “ekle” tıklaması Scalar’daki POST’tur. 4 Ağustos’ta aynı POST’u `CharacterCreatePage` yapacak.

#### Bu kodu kim tetikliyor?

İlk kez HTTP var. Scalar veya herhangi bir client `POST https://localhost:7275/api/characters` + JSON → `Create` → `Send` → dünkü `ValidationBehavior` → `CreateCharacterCommandHandler` → `Characters` INSERT → 201 + id.

Frontend henüz yok. Sonradan: `CharacterCreatePage` → `apiFetch('/api/characters', { method: 'POST' })` → bu action. `[HasPermission]` o gün yoktu; şimdi Admin (veya `characters.create` yetkisi) olmadan 403.

#### Bu adımda yapılan / kalan iz

WeatherForecast silindi — şablon “çalışıyor” yanılsaması bitsin diye. `[ProducesResponseType(400)]` yazıldı ama boş isim hâlâ 500; belge ile davranış ayrıldı. Controller’a iş koymamak (SQL, `Character.Create`) bilinçli: kalın kısım handler’da.

Sık düşülen hata: `[Route("api/characters")]` elle yazıp sınıf adını değiştirince sapmak; `[controller]` token’ı buna karşı. Bir diğeri: `Ok(id)` deyip 200 dönmek — client “yeni kaynak” olduğunu `201` + `Location`’dan anlar. Bir diğeri: Scalar’da Character JSON’unu yanlışlıkla Register endpoint’ine yapıştırmak (Register 16 Temmuz’da gelecek; CHECKPOINT’te bu karışıklık notlu).

#### Sonuçta ne kazandık

İskelet artık **çağrılabilir**: tarayıcıdan POST, geçerli gövdeyle satır oluşur, id 201 ile döner. Doğrulama exception’ı hâlâ düzgün 400 değil; liste/detay GET yok. O ikisi yarın.

---

### 4. 08 Temmuz — Validation 400 ve Character GET

**Commit:** `1d603c6` (8 Temmuz, “FluentValidationExceptionMiddleware + GetCharacters / GetCharacterById”).

Bu adımda dosyaları şu sırayla ekledik. Önce `FluentValidationExceptionMiddleware`, çünkü dünkü POST boş `Name`’de `ValidationException` 500 oluyordu; belgede 400 yazıyordu. Sonra `UseFluentValidationExceptionHandler` extension’ı — `Program.cs` `UseMiddleware<...>` kalabalığı olmasın diye. `Program.cs`’e çağrıyı `MapControllers`’tan **önce** koyduk, yoksa `try/catch` controller’ı sarmaz. Ardından okuma tarafı: `GetCharactersQuery` + handler (sayfalı liste), `GetCharacterByIdQuery` + handler (tek kayıt), controller’a iki GET. CQRS’in Q’su burada başladı; şimdiye kadar yalnızca C (Create) vardı.

#### Middleware — exception’ı 400 JSON yapmak

```1:44:ReactBattleArena/ReactBattleArena.Api/Middleware/FluentValidationExceptionMiddleware.cs
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ReactBattleArena.Api.Middleware;

public sealed class FluentValidationExceptionMiddleware
{ //Middleware ValidationException yakalar
    //Hata olunca exception'ı HTTP 400'e çevirir
    private readonly RequestDelegate _next;

    public FluentValidationExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

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

            var problem = new ValidationProblemDetails// ValidationProblemDetails Standart 400 JSON formatı
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Errors = errors
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
```

Bu sınıf o günden bugüne **değişmedi**. ASP.NET Core’da middleware, `InvokeAsync` içinde `_next`’i çağırarak isteği zincirdeki sonrakine verir. Görünmeyen kısım sarmalamadır: `try` içindeki `_next` “bundan sonra kayıtlı her şey”dir (HTTPS, auth, controller, MediatR, handler). `ValidationBehavior` exception fırlatınca exception bu `catch`’e kadar çıkar. Başka exception türleri yakalanmaz; onlar hâlâ 500’dür.

`ex.Errors` FluentValidation’ın alan + mesaj listesidir. `GroupBy(PropertyName)` aynı alana birden fazla kural kırılırsa dizi yapar (`Name: ["must not be empty"]`). Boş property adı `"_"` olur — modele bağlı olmayan hata. `ValidationProblemDetails` ASP.NET’in standart 400 gövdesidir; `application/problem+json`. React tarafı sonradan `response.json()` ile `errors` sözlüğünü okuyabilir.

Eşleme: MVC’de `if (!ModelState.IsValid) return BadRequest(ModelState);` her action’da veya bir filter’da. Burada action hiçbir şey yazmaz; `Send` exception atar, HTTP katmanı çevirir. `[ApiController]`’ın otomatik 400’ü binding içindir (JSON bozuk, tip uymaz). FluentValidation kuralı (`Name` boş ama string gelmiş) binding’den geçer, pipeline’da patlar; o yüzden ayrı middleware şart.

#### Extension ve sıra

```1:12:ReactBattleArena/ReactBattleArena.Api/Extensions/ApplicationBuilderExtensions.cs
using ReactBattleArena.Api.Middleware;
//Extension Program.cs temiz kalır

namespace ReactBattleArena.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseFluentValidationExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<FluentValidationExceptionMiddleware>();
    }
}
```

Tek satırlık sarmalayıcı: `Program.cs` middleware sınıf adını tekrar etmesin. `this IApplicationBuilder` — dünkü `AddApplication`’ın `IServiceCollection` extension’ı ile aynı fikir, bu kez istek pipeline’ı.

```86:93:ReactBattleArena/ReactBattleArena.Api/Program.cs
app.UseFluentValidationExceptionHandler();
app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

8 Temmuz’da CORS ve Authentication yoktu; `UseFluentValidationExceptionHandler()` + `UseHttpsRedirection` + `UseAuthorization` + `MapControllers` vardı. Önemli olan sıra: exception middleware **`MapControllers`’tan önce**. Tersine koyarsan `_next` controller’ı kapsamaz, `ValidationException` yine 500’e düşer.

Boş `Name` ile POST artık 400 + problem JSON. Handler `Add` / `SaveChanges` çalışmaz — behavior `next()` demeden fırlatır, catch HTTP’yi bitirir.

#### Query: sayfalı liste

Command yazardı; query okur. `IRequest<PagedCharacterRowsResult>` — “bu mesajın cevabı sayfa + toplam.”

```1:21:ReactBattleArena/ReactBattleArena.Application/Characters/Queries/GetCharactersQuery.cs
using MediatR;

namespace ReactBattleArena.Application.Characters.Queries;

public sealed record GetCharactersQuery(int Page, int PageSize)
    : IRequest<PagedCharacterRowsResult>;

public sealed record CharacterRowDto(
    Guid Id,
    string Name,
    string Universe,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    string? ImageUrl,
    DateTime CreatedAtUtc);

public sealed record PagedCharacterRowsResult(
    IReadOnlyList<CharacterRowDto> Items,
    int TotalCount);
```

Query, satır DTO’su ve sayfa sonucu aynı dosyada. `CharacterRowDto`’da `Biography` yok — liste kartı kısa tutulsun, detay ayrı. Bu üç tip o günden bugüne **değişmedi**. Bu query’nin validator’ı yok; bölüm 2’deki `if (!_validators.Any()) return await next();` yüzünden behavior doğrudan handler’a geçer.

```17:46:ReactBattleArena/ReactBattleArena.Application/Characters/Queries/GetCharactersQueryHandler.cs
    public async Task<PagedCharacterRowsResult> Handle(
        GetCharactersQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.Characters
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CharacterRowDto(
                c.Id,
                c.Name,
                c.Universe,
                c.Rarity,
                c.BaseAttack,
                c.BaseDefense,
                c.BaseSpeed,
                c.ImageUrl,
                c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedCharacterRowsResult(items, total);
    }
```

`Math.Max(1, page)` — 0 veya negatif sayfa 1 olur. `Math.Clamp(pageSize, 1, 200)` — 0 istenirse 1, 10_000 istenirse 200; tabloyu tek istekte boşaltmayı keser. Controller varsayılanı `pageSize = 20`; 20 clamp aralığında kalır.

`AsNoTracking()` okumadır: EF değişikliği izlemez, `SaveChanges` zaten yok. `OrderByDescending(CreatedAtUtc)` **Skip/Take’den önce** — sırasız skip her seferinde başka satır kaçırır. `CountAsync` aynı `query` üzerinden toplam (sayfa sayısı = ceil(total/pageSize) frontend’de). `Skip((page - 1) * pageSize)` 0 tabanlı ofset: sayfa 1 → Skip 0. `Select` projeksiyonu SQL’de çalışır; entity’yi belleğe çekip map etmeyiz, `Biography` SELECT’e girmez.

12 Temmuz’da `GetUsersQueryHandler` aynı Max / Clamp / Skip kalıbını kopyalayacak.

#### Query: tek kayıt

```1:15:ReactBattleArena/ReactBattleArena.Application/Characters/Queries/GetCharacterByIdQuery.cs
using MediatR;
namespace ReactBattleArena.Application.Characters.Queries;

public sealed record GetCharacterByIdQuery(Guid Id) : IRequest<CharacterDetailDto?>;
public sealed record CharacterDetailDto(
    Guid Id,
    string Name,
    string Universe,
    string? Biography,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    string? ImageUrl,
    DateTime CreatedAtUtc);
```

Cevap `CharacterDetailDto?` — yoksa `null`. Detayda `Biography` var; liste DTO’sunda yoktu.

```17:36:ReactBattleArena/ReactBattleArena.Application/Characters/Queries/GetCharacterByIdQueryHandler.cs
    public async Task<CharacterDetailDto?> Handle(
        GetCharacterByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Characters
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CharacterDetailDto(
                c.Id,
                c.Name,
                c.Universe,
                c.Biography,
                c.Rarity,
                c.BaseAttack,
                c.BaseDefense,
                c.BaseSpeed,
                c.ImageUrl,
                c.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }
```

`FirstOrDefaultAsync` satır yoksa `null`. Handler 404 bilmez; HTTP’ye çevirmek controller’ın işi. `Where` + `Select` yine SQL. Bu iki handler da o günden bugüne **değişmedi**. `AddMediatR` taraması dünden beri açık; yeni handler için `AddApplication`’a satır yazılmadı.

#### Controller GET

```24:44:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
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

`[FromQuery]` → `GET /api/characters?page=1&pageSize=20`. `{id:guid}` route kısıtı: `/api/characters/Franky` bu action’a bind olmaz (ileride React’te isimle URL denemek 404). `NotFound()` 404, `Ok` 200. GET’lerde `[HasPermission]` yok; o gün de bugün de liste/detay açık (yazma 22 Ağustos’ta kilitlenecek).

#### Bu kodu kim tetikliyor?

POST boş isim: Scalar / sonra `CharacterCreatePage` → `Create` → `Send` → `ValidationBehavior` exception → bu middleware → **400** + `errors.Name`.

Liste: 30 Temmuz `CharactersPage` `GET /api/characters?page&pageSize` → `GetPaged` → bu query. Detay: 5 Ağustos `CharacterDetailPage` `GET /api/characters/:id` → `GetById`. 8 Temmuz’da frontend yok; Scalar’dan iki GET denendi.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: middleware’i `MapControllers`’tan sonra yazmak — 500 devam eder. Bir diğeri: `Skip/Take` deyip `OrderBy` unutmak — her istekte kayan sayfa. Bir diğeri: `pageSize`’ı handler’da sınırlamamak — `?pageSize=100000` tüm tabloyu çeker. `CountAsync`’i Skip’ten sonraki query’de yapmak da yanlış toplam verir; toplam filtrelenmiş kaynağın tamamı olmalı, o yüzden `Count` `Skip`’ten önce, aynı `query` üzerinde.

#### Sonuçta ne kazandık

Yazma doğrulaması artık gerçek **400**. Okuma var: sayfalı liste + id ile detay (yoksa 404). Karakter CRUD’sunun C ve R’si tamam; U ve D yarın (9 Temmuz).

---

### 5. 09 Temmuz — Update / Delete, 204, tam Character CRUD

**Commit:** `6bd07f6` (9 Temmuz, “update delete crud + POST geri eklendi + MediatR yorumları”).

Bu adımda dosyaları şu sırayla ekledik. Önce `UpdateCharacterCommand` + validator + handler — 2 Temmuz’daki `Character.Update` nihayet bir yazma komutundan çağrılacak. Sonra `DeleteCharacterCommand` + handler. En sonda controller’a `PUT` ve `DELETE`. Aynı commit’te POST Create bir ara silinmiş, geri eklenmiş; bugün duruyor. `DependencyInjection` ve Delete dosyalarına MediatR “tip eşleşmesi” yorumları yazıldı — `Send`’in handler’ı isimle değil generic arayüzle bulduğunu o gün netleştirmek için.

#### Update command ve validator

```1:15:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/UpdateCharacterCommand.cs
using MediatR;

namespace ReactBattleArena.Application.Characters.Commands;

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
//IRequest<bool> → bulundu ve güncellendiyse true, yoksa false.
```

Create `IRequest<Guid>` idi (yeni id). Update `IRequest<bool>`: kayıt yoksa `false`, controller onu 404 yapacak. `Id` command’da var; HTTP’de URL’den gelecek, JSON body’sinden değil.

```1:19:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/UpdateCharacterCommandValidator.cs
using FluentValidation;

namespace ReactBattleArena.Application.Characters.Commands;

public sealed class UpdateCharacterCommandValidator : AbstractValidator<UpdateCharacterCommand>
{
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
}
```

Create validator’ının kopyası + `Id.NotEmpty()` (`Guid.Empty` olmasın). `AddValidatorsFromAssembly` bunu kendiliğinden alır. Boş isimle PUT → yine `ValidationException` → dünkü middleware → 400; handler’a inilmez.

#### Update handler — izlenen entity

```16:36:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/UpdateCharacterCommandHandler.cs
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

GET handler’larında `AsNoTracking` vardı; burada **yok**. Görünmeyen mekanizma EF change tracking: `FirstOrDefaultAsync` entity’yi bağlama alır, `private set` alanların eski değerini bilir. `entity.Update(...)` (2 Temmuz, Domain) setter’ları içeriden çağırır. `SaveChangesAsync` farkı görür, `UPDATE` SQL üretir. `AsNoTracking` koysaydın `Update` bellekte kalır, tablo değişmezdi.

Kayıt yoksa `false` — exception yok, 404’ü HTTP katmanı seçer. Create’deki `Add` yeni satırdı; burada mevcut satırın üzerinde `Update` metodu. Handler alanları tek tek `entity.Name = ...` yazamaz (`private set`); factory/metot kuralı hâlâ duruyor.

#### Delete — aynı bool, Remove

```3:6:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/DeleteCharacterCommand.cs
namespace ReactBattleArena.Application.Characters.Commands;

// MediatR, command tipine göre handler'ı DI'dan bulur. Özel bir "yakalama" yok; tip eşleşmesi vardır.
public sealed record DeleteCharacterCommand(Guid Id) : IRequest<bool>;
```

Tek alan: silinecek `Id`. Validator yok; `ValidationBehavior` boş liste görünce `next()` der.

```7:30:ReactBattleArena/ReactBattleArena.Application/Characters/Commands/DeleteCharacterCommandHandler.cs
// IRequestHandler<DeleteCharacterCommand, bool> → MediatR, Send(DeleteCharacterCommand) gelince bu sınıfı seçer.
// Akış: ValidationBehavior (varsa) → Handle → bool döner. "Yakalama" = generic interface eşleşmesi.
public sealed class DeleteCharacterCommandHandler : IRequestHandler<DeleteCharacterCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public DeleteCharacterCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

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
}
```

Yoksa `false` (404). Varsa `Remove` + `SaveChanges` → `DELETE` SQL. Yorum o günün kafa karışıklığını çözüyor: MediatR event bus gibi isim dinlemez. `Send(new DeleteCharacterCommand(id))` der, DI’dan `IRequestHandler<DeleteCharacterCommand, bool>` ister, tarama `DeleteCharacterCommandHandler`’ı vermiştir.

```13:16:ReactBattleArena/ReactBattleArena.Application/Common/DependencyInjection.cs
        // Application assembly'sindeki tüm IRequestHandler<,> implementasyonlarını tarar ve DI'a ekler.
        // DeleteCharacterCommandHandler da burada kayıt olur — elle AddScoped yazmana gerek yok.
        // İlgili handler yoksa Send çağrısında "handler bulunamadı" hatası alırsın.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
```

Bu üç yorum satırı bu commit’te eklendi. Yeni handler için `AddScoped<DeleteCharacterCommandHandler>` yazılmaz.

#### Controller: 204 veya 404

```69:107:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
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

    [HasPermission(PermissionCodes.CharactersDelete)]  // DELETE
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Controller command'ı Send ile yollar → MediatR tipine bakar → IRequestHandler'ı DI'dan alır → Handle çalıştırır.
        // Gelen tip: DeleteCharacterCommand → aranan: IRequestHandler<DeleteCharacterCommand, bool> → bulunan: DeleteCharacterCommandHandler
        var deleted = await _mediator.Send(new DeleteCharacterCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
```

9 Temmuz’da `[HasPermission]` yoktu; PUT/DELETE açıktı. `id` route’tan (`{id:guid}`), alanlar `CreateCharacterRequest` — Create ile aynı JSON şekli, ayrı Update DTO yok. `Id` body’sinde yok; URL’deki id command’a gider, client body’den id değiştiremez.

`NoContent()` HTTP **204**: işlem oldu, gövde yok. 200 + güncel karakter JSON’u da olabilirdi; biz “başarı = boş cevap” dedik. `false` → `NotFound()` 404. PUT’da 400 hâlâ validator + middleware.

Eşleme: MVC’de `return NoContent();` / `return NotFound();`. `fetch` tarafında 204’te `response.json()` çağırma — body yoktur. 6 Ağustos’ta `CharacterEditPage` PUT 204 bekleyecek, detay sayfası DELETE öncesi `confirm` sonra 204.

#### Bu kodu kim tetikliyor?

Scalar `PUT /api/characters/{id}` + JSON → Update action → command → handler → `Character.Update` → SQL UPDATE → 204. `DELETE /api/characters/{id}` → Remove → 204. Olmayan guid → 404.

Frontend henüz yok. Sonradan: `CharacterEditPage` PUT, `CharacterDetailPage` DELETE. Yetki satırları 22 Ağustos.

#### Bu adımda yapılan / kalan iz

POST bu commit’te silinip geri geldi — CRUD’u PUT/DELETE’e bağlarken Create’i kaybetmeme. Sık düşülen hata: Update’de `AsNoTracking` (değişiklik SQL’e yansımaz). Bir diğeri: başarıda `Ok(entity)` dönüp 204 sözleşmesini bozmak; React `json()` bekler, 204’te patlar. Bir diğeri: handler bulunamadı — tarama Application assembly’sinde, Delete handler’ı ekledin ama projeyi yeniden derlemeden eski Api process çalışıyor olabilir.

#### Sonuçta ne kazandık

Character HTTP CRUD tamam: POST 201, GET liste/detay, PUT 204, DELETE 204, yoksa 404, kural ihlali 400. İkinci aggregate (User) 12 Temmuz. Frontend 29 Temmuz.

---

### 6. 12 Temmuz — User modülü, unique index, AddUsers (controller henüz yok)

**Commit:** `8482493` (12 Temmuz, “User domain crud persistence … UsersController eksik”).

Bu adımda dosyaları şu sırayla ekledik. Önce Domain’de `User` — Character ile aynı kalıp (private ctor, `Create`, `Update`), ikinci aggregate. Sonra Application’da Create / Update / Delete command’ları ve GetUsers / GetUserById query’leri; `AddMediatR` taraması zaten açık, `AddApplication`’a yeni satır yazılmadı. Infrastructure’da `UserConfiguration`, `DbSet<User>`, migration `AddUsers`. Api’de `CreateUserRequest` duruyordu ama **`UsersController` yoktu** — commit mesajı bunu yazar. Şablon `Controllers/Class.cs` bu gün görünür, sonra silinir. HTTP kapısı 16 Temmuz’da Register ile açılacak.

#### User entity — 12 Temmuz’da şifre ve rol yoktu

```1:73:ReactBattleArena/ReactBattleArena.Domain/Users/User.cs
namespace ReactBattleArena.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    public string UserName { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string? DisplayName { get; private set; }

    public string PasswordHash { get; private set; } = null!;

    // Arena / ödül için; Auth sonrası da kullanılacak
    public int Points { get; private set; }

    public string Role { get; private set; } = null!;
    //= null!; = “derleyiciye: başlangıçta null görünebilir ama runtime’da asla null kalmayacak” demek.
    //null-forgiving (!) işareti

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

    public void Update(string userName, string email, string? displayName)
    {
        UserName = userName;
        Email = email;
        DisplayName = displayName;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void AddPoints(int amount)
    {
        if (amount <= 0)
            return;

        Points += amount;
    }

    public void SetRole(string role)
    {
        Role = role;
    }
}
```

12 Temmuz’da `PasswordHash`, `Role`, `SetPasswordHash`, `SetRole` yoktu. `Create` yalnızca `userName, email, displayName, utcNow` alıyordu; `Points = 0` factory’de. `AddPoints` arena için şimdiden duruyordu, o gün kimse çağırmıyordu. `null!` Character’dakiyle aynı: EF doldurana kadar uyarıyı kapat.

`Update` şifre ve rolü değiştirmez — profil alanları ayrı, parola ayrı (`SetPasswordHash`, 16 Temmuz). `Role` string’i 28 Temmuz’da gelecek, 20 Ağustos’ta `UserRoles` tablosuna taşınacak; kolon bir süre durur.

#### Unique index — Character’de olmayan kural

```1:24:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactBattleArena.Domain.Users;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DisplayName).HasMaxLength(100);

        builder.HasIndex(x => x.UserName).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Role).IsRequired().HasMaxLength(50);
        //UserName ve Email unique — aynı kullanıcı / mail iki kez eklenemez.
    }
}
```

12 Temmuz’da `PasswordHash` ve `Role` satırları yoktu; unique index’ler vardı. Character’de `(Universe, Name)` unique değildi; iki aynı isim olabilirdi. Kullanıcıda aynı `UserName` veya `Email` iki satır olamaz — SQL unique index. Validator `EmailAddress()` format bakar, teklik DB’dedir. 12 Temmuz’da aynı mail ikinci kez eklenirse SQL exception, büyük ihtimalle **500**. Duplicate’i 400 yapan `ValidationFailure` 16 Temmuz’da gelecek.

`ApplyConfigurationsFromAssembly` (bölüm 1) bu sınıfı kendiliğinden alır.

#### AddUsers migration — InitialCreate’e kolon yok

Migration dosya tarihi 10 Temmuz (`20260710134419`), commit 12 Temmuz. `InitialCreate`’e `Users` yapıştırmadık; yeni versiyon.

```12:48:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260710134419_AddUsers.cs
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Points = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
```

`Up` tablosu PasswordHash / Role içermez — onlar sonraki migration’lar. Unique index’ler `IX_Users_Email` ve `IX_Users_UserName`. `Down` tabloyu siler.

Aynı gün `IApplicationDbContext` ve `ApplicationDbContext`’e `DbSet<User> Users` eklendi. Bugünkü arayüzde RBAC ve RefreshToken set’leri de var; 12 Temmuz’da yalnızca `Characters` + `Users` + `SaveChangesAsync` vardı.

#### Create CQRS — o gün şifresiz

```1:9:ReactBattleArena/ReactBattleArena.Application/Users/Commands/CreateUserCommand.cs
using MediatR;

namespace ReactBattleArena.Application.Users.Commands;

public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string? DisplayName,
    string Password) : IRequest<Guid>;
```

12 Temmuz’da `Password` yoktu. 16 Temmuz’da eklendi. Validator o gün UserName 50, Email `EmailAddress()` 200, DisplayName 100; `Password` kuralı da 16 Temmuz.

Handler o gün Character Create’in kopyasıydı: `User.Create(...)`, `_db.Users.Add`, `SaveChanges`, `Id` dön. Bugünkü handler `IPasswordHasher` ve `Roles.Player` kullanır — bölüm 7–9. Update/Delete `IRequest<bool>` ve 204/404 kalıbı Character ile aynı; GetById `UserDetailDto?` (PasswordHash yok — liste/detay şifreyi dışarı vermez).

#### Paging — Character ile aynı üç satır

```17:42:ReactBattleArena/ReactBattleArena.Application/Users/Queries/GetUsersQueryHandler.cs
    public async Task<PagedUserRowsResult> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAtUtc);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserRowDto(
                u.Id,
                u.UserName,
                u.Email,
                u.DisplayName,
                u.Points,
                u.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedUserRowsResult(items, total);
    }
```

`Math.Max` / `Clamp` / `AsNoTracking` / `OrderBy` / `Count` / `Skip` / `Take` bölüm 4’teki GetCharacters ile aynı fikir, `_db.Users`. `UserRowDto`’da şifre yok. Bu handler o günden bugüne **projeksiyon olarak** aynı.

#### Controller neden yoktu?

`CreateUserRequest` (UserName, Email, DisplayName) Api’de duruyordu; `Send` edecek action yoktu. Character’de 2 Temmuz handler, 7 Temmuz POST arası gibi: iş katmanı hazır, HTTP yarın. `UsersController` bugün `api/users` CRUD + `[Authorize]`; o 16–27 Temmuz. React Register `POST /api/auth/register` kullanır, bu admin `CreateUser` değil — karıştırma.

#### Bu kodu kim tetikliyor? (o gün: Scalar’dan user POST yok)

12 Temmuz’da User’ı HTTP ile ekleyemezdin. Tablo `database update` ile oluşur, handler derlenir. 16 Temmuz `POST /api/auth/register` ve Users Create bu handler’lara bağlanacak. Frontend Register 30–31 Temmuz.

#### Bu adımda yapılan / kalan iz

`Class.cs` şablon controller — `dotnet new` artığı; silindi. Unique’i yalnızca validator’da sanmak: DB index yoksa yarışta iki aynı email girer. Unique var, güzel 400 yok: SQL patlar (500) ta ki Register duplicate’i yakalayana kadar.

Sık düşülen hata: `InitialCreate`’i elle düzenleyip Users eklemek — geçmiş migration değişmez, yeni `AddUsers` gerekir. Bir diğeri: GetUsers DTO’suna `PasswordHash` koymak.

#### Sonuçta ne kazandık

İkinci aggregate: `Users` tablosu, unique UserName/Email, Character ile aynı CQRS iskeleti. Henüz parola, rol, login, HTTP yok. Auth 16 Temmuz.

---

### 7. 16 Temmuz — Register, BCrypt, duplicate 400

**Commit:** `a8bc8b5` (16 Temmuz, “register password … duplicate username email ValidationFailure 400”).

Bu adımda dosyaları şu sırayla ekledik. Önce Domain’e `PasswordHash` + `SetPasswordHash` ve migration `AddUserPasswordHash` — şifre düz kolon olmasın diye. Sonra Application’da `IPasswordHasher` (sözleşme), Infrastructure’da `BCryptPasswordHasher` (somut), `AddSingleton` kaydı — Application BCrypt NuGet’ine bakmasın diye, `IApplicationDbContext` ile aynı ok. Sonra `RegisterCommand` + validator + handler (duplicate `AnyAsync` → `ValidationException`) ve `AuthController` `POST /api/auth/register`. Aynı gün `UsersController` ve `CreateUserCommand` da parolayı hash’lemeye başladı. Klasör adı `Authentication` (kimsin); `Authorization` (ne yapabilirsin) 27–28 Temmuz.

#### Hash sözleşmesi ve BCrypt

```1:8:ReactBattleArena/ReactBattleArena.Application/Abstractions/IPasswordHasher.cs
namespace ReactBattleArena.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password,string PasswordHash);
}
```

`Hash` kayıtta, `Verify` login’de (bölüm 8). Application yalnızca bu arayüzü görür. `Verify` ikinci parametre adı `PasswordHash` — C# izin verir, alışılmadık; implementasyon `passwordHash` yazar. `bool Verify` 16 Temmuz’da duruyordu, çağıran login 22 Temmuz.

```1:16:ReactBattleArena/ReactBattleArena.Infrastructure/Security/BCryptPasswordHasher.cs
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
```

BCrypt çıktısı geri çözülmez; “şifreyi aç” diye bir metot yoktur. `Verify` aday parolayı hash ile karşılaştırır, eşitse `true`. Aynı parola her `Hash`’te farklı string üretebilir (salt); bu yüzden DB’de hash saklanır, login’de düz karşılaştırma yapılmaz.

```25:25:ReactBattleArena/ReactBattleArena.Infrastructure/DependencyInjection.cs
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
```

Singleton: hasher state tutmaz, istek boyu scoped olmasına gerek yok. Jwt ve refresh kayıtları bu dosyada sonra eklendi.

Eşleme: ASP.NET Identity `IPasswordHasher<TUser>` aynı fikir. Düz `SHA256(password)` yetmez (rainbow table); BCrypt salt + work factor ekler.

#### Kolon: AddUserPasswordHash

```12:28:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260716105419_AddUserPasswordHash.cs
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");
        }
```

`AddUsers` tablosuna kolon. `defaultValue: ""` mevcut satırlar (varsa) boş hash ile geçsin diye — 16 Temmuz’da büyük ihtimalle boş tablo. `UserConfiguration` `HasMaxLength(500)` aynı gün. Düz `Password` kolonu yok.

#### Register command, kural, duplicate → 400

```1:9:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommand.cs
using MediatR;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed record RegisterCommand(
    string UserName,
    string Email,
    string? DisplayName,
    string Password) : IRequest<Guid>;
```

```6:14:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommandValidator.cs
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.DisplayName).MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}
```

Kısa şifre pipeline’da 400, handler’a inmez. Unique çakışma validator’da yok — o DB’ye bakmalı.

```22:62:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommandHandler.cs
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
            Roles.Player,
            DateTime.UtcNow);

        _db.Users.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var playerRole = await _db.Roles.SingleAsync(
            r => r.Name == Roles.Player, cancellationToken);
        //Rol yoksa (seed çalışmamış) sessizce geçme, patlat ki fark edesin.

        _db.UserRoles.Add(UserRole.Create(entity.Id, playerRole.Id));
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
```

`AnyAsync` unique index patlamadan önce “alınmış mı” bakar. `ValidationException` + `ValidationFailure` bölüm 4’teki middleware’e düşer → **400** + alan adı. SQL unique hâlâ emniyet ağıdır (yarış: iki istek aynı anda `AnyAsync` false görebilir); o durumda 500 olabilir.

16 Temmuz’da `Roles.Player` ve `UserRoles` yoktu; `User.Create(..., passwordHash, utcNow)` idi, tek `SaveChanges`. `Roles.Player` 28 Temmuz, `UserRoles` satırı 24 Ağustos. Ham `request.Password` DB’ye yazılmaz; `passwordHash` yazılır.

`CreateUserCommandHandler` aynı gün `IPasswordHasher.Hash` kullanır — admin’in `POST /api/users` de düz parola saklamasın.

#### AuthController — ilk auth HTTP

```31:44:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
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

16 Temmuz’da `[AllowAnonymous]` yoktu (global Authorize de yoktu). 27 Temmuz’da eklendi. Route `api/[controller]` + `register` → `POST /api/auth/register`. 201 + `Location /api/users/{id}`. Login ve `/me` bu sınıfta sonra.

```1:10:ReactBattleArena/ReactBattleArena.Api/Contracts/RegisterRequest.cs
namespace ReactBattleArena.Api.Contracts
{
    public sealed class RegisterRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}
```

File-scoped namespace değil, süslü parantez — Character request’ten farklı stil, davranış aynı. JSON’da `password` gelir; cevapta hash dönmez, yalnızca `Guid`.

#### Bu kodu kim tetikliyor?

Scalar `POST /api/auth/register`. 30–31 Temmuz `RegisterPage` aynı URL. `CreateUser` (`POST /api/users`) ayrı kapı: kayıt vs admin kullanıcı açma. CHECKPOINT uyarısı: Scalar’da Character JSON’unu Register’a yapıştırma — UserName/Email/Password 400’ü odur.

#### Bu adımda yapılan / kalan iz

`Authentication` klasörü Authorization ile karışmasın diye özellikle seçildi. Sık düşülen hata: parolayı `User` DTO’sunda geri vermek. Bir diğeri: duplicate’i yakalamadan unique index’e bırakmak — 500. Bir diğeri: `Hash` deyip `Verify`’de düz string karşılaştırmak — BCrypt hash `==` parola asla tutmaz.

#### Sonuçta ne kazandık

Kayıt var: şifre hash, kısa şifre 400, alınmış kullanıcı/mail 400, 201 + id. Login ve JWT yok; `Verify` henüz çağrılmıyor. 22 Temmuz.

---

### 8. 22 Temmuz — Login, JWT, UseAuthentication

**Commit:** `9a32d34` (22 Temmuz, “login jwt … LoginResult token test edildi”).

Bu adımda dosyaları şu sırayla ekledik. Önce `JwtOptions` + `appsettings` `Jwt` bölümü (Key, Issuer, Audience, ExpireMinutes) — imza anahtarı kodda hard-code olmasın. Sonra `IJwtTokenService` / `JwtTokenService` ve DI `AddSingleton`. Sonra `LoginCommand` + validator + handler: `Verify` ilk kez çağrılır, başarıda token üretilir, başarısızda `null`. `AuthController` `POST /api/auth/login` `null` → 401, dolu → 200 + `LoginResult`. En sonda `Program.cs` `AddAuthentication(JwtBearer)` ve `UseAuthentication` — `UseAuthorization`’dan **önce**. 22 Temmuz’da action’larda henüz `[Authorize]` yoktu; token üretilir, kapılar yarın kilitlenir (bölüm 9).

#### JwtOptions ve servis

```1:15:ReactBattleArena/ReactBattleArena.Infrastructure/Security/JwtOptions.cs
namespace ReactBattleArena.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpireMinutes { get; set; } = 60;
    public int RefreshExpireDays { get; set; } = 7;
}
```

`SectionName = "Jwt"` → `Configure<JwtOptions>(configuration.GetSection("Jwt"))`. 22 Temmuz’da `RefreshExpireDays` yoktu (28–29 Ağustos). Key değerini notlara kopyalamıyorum; `appsettings.Development.json` içinde, 32 karakter kuralı HMAC için.

```1:7:ReactBattleArena/ReactBattleArena.Application/Abstractions/IJwtTokenService.cs
using ReactBattleArena.Domain.Users;

namespace ReactBattleArena.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
```

Application `User` alır, string token döner; JWT kütüphanesini Infrastructure bilir.

```20:41:ReactBattleArena/ReactBattleArena.Infrastructure/Security/JwtTokenService.cs
    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
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

Görünmeyen mekanizma imzadır: sunucu token’ı tabloda saklamaz. Üç parça (header.payload.imza) HMAC-SHA256 ile Key’e bağlanır; Api gelen token’ı aynı Key ile doğrular. Key sızarsa herkes token basar.

22 Temmuz’da `ClaimTypes.Role` yoktu (`User.Role` kolonu 28 Temmuz). `sub` / `NameIdentifier` aynı id — `/me` iki isimden birini arar. `ExpireMinutes` (varsayılan 60) `expires` claim’i. BCrypt hash ≠ JWT imzası: biri parolayı saklar, öbürü isteği imzalar.

#### Login: aynı 401, token ya da hiç

```1:14:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LoginCommand.cs
using MediatR;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed record LoginCommand(string UserNameOrEmail, string Password)
    : IRequest<LoginResult?>;

public sealed record LoginResult(
    Guid UserId,
    string UserName,
    string Email,
    string Token,
    string RefreshToken);
//null → kullanıcı yok / şifre yanlış → controller 401.
```

`IRequest<LoginResult?>` — yok/yanlış şifre `null`, controller 401. 22 Temmuz’da `RefreshToken` alanı yoktu (29 Ağustos). `UserNameOrEmail`: tek kutu, kullanıcı adı veya mail.

```28:41:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LoginCommandHandler.cs
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

        var token = _jwtTokenService.CreateToken(user);
```

Kullanıcı yok **ve** şifre yanlış **aynı** `null`. “Bu email kayıtlı değil” sızmaz. `Verify` 16 Temmuz’daki BCrypt. Bugünkü handler ardından refresh satırı yazar (bölüm 33); 22 Temmuz’da `return new LoginResult(..., token);` ile bitiyordu.

Boş kullanıcı adı validator’da 400 (`NotEmpty`, şifre min 6) — bu 401 değil, kural ihlali.

```46:59:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
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

`POST /api/auth/login`. `[AllowAnonymous]` 27 Temmuz. 200 body’sinde token (ve bugün refresh). 401 gövdesiz.

```1:8:ReactBattleArena/ReactBattleArena.Api/Contracts/LoginRequest.cs
namespace ReactBattleArena.Api.Contracts;

public sealed class LoginRequest
{
    public string UserNameOrEmail { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
```

#### Program: JwtBearer ve sıra

```47:64:ReactBattleArena/ReactBattleArena.Api/Program.cs
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
```

Key yoksa uygulama açılmaz. Issuer/Audience/imza/süre doğrulanır; süresi dolmuş token reddedilir. 22 Temmuz’daki kayıt bugünküyle aynı fikir; CORS ve OpenAPI transformer sonra.

```90:91:ReactBattleArena/ReactBattleArena.Api/Program.cs
app.UseAuthentication();
app.UseAuthorization();
```

Sıra zorunlu: önce kimsin (`UseAuthentication` token’ı `HttpContext.User` yapar), sonra ne yapabilirsin (`UseAuthorization`). Tersi: `[Authorize]` user’ı boş görür, herkes 401. 22 Temmuz’da `[Authorize]` action’da yoktu; sıra yine doğru kuruldu ki yarın kilit takılınca çalışsın.

Eşleme: cookie auth yerine Bearer. React `Authorization: Bearer <token>` (`LoginPage` + `localStorage`, 30 Temmuz). MVC’de bazen cookie; burada stateless JWT.

#### Bu kodu kim tetikliyor?

Scalar `POST /api/auth/login`. 30 Temmuz `LoginPage` `fetch` / sonra `apiFetch` aynı URL, cevaptaki `token` saklanır. Character POST o gün hâlâ tokensız açılabilirdi.

#### Bu adımda yapılan / kalan iz

Commit’te `LoginResponse.cs` de vardı; asıl cevap `LoginResult` (Application). Sık düşülen hata: kullanıcı yokta 404, yanlış şifrede 401 — email sızdırır. Bir diğeri: `UseAuthorization`’ı `UseAuthentication`’dan önce yazmak. Bir diğeri: JWT Key’i git’e kısa string koymak — HMAC zayıf kalır. Token’ı URL query’de taşımak da log’a sızar; header’da Bearer.

#### Sonuçta ne kazandık

Login 200 + JWT veya 401. Hash `Verify` çalışıyor. Endpoint’ler henüz token zorunlu değil. Kimlik kapısı 27–28 Temmuz.

---

### 9. 27–28 Temmuz — Authorize, string Role, Player 403, Scalar Bearer

**Commit’ler:** `6563bde` (27 Temmuz, “[Authorize] + /me”), `9b7e1d0` (28 Temmuz, “Roles Admin/Player + Character CUD Admin + Player 403”).

Bu adımda dosyaları şu sırayla ekledik. 27 Temmuz’da Character/User **yazma** action’larına `[Authorize]`, login/register’a `[AllowAnonymous]`, `GET /api/auth/me` token’dan kimlik. Token yoksa 401; rol henüz yok, herhangi bir login CUD yapabilirdi. 28 Temmuz’da `Users.Role` kolonu (`AddUserRole`), `Roles` sabitleri, JWT’ye `ClaimTypes.Role`, CUD `[Authorize(Roles = Roles.Admin)]`, Register/CreateUser varsayılan `Player`, Scalar’a Bearer kilidi. Player ile POST denendi → **403**. `CreateCharacterRequest`’ten yanlışlıkla eklenen `Password` aynı gün silindi.

#### 401 vs 403, AllowAnonymous, /me

27 Temmuz: `[Authorize]` = “geçerli JWT şart.” Yok/bozuk → **401**. GET liste/detay’da attribute yok — katalog açık kaldı.

```31:32:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
    [HttpPost("register")]
```

Login’de aynı. Yorum: sınıf veya global `[Authorize]` gelse bile kayıt/giriş çalışsın. 22 Temmuz’da bu satır yoktu; o gün zaten kilit yoktu.

```62:84:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idValue, out var userId))
            return Unauthorized();

        var codes = await _permissions.GetCodesAsync(userId, cancellationToken);

        return Ok(new
        {
            id = userId,// out var daki userId
            userName = User.Identity?.Name,
            email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value,
            permissions = codes
        });
    }
```

27 Temmuz’da `Me` senkrondu, yalnızca claim: `id`, `userName`, `email`. `permissions` 24 Ağustos (`GET /me` RBAC). Amaç aynı: React yenilenince login JSON’u gitmiştir; token duruyorsa `/me` “ben kimim” der. 30 Temmuz’da `App` / sonra `PermissionContext` bunu çağıracak.

Eşleme: MVC `[Authorize]` / `[AllowAnonymous]`. 401 = kimlik yok; 403 = kimlik var, yetki yok. Cookie yerine `Authorization: Bearer`.

#### Role kolonu ve sabitler

```12:19:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260728162121_AddUserRole.cs
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Player");
        }
```

Mevcut satırlar `Player`. Register `User.Create(..., Roles.Player, ...)`. İlk Admin: SSMS’te `Role = Admin` **ve yeniden login** — eski JWT’de rol claim’i yok/eski.

```1:22:ReactBattleArena/ReactBattleArena.Domain/Authorization/Roles.cs
using System.Numerics;

namespace ReactBattleArena.Domain.Authorization;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Player = "Player";
    public const string ShopOwner = "ShopOwner";

}

//Authorization(roller)
//Authentication = kimsin? (JWT)
//Authorization = ne yapabilirsin? (rol)

//Rol Yetki(şimdilik)
//Player
//Login, katalog GET, kendi profili
//Admin
//Character Create / Update / Delete
//Register → varsayılan Player.İlk Admin’i SSMS’te Role = Admin yaparak veririz (basit).
```

28 Temmuz’da `ShopOwner` yoktu; sonra eklendi. `using System.Numerics` kullanılmıyor — yanlış import izi. String sabit: `[Authorize(Roles = Roles.Admin)]` yazım hatasını derleyiciye yakalatır (`"Adimn"` değil).

JWT’ye `ClaimTypes.Role` 28 Temmuz’da eklendi (bölüm 8’deki `CreateToken` listesinin son satırı). `[Authorize(Roles = ...)]` bu claim’e bakar.

#### Character CUD: Admin, Player 403

27 Temmuz’da CUD yalnızca `[Authorize]` idi — her login yazabilirdi. 28 Temmuz’da `[Authorize(Roles = Roles.Admin)]`. Bugün o satır `[HasPermission(...)]` (22 Ağustos, bölüm 26); rol adı yetki koduna taşındı.

```46:47:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersCreate)]  // POST
    [HttpPost]
```

28 Temmuz test: Player token ile POST → **403** (tanındın, Admin değilsin). Token yok → **401**. Admin token → 201. GET hâlâ serbest.

Bu model yetmez: “Admin her şeyi yapar” tek string’de gömülü. 20 Ağustos RBAC (`UserRoles` / `RolePermission`) bunu parçalar; `Users.Role` kolonu bir süre daha durur, `HasPermission` onu okumaz.

#### Scalar Bearer kilidi

```7:48:ReactBattleArena/ReactBattleArena.Api/OpenApi/BearerSecuritySchemeTransformer.cs
internal sealed class BearerSecuritySchemeTransformer
    : IOpenApiDocumentTransformer
{
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

    public BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    {
        _authenticationSchemeProvider = authenticationSchemeProvider;
    }

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes = await _authenticationSchemeProvider.GetAllSchemesAsync();

        if (!authenticationSchemes.Any(s => s.Name == "Bearer"))
            return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header. Login ile aldigin token'i yapistir."
        };

        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
        {
            operation.Value.Security ??= [];

            operation.Value.Security.Add(new OpenApiSecurityRequirement 
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        }
    }
}
```

OpenAPI belgesine “Bearer JWT yapıştır” kutusu. `Program.cs` `AddDocumentTransformer<BearerSecuritySchemeTransformer>()` + Scalar `AddPreferredSecuritySchemes("Bearer")`. 27 Temmuz notu “yarın”; 28 Temmuz geldi. Login’den token al, kilit ikonuna yapıştır, POST dene.

Aynı commit’te Character request’ten `Password` kaldırıldı — Register gövdesi karakter POST’una karışmasın.

#### Bu kodu kim tetikliyor?

Scalar: login → token → Bearer → Admin POST 201, Player POST 403, tokensız POST 401, GET 200. React 30 Temmuz’da `Authorization: Bearer` + 31 Temmuz’da Player 403 ile create form. `/me` 24 Ağustos’ta yetki listesi için tekrar öne çıkar.

#### Bu adımda yapılan / kalan iz

İlk Admin SSMS + yeniden login unutulursa eski Player JWT 403 vermeye devam eder. Sık düşülen hata: 401 ile 403’ü aynı sanmak. Bir diğeri: GET’e de `[Authorize]` koyup kataloğu kilitlemek — o gün bilinçli açık. String `Role` yetmeyecek; notun altındaki yorum “şimdilik.”

#### Sonuçta ne kazandık

Authentication (JWT) + kaba Authorization (Admin/Player string). Blok A backend temeli burada durur. JWT’yi Scalar’da yapıştırarak yaşamak web arayüzü olmadan zorlaştı; 29 Temmuz’da React’e geçtik (Blok B).

---

## Blok B — React temeli (29 Temmuz – 13 Ağustos)

Backend hazır: login JWT, Character CRUD, Player 403. Eksik olan ekran. `web/` ayrı process (Vite 5173); Api 7275. WinForms yok — Razor/HTML/`fetch` eşlemesi.

---

### 10. 29 Temmuz — Faz 0 kavramlar + Vite (`web/`)

**Commit:** `df156aa` (29 Temmuz, “faz0 kavram faz1 vite … 5173 ExecutionPolicy”).

Bu adımda sırayla: önce Faz 0 kavramlar (component, props/state, JSX, `interface`) notlara yazıldı, çünkü ertesi gün Login formu bunlarsız okunmaz. Sonra `npm create vite@latest web -- --template react-ts`, `npm install`, `npm run dev`. Aynı commit’te Api’ye CORS 5173 eklendi — tarayıcı 5173’ten 7275’e `fetch` atınca tarayıcı origin’i kontrol eder. 29 Temmuz’da Login sayfası yoktu; ekran Vite şablonunun sayacıydı.

#### Component ve JSX (bugünkü App)

```10:27:web/src/App.tsx
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
```

Component: UI döndüren fonksiyon. C#’ta küçük bir View / Razor sayfası gibi düşün — ama class değil, fonksiyon. 29 Temmuz’da `App` Vite şablonuydu (`useState(0)` sayaç, logo); router 3 Ağustos. İskelet aynı: `function App() { return (...JSX...) }`.

JSX HTML değildir. HTML’e benzer, `.tsx` içinde yazılır; tarayıcı JSX çalıştırmaz. Görünmeyen adım: Vite + `@vitejs/plugin-react` kaydettiğin dosyayı `React.createElement(...)` JavaScript’ine çevirir, tarayıcı onu alır. HTML’de `class`, JSX’te `className` — `class` JS’te rezerve.

Eşleme: Razor `.cshtml` sunucuda HTML üretir. JSX tarayıcıda, kaydedince HMR ile yenilenir; `dotnet run` gibi `npm run dev` açık kalır.

#### Props, interface, state

Props ve `interface` için bugünkü kart (4 Ağustos’ta yazıldı; fikir 29 Temmuz’da):

```3:14:web/src/CharacterCard.tsx
interface CharacterCardProps {
  id: string
  name: string
  universe: string
  rarity: number
  imageUrl?: string | null
}

function CharacterCard({ id, name, universe, rarity, imageUrl }: CharacterCardProps) {
  return (
      <Link to={`/characters/${id}`} className="character-card-link">
        <article className="character-card">
```

`interface` C# `interface` gibi sözleşme; runtime’da nesne üretmez, `tsc` kontrol eder. Props = dışarıdan gelen parametre (`CharacterCardProps`). Component kendi props’unu set etmez; parent verir. `?` = isteğe bağlı, C# `string?` gibi.

State 29 Temmuz’da şablonda `const [count, setCount] = useState(0)` idi. Bugün sayaç yok; aynı kanca Login’de (30 Temmuz):

```6:10:web/src/LoginPage.tsx
function LoginPage() {
  const navigate = useNavigate()
  const [userNameOrEmail, setUserNameOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
```

`useState` component’in kendi tuttuğu değer; `setX` deyince React o fonksiyonu yeniden çalıştırır, JSX güncel değeri basar. C# private field ekranı kendiliğinden yenilemez; burada yeniler. Formun `fetch`’i bölüm 11–12.

#### Vite girişi: html, main, script

```1:13:web/index.html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/favicon.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>web</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
```

Tek HTML kabuğu. `#root` boş; React buraya ağacı basar. `type="module"` ES module — Vite `main.tsx`’i sunar.

```1:13:web/src/main.tsx
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

29 Temmuz’da `BrowserRouter` yoktu; yalnızca `<App />`. `createRoot(...).render` Razor Layout + `_ViewStart` gibi: tek giriş, içindeki `App` değişir. `StrictMode` geliştirmede bazı effect’leri iki kez çalıştırır (bölüm 19); production’da bir. `getElementById('root')!` — `!` “null değil”, `index.html`’de div var.

```6:10:web/package.json
  "scripts": {
    "dev": "vite",
    "build": "tsc -b && vite build",
    "lint": "eslint .",
    "preview": "vite preview"
  },
```

`npm run dev` → `vite` → `http://localhost:5173/`. Bitmemeli; `Ctrl+C` durdurur. Kaydet = HMR. `dotnet run` Api 7275; iki process. `react-router-dom` 3 Ağustos; 29 Temmuz’da yalnız `react` + `react-dom`.

```1:7:web/vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
})
```

JSX dönüşümü bu plugin. 29 Temmuz’dan bugüne aynı.

Kurulum (PowerShell):

```
cd D:\ReactBattleArena
npm create vite@latest web -- --template react-ts
cd web
npm install
npm run dev
```

ESLint seçildi. `npm.ps1 is not digitally signed` → `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` veya `npm.cmd run dev`.

#### CORS — 5173 → 7275

```25:38:ReactBattleArena/ReactBattleArena.Api/Program.cs
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

Aynı gün `app.UseCors()` (`UseAuthentication`’dan önce). CORS ≠ auth: tarayıcı origin’e izin verir; JWT ayrı. Scalar 7275’ten 7275’e gittiği için CORS’a takılmazdı; Vite sayfası takılırdı.

#### Bu kodu kim tetikliyor?

Tarayıcı 5173: `index.html` → `main.tsx` → `App`. Api’ye istek henüz yok (Login yarın). İki sunucu: `npm run dev` + `dotnet run`.

#### Bu adımda yapılan / kalan iz

Şablon sayacı ertesi gün Login’e gidecek. Sık düşülen hata: `npm run dev`’in bitmesini beklemek. Bir diğeri: Api’yi açmadan frontend’den `fetch` — CORS veya sertifika (Firefox). ExecutionPolicy’yi MachinePolicy yapmak yerine `CurrentUser`.

#### Sonuçta ne kazandık

`web/` ayakta, 5173, kavramlar (component / props / state / JSX / interface) ve CORS. Login formu 30 Temmuz.

---

### 11. 30 Temmuz — Login: controlled input, form state

**Commit:** `a2da116` (30 Temmuz, “faz2 login faz3 characters” — bu bölüm yalnızca form; `fetch`/JWT bölüm 12, aynı commit).

Bu adımda dosyaları şu sırayla ekledik. Önce `LoginPage.tsx` — şablon `App` sayacı silinsin, ekranda form olsun. Sonra iki `useState` (`userNameOrEmail`, `password`) backend `LoginRequest` ile aynı isimler (JSON camelCase). Sonra `value` + `onChange` (controlled input), `handleSubmit` + `e.preventDefault()`. İlk pedagoji parçası `console.log` idi; aynı gün `fetch` eklendi (bölüm 12). `App.tsx` bir süre yalnızca `<LoginPage onLogin={...} />` gösterdi; router 3 Ağustos.

#### State ve controlled input

```6:10:web/src/LoginPage.tsx
function LoginPage() {
  const navigate = useNavigate()
  const [userNameOrEmail, setUserNameOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
```

30 Temmuz’da `useNavigate` yoktu; `LoginPage({ onLogin }: LoginPageProps)` vardı — “giriş oldu” sinyali parent `App`’e props ile. Router gelince `onLogin` kalktı, `navigate('/characters')` geldi. `error` aynı gün fetch ile eklendi.

`useState('')` başlangıç boş string. İsimler C# `LoginRequest.UserNameOrEmail` / `Password` — JSON’da camelCase.

```65:88:web/src/LoginPage.tsx
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
```

Controlled input: kutunun gösterdiği metin state’tir (`value={userNameOrEmail}`). Her tuş `onChange` → `e.target.value` → `setUserNameOrEmail` → React `LoginPage`’i yeniden çalıştırır → input yeni `value` ile çizilir. Düz HTML’de değer tarayıcının DOM’unda durur, sen okumak için form submit veya JS ile çekersin. React’te kaynak of truth state.

`type="password"` karakterleri gizler; state’te düz metin durur (ekrana yıldız, değişkene gerçek şifre). `label` input’u sarmalayınca tıklanınca kutu odaklanır; ayrı `htmlFor` gerekmez.

Eşleme: Razor’da `<input asp-for="UserNameOrEmail" />` + ViewModel. `onChange` her tuşta setter; MVC’de genelde submit’te model binder. `button type="submit"` formun `onSubmit`’ini tetikler; `type="button"` tetiklemez.

#### preventDefault — sayfa yenilenmesin

```12:14:web/src/LoginPage.tsx
  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
```

HTML form varsayılanı: submit → tarayıcı GET/POST ile sayfayı yeniden yükler, SPA state gider. `preventDefault()` bunu keser; handler çalışır, 5173 tek sayfa kalır. `React.FormEvent` form olayının tipi.

`{error && <p>{error}</p>}`: `error` boş string ise sol taraf falsy, `<p>` çizilmez; doluysa paragraf. `&&` JSX’te koşullu render (bölüm 17). İlk `console.log` parçasında `error` yoktu.

#### Bu kodu kim tetikliyor?

Kullanıcı 5173’te yazar, Giriş’e basar. 30 Temmuz öğleden sonra bu `handleSubmit` `POST /api/auth/login` atacak (bölüm 12) → `LoginCommand`. Şimdilik kavram: kutu = state.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `value` yazıp `onChange` unutmak — kutu kilitlenir, yazılmaz. Bir diğeri: `preventDefault` unutmak — F5 gibi yenilenir, log kaybolur. Bir diğeri: `value={userNameOrEmail}` yerine `defaultValue` — o uncontrolled’dır, state ile senkron kaçırılır.

#### Sonuçta ne kazandık

Login ekranı iki kutuyu React state’te tutuyor; submit sayfayı yıkmıyor. Token ve `fetch` bir sonraki parça (aynı gün, bölüm 12).

---

### 12. 30 Temmuz — `fetch`, JWT, `localStorage`

**Commit:** `a2da116` (aynı 30 Temmuz commit’i, formdan sonra).

Bu adımda `handleSubmit` `console.log` olmaktan çıktı: `async`, `POST https://localhost:7275/api/auth/login`, JSON body, başarıda `localStorage.setItem('token', data.token)` + `onLogin()`. CORS 29 Temmuz’da vardı; ilk gerçek tarayıcı→Api çağrısı bu. 11 Ağustos’ta `api.ts` / `apiFetch` aynı işi tek yere aldı; Login’deki eski `fetch` yorum satırı olarak duruyor.

#### O günkü `fetch` ve bugünkü `apiFetch`

```17:34:web/src/LoginPage.tsx
      // const response = await fetch('https://localhost:7275/api/auth/login', {
      //   method: 'POST',
      //   headers: {
      //     'Content-Type': 'application/json',
      //   },
      //   body: JSON.stringify({
      //     userNameOrEmail,
      //     password,
      //   }),
      // })

      // if (!response.ok) {
      //   setError('Giriş başarısız')
      //   return
      // }

      // const data = await response.json()
      // localStorage.setItem('token', data.token)
```

30 Temmuz’da bu yorum **çalışan** koddu. `fetch` tarayıcının `HttpClient`’ı: URL, method, header, body. `Content-Type: application/json` olmazsa Api model binder boş görebilir. `JSON.stringify` C# `PostAsJsonAsync`. Alan adları `LoginRequest` ile camelCase.

```40:57:web/src/LoginPage.tsx
      const response = await apiFetch('/api/auth/login',{
        method: 'POST',
        auth: false,
        body: {
          userNameOrEmail,
          password
        },
      })

      if (!response.ok) {
        setError('Giriş başarısız')
        return
      }

      const data = await response.json()
      setToken(data.token)
      setRefreshToken(data.refreshToken)
      navigate('/characters')
```

30 Temmuz’da yorumdaki blok **çalışan** koddu. `fetch` tarayıcının `HttpClient`’ı: URL, method, header, body. `Content-Type: application/json` olmazsa Api model binder boş görebilir. `JSON.stringify` C# `PostAsJsonAsync`. Alan adları `LoginRequest` ile camelCase.

`auth: false` — login’de henüz token yok, Bearer ekleme. Varsayılan `apiFetch` Bearer takar (karakter listesi, bölüm 13). `setRefreshToken` 29 Ağustos; 30 Temmuz’da yalnız `token`. `navigate` 3 Ağustos; o gün `onLogin()` parent’a “artık listeyi göster.”

```30:53:web/src/api.ts
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

`API_BASE` `https://localhost:7275`. Path `/api/auth/login`. Bu dosya 11 Ağustos; davranış 30 Temmuz `fetch`’i ile aynı, URL tekrarı yok.

#### Token’ı saklamak

```3:8:web/src/api.ts
export function getToken(): string | null {
  return localStorage.getItem('token')
}

export function setToken(token: string) {
  localStorage.setItem('token', token)
}
```

`localStorage` origin’e (5173) bağlı anahtar-değer. F5 atınca JS state ölür; `token` kalır. 30 Temmuz’da `LoginPage` doğrudan `setItem` yazıyordu. Cookie HttpOnly değil — XSS ile okunabilir; o gün bilinçli basit tuttu. Refresh ayrı anahtar (bölüm 33).

Görünmeyen akış: login cevabındaki `data.token` string’i çekmeceye konur. Sonraki `GET /api/characters` aynı çekmeceden okuyup `Authorization: Bearer ...` basar (bölüm 13). Sunucu token’ı session tablosunda tutmaz; JWT imzasını doğrular (bölüm 8).

#### `ok`, 401, catch

`response.ok` = status 200–299. Yanlış şifre **401** → `ok` false → “Giriş başarısız” (email sızdırmaz, backend ile aynı). Validation 400 de `ok` değil; aynı mesaj — kaba ama o gün yeterli.

`catch`: ağ, CORS, sertifika. Firefox’ta Api kapalıyken status bazen null; “API’ye ulaşılamadı.” CORS 29 Temmuz’da 5173 origin’e izin veriyordu; bu çağrı o izni ilk kullandı. CORS ≠ 401.

Eşleme: `HttpClient.PostAsJsonAsync` + `IsSuccessStatusCode`. `LoginResult.Token`. Scalar’da login body’si aynı JSON.

#### Bu kodu kim tetikliyor?

Form submit → `POST /api/auth/login` → `LoginCommand` → 200 + token veya 401. Application sekmesi → Local Storage → `http://localhost:5173` → `token`. Karakter listesi henüz bu bölümün işi değil (hemen ardından, bölüm 13).

#### Bu adımda yapılan / kalan iz

Başarıda ekranda “hoş geldin” yoktu; token yazılıp listeye geçildi. Sık düşülen hata: `http://localhost:7275` (https profili 7275 https). Bir diğeri: `JSON.stringify` unutup `[object Object]` göndermek — `apiFetch` bunu gizler, ham `fetch` gizlemez. Bir diğeri: token’ı değişkende tutup F5’te kaybetmek.

#### Sonuçta ne kazandık

Tarayıcı login oluyor, JWT 5173’te duruyor. Liste + Bearer sonraki saat (aynı commit, bölüm 13).

---

### 13. 30 Temmuz — `CharactersPage`, Bearer header, ilk `useEffect`

**Commit:** `a2da116` (aynı 30 Temmuz commit’i; form bölüm 11, login `fetch` bölüm 12, bu bölüm liste).

Bu adımda dosyaları şu sırayla ekledik. Önce `CharactersPage.tsx` — token çekmecede duruyordu ama kimse GET atmıyordu; login’den sonra bir ekran lazımdı. Sonra `App.tsx` içinde `isLoggedIn`: token yoksa Login, varsa liste. `useEffect(..., [])` sayfa ilk çizilince `load()` çalışsın diye. `Authorization: Bearer …` Scalar’daki Authorize kutusunun tarayıcı hali. Router, kart grid, `apiFetch` ve yetki kapısı o gün yoktu; hepsi sonra geldi.

#### Neden bu sayfa?

Bölüm 12 token’ı yazdı. Yazmak yetmez: bir sonraki istek header’da taşımazsa Api seni “login olmuş kullanıcı” saymaz. `GET /api/characters` o gün (ve bugün) controller’da `[Authorize]` yok — liste aslında tokensız da 200 döner. Bearer’ı yine koyduk, çünkü ertesi gün Admin `POST` zorunlu olacaktı; header’ı şimdi öğrenmek Scalar’a dönmekten kolaydı.

#### O gün `App`, bugün `Routes`

```10:21:web/src/App.tsx
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
```

30 Temmuz’da `Routes` yoktu. `App` şuna yakındı: `useState(() => !!localStorage.getItem('token'))` — `!!` token string ise `true`. `!isLoggedIn` ise `<LoginPage onLogin={() => setIsLoggedIn(true)} />`, değilse `<CharactersPage />`. Login başarısında `onLogin()` bu bayrağı kaldırıyordu; F5 atınca JS state ölür ama `localStorage` kalır, lazy initializer yine `true` der, liste açılır. URL hâlâ `/` idi. 3 Ağustos’ta gerçek path’ler geldi (bölüm 15); layout 9 Ağustos (bölüm 20).

#### `CharacterRow` — JSON’un TypeScript yüzü

```10:16:web/src/CharactersPage.tsx
interface CharacterRow {
  id: string
  name: string
  universe: string
  rarity: number
  imageUrl?: string | null
}
```

30 Temmuz’da `imageUrl` yoktu; dört alan yetiyordu. Karşı taraf `CharacterRowDto`: `Id`, `Name`, `Universe`, `Rarity`, … `System.Text.Json` camelCase ile `id` / `name` yollar. Guid JSON’da string olur — bu yüzden `id: string`, `Guid` değil. `interface` derleme zamanı sözleşmesi; runtime’da `response.json()` yine `any` gibi gelir, yanlış alan yazarsan `undefined` görürsün, C# compiler gibi kızmaz.

```19:21:ReactBattleArena/ReactBattleArena.Application/Characters/Queries/GetCharactersQuery.cs
public sealed record PagedCharacterRowsResult(
    IReadOnlyList<CharacterRowDto> Items,
    int TotalCount);
```

Cevap `{ items: [...], totalCount: N }`. Liste `data.items`; `data` tek başına dizi değil. Razor’da `@Model.Items` ile aynı şekil, isim camelCase.

#### `load` — GET + Bearer

```62:72:web/src/CharactersPage.tsx
  async function load() {
    try{
      const response = await apiFetch('/api/characters?page=1&pageSize=20')

      if(!response.ok) {
        setError('Karakterler Alınmadı')
        return
      }

      const data = await response.json()
      setItems(data.items)
```

Bugün `apiFetch` varsayılan `auth: true` ile `Authorization: Bearer ${getToken()}` basar (bölüm 12’deki `api.ts`). 30 Temmuz’da `load` `useEffect`’in *içindeydi* ve ham `fetch` vardı: `localStorage.getItem('token')` yoksa “Token yok — önce login ol”; varsa `headers: { Authorization: \`Bearer ${token}\` }` ve tam URL `https://localhost:7275/api/characters?page=1&pageSize=20`. `Bearer` ile token arasında boşluk unutulursa Api 401 görür. Query `page` / `pageSize` `GetCharactersQuery` + `Math.Clamp` (bölüm 4).

```24:33:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
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

GET’te `[HasPermission]` yok. Login’siz de 200. Handler `AsNoTracking`, `Skip`/`Take`, `CharacterRowDto`. Eşleme: `HttpClient` `DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token)` sonra `GetFromJsonAsync`. Scalar’da Authorize’a JWT yapıştırıp GET denemek aynı header.

`items` başlangıcı `useState<CharacterRow[]>([])` — ilk boyada liste boş, `load` bitince dolar. Bu yüzden ilk karede “veri yok” normal; hata değil.

#### `useEffect` — sayfa durunca bir kez çalıştır

```86:88:web/src/CharactersPage.tsx
  useEffect(() => {
    load()
  }, [])
```

30 Temmuz’da `load` bu callback’in içinde tanımlıydı; 31 Temmuz’da (bölüm 14) dışarı alındı ki create sonrası tekrar çağrılsın. Bugün yine dışarıda: `useEffect` sadece `load()` der.

`useEffect` görünmeyen bir kanca: fonksiyon body her render’da çalışır (JSX’i üretir); effect **çizimden sonra** çalışır. İkinci argüman `[]` “bağımlılık yok, yalnızca bu bileşen ilk kez ekrana konunca.” ASP.NET karşılığı: Razor Page `OnGetAsync` / Blazor `OnInitializedAsync` — sayfa açılınca bir kez sunucuya git. Fark: orada istek zaten sayfa isteğidir; burada 5173 HTML’i çoktan geldi, ikinci bir `fetch` Api’ye gider.

`load()`’u JSX içinde, `map`’ten önce çıplak çağırsan her render’da yeni GET + `setItems` + yeni render döngüsü. `useEffect` o döngüyü keser. StrictMode geliştirmede effect’i iki kez koşturur — bölüm 19; 30 Temmuz’da iki Network satırı “bug” sanılabilirdi.

#### Listeyi çizmek — `map` ve `key`

```90:97:web/src/CharactersPage.tsx
  return (
    <div className="characters-page">
      <div className="characters-page__header">
        <h1>Karakterler</h1>
        <div className="characters-page__actions">
          {hasPermission(permissions, PERMISSIONS.charactersCreate) && (
            <Link to="/characters/new">Karakter ekle</Link>
          )}
```

`hasPermission && <Link>` 24 Ağustos (bölüm 28). 30 Temmuz’da bu kapı yoktu; herkes listeyi görüyordu, Ekle ayrı sayfa da yoktu.

```104:118:web/src/CharactersPage.tsx
      {error && <p>{error}</p>}
      <div className="characters-grid">
        {items.map((c) => (
          <CharacterCard
            key={c.id}
            id= {c.id}
            name={c.name}
            universe={c.universe}
            rarity={c.rarity}
            imageUrl={c.imageUrl}
          />
        ))}
      </div>
    </div>
  )
```

30 Temmuz’da header link, `hasPermission`, `CharacterCard` ve CSS grid yoktu. JSX şuydu: `<ul>{items.map((c) => <li key={c.id}>{c.name} — {c.universe} (rarity {c.rarity})</li>)}</ul>`. `map` C# `foreach`: her satır için bir eleman. `key={c.id}` React’e “bu DOM düğümü hangi kayda ait” der; index `key` silme/eklemede satırları karıştırır. `CharacterCard` 4 Ağustos (bölüm 16); yetki linki 24 Ağustos (bölüm 28). `{error && <p>}` login’deki aynı kalıp.

#### Bu kodu kim tetikliyor?

Login 200 + token → o gün `onLogin` → `CharactersPage` mount → effect → `GET /api/characters?page=1&pageSize=20` + Bearer → `GetPaged` → `GetCharactersQueryHandler` → `items` state → `map`. Network’te 5173’ten 7275’e giden istek; CORS 29 Temmuz’da açılmıştı.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `data` dizidir sanıp `.map` — `data.items` lazım. Bir diğeri: `key` unutmak (console uyarısı). Bir diğeri: `http://localhost:7275` (https profili 7275 https). Bearer’ı koyup GET’in hâlâ tokensız da çalıştığını unutmak — “token çalışıyor” kanıtı asıl 31 Temmuz POST 201/403 (bölüm 14). `load`’u effect içinde bırakınca create sonrası listeyi yenilemek için fonksiyonu dışarı almak gerekti; o küçük taşıma ertesi gün.

#### Sonuçta ne kazandık

Login’den sonra karakter listesi 5173’te görünüyor; ilk `useEffect` ve Bearer header duruyor. Kayıt formu ve Admin ekleme 30–31 Temmuz (bölüm 14).

---

### 14. 30–31 Temmuz — `RegisterPage` ve Admin karakter ekleme, 403, liste yenileme

**Commit’ler:** `25d6294` (30 Temmuz akşam, Register), `bc2ce5f` (31 Temmuz, create formu `CharactersPage` içinde).

JWT login + liste vardı; yeni kullanıcı hâlâ Scalar’dan `POST /api/auth/register` atıyordu. Aynı akşam `RegisterPage` eklendi: Login’deki form kalıbı, token yok, başarıda girişe dönüş. Router yoktu — `App` içinde `authView: 'login' | 'register'`. Ertesi gün Admin’in `POST /api/characters` işi arayüze indi: form + Bearer, Player’da **403**, başarıda `await load()`. Form o gün listeyle aynı dosyadaydı; 4 Ağustos’ta `/characters/new` sayfasına taşındı (bölüm 16). Bugünkü kod o taşınmış hâli + `apiFetch`.

#### Register — neden ayrı sayfa, neden token yok

```7:14:web/src/RegisterPage.tsx
function RegisterPage() {
  const navigate = useNavigate()
  const [userName, setUserName] = useState('')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
```

Login ile aynı controlled input fikri; dört alan `RegisterRequest` ile camelCase.

```35:56:web/src/RegisterPage.tsx
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

      if (!response.ok) {
        setError('Kayıt başarısız (kullanıcı/email dolu veya validation)')
        return
      }

      setSuccess('Kayıt OK — şimdi giriş yap')
      navigate('/login')
    } catch {
      setError('API’ye ulaşılamadı (backend çalışıyor mu?)')
    }
  }
```

30 Temmuz’da `useNavigate` yoktu. Props: `onRegistered` ve `onBack`. `fetch` tam URL + `Content-Type` + `JSON.stringify`. Başarıda `onRegistered()` — parent `setAuthView('login')`. Token yazılmaz: backend `Created` + Guid döner, `LoginResult` değil (bölüm 7). `auth: false` login ile aynı gerekçe — henüz Bearer yok, varsayılan `apiFetch` token aramasın.

`displayName: displayName || null` — kutu boşsa `""` değil `null`; `RegisterRequest.DisplayName` `string?`. İsimler C# `UserName` / `Email` / `Password` ile camelCase.

```31:44:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
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

`[AllowAnonymous]` global `[Authorize]` gelse bile kayıt açılsın diye (yorum satırı controller’da duruyor). Duplicate username/email 400 (bölüm 7); frontend hepsini tek cümlede toplar, `ValidationProblemDetails` ayrıştırmaz — kaba, o gün yeterli.

Login’den kayıt linki bugün URL:

```89:91:web/src/LoginPage.tsx
      <p>
        <Link to="/register">Kayıt ol</Link>
      </p>
```

30 Temmuz’da `Link` yoktu. `LoginPage` `onGoRegister` alıyordu; `type="button"` “Kayıt ol” `setAuthView('register')` — `type="submit"` olsa form login’i de tetiklerdi. `RegisterPage` altındaki “Girişe dön” de `type="button"` + `onBack`. `authView` bir string state’ti, adres çubuğu değişmezdi. 3 Ağustos’ta `/register` gerçek route oldu (bölüm 15).

#### 31 Temmuz — create formu ve `load`’un dışarı çıkması

O gün form `CharactersPage.tsx` içindeydi: liste + “Yeni karakter (Admin)” aynı return. `load` `useEffect` içinden **component gövdesine** alındı; effect yalnızca `load()` çağırdı. Gerekçe: POST 201’den sonra aynı fonksiyonu `await load()` ile tekrar koşturmak. Effect içinde tanımlı fonksiyon dışarıdan çağrılamaz.

Bugün aynı POST `CharacterCreatePage`’de. Liste ayrı route; create sonrası `loadPreview()` + `navigate('/characters')`.

```99:137:web/src/CharacterCreatePage.tsx
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

      if (!response.ok) {
        const problem = await response.json().catch(() => null)
        const messages = problem?.errors
            ? Object.values(problem.errors).flat().join(' | ')
            : problem?.title ?? `Hata ${response.status}`
        setFormError(String(messages))
        return
        }

      setFormSuccess('Karakter eklendi')
      setName('')
      setUniverse('')
      setBiography('')
      setImageUrl('')
      await loadPreview()
      navigate('/characters')
    } catch {
      setFormError('API’ye ulaşılamadı')
    }
  }
```

31 Temmuz’da `apiFetch` yoktu: `fetch('https://localhost:7275/api/characters', { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: \`Bearer ${token}\` }, body: JSON.stringify({...}) })`. 403 mesajı açıkça “Yetkin yok (Admin gerekli)”. Validation tek cümleydi (`Karakter eklenemedi`); `problem.errors` ayrıştırması sonra. Navigate yoktu — aynı sayfada `await load()`, `<ul>` yeni satırı gösterirdi. `rarity` / attack alanları `type="number"` + `Number(e.target.value)`: input her zaman string verir; C# `int` bekler, string kalırsa binder 400.

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

31 Temmuz’da attribute `[Authorize(Roles = Roles.Admin)]` idi (bölüm 9). Ağustos RBAC ile `HasPermission(CharactersCreate)` oldu (bölüm 26). Davranış Player için aynı: token geçerli, fiil yok → **403**. **401** = tanımıyorum (token yok/bozuk). **403** = tanıdım, bu POST’u yapamazsın. `fetch` 403’te throw etmez; `response.ok` false, `status === 403` ayrı dal.

Test o gün: Admin login → Ekle → listede satır. Player token ile aynı form → 403. Rol değişince Local Storage `token` sil + yeniden login — eski JWT’de hâlâ eski `role` claim’i vardı.

#### Bu kodu kim tetikliyor?

Kayıt: 5173 form → `POST /api/auth/register` → `RegisterCommand` → 201 + Guid veya 400. Sonra login (bölüm 12). Ekleme: Admin Bearer → `POST /api/characters` → `CreateCharacterCommand` → 201; Player aynı body → 403. GET liste hâlâ herkese açık.

#### Bu adımda yapılan / kalan iz

Register: boş `displayName`’i `null` yapmamak. `type="submit"` ile “Girişe dön” — yanlış POST. Kayıt 201’de token bekleyip `data.token` okumak — cevap Guid. Create: `load`’u effect içinde bırakıp “ekledim ama liste duruyor”. Player ile denerken Admin token’ının durması — 201 gelir, 403 testi yalan olur. `Number(...)` unutunca rarity string gider. Formu gizlemeden Player’a göstermek o gün kasıtlıydı: 403’ü görmek için. UI gizleme ≠ yetki; asıl kapı API (bölüm 28’de link gizlenecek).

#### Sonuçta ne kazandık

Kayıt tarayıcıdan; Admin karakter ekliyor, Player 403 yiyor, başarıda liste yenileniyor. URL’li sayfalar ve Logout henüz yok (3 Ağustos, bölüm 15).

---

### 15. 03 Ağustos — react-router: `BrowserRouter`, `Routes`, `Link`, `useNavigate`, Logout

**Commit:** `be629dd` (3 Ağustos).

Bu adımda dosyaları şu sırayla değiştirdik. Önce `web` içinde `npm install react-router-dom` — URL’yi React’in dinlemesi için paket lazımdı. Sonra `main.tsx`’te `BrowserRouter` ile `App` sarıldı, çünkü `Routes` ancak bu sargının altında çalışır. Sonra `App.tsx`: `isLoggedIn` ve `authView` silindi, path → sayfa tablosu geldi (`/login`, `/register`, `/characters`). Login/Register props’larını (`onLogin`, `onGoRegister`, `onRegistered`) kaldırıp `useNavigate` + `Link` koyduk. `CharactersPage` token yoksa `<Navigate to="/login" />`, Çıkış token silip `/login`’e götürdü. Nested `AppLayout` / `<Outlet />` o gün yoktu (9 Ağustos, bölüm 20); çıkış butonu o yüzden önce liste sayfasındaydı.

#### Neden router?

30 Temmuz’da adres çubuğu hep `/` idi. Login mi Register mı `authView` state’i biliyordu; F5 veya link kopyalamak mümkün değildi. ASP.NET’te `MapControllers` + `[Route("api/[controller]")]` zaten path ile action seçer. SPA’de HTML tek (`index.html`); “hangi ekran”ı tarayıcı path’ine biz bağlarız. `authView` geçici bir `if` idi; router kalıcı adres.

#### `BrowserRouter` — URL’yi kim dinler

```7:13:web/src/main.tsx
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>,
)
```

`BrowserRouter` görünmeyen kabuk: adres çubuğunu dinler, `App` içindeki `Routes`’a “şu an path bu” der. Tam sayfa yenilemez. Eşleme: Kestrel `UseRouting` + endpoint eşlemesi sunucuda olur; burada eşleme **5173’te, zaten yüklenmiş JS içinde**. F5 `/characters`’a basınca Vite yine `index.html` verir, React baştan ayağa kalkar, router path’i okur, `CharactersPage`’i seçer. Api’ye “HTML sayfası ver” demez; Api hâlâ JSON.

Paket `web/package.json` içinde `react-router-dom`. O gün `npm install`; bugün `^7.18.2`.

#### `Routes` — path tablosu

```10:27:web/src/App.tsx
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
```

3 Ağustos’ta `AppLayout`, create, detail, edit yoktu. Tablo şuydu: `/login`, `/register`, `/characters`, `/` ve `*` → `/characters`. `element={...}` o path eşleşince çizilecek bileşen. `Navigate` `replace` ile history’de `/` birikmesin diye; tarayıcı geri tuşu boş `/`’ye takılmaz.

`*` bilinmeyen path (yazım hatası, eski yer imi). ASP.NET’te olmayan route genelde 404; burada kasıtlı olarak listeye düşürdük — ürün kararı, framework zorunluluğu değil.

`AppLayout` sargısı 9 Ağustos: login/register layout’suz kalsın, karakter sayfaları ortak header alsın (bölüm 20). 3 Ağustos’ta `/characters` doğrudan `CharactersPage` idi.

#### `Link` ve `useNavigate`

```89:91:web/src/LoginPage.tsx
      <p>
        <Link to="/register">Kayıt ol</Link>
      </p>
```

`<a href="/register">` tam belge ister (Vite `index.html` + React sıfırdan). `Link` aynı path değişimini **JS ile** yapar: form state gerekmez, login’deki yazdığın silinmez çünkü Login unmount olur ama SPA çökmez. Register’daki “Girişe dön” aynı şekilde `Link to="/login"`. 30 Temmuz’da bunlar `onGoRegister` / `onBack` callback’ti.

```6:7:web/src/LoginPage.tsx
function LoginPage() {
  const navigate = useNavigate()
```

```54:57:web/src/LoginPage.tsx
      const data = await response.json()
      setToken(data.token)
      setRefreshToken(data.refreshToken)
      navigate('/characters')
```

`useNavigate` programatik geçiş: submit handler içinde “git”. `onLogin()` kalktı; parent’a sinyal yok, adres değişir, `Routes` `CharactersPage`’i seçer. `setRefreshToken` 29 Ağustos (bölüm 33); 3 Ağustos’ta yalnız token + `navigate`. Register başarıda `navigate('/login')` — hâlâ token yok.

Hook kuralı: `useNavigate` fonksiyon bileşeninin gövdesinde, `if` dönüşünden **önce**. Router sargısı dışında çağırırsan patlar — bu yüzden `BrowserRouter` `main`’de en dışta.

#### Logout — token sil, adresi login yap

3 Ağustos’ta çıkış `CharactersPage` içindeydi: `localStorage.removeItem('token')` + `navigate('/login')`. Token yoksa aynı sayfada `return <Navigate to="/login" replace />`. Bugün her ikisi `AppLayout`’ta (liste, ekle, detay, edit ortak).

```11:14:web/src/api.ts
export function clearToken() {
  localStorage.removeItem('token')
  localStorage.removeItem('refreshToken')
}
```

```52:55:web/src/AppLayout.tsx
  function handleLogout() {
    clearToken()
    navigate('/login')
  }
```

```69:75:web/src/AppLayout.tsx
              <button
                type="button"
                className="app-header__logout"
                onClick={handleLogout}
              >
                Çıkış
              </button>
```

`type="button"` — header’da form yok ama alışkanlık: submit tetikleme. `clearToken` hem access hem refresh siler (refresh 29 Ağustos). 3 Ağustos’ta tek `removeItem('token')`.

Eşleme: MVC’de `SignOut` cookie’yi düşürür. Burada sunucu session tablosu yok (JWT, bölüm 8). Token’ı tarayıcıdan silmek **yeni isteklerin** Bearer taşımamasıdır; eski JWT imzası süre dolana kadar teoride hâlâ geçerlidir — çalınmış token ayrı konu. Çıkış = çekmeceyi boşalt + `/login` çiz.

```39:41:web/src/AppLayout.tsx
  if (!token) {
    return <Navigate to="/login" replace />
  }
```

Guard: `/characters` açıldı, token yok → login. `replace` ile korumalı sayfa history’de kalmasın (geri tuşu yine boş listeye düşmesin). 3 Ağustos’ta bu `if` `CharactersPage`’in başındaydı; create/detail henüz yoktu.

#### Bu kodu kim tetikliyor?

Adres `/login` → `LoginPage` (Api yok). Giriş 200 → `navigate('/characters')` → GET liste (bölüm 13). `/register` → Register POST (bölüm 14). Çıkış → localStorage boş, `/login`. Backend’de yeni endpoint yok; değişen yalnızca 5173’ün hangi bileşeni çizdiği.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `BrowserRouter`’ı unutup `useNavigate` — runtime hata. Bir diğeri: `Link` yerine `<a>` — F5 gibi yenilenir, yavaş ve state gider. Bir diğeri: `authView`’i silmeden router eklemek — iki kaynak, hangisi doğru belirsiz. Logout’ta token silmeden sadece `navigate('/login')` — F5’tе initializer yine listeye atardı (3 Ağustos’ta `isLoggedIn` kalkmıştı ama token duruyordu). Form hâlâ listeyle aynı sayfadaydı; URL `/characters` tek ekrandı.

#### Sonuçta ne kazandık

Login, kayıt, liste gerçek URL. Çıkış token’ı siler. Ertesi gün liste ile ekleme ayrıldı, kart grid geldi (bölüm 16).

---

### 16. 04 Ağustos — `CharacterCard`, CSS Grid, liste / ekle sayfası ayrımı

**Commit:** `2aa6734` (4 Ağustos).

Bu adımda dosyaları şu sırayla ekledik. Önce `CharacterCard.tsx` — listedeki `<li>` tekrarı kart olsun, props ile tek görünüm. Sonra `CharactersPage.css` içinde `.characters-grid` (`display: grid`); mega Grid component yok, yalnız Characters. Sonra form `CharactersPage`’den çıktı: `CharacterCreatePage.tsx` + route `/characters/new`. Liste sayfasında `Link` “Karakter ekle”; create’te form + altta aynı kartlarla önizleme, başarıda `navigate('/characters')`. `App.tsx`’e bir `Route` eklendi. Detay `/characters/:id` ertesi gün (bölüm 17); o yüzden kart o gün henüz `Link` değildi.

#### Neden liste ve create ayrılsın?

31 Temmuz’da form listenin üstündeydi: uzun, Player 403 görmek için oradaydı, ekran kalabalıktı. CRUD’da liste GET, create POST — iki iş, iki adres. ASP.NET’te de `GET /api/characters` ile `POST /api/characters` ayrı action; Scalar’da ayrı. Arayüzde `/characters` ve `/characters/new` aynı ayrım. “Tek mega Grid her listeye” yapmadık: Users tablosu ≠ karakter kartı; erken abstraction props cehennemi (öğrenim notundaki karar).

#### `CharacterCard` — props ile tek görünüm

```1:26:web/src/CharacterCard.tsx
import { Link } from 'react-router-dom'

interface CharacterCardProps {
  id: string
  name: string
  universe: string
  rarity: number
  imageUrl?: string | null
}

function CharacterCard({ id, name, universe, rarity, imageUrl }: CharacterCardProps) {
  return (
      <Link to={`/characters/${id}`} className="character-card-link">
        <article className="character-card">
              {imageUrl ? (
                <img src={imageUrl} alt={name} className="character-card__image" />
              ) : (
                <div className="character-card__placeholder">No image</div>
              )}
              <h3 className="character-card__name">{name}</h3>
              <p className="character-card__meta">{universe}</p>
              <p className="character-card__meta">Rarity {rarity}</p>
            </article>
      </Link>
  )
}
```

4 Ağustos’ta `id` ve `Link` yoktu: yalnız `name`, `universe`, `rarity`, `imageUrl`; kök `<article>`. 5 Ağustos’ta detay route gelince `id` zorunlu oldu, kart tıklanınca `/characters/${id}` (bölüm 17). `CharacterCreatePage` map’ine `id={c.id}` eklenmezse TS 2741 — o gün unutulursa derleme kızar.

`imageUrl ? <img> : <div>No image</div>`: DTO `string?`; boşsa kırık resim ikonu yerine placeholder. `{condition ? A : B}` `&&` değil — iki taraftan biri mutlaka çizilir.

Props = parent’ın verdiği parametre (Faz 0). Eşleme: Razor partial `_CharacterCard.cshtml` + model. Kartın kendisi `fetch` atmaz; `items`’ı sayfa yükler, kart sadece çizer.

```105:116:web/src/CharactersPage.tsx
      <div className="characters-grid">
        {items.map((c) => (
          <CharacterCard
            key={c.id}
            id= {c.id}
            name={c.name}
            universe={c.universe}
            rarity={c.rarity}
            imageUrl={c.imageUrl}
          />
        ))}
      </div>
```

`key` hâlâ `c.id` (bölüm 13). Liste sayfasında form yok; header’da ekle linki:

```90:97:web/src/CharactersPage.tsx
  return (
    <div className="characters-page">
      <div className="characters-page__header">
        <h1>Karakterler</h1>
        <div className="characters-page__actions">
          {hasPermission(permissions, PERMISSIONS.charactersCreate) && (
            <Link to="/characters/new">Karakter ekle</Link>
          )}
```

4 Ağustos’ta `hasPermission` yoktu; link herkese görünürdü, Player forma girip POST’ta 403 yerdi (bölüm 14’teki kasıt). Link gizleme 24 Ağustos (bölüm 28).

#### CSS Grid — sütun sayısı ekrana göre

```97:104:web/src/CharactersPage.css
.characters-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 1rem;
  list-style: none;
  padding: 0;
  margin: 0;
}
```

`display: grid` çocukları ızgaraya dizer. `minmax(180px, 1fr)`: sütun en az 180px, kalan boşluğu paylaş (`1fr`). `auto-fill` geniş ekranda daha çok sütun, daralınca az. Bootstrap/`DataGrid` şart değil. `CharactersPage.css` hem liste hem create tarafından `import` edilir — aynı class, iki sayfa.

#### `/characters/new` — form taşındı

```16:18:web/src/App.tsx
      <Route element={<AppLayout />}>
        <Route path="/characters" element={<CharactersPage />} />
        <Route path="/characters/new" element={<CharacterCreatePage />} />
```

4 Ağustos’ta `AppLayout` yoktu; `/characters/new` düz `Route` idi, `/characters` ile kardeş. POST gövdesi bölüm 14’teki `handleCreate`; başarıda artık `await load()` yetmez çünkü liste başka sayfada — `navigate('/characters')` (bugün `loadPreview` + navigate).

```151:156:web/src/CharacterCreatePage.tsx
  return (
    <div className="characters-page">
      <div className="characters-page__header">
        <h1>Karakter ekle</h1>
        <Link to="/characters">Listeye dön</Link>
      </div>
```

```238:253:web/src/CharacterCreatePage.tsx
      <h2>Mevcut karakterler</h2>
      <p className="characters-hint">
        Şimdilik önizleme (sık kullanılanlar backend sonra). Aynı kart grid.
      </p>
      <div className="characters-grid">
        {items.map((c) => (
          <CharacterCard
            key={c.id}
            id={c.id}
            name={c.name}
            universe={c.universe}
            rarity={c.rarity}
            imageUrl={c.imageUrl}
          />
        ))}
      </div>
```

Alt grid kasıtlı önizleme: “sık kullanılan” backend’i yoktu, mevcut sayfa 1 listesi duruyor. Aynı `CharacterCard` — iki yerde aynı görünüm görünce ortak component çıkarma kuralına uyduk (2. tekrar).

`/characters/new` statik path. Yarın `/characters/:id` eklenince **sıra** önemli: `new` `:id`’den önce durmazsa React `id = "new"` sanır, Guid parse patlar (bölüm 17). 4 Ağustos’ta `:id` olmadığı için bu tuzak henüz yoktu.

#### Bu kodu kim tetikliyor?

`/characters` → GET liste → kart grid. `/characters/new` → (token guard) form; Ekle → `POST /api/characters` (bölüm 14) → 201 veya 403 → listeye dön. Backend’e yeni action yok; yeni olan 5173 route ve CSS.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: grid’i `CharactersPage.css`’e yazıp create sayfasında `import` unutmak — kartlar alt alta. Bir diğeri: `map`’te `CharacterCard`’a `key` vermek ama `id` prop’unu unutmak (detay günü). Bir diğeri: form state’ini liste sayfasında bırakıp route eklemek — iki kaynak. 400’de `errors` (Name/Universe, Rarity 1–5, ImageUrl max 500) create sayfasında gösterilmeye başlandı; 31 Temmuz’daki tek cümleden netleşti.

#### Sonuçta ne kazandık

Katalog kart grid; ekleme ayrı URL. Sonraki gün detay `/characters/:id` + `useParams` (bölüm 17).

---

### 17. 05 Ağustos — Detay `/characters/:id`, `useParams`, `&&` ile koşullu render

**Commit:** `fd511ca` (5 Ağustos).

Bu adımda dosyaları şu sırayla ekledik. Önce `CharacterDetailPage.tsx` — listede yalnız özet vardı (`CharacterRow`); biyografi, stat, `createdAtUtc` `GET /api/characters/{id}` ile geliyor (`CharacterDetailDto`, bölüm 4). Sonra `App.tsx`’e `/characters/:id`. `new` route **üstte** durdu; yoksa `id` değeri `"new"` olur, Guid parse / 404. `CharacterCard` `id` prop + `Link` aldı; liste ve create `map`’ine `id={c.id}` (TS 2741). JSX’te `{loading && …}` / `{error && …}` / `{character && (…)}` aynı gün oturdu. Sil ve Düzenle ertesi gün (bölüm 18).

#### `useParams` — URL’deki delik

```22:23:web/src/CharacterDetailPage.tsx
function CharacterDetailPage() {
  const { id } = useParams<{ id: string }>()
```

`App`’te path `/characters/:id`. `:id` şablon deliği; gerçek adres `/characters/3fa8…` ise `id` o string. Eşleme: `[HttpGet("{id:guid}")] GetById(Guid id)` — sunucu route’tan Guid bağlar. React tarafında parametre **her zaman string** (veya yoksa `undefined`); Guid’e çevirmeyiz, URL’ye olduğu gibi basarız. `useParams<{ id: string }>()` TypeScript’e “bu key bekleniyor” der; runtime hâlâ router’ın verdiği nesne.

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

`{id:guid}` constraint: `"new"` Guid değil → ASP.NET bu action’a düşmez (404). Asıl koruma yine React tarafında `/characters/new`’in ayrı route olması: Detail hiç mount olmasın.

```5:15:ReactBattleArena/ReactBattleArena.Application/Characters/Queries/GetCharacterByIdQuery.cs
public sealed record GetCharacterByIdQuery(Guid Id) : IRequest<CharacterDetailDto?>;
public sealed record CharacterDetailDto(
    Guid Id,
    string Name,
    string Universe,
    string? Biography,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    string? ImageUrl,
    DateTime CreatedAtUtc);
```

Liste DTO’su (`CharacterRowDto`) özet; detayda `Biography` + `CreatedAtUtc`. JSON’da `createdAtUtc` string (ISO). Handler `FirstOrDefaultAsync` → yoksa `null` → 404.

#### Route sırası — `new` `:id`’den önce

```16:21:web/src/App.tsx
      <Route element={<AppLayout />}>
        <Route path="/characters" element={<CharactersPage />} />
        <Route path="/characters/new" element={<CharacterCreatePage />} />
        <Route path="/characters/:id/edit" element={<CharacterEditPage />} />
        <Route path="/characters/:id" element={<CharacterDetailPage />} />
      </Route>
```

5 Ağustos’ta `AppLayout` ve `:id/edit` yoktu: `/characters`, `/characters/new`, `/characters/:id`. Statik `new`, dinamik `:id`’den önce yazıldı. Router çoğu sürümde daha spesifik path’i zaten öne alır; yine de `new`’i üstte tutmak alışkanlık. `:id/edit` 6 Ağustos; bugün `:id`’den **önce** (bölüm 18).

#### Kart tıklanınca detay

```11:13:web/src/CharacterCard.tsx
function CharacterCard({ id, name, universe, rarity, imageUrl }: CharacterCardProps) {
  return (
      <Link to={`/characters/${id}`} className="character-card-link">
```

4 Ağustos’ta kart `Link` değildi (bölüm 16). Template string: `` `/characters/${id}` `` — C# `$"/characters/{id}"`. Liste `GET` paging; detay ayrı `GET` by id. Kart `fetch` atmaz.

```9:20:web/src/CharacterDetailPage.tsx
interface CharacterDetail {
  id: string
  name: string
  universe: string
  biography?: string | null
  rarity: number
  baseAttack: number
  baseDefense: number
  baseSpeed: number
  imageUrl?: string | null
  createdAtUtc: string
}
```

`useState<CharacterDetail | null>(null)`: daha gelmedi veya 404. `loading` başlangıç `true` — ilk karede boş kart yok, “Yükleniyor…”.

```56:72:web/src/CharacterDetailPage.tsx
        const response = await apiFetch(`/api/characters/${id}`)

        if (response.status === 404) {
          setError('Karakter bulunamadı')
          setCharacter(null)
          setLoading(false)
          return
        }

        if (!response.ok) {
          setError('Karakter alınamadı')
          setLoading(false)
          return
        }

        const data = await response.json()
        setCharacter(data)
```

5 Ağustos’ta ham `fetch` + Bearer + tam URL. 404’ü genel `!ok`’dan ayırmak: silinmiş / yanlış Guid ayrı cümle. `setCharacter(data)` — liste gibi `data.items` yok; gövde tek nesne.

```87:88:web/src/CharacterDetailPage.tsx
    load()
  }, [id, token])
```

Liste `[]` idi (bölüm 13): sayfa bir kez. Burada `id` değişince (başka karta tıklamak, SPA unmount olmayabilir) yeni GET. `token` da: login sonrası. Derinlemesine bağımlılık dizisi bölüm 19.

#### `&&` — koşul doğruysa sağdakini çiz

```160:164:web/src/CharacterDetailPage.tsx
      {loading && <p>Yükleniyor…</p>}
      {error && <p>{error}</p>}
      {deleteError && <p>{deleteError}</p>}

      {character && (
```

JSX `{…}` içi JavaScript ifadesi. `A && B`: A falsy ise sonuç A (React `false` / `""` / `null` çizmez); A truthy ise sonuç B. `{error && <p>{error}</p>}` — boş string falsy, paragraf yok; doluysa kırmızı cümle. `{character && ( <article>…` — `null` iken article yok; gelince detay. Login’deki `{error && …}` aynı kalıp (bölüm 11).

`deleteError` 6 Ağustos (bölüm 18); 5 Ağustos’ta yoktu.

```182:182:web/src/CharacterDetailPage.tsx
          {character.biography && <p>{character.biography}</p>}
```

Biyografi `null`/boşsa o `<p>` hiç yok. Resim `? :` (bölüm 16): iki taraftan biri mutlaka (img veya placeholder). `&&` “yoksa hiçbir şey”; ternary “ya bu ya şu”.

`if (character) { return <article> }` de olurdu; `&&` aynı işi JSX içinde, erken `return` olmadan.

#### Bu kodu kim tetikliyor?

Kart `Link` → `/characters/{guid}` → `CharacterDetailPage` mount → effect → `GET /api/characters/{id}` → `GetById` → `GetCharacterByIdQueryHandler` → 200 DTO veya 404. Backend 5 Ağustos’ta yeni yazılmadı; endpoint Temmuz’dan beri vardı (bölüm 4).

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `/characters/:id`’yi `new`’in **üstüne** koymak — create sayfası detay sanılır, `id === "new"`, API 404. Bir diğeri: `data.items` beklemek — detay dizi değil. Bir diğeri: `{character.name}` demek `character` hâlâ `null` iken — `&&` veya optional chaining olmadan patlar. `key` var `id` prop yok → TS 2741 create map’te; 6 Ağustos commit’inde create’e `id={c.id}` eklendi.

#### Sonuçta ne kazandık

Kart → detay URL → tek karakter GET. `useParams` ve `&&` duruyor. Ertesi gün PUT / DELETE aynı `id` üzerinden (bölüm 18).

---

### 18. 06 Ağustos — Edit (PUT 204) ve Delete (`confirm`), `useState` başlangıç değerleri

**Commit:** `c917f83` (6 Ağustos).

Bu adımda dosyaları şu sırayla ekledik. Önce `CharacterEditPage.tsx`: aynı `id` ile **önce GET** (formu doldur), Kaydet’te **PUT**, 204’te `response.json()` yok, `navigate` detaya. `App.tsx` `/characters/:id/edit`. Detay header’a “Düzenle” `Link` + “Sil” (`window.confirm` → `DELETE` → 204 → liste). `CharacterCreatePage` map’ine `id={c.id}` (dün kart `id` zorunlu olmuştu). `useState(10)` / `useState(1)` “üzerine yazar mı?” aynı gün netleşti: hayır, GET `setBaseAttack(data.baseAttack)` yazar. Yetki kapısı (`hasPermission` + Navigate) Ağustos sonu (bölüm 29); o gün form herkese, API 403.

#### Edit route — daha spesifik olan önce

```19:20:web/src/App.tsx
        <Route path="/characters/:id/edit" element={<CharacterEditPage />} />
        <Route path="/characters/:id" element={<CharacterDetailPage />} />
```

6 Ağustos commit’inde sıra tersineydi (`:id` sonra `:id/edit`). Segment sayısı farklı olduğu için ikisi de çalışır; yine de `…/edit` üstte alışkanlık. `useParams` edit’te de aynı `id`.

Detaydan geçiş: `Link to={\`/characters/${id}/edit\`}`. Bugün `hasPermission(..., charactersUpdate)` ile gizlenir (bölüm 28); 6 Ağustos’ta link herkese görünürdü.

#### `useState(10)` iskelet, GET asıl değer

```15:22:web/src/CharacterEditPage.tsx
  const [name, setName] = useState('')
  const [universe, setUniverse] = useState('')
  const [biography, setBiography] = useState('')
  const [rarity, setRarity] = useState(1)
  const [baseAttack, setBaseAttack] = useState(10)
  const [baseDefense, setBaseDefense] = useState(10)
  const [baseSpeed, setBaseSpeed] = useState(10)
  const [imageUrl, setImageUrl] = useState('')
```

`useState(10)` **yalnız ilk render**. “Kayıtlı attack 100 ise 10 mu kalır?” Hayır. Effect GET bitince setter’lar gerçek DTO’yu koyar. Create’te GET yok; oradaki 10/1 kullanıcının başlangıç rakamı, kasıtlı default.

```67:75:web/src/CharacterEditPage.tsx
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

`?? ''`: `biography` / `imageUrl` null ise controlled input `value={null}` olmasın diye boş string. Aynı GET detaydaki `GetById`; edit **ikinci** bir istek atar, liste state’ini paylaşmaz (Context yoktu).

```181:182:web/src/CharacterEditPage.tsx
      {!loading && !loadError && (
        <form className="characters-form" onSubmit={handleSubmit}>
```

Form yüklenene kadar gizli: kullanıcı 10’u bir kare bile görmez. `loading` true iken üstte ayrıca erken `return <p>Yükleniyor…</p>` var (164–166) — iki katman; ikincisi (`{loading &&` satır 178) pratikte o early return yüzünden pek görünmez. 6 Ağustos’ta kapı sırası sade load/error/form’du; `hasPermission` Navigate sonradan eklendi (bölüm 29).

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

Hook’lar (`useEffect`, `useState`) bu `if`’lerden **önce** — kurallar. 6 Ağustos’ta permission `if`’i yoktu.

#### PUT 204 — body yok, `json()` çağırma

```116:128:web/src/CharacterEditPage.tsx
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
```

Gövde `CreateCharacterRequest` ile aynı şekil (bölüm 5). 6 Ağustos’ta `JSON.stringify` + `Content-Type` + Bearer elle.

```130:155:web/src/CharacterEditPage.tsx
      if (response.status === 401) {
        setFormError('Oturum yok — tekrar giriş yap')
        return
      }

      if (response.status === 403) {
        setFormError('Yetkin yok')
        return
      }

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

**204** = başarı, gövde boş. `response.json()` boş body’de throw / parse hatası. Create 201 Guid döner, `json()` vardır; update’de yoktur. Eşleme: `NoContent()`.

```69:93:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
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

6 Ağustos’ta `[Authorize(Roles = Admin)]`. Handler `false` → 404 (id yok). Validation 400 → `errors` birleştirilir. Player PUT → 403.

`handleSubmit` `useEffect` içinde değil: Kaydet tıklanınca. Effect yalnız load (bölüm 19 ile karıştırma).

#### Delete — `confirm`, sonra DELETE

```94:98:web/src/CharacterDetailPage.tsx
  async function handleDelete() {
    if (!id || !token) return

    const ok = window.confirm('Bu karakteri silmek istediğine emin misin?')
    if (!ok) return
```

`confirm` iptalde `false` — istek yok. `handleDelete` sayfa fonksiyonunun **içinde**; ayrı dosya / `useEffect` değil. Bugün tanım, token yoksa `Navigate` **sonrasında** — o render’da Sil butonu zaten çizilmez.

```113:137:web/src/CharacterDetailPage.tsx
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

204 → liste. 401/403/404 ayrı `deleteError`; detay `error` (GET) ile karışmasın. Sil butonu `type="button"` — form yok ama alışkanlık.

```95:107:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersDelete)]  // DELETE
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Controller command'ı Send ile yollar → MediatR tipine bakar → IRequestHandler'ı DI'dan alır → Handle çalıştırır.
        // Gelen tip: DeleteCharacterCommand → aranan: IRequestHandler<DeleteCharacterCommand, bool> → bulunan: DeleteCharacterCommandHandler
        var deleted = await _mediator.Send(new DeleteCharacterCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
```

Yine Temmuz’daki komut (bölüm 5); 6 Ağustos’ta arayüz bağlandı. Attribute o gün Role, bugün permission.

Frontend CRUD o gün kapandı: liste, create, detay, edit, sil.

#### Bu kodu kim tetikliyor?

Düzenle: `/characters/{id}/edit` → GET detay DTO → form state → PUT → `UpdateCharacterCommand` → 204 veya 400/403/404 → `navigate` detaya. Sil: confirm → DELETE → `DeleteCharacterCommand` → 204 → `/characters`.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: PUT 204’te `await response.json()`. Bir diğeri: `useState(10)`’un “kalıcı default” sanılması. Bir diğeri: `biography` null iken `value={biography}` — React uyarısı; `?? ''`. Player ile Kaydet/Sil 403 — beklenen. `handleSubmit`’i effect’e koymak — her id değişiminde PUT. Create map’te `id` unutulursa kart derlenmez.

#### Sonuçta ne kazandık

Katalog tam CRUD arayüzde: GET by id, PUT 204, DELETE + confirm. `useEffect` bağımlılık dizisi ve StrictMode bir sonraki not (bölüm 19).

---

### 19. 06 Ağustos sonrası — `useState` / `useEffect` derinlemesine: bağımlılık dizisi, StrictMode, `setLoading`

**Commit yok** (6 Ağustos CRUD notlarının devamı; yeni dosya eklenmedi).

Bu adımda kod yazmadık; 17–18’deki `CharacterDetailPage` / `CharacterEditPage` kalıbını durup parçaladık. Sıra şöyleydi. Önce `useState(true)` + `setLoading`: React mi biz mi, `loading` diye sihirli bir kelime var mı. Sonra `useEffect` gövdesi: render UI üretir, GET **çizimden sonra**. Sonra ikinci argüman `[id, token]` versus listenin `[]`. Sonra “`load()` kendini mi çağırıyor?” ve `handleSubmit`’in effect’te olmaması. En sonda `main.tsx` `StrictMode`: geliştirmede effect iki kez. Bugünkü kod `apiFetch` ve `getToken`; 6 Ağustos’ta sayfalar hâlâ ham `fetch` + `localStorage.getItem('token')` idi. Davranış aynı.

#### `loading` / `setLoading` — kim ne üretir

```29:32:web/src/CharacterDetailPage.tsx
  const [character, setCharacter] = useState<CharacterDetail | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [deleteError, setDeleteError] = useState('')
```

```24:26:web/src/CharacterEditPage.tsx
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [formError, setFormError] = useState('')
```

`useState` React’ten gelir (`import { useState } from 'react'`). `loading` bizim seçtiğimiz **değişken adı** — `isLoading` de olurdu; React “loading” diye bir kavram dayatmaz. `setLoading` React’in ürettiği setter; isim kuralı `set` + state adı. `true` bizim başlangıç: ilk boyada “Yükleniyor…” görülsün, boş kart/form bir kare parlamasın.

`setLoading` otomatik çağrılmaz. GET başında `setLoading(true)`, bitince `false` **biz** deriz. ASP.NET’te de `ViewBag` / Razor “yükleniyor” bayrağı framework doldurmaz; sen koyarsın. `fetch`’in kendisi `loading` state’i yok.

Edit’te `useState(10)` iskeleti bölüm 18’de kaldı: ilk render, GET `setBaseAttack(data.baseAttack)` üzerine yazar. Create’te GET yok; 10 orada kullanıcı default’u.

#### `useEffect` — çizimden sonra yan etki

```35:45:web/src/CharacterDetailPage.tsx
  useEffect(() => {
    async function load() {
      if (!token || !id) {
        setError('Id veya token yok')
        setLoading(false)
        return
      }

      setLoading(true)
      setError('')
```

```80:88:web/src/CharacterDetailPage.tsx
      } catch {
        setError('API’ye ulaşılamadı')
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [id, token])
```

Bileşen fonksiyonu her render’da çalışır ve JSX üretir. `useEffect`’e verdiğin fonksiyon **boya bittikten sonra** çalışır. Tek benzetme: kurye, paket (HTML) kapıya dayandıktan sonra yola çıkar — GET, abonelik, `document.title`. Render’ın işi ekranı tarif etmek; “gidip 7275’ten JSON al” yan etkidir, gövdeye çıplak yazılırsa her `setCharacter` yeni render + yeni GET döngüsü.

ASP.NET karşılığı Razor Page `OnGetAsync` / controller action: orada sayfa isteği **zaten** o GET’tir; HTML sunucuda üretilir. Burada Vite `index.html` + JS çoktan geldi; ikinci bir `fetch` Api’ye gider. Blazor `OnInitializedAsync` zamansal olarak daha yakın: bileşen durdu, sonra veri.

`async function load()` effect **içinde** yardımcı. Recursive değil: `load` kendini çağırmaz. Akış: effect çalıştı → `load` tanımlandı → `load()` bir kez → bitti. `finally` hem 200 hem catch’te `setLoading(false)` — 404 dallarında ayrıca `false` var; çift çağrı zararsız.

```86:88:web/src/CharactersPage.tsx
  useEffect(() => {
    load()
  }, [])
```

Liste `[]`: bağımlılık yok, yalnız **ilk mount**. Detay/edit `[id, token]`: ilk mount **ve** `id` veya `token` değişince. Aynı `CharacterDetailPage` açıkken karttan başka Guid’e `Link` (SPA bazen sayfayı unmount etmeden param değiştirir) yeni GET ister. `[]` kalsaydı URL değişir, ekran eski karakterde kalırdı.

`handleSubmit` / `handleDelete` effect’te değil. Kaydet ve Sil tıklanınca çalışır (`onSubmit` / `onClick`). Effect otomatik load; submit kullanıcı kapısı. İkisini karıştırmak: her `id` değişiminde PUT.

#### StrictMode — geliştirmede iki kez

```7:12:web/src/main.tsx
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>,
```

`<StrictMode>` production davranışı değil; geliştirmede effect’i **mount → cleanup → tekrar mount** eder. Network’te aynı `GET /api/characters/{id}` iki satır görürsün. Bug değil. Production build’de tek. Kapatma: gerçek cleanup hatalarını (abonelik sızması) gizlemek olur. 9 Ağustos notunda listenin 3–4 sn gelmesi şüphelilerinden biri çift fetch’ti; asıl soğuk Api / sertifika ayrı konu.

eslint `react-hooks/exhaustive-deps`: `[id, token]` içinde kullanılan değerler diziye yazılsın. `id`’yi unutup `[]` bırakmak “bazen eski detay” bug’ıdır. `load`’u diziye koymak ayrı tartışma; 6 Ağustos’ta dizi yalnız `id` ve `token`.

#### Bu kodu kim tetikliyor?

Detay veya edit açılınca React boyar (`loading === true`) → effect → `GET /api/characters/{id}` (bölüm 17–18). `id` değişince effect yeniden. Kaydet hâlâ `handleSubmit` → PUT. Backend’e yeni endpoint yok.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `load()`’u JSX gövdesinde çağırmak — sonsuz istek. Bir diğeri: `useEffect`’in `async` olması (`useEffect(async () => …)` React istemez; içeride `async function load`). Bir diğeri: StrictMode iki Network satırını bug sanıp StrictMode’u silmek. Bir diğeri: `setLoading`’i React’in otomatik sandığı için `finally` unutmak — “Yükleniyor…” sonsuz (kapı hatası bölüm 29’da `meLoaded` ile tekrar çıkacak).

#### Sonuçta ne kazandık

`useState` isimleri bizim, setter React’in; `useEffect` çizimden sonra; `[]` ≠ `[id, token]`; StrictMode çift GET normal. 9 Ağustos’ta ortak kabuk: `AppLayout` + `<Outlet />` (bölüm 20).

---

### 20. 09 Ağustos — `AppLayout`, nested routes, `<Outlet />`, path’siz parent

**Commit:** `4fcc8af` (9 Ağustos).

Bu adımda dosyaları şu sırayla ekledik. Önce `AppLayout.tsx` + `AppLayout.css`: üst menü (brand, Karakterler, Çıkış), token yoksa login, `Outlet` boşluğu. Sonra asıl kilit `App.tsx` — layout dosyası tek başına çizilmez; çocuk route’lar parent’ın **altında** olmalı. İlk denemede layout yazılıp `App.tsx` düz route’ta kalınca menü hiç görünmedi. Sonra `CharactersPage`’den token guard ve Çıkış silindi; koruma layout’a taşındı. Login/Register parent **dışında** kaldı — üst menüsüz form. `PermissionContext`, `/me`, `meLoaded` o gün yoktu (26–27 Ağustos, bölüm 30–31); bugünkü `AppLayout` onları da taşıyor.

#### Neden ortak kabuk?

Liste, ekle, detay, edit hepsinde aynı marka + Çıkış vardı veya olmalıydı. Dördüne kopyalamak DRY değil; birini unutunca o sayfada çıkış yok. ASP.NET’te `_Layout.cshtml` + `@RenderBody()` aynı iş: iskelet sabit, orta değişken. React Router’da iskelet parent `element`, orta `<Outlet />`.

Karar: üst menü, sol sidebar yok, dropdown yok. Grid tam genişlik kalsın diye dikey değil yatay header. Ekle linki listede kaldı (yetki sonrası gizlenecek, bölüm 28).

#### Path’siz parent ve çocuk tablosu

```10:27:web/src/App.tsx
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
```

`<Route element={<AppLayout />}>` **path yok**. “Şu çocukları sarmala” der; URL’yi çocuklar taşır (`/characters`, `/characters/new`, …). Parent’a `path="/characters"` yazsaydın iç içe göreli path matematiği ayrı konu — biz absolute child path kullandık.

`/login` ve `/register` bu bloğun **kardeşi**. Kullanıcı kayıt formunda “Karakterler / Çıkış” görmesin; token zaten yok. Layout child’ına girmeden login çizilir.

`/` ve `*` hâlâ listeye `Navigate`. Token yoksa layout kendi içinde login’e atar: `/` → `/characters` → `AppLayout` → token yok → `/login`.

#### `<Outlet />` — RenderBody

```57:81:web/src/AppLayout.tsx
  return (
    <PermissionContext.Provider value={{ permissions }}>
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
              <button
                type="button"
                className="app-header__logout"
                onClick={handleLogout}
              >
                Çıkış
              </button>
          </header>
        <main className="app-main">
          <Outlet />
        </main>
    </div>
    </PermissionContext.Provider>
```

9 Ağustos’ta `PermissionContext.Provider` yoktu; kök doğrudan `<div className="app-shell">`. Header + `<main><Outlet /></main>` aynıydı. Router `/characters` deyince: (1) parent `AppLayout` çizilir, (2) uyan child `CharactersPage`, (3) child **Outlet’in olduğu yere** konur. `/characters/:id` → aynı header, Outlet içinde detay. Sayfa değişince header unmount olmaz; yalnız Outlet içeriği değişir. `_Layout.cshtml` + `@RenderBody()`: layout bir, body action’a göre.

```1:6:web/src/AppLayout.css
.app-shell {
  min-height: 100svh;
  display: flex;
  flex-direction: column;
  text-align: left;
  background: var(--bg);
```

Kabuk dikey flex: header üstte, `app-main` kalan yükseklik. Renk kilidi yarın (bölüm 21); o gün `var(--bg)` henüz palet notu yoktu, class isimleri duruyordu.

#### Token guard ve Çıkış — tek yer

```10:13:web/src/AppLayout.tsx
function AppLayout() {
  const navigate = useNavigate()
  // const token = localStorage.getItem('token') ortak auth
  const token = getToken()
```

```39:45:web/src/AppLayout.tsx
  if (!token) {
    return <Navigate to="/login" replace />
  }

  if (!meLoaded) {
  return <p>Yükleniyor…</p>
}
```

9 Ağustos’ta `meLoaded` yoktu: token yok → login, varsa hemen header + Outlet. `getToken()` 11 Ağustos `api.ts` (bölüm 22); o gün `localStorage.getItem('token')`. `Navigate replace`: korumalı URL history’de birikmesin.

```17:36:web/src/AppLayout.tsx
  useEffect(() => {
  if (!token) {
    return
  }

  async function loadMe() {
    try {
      const meResponse = await apiFetch('/api/auth/me')
      if (meResponse.ok) {
        const me = await meResponse.json()
        setPermissions(me.permissions ?? [])
      }
    } catch {
      // /me gelmese de meLoaded bitsin; yoksa sonsuz Yükleniyor
    }
    setMeLoaded(true)
  }

  loadMe()
}, [token])
```

Bu blok 26–27 Ağustos (bölüm 30–31). 9 Ağustos layout’unda `/me` yoktu. Bugün tek `/me` burada; çocuk sayfalar `usePermissions` okur. `meLoaded` bitmeden Outlet yok — sonsuz “Yükleniyor” tuzağı bölüm 29.

```52:55:web/src/AppLayout.tsx
  function handleLogout() {
    clearToken()
    navigate('/login')
  }
```

3 Ağustos’ta Çıkış `CharactersPage`’deydi (bölüm 15). 9 Ağustos’ta `localStorage.removeItem('token')` + `navigate`. Bugün `clearToken()` refresh’i de siler (bölüm 33). `CharactersPage` yorum satırlarında o günkü taşıma duruyor: Navigate ve logout “AppLayout’dan yapılacağı için sildik.”

#### Bu kodu kim tetikliyor?

`/characters` (ve new/edit/detay) → `AppLayout` mount → (bugün `/me`) → Outlet’te ilgili sayfa. `/login` layout’suz `LoginPage`. Backend’e 9 Ağustos’ta yeni endpoint yok.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `AppLayout.tsx` yazıp `App.tsx`’te hâlâ düz `<Route path="/characters" element={<CharactersPage />} />` bırakmak — menü hiç çizilmez. Bir diğeri: Login’i parent’ın **içine** almak — formun üstünde Çıkış, token yokken garip döngü. Bir diğeri: her child’da token `Navigate` kopyası bırakmak — layout zaten atıyor. Outlet’i unutup `{children}` beklemek — nested `Route` `children` prop vermez, `Outlet` şart.

#### Sonuçta ne kazandık

Korumalı sayfalar ortak üst menü + tek Çıkış; login/kayıt çıplak. Sonraki kısa bölüm palet kilidi (bölüm 21).

---

### 21. 10 Ağustos — 60-30-10 renk kararı ve palet kilidi

**Commit:** `b3ff9a8` (10 Ağustos). Plan bunu kısa tutar: CSS dersi değil, **rol kilidi**.

Bu adımda dosyaları şu sırayla değiştirdik. Önce `index.css` `:root` — hex’ler dağınık class’larda durmasın, isimlendirilmiş değişken olsun (`--bg`, `--surface`, `--accent`). Sonra `CharactersPage.css` ve `AppLayout.css` aynı değişkenleri kullandı: kart/header yüzey, buton vurgu. `palettes-reference.css` import edilmez; teal ve reddedilen açık temalar yedek kopya. Teal denemesi (`#00ADB5`) iyi durmuştu; aynı 60-30-10 iskeleti **mor hue** ile kilitlendi. Beyaz kartlı deneme (HH12) paletten kopuk “sırıtıyordu”, reddedildi.

#### 60-30-10 — oran değil, iş

Üç sayı bir yasa değil; hiyerarşi: büyük kısım zemin (göz dinlenir), orta kısım yüzey (kart, header, form — zeminden ayrılır, bağırmaz), az kısım vurgu (buton, “tıkla”). Comics color script aynı fikir: atmosfer çok, kostüm orta, neon az. Anime’de resmi kanun yok; stüdyo aynı mantığı kullanır.

Eşleme: `appsettings.json`’da connection string’i her controller’a gömmezsin; bir key, her yer okur. `var(--accent)` aynı: hex bir yerde, butonlar onu tüketir. Bir yerde `#b39bc9` yazıp başka yerde rastgele mor = iki kaynak.

```1:29:web/src/index.css
/* Ayni tema iskeleti (teal gibi), hue = mor
   60% koyu mor zemin | 30% acik mor kart | 10% kontrast buton
*/

:root {
  --bg: #1e1a24;
  --surface: #2e2838;
  --muted: #262030;
  --border: #3d364a;
  --text: #d8d2e3;
  --text-h: #f3eef8;

  --primary: #b39bc9;
  --primary-hover: #c9b6db;
  --secondary: #2e2838;

  --accent: #b39bc9;
  --accent-hover: #c9b6db;
  --danger: #e57373;
  --radius: 10px;
  --shadow: 0 1px 2px rgba(0, 0, 0, 0.35), 0 8px 24px rgba(0, 0, 0, 0.35);

  --sans: "Segoe UI", system-ui, sans-serif;
  --heading: "Segoe UI", system-ui, sans-serif;

  font: 16px/1.45 var(--sans);
  color: var(--text);
  background: var(--bg);
  color-scheme: dark;
```

`#1e1a24` sayfa arkası (%60). `#2e2838` header / kart / form (%30). `#b39bc9` açık lavanta vurgu (%10) — koyu zeminde kontrast. Yazı `#f3eef8` / `#d8d2e3`. `color-scheme: dark` tarayıcı form kontrollerini koyu varsayar.

```27:35:web/src/CharactersPage.css
.characters-page__actions a,
.characters-page__actions button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.45rem 0.9rem;
  border-radius: 999px;
  border: 1px solid transparent;
  background: var(--accent);
```

Buton hex ezmez; `--accent` yer. Header yüzey:

```9:17:web/src/AppLayout.css
.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.85rem 1.5rem;
  border-bottom: 1px solid var(--border);
  background: var(--surface);
  box-shadow: var(--shadow);
```

```1:4:web/src/palettes-reference.css
/*
  Bu dosya IMPORT EDILMEZ — sadece kopyala-yapistir referansi.
  B/C/D denemek: ilgili :root icindeki degiskenleri index.css :root ile degistir.
  (Yorum icine baska yorum yazma — CSS'te star-slash erken kapanir.)
*/
```

Eski teal ve açık B/C denemeleri burada. Aktif tema `index.css`. Yorum içine `/* */` yazmak yorumu erken kapatır — dosya başındaki uyarı o yüzden.

#### Bu kodu kim tetikliyor?

Hiçbir API. `index.css` `main.tsx` ile yüklenir; `:root` tüm 5173 ağacına iner. Backend palet bilmez.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: her yere beyaz kart — %30 rolü var ama seçilen `--surface` değil, paletten kopuk. Secondary ile accent birbirine çok yakın (iki soluk mor) → %10 kaybolur. Üçü de kapkara → yazı okunmaz. Dribbble’dan hex alıp role map etmemek. Palet yeniden seçilebilir; önce roller, sonra hex. Framework (Tailwind tema paketi) şart değil.

#### Sonuçta ne kazandık

Zemin / yüzey / vurgu kilitli, tek `:root`. Ertesi gün tekrarlayan `fetch` tek dosyaya indi (bölüm 22).

---

### 22. 11 Ağustos — `api.ts` / `apiFetch`, TypeScript tipleri, sayfa migrasyonu

**Commit:** `cede43b` (11 Ağustos).

Layout ve palet duruyordu; her sayfa hâlâ `https://localhost:7275` + `Authorization: Bearer` + `JSON.stringify` kopyalıyordu. Bu adımda önce `web/src/api.ts` yazıldı: `API_BASE`, `getToken` / `setToken` / `clearToken`, `apiFetch`. Sonra Login, Register, liste, create, detail, edit, `AppLayout` aynı helper’a geçti. Eski `fetch` blokları yorumda kaldı (öğrenim). TypeScript dili aynı gün: `type`, `Promise<Response>`, `Record<string, string>`, `auth: false`. Refresh token helper’ları 29 Ağustos (bölüm 33); 11 Ağustos’ta `clearToken` yalnız `token` siliyordu.

#### Neden tek helper?

DRY: URL veya header bir yerde yanlışsa yedi dosyada ararsın. C# tarafında `HttpClient` + `BaseAddress` + `DefaultRequestHeaders.Authorization` aynı fikir. `apiFetch` `Response` döner; 401’i sayfa yorumlar — 11 Ağustos’ta henüz sessiz yenileme yok (bölüm 34).

```1:9:web/src/api.ts
export const API_BASE = 'https://localhost:7275'

export function getToken(): string | null {
  return localStorage.getItem('token')
}

export function setToken(token: string) {
  localStorage.setItem('token', token)
}
```

```11:14:web/src/api.ts
export function clearToken() {
  localStorage.removeItem('token')
  localStorage.removeItem('refreshToken')
}
```

11 Ağustos’ta `clearToken` yalnızca `token` kaldırıyordu. `getRefreshToken` / `setRefreshToken` satır 16–21 sonra eklendi.

#### `type` C# `class` değil

```23:28:web/src/api.ts
type ApiFetchOptions = {
  method?: string
  body?: unknown
  /** false = login/register (Bearer yok). Varsayılan true. */
  auth?: boolean
}
```

Runtime’da nesne üretmez; derleme zamanı şekil tarifi. C# `record` / DTO `class`’ın *şekline* yakın, `new ApiFetchOptions()` yok. `?` opsiyonel alan. `unknown` = “bir şey gelebilir, önce daralt” — `object`’ten daha sıkı niyet. `interface` de olurdu; burada `type` seçildi.

#### `Promise<Response>` ve varsayılan options

```30:54:web/src/api.ts
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

`async` her zaman Promise döner. `Promise<Response>` = bitince tarayıcı `fetch` cevabı; `.ok`, `.status`, `.json()`. C# kabaca `Task<HttpResponseMessage>`. `options = {}`: ikinci argüman yoksa boş obje — `apiFetch('/api/characters')` GET + `auth: true`.

`const { method = 'GET', body, auth = true } = options` destructuring: alan yoksa sağdaki default. `Record<string, string>` C# `record` **değil**; anahtarı string, değeri string sözlük (`Dictionary<string, string>`). `headers['Content-Type']` veya `headers.Authorization` aynı map.

`body !== undefined` iken `JSON.stringify` burada — sayfa `[object Object]` gönderemez (bölüm 12’deki hata). `path` göreli (`/api/...`); `API_BASE` öne eklenir.

#### `auth: false` — login/register

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

`auth === false` → `if (auth)` çalışmaz → Bearer yok. Login’de henüz token yok; varsayılan `true` eski/boş token’ı yanlışlıkla basmasın. Register aynı `auth: false`. Liste/create/detay/edit `auth` vermez → `true` → Bearer.

Akış: path + base → method/body/auth ayıkla → headers → body varsa JSON → auth ve token varsa Bearer → `fetch` → `Response`. Sayfa `ok` / `status` yorumlar.

| Durum | `fetch` | Sayfa |
|--------|---------|--------|
| Ağ, SSL, Api kapalı | **throw** | `catch` → “API’ye ulaşılamadı” |
| 401 / 400 / 403 | throw **etmez** | `response.ok === false` |

Helper bozuk sanma; çoğu “ulaşılamadı” backend kapalı veya 7275 sertifikası.

#### Hangi sayfa taşındı

Login `setToken`; Register POST; `CharactersPage` GET liste; create GET preview + POST; detail GET + DELETE; edit GET + PUT; `AppLayout` `getToken` + logout `clearToken`. Tek canlı `fetch` `api.ts` içinde. Link `to={...}` hâlâ router path; Api’ye gitmek `apiFetch` ister (bölüm 23 eşlemesi).

#### Bu kodu kim tetikliyor?

Önceki bölümlerdeki aynı endpoint’ler: `POST /api/auth/login`, `GET /api/characters`, PUT/DELETE by id. Değişen 5173’ün `HttpClient` tek kapı. Backend 11 Ağustos’ta yeni action yok.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `auth: false` unutup login’e Bearer. Bir diğeri: `apiFetch`’in 401’de otomatik login sandırmak — o gün (ve bugün, 34 yazılana kadar) sayfa `ok` bakar. Bir diğeri: `Record`’ı C# `record` sanmak. Path’e tam URL yazmak → `https://localhost:7275https://...`. Login hâlâ `setRefreshToken` (29 Ağu); 11 Ağustos’ta yalnız `setToken`.

#### Sonuçta ne kazandık

URL, JSON, Bearer tek dosyada. Sonraki bölüm Blok A ile Blok B’yi satır satır bağlar: hangi `apiFetch` hangi controller metoduna gider (bölüm 23).

---

### 23. 13 Ağustos — Tekrar: hangi frontend çağrısı hangi backend metoda gider

**Commit:** `e02f0e2` (13 Ağustos). Kod eklenmedi; `REACT-OGRENIM.md`’e test soruları yazıldı. Bu V2 bölümü aynı soruları **bugünkü** koda bağlar. Blok A (Temmuz API) ile Blok B (Ağustos React) burada birleşir. 13 Ağustos’ta PUT hâlâ `[Authorize(Roles = Admin)]` idi; bugün `HasPermission` (bölüm 26). Router path ile API path o gün karışıyordu — asıl konu o.

#### İki farklı “yol”

React Router path = adres çubuğu → hangi **sayfa**. API HTTP path = `apiFetch` → hangi **controller action**. İkisi `characters` ve `id` kelimelerini paylaşır, aynı şey değildir. `<Link to={...}>` neredeyse her zaman router. Backend için sayfa içinde `apiFetch`. “Edit” yazdık diye C#’ta `Edit` metodu arama: bizde `Update`, HTTP `PUT`.

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

Bu `POST /api/auth/login` → `AuthController.Login` → `LoginCommand` (bölüm 8). `Link to="/register"` ise yalnız `RegisterPage` açar; register POST ayrı `apiFetch` (bölüm 14).

#### Liste GET → `GetPaged` (isim URL’de yok)

```62:64:web/src/CharactersPage.tsx
  async function load() {
    try{
      const response = await apiFetch('/api/characters?page=1&pageSize=20')
```

```12:32:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
[ApiController]//Otomatik model binding + 400 davranışı
[Route("api/[controller]")]
public sealed class CharactersController : ControllerBase
{
    private readonly IMediator _mediator;
    //HTTP → MediatR köprüsü; controller iş mantığı bilmez

    public CharactersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedCharacterRowsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedCharacterRowsResult>> GetPaged(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCharactersQuery(page, pageSize), cancellationToken);
        return Ok(result);
```

ASP.NET **HTTP yöntemi + route şablonu** ile eşler. `GetPaged` Scalar’da görünür; fetch’te yazılmaz. `[controller]` → `Characters` → `/api/characters`. Query `page` / `pageSize` model bind. `Send(GetCharactersQuery)` → handler → `{ items, totalCount }` JSON camelCase. React `setItems(data.items)`. MVC View action adı gibi URL’ye gömülmez.

#### `Clamp` 200 ve `Skip` — koruma ve 0 tabanlı ofset

```21:32:ReactBattleArena/ReactBattleArena.Application/Characters/Queries/GetCharactersQueryHandler.cs
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.Characters
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
```

`Math.Max(1, page)`: `page=0` yine 1. `Clamp(..., 1, 200)`: **200 varsayılan sayfa boyutu değil**, sunucu tavanı; `pageSize=5000` DB’yi kilitlemesin. Frontend sabit 20 ister, 20 gelir. `AsNoTracking` salt okuma. `OrderByDescending` Skip’ten **önce** — yoksa dilim rastgele kayar.

`(page - 1) * pageSize`: UI 1. sayfa der, SQL ofset 0’dan sayar (dizi indeksi gibi). Sayfa 1 → Skip 0; sayfa 2 → Skip 20. `Take` dilim uzunluğu. `CountAsync` toplam (ileride “1/5”).

#### Karttaki `Link` GetById çağırmaz

```11:13:web/src/CharacterCard.tsx
function CharacterCard({ id, name, universe, rarity, imageUrl }: CharacterCardProps) {
  return (
      <Link to={`/characters/${id}`} className="character-card-link">
```

Bu satır API atmaz. Router `/characters/{guid}` → `CharacterDetailPage`. Orada `useParams` + `apiFetch(\`/api/characters/${id}\`)` → `[HttpGet("{id:guid}")] GetById` → `GetCharacterByIdQuery`. Liste DTO’sunda stats vardır (`CharacterRowDto.BaseAttack` …); kart **göstermez** — unutulmuş API değil, UI tercihi. Arena’da ATK satırı ileride ürün kararı.

SPA “tüm veriyi bir kez çek” demez: tam HTML reload olmadan ekran değişir; veri hâlâ HTTP. Liste kendi GET’i, detay **yeni** GetById, edit **yine yeni** GetById. Liste state’i props ile taşınmaz. Avantaj: başka admin güncellediyse taze satır. Dezavantaj: ekstra istek.

#### `/characters` açılınca sıra

Adres `/characters` (veya `/` → Navigate). `AppLayout` + Outlet → `CharactersPage` mount. İlk render `items = []`, grid boş. `useEffect(..., [])` → `load` → GetPaged → `setItems` → `map` → `CharacterCard`. Effect derinliği bölüm 19’da kaldı.

#### Detay → Edit: Link router, Kaydet PUT

```148:149:web/src/CharacterDetailPage.tsx
          {hasPermission(permissions, PERMISSIONS.charactersUpdate) && (
            <Link to={`/characters/${id}/edit`}>Düzenle</Link>
```

13 Ağustos’ta `hasPermission` yoktu; link herkese görünürdü. Asıl kapı yine API. `Link` → `/characters/:id/edit` (route `:id`’den önce, bölüm 18). Edit kendi GET’i (yine GetById) → `setName(data.name)` … Kaydet:

```116:118:web/src/CharacterEditPage.tsx
      const response = await apiFetch(`/api/characters/${id}`, {
        method: 'PUT',
        body: {
```

Backend `[HttpPut("{id:guid}")] Update`. 13 Ağustos’ta `[Authorize(Roles = Admin)]`; bugün `HasPermission(CharactersUpdate)`. 204 → `json()` yok → `navigate` detaya. Detaydaki `character` state edit’e geçmez. Player PUT → 403.

#### `App.tsx` veri koymaz

```16:21:web/src/App.tsx
      <Route element={<AppLayout />}>
        <Route path="/characters" element={<CharactersPage />} />
        <Route path="/characters/new" element={<CharacterCreatePage />} />
        <Route path="/characters/:id/edit" element={<CharacterEditPage />} />
        <Route path="/characters/:id" element={<CharacterDetailPage />} />
      </Route>
```

Harita: URL → component. JSON her sayfanın `apiFetch` + `useState` işi. `Outlet` child’ı ortada (bölüm 20). ASP.NET endpoint routing’e benzer fikir; burada UI ekranı seçersin, JSON endpoint değil.

#### Bu kodu kim tetikliyor?

Aynı Temmuz controller’ları, Ağustos sayfaları. Yeni backend yok. Tablo (13 Ağu kafasındaki hali, bugün permission id’leri aynı path):

Login POST `/api/auth/login` → `Login`. Register POST `/api/auth/register` → `Register`. Liste GET query → `GetPaged`. Kart tıklanınca router; detay GET `{id}` → `GetById`. Create POST `/api/characters` → `Create`. Edit PUT → `Update`. Sil DELETE → `Delete`.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `GetPaged`’i URL’ye yazmak. `Link`’i GetById sanmak. Edit state’inin detaydan geldiğini sanmak. 204’te `json()`. “SPA = cache” — her ekran kendi GET’i.

#### Sonuçta ne kazandık

İki yol ayrıldı: 5173 sayfa, 7275 action. Katalog CRUD iki taraftan okunur. Yetki hâlâ “Admin string”; 20 Ağustos’ta fiil tablolarına geçildi (Blok C, bölüm 24).

---

## Blok C — Backend'e dönüş: RBAC (20–24 Ağustos)

React CRUD ve `apiFetch` duruyordu. `Users.Role` + `[Authorize(Roles = Admin)]` ShopOwner veya “Player yarın create etsin” deyince her attribute’u elle değiştirmek demekti. Bu blokta yetki **permission koduna** bağlanır; frontend 24 Ağustos’ta UI gizlemeye geçer. Refresh token ayrı (Blok E).

---

### 24. 20 Ağustos — `Role`, `Permission`, `UserRole`, `RolePermission`, composite PK

**Commit:** `eaa0c81` (20 Ağustos).

Bu adımda dosyaları şu sırayla ekledik. Önce Domain `Role` ve `Permission` — fiil ve çanta isim nesneleri, henüz HTTP yok. Sonra join entity’ler `UserRole` ve `RolePermission` (ara tablo; `User` üzerinde `ICollection` yazmadık). Sonra Infrastructure configuration: tablo adı, unique `Name`/`Code`, **composite PK**. `IApplicationDbContext` + `ApplicationDbContext` `DbSet`’leri. **Migration yoktu** — tablo 21 Ağustos’ta `AddRbacTables` (bölüm 25). `Users.Role` string kolonu duruyordu (bugün de duruyor); JWT claim hâlâ o string. `[HasPermission]` 22 Ağustos (bölüm 26).

#### Neden `User.Role` tek kolon yetmez?

```22:23:ReactBattleArena/ReactBattleArena.Domain/Users/User.cs
    public string Role { get; private set; } = null!;
    //= null!; = “derleyiciye: başlangıçta null görünebilir ama runtime’da asla null kalmayacak” demek.
```

```5:9:ReactBattleArena/ReactBattleArena.Domain/Authorization/Roles.cs
public static class Roles
{
    public const string Admin = "Admin";
    public const string Player = "Player";
    public const string ShopOwner = "ShopOwner";
```

Bölüm 9: JWT `ClaimTypes.Role`, karakter yazma `[Authorize(Roles = Admin)]`. İş kuralı **rol ismine gömülü**. ShopOwner eklemek = her Admin kontrolünü ve her butonu tek tek düşünmek. Player yarın karakter eklesin = controller’ı yeniden yazmak. Hedef cümle: “Admin mi?” değil, “`characters.create` var mı?”

AuthN: kimsin (login, BCrypt, JWT) — 401. AuthZ: ne yapabilirsin — 403. Frontend buton gizlemek yetki değildir; API yine 403 (bölüm 28).

`User.RoleId` tek FK de yetmez: one-to-many, bir kişinin **tek** rolü. “Player + ShopOwner” imkânsız (veya virgüllü string, kırılır).

#### Role ve Permission — çanta ve fiil

```3:21:ReactBattleArena/ReactBattleArena.Domain/Authorization/Role.cs
public sealed class Role
{
    private Role()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public static Role Create(string name)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name
        };
    }
}
```

```3:21:ReactBattleArena/ReactBattleArena.Domain/Authorization/Permission.cs
public sealed class Permission
{
    private Permission()
    {
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public static Permission Create(string code)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Code = code
        };
    }
}
```

Role = isim çantası (`Admin`, `Player`). Tek başına endpoint korumaz. Permission = somut fiil string: `characters.create` / `update` / `delete`, `shop.items.create` (shop ekranı yok, kod kayıt için). Private constructor + `Create` factory, Character/User ile aynı DDD kalıbı (bölüm 1). `Roles.ShopOwner` sabiti 9 Ağustos yorumundan kalma; çanta satırı seed’de gelecek (bölüm 25).

#### Ara tablolar — many-to-many

```1:20:ReactBattleArena/ReactBattleArena.Domain/Authorization/UserRole.cs
namespace ReactBattleArena .Domain.Authorization;

public sealed class UserRole
{
    private UserRole()
    {

    }
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
}
```

Dosyada namespace’te bir boşluk var (`ReactBattleArena .Domain`) — 20 Ağustos’tan kalan yazım; derleyici aynı assembly içinde yine bu tipi görür. `UserRole`: kim hangi çantaya üye. İki satır = iki rol (Mehmet Player **ve** ShopOwner).

```3:21:ReactBattleArena/ReactBattleArena.Domain/Authorization/RolePermission.cs
public sealed class RolePermission
{
    private RolePermission()
    {
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        };
    }
}
```

`RolePermission`: bu çanta bu fiili yapabilir. Player’a create vermek = buraya satır; `Create` action aynı kalır. Login’de iki rolün yetkileri **birleşir** (union). Mehmet figür ekler (shop permission), karakter ekleyemez (`characters.create` hiçbir rolünde yoksa).

Eşleme: SQL join `Users`–`UserRoles`–`Roles`; Identity’de `AspNetUserRoles` aynı fikir. EF `HasMany` + join entity; `User` sınıfına `ICollection<UserRole>` yazmadık, ilişki configuration’da durur.

#### Composite PK ve silme davranışı

```13:26:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserRoleConfiguration.cs
        builder.ToTable("UserRoles");
        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        //Cascade (User): kullanıcı silinince o kullanıcının UserRoles satırları da silinsin.

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        // Restrict(Role): rol hâlâ birine bağlıysa rolü silemezsin.
```

İki kolon birlikte birincil anahtar: aynı kullanıcı-rol çifti iki kez yazılamaz, ayrı `Id` Guid’i yok. `WithMany()` boş: navigation collection yok, FK yine var. User silinince üyelikler gitsin (Cascade). Rol silinmesin, hâlâ üye varken (Restrict).

```11:22:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/RolePermissionConfiguration.cs
        builder.ToTable("RolePermissions");
        builder.HasKey(x => new { x.RoleId, x.PermissionId });

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
```

Her iki FK Restrict: fiil veya rol hâlâ bağlıyken silme. `Roles.Name` ve `Permissions.Code` unique (configuration dosyaları).

```12:16:ReactBattleArena/ReactBattleArena.Application/Abstractions/IApplicationDbContext.cs
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
```

20 Ağustos’ta `RefreshTokens` yoktu (bölüm 32). `DbSet` handler’ın tabloya uzanması; Application Infrastructure class’ını görmez (bölüm 1).

#### Bu kodu kim tetikliyor?

20 Ağustos’ta **hiçbir HTTP**. Tablolar henüz migration’sız; login hâlâ `Users.Role` string + JWT. Frontend aynı `apiFetch`. Seed ve `dotnet ef` ertesi gün (bölüm 25).

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `User.Role`’ü silip JWT’yi unutmak — kolon duruyor, claim hâlâ oradan (login handler değişmedi). Join’e `Id` Guid koyup aynı çifti iki kez eklemek. `ICollection` yazmadan ilişkinin “yok” sanılması. Permission’ı JWT’ye gömmek — o gün bilinçli olarak DB’de bırakıldı (bölüm 26: her istekte join).

#### Sonuçta ne kazandık

Çanta ve fiil nesneleri + ara tablolar modelde. SQL ve seed yok; endpoint hâlâ Admin string. 21 Ağustos’ta tablo + seeder (bölüm 25).

---

### 25. 21 Ağustos — `AddRbacTables`, `PermissionCodes`, `AuthSeeder`, `CreateScope`

**Commit:** `de5d782` (21 Ağustos).

Bu adımda dosyaları şu sırayla ekledik. Önce `PermissionCodes` — fiil string’leri sihirli metin olmasın, C# sabiti olsun (`characters.create`). `Roles.ShopOwner` sabiti aynı gün netleşti. Sonra `dotnet ef migrations add AddRbacTables`: dünün configuration’ı SQL’e döküldü (`Users.Role` kolonuna dokunulmadı). Sonra `AuthSeeder` — katalog satırları (rol, fiil, RolePermission, eski `Users.Role` → `UserRoles`); idempotent, “yoksa ekle”. En sonda `Program.cs` `CreateScope` + `SeedAsync`: DbContext scoped, `Program` istek değil. `[HasPermission]` yoktu (bölüm 26); login hâlâ JWT’ye `Users.Role` yazar. Register hâlâ yalnız string `Player` yazar; `UserRoles` satırı bir sonraki API açılışında seed’den gelir (bölüm 27’de Register doğrudan yazacak).

#### `PermissionCodes` — fiil adı tek yerde

```1:9:ReactBattleArena/ReactBattleArena.Domain/Authorization/PermissionCodes.cs
namespace ReactBattleArena.Domain.Authorization;

public static class PermissionCodes
{
    public const string CharactersCreate = "characters.create";
    public const string CharactersUpdate = "characters.update";
    public const string CharactersDelete = "characters.delete";
    public const string ShopItemsCreate = "shop.items.create";
}
```

Noktalı string DB `Permissions.Code` ile aynı. Attribute ve seeder bu sabitleri kullanır; `"characters.create"`’i üç dosyada elle yazmak typo üretir. `ShopItemsCreate` shop ekranı yokken kayıt — RolePermission’da ShopOwner’a bağlanacak. Eşleme: C#’ta `Roles.Admin` sabiti (bölüm 9); burada fiil, rol adı değil.

#### Migration — dört tablo, `Users.Role` duruyor

`AddRbacTables` `Up` configuration ile hizalı. Designer/snapshot alıntılanmaz. `Permissions` örneği:

```14:24:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260821102302_AddRbacTables.cs
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
```

`Roles` aynı fikir, `Name` max 50. Ara tablo composite PK + Restrict/Cascade:

```70:83:ReactBattleArena/ReactBattleArena.Infrastructure/Migrations/20260821102302_AddRbacTables.cs
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
```

FK bağ tablosunda: `Users`’ta `RoleId` yok. Unique index `IX_Permissions_Code`, `IX_Roles_Name`. `Down` dört tabloyu düşürür; `Users`’a dokunmaz. Komut: Api startup proje, Infrastructure migration projesi (bölüm 1’deki `ef` alışkanlığı).

#### `AuthSeeder` — yoksa ekle, string rolü join’e çevir

```9:29:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(db, Roles.Admin, cancellationToken);
        await EnsureRoleAsync(db, Roles.Player, cancellationToken);
        await EnsureRoleAsync(db, Roles.ShopOwner, cancellationToken);

        await EnsurePermissionAsync(db, PermissionCodes.CharactersCreate, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.CharactersUpdate, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.CharactersDelete, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.ShopItemsCreate, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersCreate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersUpdate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersDelete, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.ShopItemsCreate, cancellationToken);

        await EnsureRolePermissionAsync(db, Roles.ShopOwner, PermissionCodes.ShopItemsCreate, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
```

İlk `SaveChanges` rol ve fiil Id’lerinin oluşması için: `RolePermission` Guid ister, henüz insert olmamış satırda Id yok. Admin dört fiili alır; ShopOwner yalnız shop; Player’a `RolePermission` yok — katalog GET zaten açık, yazma 403 kalır (kapı hâlâ Role attribute, yarın permission).

```59:75:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
    private static async Task EnsureRoleAsync(
        ApplicationDbContext db,
        string name,
        CancellationToken cancellationToken)
    {
        if (!await db.Roles.AnyAsync(r => r.Name == name, cancellationToken))
            db.Roles.Add(Role.Create(name));
    }

    private static async Task EnsurePermissionAsync(
        ApplicationDbContext db,
        string code,
        CancellationToken cancellationToken)
    {
        if (!await db.Permissions.AnyAsync(p => p.Code == code, cancellationToken))
            db.Permissions.Add(Permission.Create(code));
    }
```

Idempotent: ikinci `dotnet run` aynı `Admin` satırını çoğaltmaz (`Name` unique de patlardı). `EnsureRolePermissionAsync` `(RoleId, PermissionId)` var mı diye bakar.

```31:56:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/AuthSeeder.cs
        var rolesByName = await db.Roles.ToDictionaryAsync(r => r.Name, cancellationToken);
        var users = await db.Users.ToListAsync(cancellationToken);

        // Composite PK çiftleri — "bu kullanıcıya bu rol zaten verilmiş mi?"
        var existingPairs = (await db.UserRoles.ToListAsync(cancellationToken))
            .Select(x => (x.UserId, x.RoleId))
            .ToHashSet();

        foreach (var user in users)
        {
            // Eski kolon Users.Role (string) → yeni UserRoles satırı
            var roleName = string.IsNullOrWhiteSpace(user.Role) ? Roles.Player : user.Role;
            if (!rolesByName.TryGetValue(roleName, out var role))
                role = rolesByName[Roles.Player];

            if (existingPairs.Contains((user.Id, role.Id)))
                continue;

            db.UserRoles.Add(UserRole.Create(user.Id, role.Id));
            existingPairs.Add((user.Id, role.Id)); // aynı kullanıcı döngüde iki kez eklenmesin
        }

        await db.SaveChangesAsync(cancellationToken);
```

`ToDictionaryAsync(r => r.Name)` tek değer değil: Key `"Admin"` → o `Role` satırı (Guid). Döngüde her user için tekrar `Roles` sorgusu yok. SSMS’te `Role = Admin` yapılmış kullanıcı `UserRoles`’a Admin Guid’i alır. Bilinmeyen string → Player. HashSet aynı çifti ikinci kez eklemesin (composite PK ihlali).

Bu seed **katalog**: her açılışta şema dolsun. Geçici test datası değil. Frontend bu gün değişmedi.

#### `CreateScope` — kökten scoped alınmaz

```68:74:ReactBattleArena/ReactBattleArena.Api/Program.cs
// DbContext scoped (istek ömrü). Program kökü request değil → CreateScope ile kısa ömürlü kapsül;
// using bitince context Dispose. Yoksa root provider'dan scoped alınamaz.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await AuthSeeder.SeedAsync(db);
}
```

`ApplicationDbContext` DI’da scoped: bir HTTP isteği boyunca bir instance. `Program.cs` istek değil; `app.Services` kök provider. Kökten scoped çekmek runtime hatası. `CreateScope()` kısa ömürlü kapsül (IHostedService / console’daki `IServiceScope` ile aynı fikir): seed bitince `using` Dispose. Bugünkü `Program.cs` üstte `PermissionPolicyProvider` kayıtları var (bölüm 26); 21 Ağustos’ta yalnız bu `using` bloğu eklendi.

#### Bu kodu kim tetikliyor?

`dotnet ef database update` (veya Api açılışında migrate politikası neyse) tabloları kurar. Her `dotnet run` → seed. Tarayıcı 5173 bu gün yeni endpoint görmez: POST karakter hâlâ `[Authorize(Roles = Admin)]`. Scalar’da Roles/Permissions tabloları dolu; JWT claim hâlâ string `role`.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `SaveChanges`’i RolePermission’dan **önce** unutmak — FK Guid boş. Migration’sız seed → tablo yok. Seed’i controller’a gömmek. Register olup Api’yi kapatmadan `UserRoles` beklemek — 21 Ağustos’ta Register join yazmıyor; Robin API açıkken kaydolduysa satır bir sonraki restart’ta gelir (bölüm 27’de kapanacak). `Users.Role`’ü drop etmek — JWT bozulur, o kolon kasıtlı durdu.

#### Sonuçta ne kazandık

Dört tablo SQL’de, fiil kodları sabit, her açılışta idempotent katalog + eski string roller join’e kopyalanıyor. Endpoint hâlâ “Admin mi?”; yarın her istekte DB join (bölüm 26).

---

### 26. 22 Ağustos — `HasPermission`, policy provider, yetki JWT’de değil DB’de

**Commit:** `441ae01` (22 Ağustos).

Bu adımda dosyaları şu sırayla ekledik. Önce Application’da `IUserPermissionService`, Infrastructure’da `UserPermissionService` — join `UserRoles` → `RolePermissions` → `Permission.Code`; `UserPermission` tablosu yok. Sonra Api `PermissionRequirement`, `HasPermissionAttribute` (`Policy = "Permission:" + kod`), `PermissionPolicyProvider` (bilmediği adı default’a devreder), `PermissionAuthorizationHandler` (token’dan id, DB’den kod listesi). `Program.cs`: `AddAuthorization()`, provider Singleton, handler Scoped. `CharactersController` Create/Update/Delete `[Authorize(Roles = Admin)]` yerine üç ayrı `[HasPermission]`. GET liste/detay açık kaldı. JWT’ye permission claim yazılmadı: canlıda satır ekleyince aynı token 201 olsun diye.

#### Join servisi — fiil kullanıcı satırında değil

```3:6:ReactBattleArena/ReactBattleArena.Application/Abstractions/IUserPermissionService.cs
public interface IUserPermissionService
{
    Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

```15:26:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/UserPermissionService.cs
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
    }
```

`Users.Role` string’i bu sorguda **yok**. Robin’in `Role = Player` kolonu yetmez; `UserRoles` satırı şart (bölüm 25 seed / 27 Register). `Distinct`: iki rol aynı fiili verse bir kez. Klasik RBAC: kullanıcıya rol, role fiil; kullanıcıya doğrudan fiil tablosu (ACL istisnası) yok — “yapmayalım diye atlanmadı”, bu ürün için doğru model.

```30:30:ReactBattleArena/ReactBattleArena.Infrastructure/DependencyInjection.cs
        services.AddScoped<IUserPermissionService, UserPermissionService>();
```

Interface Application, implement Infrastructure (DbContext gibi, bölüm 1). Scoped: istek başına bir servis, handler ile aynı ömür. Satırın üstündeki `IRefreshTokenGenerator` 29 Ağustos (bölüm 33); 22 Ağustos’ta yoktu.

JWT’ye permission gömmek ayrı okul (stateless access). Burada **anında kes/ver**: SSMS’te `RolePermissions` satırı, yeniden login yok. Token yalnız kimlik (`sub` / `NameIdentifier`).

#### Policy adı nasıl üretilir

```5:11:ReactBattleArena/ReactBattleArena.Api/Authorization/HasPermissionAttribute.cs
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = "Permission:" + permission;
    }
}
```

`[HasPermission(PermissionCodes.CharactersCreate)]` aslında `[Authorize(Policy = "Permission:characters.create")]`. Framework policy adını DI’daki tek `IAuthorizationPolicyProvider`’a sorar.

```6:37:ReactBattleArena/ReactBattleArena.Api/Authorization/PermissionPolicyProvider.cs
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string Prefix = "Permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var code = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(code))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
```

Kendi provider’ı kaydedince **default’un yerini alırız**. `[Authorize]` (`/me`, bölüm 27) ve named policy’ler hâlâ default’un işi. `_fallback = new DefaultAuthorizationPolicyProvider(options)`: yerini aldığımız sınıfın kopyasını yanında taşı, `Permission:` değilse ona sor. `GetDefaultPolicyAsync` / `GetFallbackPolicyAsync` da fallback’e — boş `[Authorize]` bozulmasın. `DefaultAuthorizationPolicyProvider` zaten public; gizlilik için `new` etmiyoruz, yerini doldurduğumuz için davranışı biz sürdürüyoruz.

Tek görünmeyen zincir: attribute bir **isim** yazar (`Permission:characters.create`); provider o ismi `PermissionRequirement` + “önce login ol” kuralına çevirir; handler isimdeki kodu DB listesinde arar.

```5:12:ReactBattleArena/ReactBattleArena.Api/Authorization/PermissionRequirement.cs
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Code { get; }
    public PermissionRequirement(string code)
    {
        Code = code;
    }
}
```

```7:28:ReactBattleArena/ReactBattleArena.Api/Authorization/PermissionAuthorizationHandler.cs
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserPermissionService _permissions;

    public PermissionAuthorizationHandler(IUserPermissionService permissions)
    {
        _permissions = permissions;
    }

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
}
```

`return` Succeed etmeden = başarısız (auth yoksa 401, user tanındı fiil yoksa 403). Token’da permission claim **aranmaz**. `RequireAuthenticatedUser` policy’de: Bearer yok → 401, Bearer var create yok → 403.

```21:23:ReactBattleArena/ReactBattleArena.Api/Program.cs
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

`UseAuthorization()` zaten vardı (bölüm 8); **silinmez** — middleware. `AddAuthorization()` DI: `IOptions<AuthorizationOptions>` provider constructor’ına lazım. Biri servis, biri boru hattı. Provider Singleton (stateless çevirmen); handler Scoped (DbContext kullanır). JwtBearer + `[Authorize(Roles=…)]` eskiden dolaylı authorization ile çalışırdı; kendi provider kaydınca `AddAuthorization()` açık yazılır.

#### Üç kapı, class’ta tek attribute yok

```46:47:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersCreate)]  // POST
    [HttpPost]
```

```69:70:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersUpdate)]  // PUT
    [HttpPut("{id:guid}")]
```

```95:96:ReactBattleArena/ReactBattleArena.Api/Controllers/CharactersController.cs
    [HasPermission(PermissionCodes.CharactersDelete)]  // DELETE
    [HttpDelete("{id:guid}")]
```

Class’a tek `[HasPermission(CharactersCreate)]` koymuyoruz: (1) GET liste/detay da kapanır veya her GET’e `[AllowAnonymous]` yağar. (2) Create / Update / Delete farklı kodlar — “düzenler ama silemez” üç kapı. Üçünü aynı metoda yığmak AND: Player’a yalnız create verirsen PUT yine 403.

GET `GetPaged` / `GetById` attribute’siz — tokensız 200 (bölüm 4, 13).

Test: Admin Bearer POST → 201. Player aynı POST → 403. SSMS `RolePermissions` Player + `characters.create`, **aynı token** POST → 201 (yeniden login yok). Satırı sil → 403. ShopOwner `shop.items.create` var, karakter yok. JWT decode’da permission arama.

#### Bu kodu kim tetikliyor?

React `apiFetch` POST/PUT/DELETE (bölüm 14, 18) aynı URL. Değişen kapı. 22 Ağustos’ta Register hâlâ `UserRoles` yazmıyordu: RolePermissions dolu, UserRoles boş → 403 (bölüm 25 seed / restart veya 27). Frontend buton gizleme yok (bölüm 28).

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: karakter JSON’unu `POST /api/auth/register`e göndermek. Join’in `Users.Role` string’ine bakacağını sanmak. Policy provider yazıp `AddAuthorization()` unutmak. `UseAuthorization` silmek. Class-level HasPermission. Permission’ı JWT’ye ekleyip SSMS değişince eski token’ın yetkisini taşımak.

#### Sonuçta ne kazandık

Yazma fiilleri DB join; JWT kimlik. `/me` listesi ve Register `UserRoles` 24 Ağustos (bölüm 27).

---

### 27. 24 Ağustos — `GET /api/auth/me` ve Register `UserRoles`

**Commit:** `c7ff318` (24 Ağustos).

22 Ağustos testi: Player rolüne fiil yazılmış, robin yine 403 — `UserRoles` boştu çünkü Register yalnız `Users.Role = Player` yazıyordu, seed Api **açılışında** dolduruyordu. Bu adımda önce `RegisterCommandHandler` ikinci `SaveChanges`: Player rolünün Guid’ine `UserRole`. `Roles`’ta Player yoksa `SingleAsync` patlar (seed çalışmamış; yutma). Sonra `AuthController.Me`: `[Authorize]`, token’dan id, aynı `GetCodesAsync`, JSON `permissions`. React henüz `/me` çağırmaz (bölüm 28); Scalar ve ertesi gün UI bu listeyi kullanacak.

#### Register — iki satır, seed’i bekleme

```42:60:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RegisterCommandHandler.cs
        var entity = User.Create(
            request.UserName,
            request.Email,
            request.DisplayName,
            passwordHash,
            Roles.Player,
            DateTime.UtcNow);

        _db.Users.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var playerRole = await _db.Roles.SingleAsync(
            r => r.Name == Roles.Player, cancellationToken);
        //Rol yoksa (seed çalışmamış) sessizce geçme, patlat ki fark edesin.

        _db.UserRoles.Add(UserRole.Create(entity.Id, playerRole.Id));
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;
```

İlk `SaveChanges` `entity.Id`’nin DB’de durması için (FK). `Users.Role` string hâlâ `Player` — JWT claim (bölüm 8) o kolondan; HasPermission join’e bakar. Yeni kullanıcı kaydolur olmaz POST create 403 (Player’da create yok) ama join **çalışır**; restart gerekmez. Seed default Player’a CUD vermez; elle `RolePermissions` eklediysen aynı oturumda 201.

`RegisterPage` `apiFetch` `auth: false` aynı (bölüm 14, 22); değişen backend.

#### `/me` — o anki join, token’da liste yok

```62:84:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idValue, out var userId))
            return Unauthorized();

        var codes = await _permissions.GetCodesAsync(userId, cancellationToken);

        return Ok(new
        {
            id = userId,// out var daki userId
            userName = User.Identity?.Name,
            email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value,
            permissions = codes
        });
    }
```

Bearer yok/bozuk → 401 (`[Authorize]`, policy provider fallback). Id parse olmazsa 401. `permissions` string dizisi; Admin seed’de dört kod (`characters.*` + `shop.items.create`), düz Player boş dizi (katalog GET ayrı, açık). SSMS `RolePermissions` değişince **sonraki** `/me` güncellenir, yeni login şart değil.

Controller’a `IUserPermissionService` ctor’dan girer (handler’daki aynı scoped servis). Anonymous `new { … }` DTO sınıfı yok — o gün yeterli.

Frontend 24 Ağustos’ta bu endpoint’i henüz bağlamadı. 5173 hâlâ 403’ü formdan görür; link gizleme ertesi oturum (bölüm 28).

#### Bu kodu kim tetikliyor?

Kayıt: `POST /api/auth/register` → `Users` + `UserRoles` Player. `/me`: `GET /api/auth/me` + Bearer → join. Create POST hâlâ `HasPermission` (bölüm 26).

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `/me`’nin JWT içindeki eski listeyi döndüğünü sanmak. Register’da `UserRoles`’u seed’e bırakmak (22 Ağu 403). Player yokken `SingleAsync`’i try/catch ile yutmak. `/me`’ye `[AllowAnonymous]`. React’te her sayfanın kendi `/me`’si — o henüz yok, 28–31’de gelecek.

#### Sonuçta ne kazandık

Yeni kullanıcı join’de görünür; `/me` o anki fiilleri söyler. UI gizleme ve sayfa kapıları Blok D (bölüm 28).

---

## Blok D — Frontend yetki (24–27 Ağustos)

Backend `HasPermission` + `/me` duruyordu; 5173 hâlâ herkese “Karakter ekle” gösteriyordu. Player forma girip 403 yerdi. Bu blokta önce link gizleme (UX), sonra URL kapısı (`Navigate`), sonra tek `/me` (Context). Asıl kapı API’de kalır.

---

### 28. 24 Ağustos — `permissions.ts`, `hasPermission`, `&&` ile link gizleme

**Commit:** `8a19f75` (24 Ağustos, `/me` backend’i ile **aynı gün** ama sonra; bölüm 27 API, bu bölüm 5173).

Bu adımda dosyaları şu sırayla ekledik. Önce `permissions.ts` — `"characters.create"` üç sayfada elle yazılmasın, `PERMISSIONS` sabiti + `hasPermission` (`includes`). Sonra `CharactersPage` `load` içinde `GET /api/auth/me`, `setPermissions(me.permissions ?? [])`. Header’da `{hasPermission(...) && <Link to="/characters/new">}`. `CharacterDetailPage` aynı diziyle Düzenle / Sil. Liste ve detay **ayrı** `/me` atıyordu; Context yoktu (bölüm 30). Bugün `usePermissions()` layout’tan okur; `load` içindeki `/me` yorumda.

#### Sabit ve `includes`

```1:9:web/src/permissions.ts
export const PERMISSIONS = {
  charactersCreate: 'characters.create',
  charactersUpdate: 'characters.update',
  charactersDelete: 'characters.delete',
} as const

export function hasPermission(permissions: string[], code: string): boolean {
  return permissions.includes(code)
}
```

`as const` değerleri literal string kilitler; yanlışlıkla `PERMISSIONS.charactersCreate = 'x'` derleme hatası. Kodlar `PermissionCodes` ile aynı (bölüm 25). `hasPermission` React hook **değil** — düz fonksiyon, C# `codes.Contains("characters.create")`. Dizi + kod → true/false. “Cüzdan nerede?” ayrı iş (`usePermissions`, bölüm 30).

```95:97:web/src/CharactersPage.tsx
          {hasPermission(permissions, PERMISSIONS.charactersCreate) && (
            <Link to="/characters/new">Karakter ekle</Link>
          )}
```

`&&` soldaki koşul, sağdaki Link değil (bölüm 17). False ise React hiçbir şey çizmez. Razor `@if (hasCreate) { <a>…</a> }`. Üç fiil ayrı: robin’de yalnız create varsa Ekle görünür, Düzenle/Sil görünmez.

```148:156:web/src/CharacterDetailPage.tsx
          {hasPermission(permissions, PERMISSIONS.charactersUpdate) && (
            <Link to={`/characters/${id}/edit`}>Düzenle</Link>
          )}
          {hasPermission(permissions, PERMISSIONS.charactersDelete) && (
            <button type="button" onClick={handleDelete}>
              Sil
            </button>
          )}
          <Link to="/characters">Listeye dön</Link>
```

24 Ağustos’ta detay kendi `load`’unda `/me` atmazsa `permissions` `[]` kalır — **zoro dahil** butonlar gizlenir. Liste `setPermissions` şarttı. Bugün ikisi de Context; unutulan sayfa `/me` tuzağı bölüm 31’de kapanır.

```74:78:web/src/CharactersPage.tsx
      // const meResponse = await apiFetch('/api/auth/me')
      // if(meResponse.ok){
      //   const me = await meResponse.json()
      //   setPermissions(me.permissions ?? [])
      // }  
```

24 Ağustos’ta bu yorum **çalışan** koddu. `?? []`: `permissions` yoksa boş dizi, `.includes` patlamasın.

#### UI gizleme ≠ yetki

Link yok diye Player `/characters/new` yazamaz sanmak yanlış. Adres durur; sayfa açılır. Asıl kapı `POST /api/characters` + `[HasPermission]` (bölüm 26) — 403. Gizleme UX: yetkisiz kişi formu doldurmasın. ASP.NET’te butonu Razor’da gizlesen action attribute durur. 25 Ağustos’ta URL’ye ikinci kapı eklendi (bölüm 29).

#### Bu kodu kim tetikliyor?

Liste mount → (o gün) GetPaged + `/me` → `permissions` → `&&` Link. Detay kendi GET + (o gün kendi) `/me`. Backend yeni değil.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `me.permission` (tekil) — API `permissions`; `undefined ?? []` → herkes yetkisiz. Detayda `/me` unutmak. Link gizleyince API’nin kapandığını sanmak. `hasPermission`’ı hook sanmak.

#### Sonuçta ne kazandık

Player’da Ekle yok, Admin’de var; POST hâlâ 403 ile duruyor. URL kapısı ve `meLoaded` ertesi gün (bölüm 29).

---

### 29. 25–26 Ağustos — Create/Edit sayfa kapıları, `meLoaded`, sonsuz Yükleniyor, Guid URL

**Commit’ler:** `32723d4` (25 Ağustos, Create), `92cd0d9` (26 Ağustos, Edit kapısı; aynı commit’te Context de var — o bölüm 30).

Link gizlenince `/characters/new` hâlâ `CharacterCreatePage` açıyordu. `AppLayout` yalnız token’a bakıyordu (kimsin). İkinci kapı **fiil**: create yoksa forma girme. `Navigate replace` geçmişe `new` yazmaz. Güvenlik hâlâ POST; kapı UX. Edit aynı iskelet, `characters.update`. Bugün `meLoaded` Create’de yok — layout’ta (bölüm 30); yorum satırları 25 Ağustos’u anlatır.

#### Neden `/me` bitmeden atılmaz?

`useState<string[]>([])` ilk anda boş. Gelmeden `hasPermission` false → Admin bir kare yetkisiz görünür, `Navigate` ile listeye düşer. C# `await GetCodesAsync` bitmeden 403 basmaz. React: `meLoaded === false` iken form yok, Navigate yok; “Yükleniyor…”. Geldikten sonra fiil yoksa `Navigate to="/characters"`.

```139:149:web/src/CharacterCreatePage.tsx
   if (!token) {
    return <Navigate to="/login" replace />
  }

  // if (!meLoaded){
  //   return <p>Yükleniyor...</p>
  // }  bunları kaldırdık çünkü artık ortak permission ekledik

  if (!hasPermission(permissions, PERMISSIONS.charactersCreate)){
    return <Navigate to="/characters" replace />
  }
```

25 Ağustos’ta `meLoaded` yorum değil, canlıydı; `permissions` sayfa `useState`’iydi; `loadPreview` hem liste hem `/me` atıyordu, `setMeLoaded(true)` try/catch **dışında** (hata olsa da bitsin). Bugün `usePermissions()`; layout `/me` bitmeden Outlet yok.

#### Hook sırası — sonsuz Yükleniyor

`if (!meLoaded) return <p>Yükleniyor` `useEffect`’ten **önce** durursa: ilk çizimde `meLoaded` zaten false → effect hiç kayıt olmaz → `loadPreview` çalışmaz → `setMeLoaded(true)` hiç gelmez → sonsuz Yükleniyor. Token `if`’i eskiden de effect üstündeydi; token varsa geçiliyordu. `meLoaded` herkese false başladığı için bu sefer **herkesi** kesti.

Doğru sıra: state → `loadPreview` tanımı → `useEffect` → `handleCreate` → üç `if` (token, meLoaded, hasPermission) → form. Hook’lar bitti, **sonra** kapı. Razor’da `OnGet` bitmeden `Redirect` yok.

İkinci yazım: `if (!response.ok) return` preview 500’de fonksiyonu keser; alttaki `setMeLoaded(true)` çalışmaz. Yetki listesine bağlı olmamalı; preview hata olsa da `/me` devam. 25 Ağustos düzeltmesi: `!ok` dalında return yok, `else` ile `setItems`.

`me.permission` (tekil) Create’de yazılırsa `?? []` → zoro da listeye atılır. Liste/detay çoğul `permissions` kullanıyordu.

#### Edit — `loading` yeter, ikinci `meLoaded` yok

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

26 Ağustos’ta Edit karakter GET için zaten `loading` tutuyordu. `/me` `load()` içinde, karakter `apiFetch`’inden **önce** (404 `return` `/me`’yi atlamasın). `setLoading(false)` `finally`. Kapı `useEffect` ve `handleSubmit` **sonra**. Player’da update yok: adres çubuğundan `.../edit` → liste. Create’de kalması çelişki değil — testte Player’a `characters.create` elle verilmişti, `update` yok.

Bugün Edit de Context; yorumdaki `/me` o günkü yer.

#### Guid URL

`:id` Guid. `/characters/Franky/edit` → `GET /api/characters/Franky` → ASP.NET `{id:guid}` eşleşmez / 404 → “Karakter bulunamadı”. Update’i olan kullanıcı kapıdan geçer, 404 görür. Form için karttaki Düzenle linki Guid yazar (`/characters/44AEDA65-…/edit`).

Katalog vs takım: `POST /api/characters` katalog kartı üretmek; her Player’ın takıma karakter alması ayrı fiil (yok). New kapısı katalog yazma.

#### Bu kodu kim tetikliyor?

`/characters/new` → (o gün sayfa `/me`) → create yoksa Navigate, varsa form → POST hâlâ 403/201. `/characters/:id/edit` → GET detay + (o gün) `/me` → update yoksa liste. Backend attribute değişmedi.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `meLoaded` `if`’ini `useEffect` üstüne koymak. Preview `!ok`’da `return`. `permission` tekil. Link gizleyince URL’nin kapanacağını sanmak. `/characters/Franky/edit`. Dört sayfanın ayrı `/me`’si — aynı gün Context’e geçiş başladı, liste bağlandı; Create/Edit/Detail bölüm 31.

#### Sonuçta ne kazandık

Yetkisiz form açılmaz; hook sırası ve Guid net. Tek `/me` kutusu bölüm 30.

---

### 30. 26 Ağustos — `PermissionContext`, `Provider`, `usePermissions`

**Commit:** `92cd0d9` (26 Ağustos; aynı commit Edit kapısını da taşıdı — kapı bölüm 29, bu bölüm kutu).

Dört sayfa ayrı `GET /api/auth/me` = aynı join dört kez. Nested route’ta `AppLayout` kalır, `Outlet` değişir: `/me`’yi layout’ta bir kez at, çocuklar diziyi okusun. Bu adımda önce `PermissionContext.tsx` (`createContext`, `usePermissions`). Sonra `AppLayout` `permissions` + `meLoaded`, effect’te `/me`, `setMeLoaded(true)` try/catch dışı, hook’lar `if (!token)` **üstünde**. Provider `Outlet`’i sarar; `/me` bitmeden Outlet yok. Liste `usePermissions`’a geçti; Create/Edit/Detail hâlâ kendi `/me` (bölüm 31). Layout `hasPermission` import etmez — cüzdanı doldurur, kapı sayfada kalır.

#### Context — görünmeyen kutu

```1:17:web/src/PermissionContext.tsx
import { createContext, useContext } from 'react'

type PermissionContextValue = {
    permissions: string[]
}

const PermissionContext = createContext<PermissionContextValue | null>(null)

export function usePermissions(): string[] {
    const ctx = useContext(PermissionContext)
    if(!ctx){
        throw new Error('usePermissions yalnızca AppLayout içinde')
    }
    return ctx.permissions
}

export { PermissionContext }
```

Tek benzetme: `permissions` dizisi bir **cüzdan**. Provider cüzdanı ağaca asar; altındaki sayfalar `usePermissions` ile aynı cüzdanı okur, prop deliğiyle taşımaz. `createContext(null)` boş kanca: Login/Register `AppLayout` dışında, Provider yok. `usePermissions` Login’de çağrılırsa throw — sessiz `[]` herkesi yetkisiz gösterir, hatayı gizler.

`usePermissions` “şu fiil var mı?” demez; yalnız diziyi verir. `hasPermission(permissions, PERMISSIONS.charactersCreate)` ayrı düz fonksiyon (bölüm 28). C# kabaca: istekte bir kez `GetCodesAsync`, sonucu `HttpContext.Items`’a koy, action’lar oradan okusun. Hâlâ DB; JWT’ye permission yazılmıyor.

#### Layout doldurur, Outlet okur

```14:45:web/src/AppLayout.tsx
  const [permissions, setPermissions] = useState<string[]>([])
  const [meLoaded, setMeLoaded] = useState(false)

  useEffect(() => {
  if (!token) {
    return
  }

  async function loadMe() {
    try {
      const meResponse = await apiFetch('/api/auth/me')
      if (meResponse.ok) {
        const me = await meResponse.json()
        setPermissions(me.permissions ?? [])
      }
    } catch {
      // /me gelmese de meLoaded bitsin; yoksa sonsuz Yükleniyor
    }
    setMeLoaded(true)
  }

  loadMe()
}, [token])
  

  if (!token) {
    return <Navigate to="/login" replace />
  }

  if (!meLoaded) {
  return <p>Yükleniyor…</p>
}
```

Hook’lar erken `return`’den önce (bölüm 29 tuzağı). Token yoksa effect `/me` atmaz. `meLoaded` false iken Provider/Outlet yok: çocuk boş diziyle “yetkin yok” deyip `Navigate` etmesin. `?? []` ve catch sonrası `setMeLoaded(true)` 25 Ağustos dersi.

```57:81:web/src/AppLayout.tsx
  return (
    <PermissionContext.Provider value={{ permissions }}>
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
              <button
                type="button"
                className="app-header__logout"
                onClick={handleLogout}
              >
                Çıkış
              </button>
          </header>
        <main className="app-main">
          <Outlet />
        </main>
    </div>
    </PermissionContext.Provider>
```

`value={{ permissions }}` her `setPermissions`’ta yeni obje; alt `usePermissions` güncellenir. Listeye dönüşte layout unmount olmaz → `/me` tekrar atılmaz. Doğru.

```18:19:web/src/CharactersPage.tsx
function CharactersPage() {
  const permissions = usePermissions()
```

26 Ağustos kanıtı: Sanji’de Ekle, Network’te listenin kendi `/me`’si yok, yalnız layout’unki. Create’e basınca o gün hâlâ sayfa `/me`’si — üç sayfayı aynı anda taşımak hangi 403’ün kimin isteği karışırdı.

```7:12:web/src/main.tsx
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>,
)
```

İlk açılışta 2 `/me`: StrictMode development’ta effect’i iki kez (bölüm 19). Production’da tek. Üçüncü satır HMR / eski Network. StrictMode kapatılmaz.

#### Bu kodu kim tetikliyor?

`/characters` → `AppLayout` mount → `GET /api/auth/me` (bölüm 27) → Provider → `CharactersPage` `usePermissions` → `hasPermission && Link`. Backend join aynı.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: Login’de `usePermissions`. Provider’ı `Outlet`’in **içine** koymak — çocuk context görmez. `meLoaded` bitmeden Outlet. Context’i `[]` default ile yaratıp throw’u kaldırmak — Login sessiz yetkisiz. Layout’ta `hasPermission` ile link gizleyip sayfanın cüzdanı boş sanması.

#### Sonuçta ne kazandık

Liste tek `/me` okuyor. Create/Edit/Detail ertesi gün bağlandı (bölüm 31).

---

### 31. 27 Ağustos — Create / Edit / Detail Context’e geçti, tek `/me`

**Commit:** `a839ea9` (27 Ağustos).

Layout cüzdanı veriyordu; üç sayfa hâlâ kendi `/me`’sini atıyordu. Bu adımda `CharacterCreatePage`, `CharacterEditPage`, `CharacterDetailPage` `usePermissions()` aldı, sayfa `/me` + `meLoaded` / `setPermissions` silindi (yorumda duruyor). Kapı `if`’leri aynı (`hasPermission` + `Navigate`). Network’te korumalı gezinti: bir layout `/me` (+ StrictMode’da ikinci), sayfa `characters` GET/PUT/DELETE — ikinci `/me` yok.

```19:21:web/src/CharacterCreatePage.tsx
  const navigate = useNavigate()
  const token = getToken()
  const permissions = usePermissions()
```

```12:13:web/src/CharacterEditPage.tsx
  const token = getToken()
  const permissions = usePermissions()
```

```26:27:web/src/CharacterDetailPage.tsx
  const token = getToken()
  const permissions = usePermissions()
```

`usePermissions` hook olduğu için koşulsuz, fonksiyon gövdesinin üstünde — `if (!token) return` **önce**. Aksi halde React hook sırası bozulur (bölüm 29). Dizi layout’tan; `hasPermission(..., PERMISSIONS.charactersCreate)` Create’de, `charactersUpdate` Edit’te, update/delete Detay’da aynı kaldı.

Yoruma alınan `/me` blokları “o gün sayfa atıyordu” belgesi; tekrar açmak çift istek üretir. `meLoaded` Create’de yok: layout Outlet’i geciktiriyor.

Liste → new → detay → edit → liste: `AppLayout` ayakta, cüzdan aynı, SSMS’te `RolePermissions` değişince **yeni** `/me` yok (layout unmount olmadı). Anında kes/ver için sayfa yenile veya token değişince effect (`[token]`). Refresh token ayrı kapı (Blok E); permission “ne yapabilirim”, refresh “oturum ne kadar açık”.

#### Bu kodu kim tetikliyor?

Aynı `/me` + aynı `HasPermission` POST/PUT/DELETE. Değişen 5173’ün kaç kez join sorması. Backend 27 Ağustos’ta yeni endpoint yok.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: üç sayfadan birinde `/me`’yi unutup silmemek — Network’te fazladan `me`. `usePermissions`’ı `if (!token)` altına almak. Context’i prop ile tekrar geçirmek. Logout `clearToken` + `/login` layout’u unmount eder; tekrar girişte yeni `/me` — doğru.

#### Sonuçta ne kazandık

Korumalı ağaçta tek cüzdan, tek `/me`. Blok D bitti. 28 Ağustos’ta `RefreshToken` tablosu (henüz login yazmıyor) — bölüm 32.

---

## Blok E — Refresh token (28 Ağustos – 8 Eylül)

Permission “ne yapabilirim” (DB join, her istek, JWT’ye gömülmez). Access JWT “kimsin, kısa”. Arena’da 60 dakikada 401 + tekrar şifre rahatsız. Bu blok oturumu **şifresiz uzatır**; yetki modeline dokunmaz. Frontend Context durur. 28 Ağustos’ta yalnız tablo; 29 Ağustos’ta login hash + ham JSON; 8 Eylül’de `POST /api/auth/refresh` + `api.ts` 401 (bölüm 34).

---

### 32. 28 Ağustos — `RefreshToken` entity, EF, `AddRefreshTokens` (tablo boş)

**Commit:** `661d306` (28 Ağustos).

Access JWT zaten vardı (bölüm 8): login bir imzalı string üretir, tabloda satır yok, `ExpireMinutes` 60, bitince 401. Permission ayrı kapı (Blok C–D). Arena için 60 dk kısa geldi; yeni access’i şifresiz basmak için **ikinci bir sır** lazım ve o sır DB’de ham durmamalı. Bu adımda önce Domain `RefreshToken` (`User` kalıbı: private ctor, `Create`, `Revoke`). Sonra `RefreshTokenConfiguration` (tablo adı, `TokenHash` 64 unique, User `Cascade`). `IApplicationDbContext` + `ApplicationDbContext` `DbSet`. En sonda migration `AddRefreshTokens` + `database update`. Login, generator, React yok — SSMS’te tablo **boş**.

Tek benzetme: iki **saat**. Access JWT kısa saat (sunucu onu kaydetmez, süre token’ın içinde). Refresh uzun saat (satır DB’de; ham metin yok, hash var). İkisini tek JWT claim’ine sıkıştırmak “yetki + oturum aynı kapı”ya döner; o yüzden ayrı tablo.

#### Domain — satır, ham token değil

```1:43:ReactBattleArena/ReactBattleArena.Domain/Authentication/RefreshToken.cs
namespace ReactBattleArena.Domain.Authentication;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

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

    public void Revoke(DateTime utcNow)
    {
        if (RevokedAtUtc is not null)
            return;

        RevokedAtUtc = utcNow;
    }
}
```

`User` ile aynı iskelet (bölüm 6): EF’nin boş ctor’u, `private set`, factory `Create`. Klasör `Domain/Authentication/` — `Authorization` (Role/Permission) değil; bu satır “oturum uzatma sırrı”, fiil listesi değil.

`TokenHash` adında **Hash** var: Create ham string almaz. Şifre gibi (bölüm 7): sızıntıda ham refresh = yeni access basma hakkı. `PasswordHash` BCrypt’tir; bu hash’in algoritması 29 Ağustos’ta SHA256 (bölüm 33). 28 Ağustos’ta yalnız kolon.

Kendi `Id`: bir kullanıcının zaman içinde **çok** satırı olur (yeniden login, ileride cihaz). `UserRole` çift PK `(UserId, RoleId)` idi — üyelik tektir. Refresh üyelik değil, oturum fişi.

`Revoke` satırı silmez, `RevokedAtUtc` yazar. Çıkış / rotation (bölüm 34) eski fişi öldürür; `is not null` ikinci kez çağrıyı no-op yapar. 28 Ağustos’ta kimse `Revoke` çağırmaz.

`ExpiresAtUtc` uzun saatin bitişi; `CreatedAtUtc` ne zaman basıldığı. JWT `exp` claim’i tabloda yok — access zaten token’ın kendi içinde ölür.

#### EF — `Persistence`, unique 64, Cascade

```1:24:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/RefreshTokenConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactBattleArena.Domain.Authentication;
using ReactBattleArena.Domain.Users;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.TokenHash).IsUnique();

        builder.HasIndex(x => x.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

`ApplyConfigurationsFromAssembly` (bölüm 1) bu sınıfı kendiliğinden alır; `OnModelCreating`’e tek tek yazılmaz. Namespace `Persistence` — `Persistance` yazımı derlenmez, `dotnet ef` de bulamaz.

`HasMaxLength(64)` SHA256 hex (32 byte → 64 karakter). Unique: aynı hash iki satır olmasın; 29 Ağustos’ta “bu ham token’ın satırı hangisi?” araması bu index’ten gidecek. `UserId` index: “bu kullanıcının fişleri” (ileride hepsini kapat).

`HasOne<User>().WithMany()` — `User` üzerinde `ICollection<RefreshToken>` yok (Role’de de collection yazmamıştık). FK yine durur. `Cascade`: kullanıcı silinince fişleri de silinir; yetim hash kalmaz.

`OnDelete(...)` satırının sonundaki noktalı virgül şart. Unutulursa CS1002, migration da üretilmez.

```8:18:ReactBattleArena/ReactBattleArena.Application/Abstractions/IApplicationDbContext.cs
public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

```24:24:ReactBattleArena/ReactBattleArena.Infrastructure/Persistence/ApplicationDbContext.cs
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

Handler Infrastructure class’ını görmez (bölüm 1). 20 Ağustos’ta bu `DbSet` yoktu; 28 Ağustos’ta eklendi. Login handler ertesi gün `Add` edecek (bölüm 33); o gün kimse set’i kullanmıyordu.

#### Migration — şema var, satır yok

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

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
```

`InitialCreate`’e kolon yapıştırmadık (bölüm 1 kuralı). `Down` tabloyu düşürür. `*.Designer.cs` ve snapshot otomatik; onlardan alıntı yok. `dotnet ef database update` sonrası SSMS’te `RefreshTokens` görünür, **sıfır satır** — login henüz `Add` etmiyor.

`dotnet-ef` araç sürümü runtime’dan eski olabilir (ör. 10.0.5 vs 10.0.9): uyarı, başarısızlık değil. `dotnet tool update --global dotnet-ef` isteğe bağlı. Bu uyarı yüzünden JwtBearer paketini güncelleme.

#### Bu kodu kim tetikliyor?

Uygulama içinden **kimse**. `POST /api/auth/login` hâlâ yalnız access JWT basar (bölüm 8); 5173 aynı. Tetikleyen `dotnet ef migrations add AddRefreshTokens` ve `database update`. Frontend ertesi gün `refreshToken` alanını okuyacak (bölüm 33). `POST /api/auth/refresh` yok — o bölüm 34, kod yazılınca.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: namespace `Persistance`. `OnDelete` noktalı virgül unutmak. Refresh’i `Users`’a kolon yapmak (bir kişi bir fiş; cihaz/yeniden login kırılır). `UserRole` gibi composite PK. Ham token’ı `nvarchar` saklamak. `InitialCreate`’i düzenlemek. EF araç uyarısını paket güncellemesi sanmak. Tabloyu görüp “login artık yeniliyor” sanmak — satır yok.

#### Sonuçta ne kazandık

Uzun saat için tablo ve hash kolonu var; kısa saat hâlâ JWT. Login ertesi gün yazdı (bölüm 33).

---

### 33. 29 Ağustos — generator, login hash DB / ham JSON, `localStorage`

**Commit:** `e45172e` (29 Ağustos).

Tablo boş duruyordu; login hâlâ yalnız access basıyordu. Bu adımda önce `JwtOptions.RefreshExpireDays` (varsayılan 7; `appsettings` `Jwt` bölümünde aynı isim — Key satırını notlara kopyalamıyorum). Sonra `IRefreshTokenGenerator` + `RefreshTokenGenerator` (rastgele ham, SHA256 hex, süre) ve DI `AddSingleton`. `LoginResult`’a `RefreshToken`; handler hash’i `RefreshTokens`’a yazar, hamı JSON’a koyar. Controller yeni action yok — 200 body’si bir alan şişer. `api.ts` `getRefreshToken` / `setRefreshToken`, `clearToken` ikisini siler. `LoginPage` `data.refreshToken` kaydeder. `POST /api/auth/refresh` ve 401’de sessiz yenileme **yok**; tarayıcı ikinci anahtarı tutar, henüz kullanmaz.

#### Süre config’de, üretim Infrastructure’da

```13:14:ReactBattleArena/ReactBattleArena.Infrastructure/Security/JwtOptions.cs
    public int ExpireMinutes { get; set; } = 60;
    public int RefreshExpireDays { get; set; } = 7;
```

22 Temmuz’da yalnız `ExpireMinutes` vardı. Kısa saat dakika, uzun saat gün — aynı `Jwt` section, iki sayı. `Configure<JwtOptions>` zaten duruyordu; yeni property bind olur.

```1:7:ReactBattleArena/ReactBattleArena.Application/Abstractions/IRefreshTokenGenerator.cs
namespace ReactBattleArena.Abstractions;

public interface IRefreshTokenGenerator
{
    (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow);
    // Yukarıdaki Create üç değeri birden döndürüyor. Buna tuple (demet) denir.
}
```

Dosya Application katmanında (handler Infrastructure class’ını görmesin). Namespace `ReactBattleArena.Abstractions` — `IApplicationDbContext` ise `ReactBattleArena.Application.Abstractions`; tutarsız ama gerçek. Dönüş **tuple**: üç `out` parametresi veya küçük DTO yerine bir `Create` üç değer.

```9:28:ReactBattleArena/ReactBattleArena.Infrastructure/Security/RefreshTokenGenerator.cs
public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly JwtOptions _options;

    public RefreshTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var expires = utcNow.AddDays(_options.RefreshExpireDays);
        return (raw, hash, expires);
        // Şifre hasher’ını (BCrypt) kullanma. BCrypt her seferinde farklı tuz üretir; TokenHash unique index ile arama bozulur. 
    }

}
```

32 rastgele byte → Base64 **ham** (cevapta bir kez). SHA256(UTF8(ham)) → hex; `Convert.ToHexString` 64 karakter, kolon `nvarchar(64)` ile örtüşür. `expires` `RefreshExpireDays` kadar ilerisi.

BCrypt **bilinçli yok**. Parolada amaç “aynı şifreyi doğrula, hash’i arama anahtarı yapma”: her `Hash` farklı tuz, `Verify` yeterli. Refresh’te amaç “istemcinin gönderdiği hamı SHA256’le, unique index’ten satırı bul”. BCrypt her seferinde başka string üretir; `WHERE TokenHash = @x` tutmaz. Paralel: parola BCrypt (bölüm 7), fiş SHA256.

```27:29:ReactBattleArena/ReactBattleArena.Infrastructure/DependencyInjection.cs
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
```

22 Ağustos’ta bu satır yoktu. Singleton: istek state’i yok, hasher / JWT servisi gibi. Scoped da çalışırdı; mevcut güvenlik servisleriyle aynı ömür seçildi.

#### Login artık satır yazar

```8:13:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LoginCommand.cs
public sealed record LoginResult(
    Guid UserId,
    string UserName,
    string Email,
    string Token,
    string RefreshToken);
```

22 Temmuz’da dördüncü alan `Token` ile bitiyordu. ASP.NET JSON camelCase: `refreshToken`. Controller imzası aynı `Ok(result)` — yeni endpoint değil, body’se bir property.

```41:51:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LoginCommandHandler.cs
        var token = _jwtTokenService.CreateToken(user);

        var utcNow = DateTime.UtcNow;
        var (rawRefresh, hash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(
            RefreshToken.Create(user.Id, hash, expires, utcNow));

        await _db.SaveChangesAsync(cancellationToken);


        return new LoginResult(user.Id, user.UserName, user.Email, token, rawRefresh);
```

22 Temmuz’da `CreateToken` sonrası `return new LoginResult(..., token)` — **SaveChanges yoktu**, JWT tabloda durmaz. Bugün tek `SaveChanges` refresh satırını basar; access hâlâ yalnızca cevapta.

Tuple açılımı: `rawRefresh` JSON, `hash` kolon. SSMS’te `TokenHash` ile F12 `refreshToken` **aynı string değildir**. Aynı olması = hamı DB’ye yazmışsın.

`Revoke` hâlâ çağrılmaz. Her login **yeni** satır; eskiler `RevokedAtUtc` null kalır (rotation bölüm 34). Kullanıcı yok / şifre yanlış yine `null` → 401, satır yok.

```46:60:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
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

Action 22 Temmuz’dan beri bu. 200’de artık `refreshToken` de var. `POST /api/auth/refresh` yok.

#### React — ikinci anahtar, henüz kullanılmıyor

```3:21:web/src/api.ts
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

11 Ağustos’ta `clearToken` yalnız `token` siliyordu. Anahtar adı JSON’daki `refreshToken` (camelCase). `apiFetch` 401 görünce **login’e atmıyor**, yenileme de yok; sayfa `ok` bakar. `getRefreshToken` bu commit’te kayıt için durur.

```54:57:web/src/LoginPage.tsx
      const data = await response.json()
      setToken(data.token)
      setRefreshToken(data.refreshToken)
      navigate('/characters')
```

30 Temmuz / 11 Ağustos’ta yalnız `setToken`. `auth: false` aynı (login’de Bearer yok). Çıkış `AppLayout` `clearToken()` — ikisi birden gider; yalnız `token` silinirse fiş localStorage’da kalır, 34 gelince yanlışlıkla kullanılır.

#### Bu kodu kim tetikliyor?

`LoginPage` → `POST /api/auth/login` → handler JWT + refresh satırı + 200 `{ token, refreshToken, ... }`. Network’te access 60 dk, refresh 7 gün ayrı. Karakter GET hâlâ Bearer access; 401 olursa bugün sessiz yenileme yok, kullanıcı tekrar login. 403 permission’dır, refresh 403’ü düzeltmez.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: refresh hash’ini BCrypt yapmak. Hamı `TokenHash` kolonuna yazmak (SSMS = localStorage). Refresh’i JWT claim’ine gömmek. `data.RefreshToken` (Pascal) — JSON camelCase, `undefined` → `setItem` `"undefined"` string. `clearToken`’dan `refreshToken`’ı unutmak. 403’te yenileme beklemek. `apiFetch`’in 401’de zaten yenilediğini sanmak.

#### Sonuçta ne kazandık

Login fişi basıyor: hash DB, ham tarayıcı. Kullanılacak yer `POST /api/auth/refresh` + `api.ts` 401 (bölüm 34).

---

### 34. 8 Eylül — `POST /api/auth/refresh` + `api.ts` 401’de sessiz yenileme

**Hedef not:** Bu kodu sen VS Code’da sırayla yazacaksın; working tree’de `POST /api/auth/refresh` henüz yok. 29 Ağustos’ta fiş basılıyordu, **kullanılmıyordu**. Aşağıdaki bloklar yazınca duracağın hâl.

Access 60 dk bitince karakter GET 401, kullanıcı şifreyi yeniden yazıyordu. `localStorage`’daki `refreshToken` duruyordu. Bu adımda önce `IRefreshTokenGenerator.Hash` — login ve refresh aynı SHA256, BCrypt değil. Sonra `RefreshCommand` + validator + handler: hamı hash’le, satırı bul, iptal/süre dolmuşsa `null` (401), `Revoke` + yeni çift (**rotation**). `RefreshRequest` + `AuthController` `POST refresh`, `[AllowAnonymous]`, cevap yine `LoginResult`. En sonda `api.ts`: 401’de `refreshSession`, yeni Bearer ile **bir kez** tekrar; refresh’in kendisi `fetch` (döngü yok). Sayfalar değişmedi — `apiFetch` içeride halleder.

Tek benzetme: **tek gişe**. İki istek aynı anda 401 olursa ikisi de aynı fişi harcamasın diye `refreshInFlight` tek Promise; ikinci gişeye gitmez, birincinin sonucunu bekler. ASP.NET’te buna yakın şey bir lock / tek seferlik rotate; tarayıcıdaki paralel `fetch`’ler ayrı `HttpContext`, o yüzden Promise modül kapsamında.

#### Aynı hash, ayrı metot

```1:10:ReactBattleArena/ReactBattleArena.Application/Abstractions/IRefreshTokenGenerator.cs
namespace ReactBattleArena.Abstractions;

public interface IRefreshTokenGenerator
{
    (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow);
    // Yukarıdaki Create üç değeri birden döndürüyor. Buna tuple (demet) denir.

    string Hash(string raw);
    // Login ve refresh aynı SHA256’yi kullansın. BCrypt değil — tuz her seferinde değişir, unique index araması bozulur.
}
```

29 Ağustos’ta `Hash` yoktu; `Create` içinde inline SHA256 vardı. Bugün `Create` `Hash(raw)` çağırır — formül kaymasın.

```17:28:ReactBattleArena/ReactBattleArena.Infrastructure/Security/RefreshTokenGenerator.cs
    public string Hash(string raw)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    public (string Raw, string Hash, DateTime ExpiresAtUtc) Create(DateTime utcNow)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes);
        var hash = Hash(raw);
        var expires = utcNow.AddDays(_options.RefreshExpireDays);
        return (raw, hash, expires);
```

İstemci hamı gönderir; sunucu `Hash` ile `TokenHash` unique index’ten bakar. BCrypt `Verify` burada yok: amaç “bu string’in satırı hangisi?”, “şifre doğru mu?” değil.

#### Rotation — eski fiş ölür, yeni çift doğar

```1:6:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommand.cs
using MediatR;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed record RefreshCommand(string RefreshToken) : IRequest<LoginResult?>;
//null → fiş yok / iptal / süresi bitmiş → controller 401.
```

Login ile aynı `LoginResult?`: yeni `token` + yeni `refreshToken`. Şifre yok. Boş gövde validator’da 400 (`NotEmpty`), 401 değil.

```5:11:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandValidator.cs
public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}
```

`AddValidatorsFromAssembly` bu sınıfı alır; `Program.cs`’e yazılmaz (bölüm 2).

```25:58:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandHandler.cs
    public async Task<LoginResult?> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var hash = _refreshTokens.Hash(request.RefreshToken);

        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null)
            return null;

        if (existing.RevokedAtUtc is not null)
            return null;

        if (existing.ExpiresAtUtc <= utcNow)
            return null;

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken);

        if (user is null)
            return null;

        existing.Revoke(utcNow);

        var (rawRefresh, newHash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(
            RefreshToken.Create(user.Id, newHash, expires, utcNow));

        await _db.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.CreateToken(user);
        return new LoginResult(user.Id, user.UserName, user.Email, token, rawRefresh);
    }
```

Yok / iptal / süresi dolmuş / kullanıcı yok → aynı `null` → 401. “Bu fiş iptal edilmiş” sızmaz (login’deki email sızdırmazlık, bölüm 8).

`Revoke` 28 Ağustos’ta duruyordu, ilk kez burada çağrılır. Eski satır silinmez; `RevokedAtUtc` dolar. Yeni satır yeni hash. İkinci kez aynı ham gelirse `RevokedAtUtc is not null` → 401. Çalınan fiş bir kez işe yarar; asıl tarayıcı bir sonraki 401’de düşer.

JWT yine tabloda yok; `CreateToken` access’i imzalar. Permission hâlâ `/me` join — refresh yetki listesini JWT’ye yazmaz.

```1:6:ReactBattleArena/ReactBattleArena.Api/Contracts/RefreshRequest.cs
namespace ReactBattleArena.Api.Contracts;

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
```

```62:76:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResult>> Refresh(
        [FromBody] RefreshRequest body,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RefreshCommand(body.RefreshToken),
            cancellationToken);

        return result is null ? Unauthorized() : Ok(result);
    }
```

`[AllowAnonymous]`: access zaten ölmüştür; Bearer şartı 401 döngüsü olur. Login gibi 200 / 401 / 400. Yeni `[HasPermission]` yok — bu oturum kapısı, fiil kapısı değil.

#### `apiFetch` 401 görünce gişeye gider

```30:67:web/src/api.ts
let refreshInFlight: Promise<boolean> | null = null

async function refreshSession(): Promise<boolean> {
  if (refreshInFlight) {
    return refreshInFlight
  }

  refreshInFlight = (async () => {
    const refreshToken = getRefreshToken()
    if (!refreshToken) {
      return false
    }

    const response = await fetch(`${API_BASE}/api/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    })

    if (!response.ok) {
      clearToken()
      return false
    }

    const data = await response.json()
    setToken(data.token)
    setRefreshToken(data.refreshToken)
    return true
  })()

  try {
    return await refreshInFlight
  } finally {
    refreshInFlight = null
  }
}
```

`refreshInFlight` doluysa ikinci 401 aynı Promise’i bekler — tek gişe. Refresh **`apiFetch` değil `fetch`**: `apiFetch` 401’de yine `refreshSession` çağırırdı; gişe kendi kuyruğuna girer, kilitlenirdi.

Başarısızda `clearToken` — hem access hem fiş. `data.refreshToken` yeni ham; eski localStorage değeri çöp (sunucu revoke etti).

```88:113:web/src/api.ts
  const response = await fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (response.status !== 401 || auth === false) {
    return response
  }

  const refreshed = await refreshSession()
  if (!refreshed) {
    return response
  }

  const retryHeaders: Record<string, string> = { ...headers }
  const newToken = getToken()
  if (newToken) {
    retryHeaders.Authorization = `Bearer ${newToken}`
  }

  return fetch(`${API_BASE}${path}`, {
    method,
    headers: retryHeaders,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })
```

11 Ağustos’ta `apiFetch` 401’i olduğu gibi dönerdi; sayfa `ok` bakardı. Bugün `auth: true` ve 401 ise bir kez yenile + tekrar. Login/register `auth: false` — yanlış şifre 401’de refresh denemez (fiş yok / anlamsız). **403 dokunulmaz**: yetki yok, süre dolmamış; refresh 403’ü 201 yapmaz.

Tekrar istek **yeni** `Authorization`. Eski header’daki ölü JWT kalırsa ikinci 401. Body aynı JSON — PUT yarım kalmaz.

Sayfa kodu (`CharactersPage`, `/me` layout) değişmedi. StrictMode çift `/me` hâlâ iki GET; ikisi 401 olursa tek refresh.

#### Bu kodu kim tetikliyor?

`GET /api/characters` (veya `/me`) + ölü access → JwtBearer 401 → `refreshSession` → `POST /api/auth/refresh` `{ refreshToken }` → handler hash + rotation → 200 yeni çift → aynı path ikinci kez Bearer yeni. 5173’te kullanıcı form görmez. Fiş 7 gün de dolduysa refresh 401, `clearToken`, sayfa gerçek 401 görür (Detay’da “Oturum yok”).

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: refresh’i `apiFetch` ile atmak (döngü). 403’te yenilemek. Login 401’de yenilemek. Rotation’suz eski fişi canlı bırakmak (iki geçerli ham). BCrypt ile aramak. Refresh’e `Authorization: Bearer` koymak. `refreshInFlight` olmadan paralel 401 — ikinci istek revoke edilmiş fişi yollar, bir istek düşer. Yeni `refreshToken`’ı `setRefreshToken` etmemek — bir sonraki 401 eski hamı yollar.

#### Sonuçta ne kazandık

Access bitince şifresiz yeni çift; permission hâlâ DB. Blok E ve V2’nin 34 bölümü kapandı.

---

### 35. 16 Eylül — `POST /api/auth/logout` + `AppLayout`’ta 401 kapısı

**Yazım notu:** Bu bölümden itibaren benzetmeye takma ad verilmiyor. Önceki
bölümlerde “fiş” diye geçen şey **refresh token**, “çanta” diye geçen şey
**rol**, “kâğıt” diye geçen şey **permission dizisi**. Bundan sonra terimler
doğrudan yazılıyor.

#### Neden bu adım geldi

15 Eylül akşamı refresh akışı uçtan uca çalışıyordu, ama `AppLayout` içindeki
Çıkış düğmesi yalnızca `clearToken()` çağırıyordu. Yani tarayıcıdaki iki anahtar
siliniyor, veritabanındaki `RefreshTokens` satırı ise `RevokedAtUtc = null`
hâlinde yedi gün daha geçerli kalıyordu. Ham refresh token bir yere kopyalanmışsa
çıkış yapmak onu geçersiz kılmıyordu; `RefreshToken.Revoke` metodu 28 Ağustos’tan
beri duruyor olmasına rağmen sadece rotation’da kullanılıyordu.

Dosya sırası şöyleydi: önce `LogoutCommand`, `LogoutCommandValidator`,
`LogoutCommandHandler` (Application katmanı), sonra `LogoutRequest` ve
`AuthController` action’ı (Api katmanı), en sonda `api.ts` içindeki `logout`
fonksiyonu ve `AppLayout`’taki `handleLogout`. Backend’i önce yazdık, çünkü
endpoint olmadan frontend’in çağıracağı bir adres yoktu ve Scalar’dan tek başına
test edilebiliyordu.

#### Command ve validator

```1:5:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LogoutCommand.cs
using MediatR;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest<bool>;
```

`RefreshCommand` ile aynı girdiyi alıyor: ham refresh token. Fark dönüş tipinde;
burada `LoginResult?` değil `bool` var, çünkü çıkışta yeni bir token çifti
üretilmiyor. `true` değeri “satır bulundu ve iptal edildi” demek, ama bu bilgi
dışarı çıkmıyor — controller her hâlükârda 204 döndürüyor.

Bir gün önce `RefreshCommand.cs` yazılırken namespace yanlışlıkla
`ReactBattleArena.Application.Commands` olmuş ve üç dosyaya fazladan `using`
eklemek gerekmişti; 16 Eylül’de o düzeltildi ve bu dosya baştan doğru namespace
ile yazıldı.

```5:11:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LogoutCommandValidator.cs
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}
```

`RefreshCommandValidator` ile birebir aynı kural. `Program.cs`’e kayıt satırı
yazılmadı; `AddValidatorsFromAssembly` assembly’yi tarayıp bu sınıfı buluyor
(bölüm 2). Boş gövde gelirse `ValidationBehavior` devreye girip 400 üretiyor.

#### Handler — `Revoke`’un ikinci kullanımı

```22:35:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/LogoutCommandHandler.cs
    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = _refreshTokens.Hash(request.RefreshToken);

        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null || existing.RevokedAtUtc is not null)
            return false;

        existing.Revoke(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
```

İlk iki satır `RefreshCommandHandler` ile aynı: istemciden gelen ham token
`Hash` ile SHA256’ya çevriliyor ve `TokenHash` unique index’i üzerinden satır
aranıyor. Buradaki fark, bulunan satırdan sonra yeni bir satır üretilmemesi.

Süre kontrolü bilinçli olarak yok. Refresh’te `ExpiresAtUtc <= utcNow` kontrolü
gerekiyordu, çünkü süresi dolmuş bir token’la yeni access vermek yanlış olurdu.
Çıkışta ise süresi dolmuş bir satırı iptal etmek zararsız; fazladan bir `if`
yazmanın getirisi yok.

`existing.Revoke(utcNow)` çağrısından sonra `Update` çağırmıyoruz. Satır sorguyla
çekildiği anda EF Core onu change tracking’e alıyor, `SaveChangesAsync` da
değişen property’yi görüp UPDATE üretiyor. Bu, refresh handler’ında öğrendiğimiz
davranışın aynısı.

Burada iptal edilen yalnızca o cihazın satırı. Kullanıcı telefonundan da girmişse
onun satırı ayrı durur ve etkilenmez. “Tüm cihazlardan çık” istenirse `UserId`
üzerinden bütün satırları dolaşmak gerekir; o ayrı bir özellik.

#### Api katmanı

```1:6:ReactBattleArena/ReactBattleArena.Api/Contracts/LogoutRequest.cs
namespace ReactBattleArena.Api.Contracts;

public sealed class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
```

`RefreshRequest` ile aynı biçim: tek alanlı bir gövde sınıfı. `LoginRequest` ve
`RegisterRequest` kalıbı (bölüm 7 ve 8).

```80:91:ReactBattleArena/ReactBattleArena.Api/Controllers/AuthController.cs
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Logout(
        [FromBody] LogoutRequest body,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new LogoutCommand(body.RefreshToken), cancellationToken);

        return NoContent();
    }
```

`[AllowAnonymous]` burada da doğru tercih, refresh action’ındaki sebeple aynı:
kullanıcı çıkış yapmak istediğinde access token çoktan ölmüş olabilir. `[Authorize]`
koysaydık “oturumu kapatamıyorum” gibi bir durum çıkardı. Kimlik kanıtı, gövdede
gönderilen refresh token’ın veritabanındaki bir satırla eşleşmesi.

Dönüş `NoContent`, yani karakter PUT/DELETE’lerinden bildiğimiz 204 (bölüm 5 ve 18).
Handler `false` dönse bile 204 veriyoruz; “bu token sistemde yoktu” bilgisini
dışarı vermiyoruz. Login’de kullanıcı adı sızdırmama kararının (bölüm 8) aynı
mantığı.

`_mediator.Send`’in dönüş değerini kullanmıyoruz. `bool`’u şimdilik sadece handler
içinde anlamlı bıraktık; ileride “çıkış yapıldı / token zaten iptalliydi” diye
log atmak istenirse orada duruyor.

#### Bu kodu kim tetikliyor?

`AppLayout`’taki Çıkış düğmesi → `logout()` → `POST /api/auth/logout`. Scalar’dan
da aynı endpoint elle çağrılabilir: login ol, cevaptaki `refreshToken`’ı gövdeye
koy, 204 al, SSMS’te `RevokedAtUtc`’nin dolduğunu gör. Ardından aynı token’la
`POST /api/auth/refresh` denenince 401 gelir, çünkü refresh handler’ı
`RevokedAtUtc is not null` kontrolünden dönüyor.

#### Frontend — `logout` fonksiyonu

```62:83:web/src/api.ts
export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken()

  if (refreshToken) {
    try {
      await fetch(`${API_BASE}/api/auth/logout`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ refreshToken }),
      })
    } catch {
      // API kapalıysa bile yerel temizlik yapılmalı; kullanıcı ekranda kalmasın
      //Üç ayrıntı var burada. Token yoksa isteği hiç atmıyoruz, çünkü validator boş değere 400 döner ve çıkış yaparken hata görmek anlamsız. 
      // İstek apiFetch değil düz fetch; apiFetch kullanırsak 401 ihtimalinde refresh denemesi yapar, oysa biz tam tersini istiyoruz. 
      // clearToken() de try/catch'in dışında, yani sunucuya ulaşılamasa bile tarayıcı temizlenir.
    }
  }

  clearToken()
}
```

Üç karar var bu fonksiyonda. `localStorage`’da refresh token yoksa istek hiç
atılmıyor, çünkü validator boş değere 400 döner ve çıkış yaparken hata görmek
anlamsız. İstek `apiFetch` değil düz `fetch` ile gidiyor; `apiFetch` kullanılsa
401 ihtimalinde `refreshSession` devreye girer, yani oturumu kapatmaya çalışırken
yenilemeye çalışırdı. `clearToken()` çağrısı `try/catch`’in dışında duruyor, bu
sayede API kapalı olsa bile tarayıcı temizlenir ve kullanıcı ekranda kilitli
kalmaz.

```57:60:web/src/AppLayout.tsx
  async function handleLogout() {
    await logout()
    navigate('/login')
  }
```

9 Ağustos’ta bu fonksiyon `localStorage.removeItem('token')` yapıyordu, 11 Ağustos’ta
`clearToken()` oldu, bugün `logout()` çağırıyor ve `async` hâle geldi. `await`
olmadan `navigate` çağırsak istek yarı yolda kesilebilirdi. `onClick={handleLogout}`
satırı değişmedi; React `async` fonksiyonu olay işleyicisi olarak kabul eder,
dönen Promise’i yok sayar.

#### `/me` 401’inde login’e dönüş — bugünün ikinci düzeltmesi

Test sırasında tarayıcıda ölü bir access token ve hiç refresh token olmayan bir
durum oluştu. Konsolda `/api/auth/me` iki kez 401 verdi (StrictMode `useEffect`’i
geliştirmede iki kez çalıştırır, bölüm 19) ama `POST /api/auth/refresh` isteği
hiç görünmedi. Sebebi `refreshSession`’ın ağa çıkmadan `if (!refreshToken) return false`
satırından dönmesiydi. Sayfa ise login’e gitmek yerine yetkisiz hâlde açık kaldı,
çünkü `loadMe` sadece `ok` durumunu ele alıyordu.

```22:38:web/src/AppLayout.tsx
  async function loadMe() {
    try {
      const meResponse = await apiFetch('/api/auth/me')
      if (meResponse.ok) {
        const me = await meResponse.json()
        setPermissions(me.permissions ?? [])
      } else if (meResponse.status === 401) {
        // apiFetch buraya gelene kadar yenilemeyi denedi ve başaramadı: oturum bitti.
        clearToken()
        navigate('/login')
        return
      }
    } catch {
      // /me gelmese de meLoaded bitsin; yoksa sonsuz Yükleniyor
    }
    setMeLoaded(true)
  }
```

Mantık şu: `apiFetch` bu satıra 401 ile geldiyse yenilemeyi zaten denemiş ve
başaramamış demektir, ikinci bir şans yok. O yüzden `clearToken()` ile iki anahtar
siliniyor ve `navigate('/login')` çağrılıyor. `return` önemli; `setMeLoaded(true)`
çalışmasın ki yönlendirme sırasında yetkisiz sayfa bir an görünmesin.

Bu kodun ASP.NET karşılığı, bir MVC uygulamasında `[Authorize]` başarısız olunca
`LoginPath`’e redirect edilmesidir. React’te böyle bir otomatik mekanizma yok;
yönlendirmeyi bizim yazmamız gerekiyor.

#### Bu adımda yapılan / kalan iz

Bugün yapılan gerçek hata: `AppLayout.tsx`’e `import { ..., logout } from './api'`
satırı yazıldı ama `api.ts`’e `logout` fonksiyonu henüz eklenmemişti. Sayfa açılmadı;
konsoldaki tipik mesaj “does not provide an export named 'logout'” şeklindedir ve
“dosya bulundu, içinde o isim yok” demektir. Import ile `export` isminin birebir
aynı olması gerekiyor.

Diğer izler: `LogoutCommandHandler`’a kullanılmayan `ReactBattleArena.Domain.Authentication`
using’i eklendi (yeni satır üretmediğimiz için gereksiz). Logout’u `apiFetch` ile
atmak (çıkışta yenileme denemesi). Çıkışta 200 bekleyip 204’ü hata sanmak. Handler’ın
`false` dönüşünü 404’e çevirmek — token’ın varlığını sızdırır. Test öncesi
`localStorage`’ı elle kurcalayıp “kod bozuldu” sanmak; oradaki iki anahtarın
tutarlı olması gerekir.

#### Sonuçta ne kazandık

Çıkış artık sunucu tarafında da gerçek: `RefreshTokens` satırı iptal ediliyor ve o
ham token bir daha yeni access üretemiyor. Ayrıca oturumu gerçekten bitmiş bir
kullanıcı yetkisiz sayfada kalmıyor, login’e yönlendiriliyor. Kalan iki madde
reuse detection (iptal edilmiş token tekrar gelirse kullanıcının tüm satırlarını
geçersiz kılmak) ve refresh token’ı `HttpOnly` cookie’ye taşıma kararı.

---

### 36. 17 Eylül — Reuse detection (iptal edilmiş refresh token tekrar gelirse)

#### Önce rotation’ı tek paragrafta tekrar edelim

**Rotation:** her yenileme isteğinde kullanılan refresh token’ın satırı iptal
edilir (`RevokedAtUtc` dolar) ve kullanıcıya yeni bir refresh token verilir. Yani
refresh token **tek kullanımlıktır**. Kodda bu iki satırın yan yana durması demek:

```59:67:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandHandler.cs
        existing.Revoke(utcNow);
        //Dört ayrı başarısızlık durumunun hepsi aynı null'u döndürüyor; login'deki "email sızdırmama" mantığının aynısı.
        //existing.Revoke(utcNow) satırından sonra ayrıca bir Update çağırmıyoruz, çünkü satırı sorguyla çektiğimiz an EF onu takibe alıyor;
        //SaveChangesAsync değişikliği kendisi UPDATE'e çeviriyor. Aynı SaveChanges hem eski satırın RevokedAtUtc'sini hem yeni satırın INSERT'ünü tek transaction'da yazıyor — rotation tam olarak bu.

        var (rawRefresh, newHash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(RefreshToken.Create(user.Id, newHash, expires, utcNow));

        await _db.SaveChangesAsync(cancellationToken);
```

Rotation’ın iki kazancı var. Sızmış bir refresh token’ın ömrü, bir sonraki
yenilemeye kadar kısalıyor. Daha önemlisi, iptal edilmiş bir refresh token ikinci
kez geldiğinde bunun **anormal** olduğunu anlayabiliyoruz — bu bölümün konusu
tam olarak o bilgiyi kullanmak.

#### Neden bu adım geldi

16 Eylül’de logout bitince auth listesinde tek madde kalmıştı. Rotation sayesinde
her refresh token tek kullanımlık olduğu için, normal çalışan bir istemci iptal
edilmiş bir refresh token’ı bir daha göndermez. Gönderildiyse iki açıklama var:
refresh token bir yere sızmış ve hem saldırgan hem gerçek kullanıcı aynı zinciri
kullanmaya çalışıyor, ya da istemcide aynı refresh token’ı iki kez yollayan bir
hata var. (Bu paragraftaki her “token” refresh token; access JWT bu tabloda
tutulmuyor.)

O güne kadar bu durumda handler sadece `null` döndürüyordu, yani istek 401 alıyor
ama saldırganın elindeki zincir yaşamaya devam ediyordu. Bu adımda tek dosyaya
dokunduk: `RefreshCommandHandler`. Frontend’de hiçbir değişiklik yapılmadı.

#### Kod

```35:49:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandHandler.cs
        if (existing.RevokedAtUtc is not null)
        {
            // Rotation yüzünden her refresh token tek kullanımlık. İptal edilmiş bir token
            // ikinci kez geldiyse aynı zinciri iki taraf tutuyor demektir; çalınmış varsayıyoruz.

            var activeTokens = await _db.RefreshTokens
                .Where(t => t.UserId == existing.UserId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var activeToken in activeTokens)
                activeToken.Revoke(utcNow);
            if (activeTokens.Count > 0)
                await _db.SaveChangesAsync(cancellationToken);
            return null;
        }
```

Sorgu, gelen satırın `UserId`’si üzerinden o kullanıcının **iptal edilmemiş** tüm
refresh token satırlarını çekiyor. `RevokedAtUtc == null` koşulu olmasa zaten
iptal edilmiş eski satırlar da gelirdi; `Revoke` metodu içinde aynı kontrol
olduğu için sonuç değişmezdi ama boşuna satır çekilirdi.

Çekilen satırlar sorgu anında EF Core’un change tracking’ine giriyor, bu yüzden
`foreach` içinde `Revoke` çağırmak yeterli; `Update` gerekmiyor. Tek
`SaveChangesAsync` hepsini UPDATE olarak yazıyor. LINQ karşılığı C# tarafında
alıştığımız `Where` ile aynı, EF bunu `WHERE UserId = @p AND RevokedAtUtc IS NULL`
sorgusuna çeviriyor.

`return null` satırının `if` bloğunun dışında değil, ama en sonunda durması
önemli: aktif satır bulunsa da bulunmasa da istek 401 dönmek zorunda. Handler
burada da “neden başarısız oldu” bilgisini dışarı vermiyor (bölüm 8’deki
sızdırmama kararı).

`if (activeTokens.Count > 0)` kontrolü **isteğe bağlı**. İlk anlatımda “gereksiz
veritabanı turunu engelliyor” demiştim, bu yanlıştı: EF Core takipte değişiklik
yoksa `SaveChangesAsync`’te veritabanına hiç gitmez, doğrudan 0 döner. Yani bu
satır sadece EF’in iç işini atlıyor. Kaldırmak da, `foreach` ile birlikte tek
`if` içine almak da doğru; ikincisi niyeti daha iyi gösterir.

#### Yanlış alarm riski ve `refreshInFlight` bağlantısı

Bu özellik yanlış alarma çok müsait. İki istek aynı anda 401 alıp ikisi de aynı
refresh token’ı gönderse, ikincisi iptal edilmiş token’la gelir ve kullanıcı
sebepsiz her yerden çıkarılır.

```23:28:web/src/api.ts
let refreshInFlight: Promise<boolean> | null = null

async function refreshSession(): Promise<boolean> {
  if (refreshInFlight) {
    return refreshInFlight
  }
```

15 Eylül’de yazdığımız bu üç satır tam bunu engelliyor: paralel 401’lerde ikinci
istek yeni bir yenileme başlatmıyor, birincinin sonucunu bekliyor. Yani
frontend’i o gün doğru yazmış olmamız, bugünkü reuse detection’ı güvenli hâle
getirdi. Geliştirmede StrictMode’un `/me` isteğini iki kez çalıştırması da
(bölüm 19) bu yüzden sorun çıkarmıyor.

#### Bu kodu kim tetikliyor?

`POST /api/auth/refresh`, gövdesinde daha önce kullanılmış bir refresh token ile.
Gerçek kullanıcı akışında bu isteği kimse atmaz; Scalar’dan elle ya da sızmış bir
token ile gelir. Frontend tarafında sonuç şu: refresh 401 → `refreshSession`
`clearToken` çağırır → `AppLayout`’un `/me` kapısı (bölüm 35) login’e yönlendirir.
Kullanıcı şifresini yeniden girer; saldırganda şifre olmadığı için zincir kopar.

#### Test (17 Eylül, başarılı)

Sırası şöyleydi:

1. Uygulamadan giriş yapıldı ve `localStorage`’daki `refreshToken` değeri
   kopyalandı — buna **A** diyoruz.
2. Scalar’dan `POST /api/auth/refresh` gövdesine A konuldu → **200**, cevapta yeni
   bir refresh token geldi — buna **B** diyoruz. SSMS’te A’nın satırında
   `RevokedAtUtc` dolmuştu (rotation).
3. Aynı istek A ile **ikinci kez** gönderildi → **401**. Bu sefer SSMS’te B’nin
   satırı da iptal edilmiş görünüyordu; reuse detection devreye girmişti.
4. Son kontrol olarak B gönderildi → **401**, çünkü B bir önceki adımda iptal
   edilmişti.
5. Tabloda o kullanıcıya ait bütün satırların `RevokedAtUtc` değeri doluydu.

Üçüncü adım bu özelliğin kanıtı: eskiden orada sadece 401 dönerdi ve B yaşamaya
devam ederdi.

#### Bu adımda yapılan / kalan iz

Sık düşülen hata: `return null`’u `if` bloğunun içine alıp aktif satır yokken
401 dönmeyi atlamak. Yalnız gelen satırı iptal edip diğerlerini bırakmak (o zaman
saldırganın zinciri yaşar). `RevokedAtUtc == null` filtresini yazmayıp tüm
geçmişi çekmek. Frontend’de `refreshInFlight` olmadan bu özelliği açmak — paralel
401’ler kullanıcıyı sebepsiz atar. Bir de benim yaptığım hata: `Count > 0`
kontrolünü “veritabanı turunu engelliyor” diye açıklamak; EF zaten değişiklik
yoksa veritabanına gitmiyor.

#### Yan not 1 — `var (rawRefresh, newHash, expires) = ...` satırı

Bu satır bölüm 33’ten beri kodda duruyordu, burada açıyoruz.

```64:65:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandHandler.cs
        var (rawRefresh, newHash, expires) = _refreshTokens.Create(utcNow);
        _db.RefreshTokens.Add(RefreshToken.Create(user.Id, newHash, expires, utcNow));
```

`Create` metodu tek bir değer döndürüyor ama o değerin üç parçası var:
`(string Raw, string Hash, DateTime ExpiresAtUtc)`. Buna **value tuple** denir.
Sol taraftaki parantezli yazım da **deconstruction**: tek satırda üç parçayı üç
ayrı yerel değişkene dağıtmak. Deconstruction yapmadan şöyle yazılabilirdi:

```csharp
var result = _refreshTokens.Create(utcNow);
// result.Raw, result.Hash, result.ExpiresAtUtc
```

Üç değerin gittiği yerler farklı, asıl mesele bu: `rawRefresh` yalnızca cevabın
JSON’una konur (`LoginResult`), `newHash` veritabanındaki `TokenHash` kolonuna
yazılır, `expires` da `ExpiresAtUtc` kolonuna. Ham token hiçbir zaman
veritabanına girmez.

Tuzak: eşleşme isimle değil **sırayla** yapılır. Sol tarafa
`var (newHash, rawRefresh, expires)` yazılsa derleyici uyarmaz, ama ham token
`TokenHash` kolonuna yazılır ve hash kullanıcıya gönderilir — derlenen, sessiz
bir güvenlik hatası. C# tarafında alternatifi `out` parametreleri veya küçük bir
`record` döndürmek olurdu; iki-üç değer hemen kullanılıyorsa tuple yeterli.

#### Yan not 2 — Süresi dolan refresh token kendi kendine iptal olur mu?

Olmaz. Arka planda çalışan hiçbir iş yok; `RefreshExpireDays = 7` yalnızca satır
oluşturulurken `ExpiresAtUtc` değerini hesaplar. Süre geçince satır aynen durur,
`RevokedAtUtc` hâlâ `null` kalır. Kontrol istek anında yapılır:

```52:53:ReactBattleArena/ReactBattleArena.Application/Authentication/Commands/RefreshCommandHandler.cs
        if (existing.ExpiresAtUtc <= utcNow)
            return null;
```

Yani **“iptal edilmiş”** ile **“süresi dolmuş”** iki ayrı ret sebebi. İkisi de
401 üretir ama veritabanında farklı görünürler: birinde `RevokedAtUtc` dolu,
öbüründe boş ama `ExpiresAtUtc` geçmişte.

Bunun güvenlik tarafındaki anlamı şu: çalınan bir refresh token en fazla yedi gün
işe yarar. Rotation varsa genelde daha az — gerçek kullanıcı bir kez yenileme
yaptığı anda çalınan satır iptal olur, çalınan token bir sonraki kullanımda reuse
detection’a düşer.

Yan etkisi: iptal edilmiş ve süresi dolmuş satırlar tabloda sonsuza kadar birikir.
Doğruluğu etkilemez, ama üretimde periyodik bir temizlik işi (örneğin süresi bir
aydan fazla önce dolmuş satırları silmek) mantıklı olur. Şimdilik yapılmadı.

#### Sonuçta ne kazandık

Çalınmış bir refresh token tek kullanımla sınırlı kalmıyor, ikinci kullanımda o
kullanıcının bütün oturumları kapanıyor. Auth listesinde kod tarafında açık madde
kalmadı; geriye yalnızca refresh token’ı `HttpOnly` cookie’ye taşıma kararı var
ve o danışman görüşü bekliyor.
