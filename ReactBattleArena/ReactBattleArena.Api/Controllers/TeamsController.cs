using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReactBattleArena.Api.Contracts;
using ReactBattleArena.Application.Teams.Commands;
using ReactBattleArena.Application.Teams.Queries;

namespace ReactBattleArena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TeamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeamsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private bool TryGetUserId(out Guid userId)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(idValue, out userId);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TeamRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeamRowDto>>> GetMine(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetMyTeamsQuery(userId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateTeamRequest body,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var id = await _mediator.Send(
            new CreateTeamCommand(userId, body.Name),
            cancellationToken);

        return CreatedAtAction(nameof(GetMine), new { id }, id);
        // GetById henüz yok; CreatedAtAction şimdilik listeye işaret eder. Sonra detay rotasına bağlarız.
    }
}