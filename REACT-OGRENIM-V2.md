# Öğrenim Notları V2 — Backend + React

Bu dosya `REACT-OGRENIM.md` arşivinin yerine, baştan ileriye doğru yeniden yazılan öğrenim notudur. Eski dosyaya dokunulmaz. Bölümler `git log` sırasıyla gider: önce backend (2 Temmuz), sonra React, sonra RBAC, sonra refresh token.

**İlerleme:** 12 / 34 yazıldı · **22 kaldı.**

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
