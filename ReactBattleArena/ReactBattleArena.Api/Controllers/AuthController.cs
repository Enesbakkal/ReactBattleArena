using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using ReactBattleArena.Api.Contracts;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Application.Authentication.Commands;
using ReactBattleArena.Domain.Authorization;
using ReactBattleArena.Domain.Users;
using System.Security;
using System.Security.Claims;

namespace ReactBattleArena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserPermissionService _permissions;

    public AuthController(IMediator mediator, IUserPermissionService permissions)
    {
        _mediator = mediator;
        _permissions = permissions;
    }

    [AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)] // Önyüze gönderilen cevapla ilgili
    public async Task<ActionResult<Guid>> Register(
        [FromBody] RegisterRequest body, // [FromBody] JSON'u RegisterRequest alanlarına bağlar. ASP.NET Core alan adını büyük-küçük harf duyarsız okur. Frontend'in userName değeri UserName alanına düşer.
        CancellationToken cancellationToken = default)
    {
        var id = await _mediator.Send(
            new RegisterCommand(body.UserName, body.Email, body.DisplayName, body.Password),
            cancellationToken);

        return Created($"/api/users/{id}", id);
        //Created HTTP 201 yazar, Location header'ına /api/users/{id} koyar, gövdeye de aynı Guid değerini basar.
    }

    [AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    //Pipeline kayıt ile aynı sıradadır. Önce UseAuthentication, sonra UseAuthorization. Login metodu [AllowAnonymous] taşır.
    //Header'da Bearer olmadığı için HttpContext.User boş kalır ve bu metot 401 vermez.
    public async Task<ActionResult<LoginResult>> Login(
    [FromBody] LoginRequest body,//Adres api/auth/login olur. [FromBody] JSON'u LoginRequest alanlarına bağlar. ASP.NET Core alan adını büyük-küçük harf duyarsız okur.
    CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new LoginCommand(body.UserNameOrEmail, body.Password),
            cancellationToken);

        return result is null ? Unauthorized() : Ok(result);
        //Controller kullanıcı aramaz ve token basmaz. null gelirse Unauthorized() HTTP 401 yazar, gövde boştur. Dolu gelirse Ok(result) HTTP 200 yazar.
    }
    //Bu kodu kim tetikliyor? Scalar POST /api/auth/login. LoginPage fetch / sonra apiFetch aynı URL,
    //cevaptaki token saklanır.

    [AllowAnonymous]
    //[AllowAnonymous] şart: bu endpoint'e gelindiğinde access token çoktan ölmüş olacak,
    //[Authorize] koyarsak 401 döngüsüne gireriz. [HasPermission] de yok, çünkü bu bir oturum kapısı, bir fiil kapısı değil.
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
    //[AllowAnonymous] burada da doğru tercih: kullanıcı access token'ı çoktan ölmüşken de çıkış yapabilmeli,
    //yoksa "çıkış yapamıyorum" gibi saçma bir durum oluşur. Kimlik kanıtı elindeki refresh token'ın kendisi.
    //Dönüş NoContent, yani PUT/DELETE'lerde alıştığın 204; handler false dönse bile 204 veriyoruz ki "bu token sistemde var mıydı" bilgisi sızmasın.



    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        //Aynı controller’da GET / api / auth / me, [Authorize].Token’daki id’den kim olduğunu döner.
        //27 Temmuz’da id, userName, email. Bugünkü permissions 24 Ağustos.Sayfa yenilenince login JSON’u uçmuştur; token duruyorsa / me kim olduğunu söyler.

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

    // İleride
    //Not: Token’da unique_name kullanıyorsak User.Identity?.Name dolu gelir; email claim adı JWT’de email olabilir.
    //Daha sağlam alternatif(Login’de koyduğun claim’lere göre) :
    //id = User.FindFirst("sub")?.Value
    //    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
    //userName = User.FindFirst("unique_name")?.Value
    //    ?? User.Identity?.Name,
    //email = User.FindFirst("email")?.Value
}
