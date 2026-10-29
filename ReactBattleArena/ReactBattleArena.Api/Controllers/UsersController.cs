using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReactBattleArena.Api.Contracts;
using ReactBattleArena.Application.Users.Commands;
using ReactBattleArena.Application.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using ReactBattleArena.Api.Authorization;
using ReactBattleArena.Domain.Authorization;
using System.Security.Claims;

namespace ReactBattleArena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(PagedUserRowsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedUserRowsResult>> GetPaged(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idValue, out var tokenUserId))
            return Unauthorized();

        var result = await _mediator.Send(
            new GetUsersQuery(page, pageSize, tokenUserId),
            cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailDto>> GetById(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idValue, out var tokenUserId))
            return Unauthorized();

        var result = await _mediator.Send(
            new GetUserByIdQuery(id, tokenUserId),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HasPermission(PermissionCodes.UsersCreate)]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateUserRequest body,
        CancellationToken cancellationToken = default)
    {
        var id = await _mediator.Send(
            new CreateUserCommand(body.UserName, body.Email, body.DisplayName, body.Password),
            cancellationToken);

        return Created($"/api/users/{id}", id);
    }

    [Authorize]
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile(
    [FromBody] UpdateUserProfileRequest body,
    CancellationToken cancellationToken = default)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idValue, out var tokenUserId))
            return Unauthorized();

        var updated = await _mediator.Send(
            new UpdateUserProfileCommand(
                tokenUserId,
                body.FirstName,
                body.LastName,
                body.Phone,
                body.Email,
                body.AvatarUrl,
                body.IsContactPublic),
            cancellationToken);

        return updated ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
    [FromBody] ChangePasswordRequest body,
    CancellationToken cancellationToken = default)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idValue, out var tokenUserId))
            return Unauthorized();

        var updated = await _mediator.Send(
            new ChangePasswordCommand(
                tokenUserId,
                body.CurrentPassword,
                body.NewPassword,
                body.ConfirmPassword),
            cancellationToken);

        return updated ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
    Guid id,
    [FromBody] CreateUserRequest body,
    CancellationToken cancellationToken = default)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idValue, out var tokenUserId))
            return Unauthorized();

        if (tokenUserId != id)
            return Forbid();

        var updated = await _mediator.Send(
            new UpdateUserCommand(id, body.UserName, body.Email, body.DisplayName),
            cancellationToken);

        return updated ? NoContent() : NotFound();
    }

    [HasPermission(PermissionCodes.UsersDelete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _mediator.Send(new DeleteUserCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
