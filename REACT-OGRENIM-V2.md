# Öğrenim Notları V2 — Backend + React

Bu dosya `REACT-OGRENIM.md` arşivinin yerine, baştan ileriye doğru yeniden yazılan öğrenim notudur. Eski dosyaya dokunulmaz. Bölümler `git log` sırasıyla gider: önce backend (2 Temmuz), sonra React, sonra RBAC, sonra refresh token.

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
