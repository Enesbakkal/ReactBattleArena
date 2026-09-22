using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ReactBattleArena.Api.Authorization;
using ReactBattleArena.Api.Extensions;
using ReactBattleArena.Api.OpenApi;
using ReactBattleArena.Application;
using ReactBattleArena.Infrastructure;
using ReactBattleArena.Infrastructure.Persistence;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApplication();// AddApplication MediatR handler'larını, FluentValidation kurallarını ve her Send öncesinde çalışan ValidationBehavior tipini kaydeder.
builder.Services.AddInfrastructure(builder.Configuration); // AddInfrastructure bu sınıfı (BCryptPasswordHasher) IPasswordHasher olarak kaydeder. Handler somut BCrypt sınıfını tanımaz.
                                                           // Constructor IPasswordHasher ister, DI BCryptPasswordHasher verir.
                                                           // AddInfrastructure, configuration'daki Jwt section'ını JwtOptions sınıfına bağlar ve JwtTokenService'i kaydeder.
                                                           
builder.Services.AddAuthorization(); //AddAuthorization() ise DI kaydıdır
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
//Provider Singleton’dır çünkü yalnız string çevirir, DbContext tutmaz.
//Handler Scoped’dur çünkü IUserPermissionService ve onun DbContext’i istek ömründedir.
//Singleton handler Scoped DbContext çekse yaşam süresi çatışırdı.

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
//Sayfa localhost:5173 üzerindedir, API https://localhost:7275 üzerindedir. Tarayıcı bunu başka origin sayar ve isteği CORS kontrolüne sokar. API şu origin'lere izin verir.
//Microsoft.EntityFrameworkCore.Design
//Migration komutları Api’yi startup proje olarak kullanır; bu paket gerekli.

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});


//GetSection("Jwt") appsettings içindeki Jwt nesnesini alır.
//Configure<JwtOptions> o nesnenin Key, Issuer, Audience, ExpireMinutes ve RefreshExpireDays alanlarını aynı adlı property'lere yazar.
//Jwt:Key boşsa API açılmaz. JwtBearer aynı anahtar, issuer, audience ve süreyi doğrular.
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("Jwt:Key is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // AddAuthentication şemayı JwtBearer yapar.
    .AddJwtBearer(options => //AddJwtBearer doğrulama kuralını kaydeder.Bu iki satır DI kaydıdır.
    { // İsteğin üstünden geçen middleware UseAuthentication ve ondan sonra UseAuthorization satırlarıdır. 
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, // ValidateLifetime true olduğu için süresi dolmuş access token geçersiz kalır.O durumda [Authorize] 401 verir.
                                     // O durumda [Authorize] 401 verir. Yenisini almak refresh token ile olur. O çağrı oturumu uzatma işidir.
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
//Key yoksa uygulama açılmaz. Issuer/Audience/imza/süre doğrulanır; süresi dolmuş token reddedilir.

var app = builder.Build();

// DbContext scoped (istek ömrü). Program kökü request değil → CreateScope ile kısa ömürlü kapsül;
// using bitince context Dispose. Yoksa root provider'dan scoped alınamaz.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await AuthSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddPreferredSecuritySchemes("Bearer");
    });
}

app.UseFluentValidationExceptionHandler(); // Program.cs bu middleware'i (FluentValidationExceptionMiddleware) UseFluentValidationExceptionHandler ile pipeline'ın başına koyar.
                                           // Exception controller'dan çıkınca HTTP cevabına burada döner.
app.UseHttpsRedirection();

//pipeline
app.UseCors();
app.UseAuthentication(); // UseAuthentication, JwtBearer ile Authorization: Bearer arar. Bu istekte (register) o header yoktur. HttpContext.User boş kalır.
//İstemci Authorization: Bearer eyJ... koyar.UseAuthentication JwtBearer ile bakar.İmza ve süre tutmazsa veya header yoksa kullanıcı boştur.
app.UseAuthorization();// pipelinedaki middleware // Hemen ardından UseAuthorization çalışır. Register metodu [AllowAnonymous] taşıdığı için boş kullanıcı burada 401 üretmez.
//Sıra zorunlu: önce kimsin (UseAuthentication token’ı HttpContext.User yapar),
//sonra ne yapabilirsin (UseAuthorization). Tersi: [Authorize] user’ı boş görür, herkes 401.

app.MapControllers();

app.Run();
