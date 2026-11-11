using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReactBattleArena.Api.Contracts;
using ReactBattleArena.Application.Battles.Commands;

namespace ReactBattleArena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BattlesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BattlesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private bool TryGetUserId(out Guid userId)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(idValue, out userId);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BattleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BattleDetailDto>> Create(
        [FromBody] CreateBattleRequest body,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(
            new CreateBattleCommand(userId, body.TeamId),
            cancellationToken);

        return result.Status switch
        {
            CreateBattleStatus.Ok => Ok(result.Battle),
            CreateBattleStatus.TeamNotFound => NotFound(),
            CreateBattleStatus.TeamIncomplete => BadRequest(new { error = "Team must have 5 characters." }),
            CreateBattleStatus.CatalogTooSmall => BadRequest(new { error = "Not enough characters for a system lineup." }),
            _ => BadRequest(),
        };
    }
    // Teams’teki TryGetUserId ile aynı. POST /api/battles + Bearer. MediatR kaydı AddApplication ile otomatik; ayrı DI satırı yok.
}