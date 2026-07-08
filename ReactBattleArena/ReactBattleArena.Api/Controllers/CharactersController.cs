using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReactBattleArena.Api.Contracts;
using ReactBattleArena.Application.Characters.Commands;
using ReactBattleArena.Application.Characters.Queries;

namespace ReactBattleArena.Api.Controllers;

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

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateCharacterRequest body,//Dışarıdan gelen JSON gövdesi
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

        return Created($"/api/characters/{id}", id);//201 + yeni Id döner
    }
}