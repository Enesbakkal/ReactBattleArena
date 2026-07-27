using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReactBattleArena.Api.Contracts;
using ReactBattleArena.Application.Authentication.Commands;
using ReactBattleArena.Domain.Users;
using System.Security.Claims;

namespace ReactBattleArena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[AllowAnonymous]//Böylece ileride global [Authorize] eklesek bile login/register çalışır.
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

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

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        return Ok(new
        { 
            id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            userName = User.Identity?.Name,
            email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
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
