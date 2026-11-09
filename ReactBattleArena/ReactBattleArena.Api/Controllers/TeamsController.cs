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

    // Aşağıdaki üç metod için Özetle: rename listedeki ad alanının PUT’udur, delete listedeki silmedir,
    // GetById düzenleme sayfasının kendi okumasıdır; üçünün ortak omurgası JWT’deki kullanıcı ile OwnerUserId eşleşmesidir.
    // Bunları yazıp Scalar’da dene; sonra üyelik (slot’a karakter ekle/çıkar) gelir.

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TeamRowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamRowDto>> GetById(
    Guid id,
    CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(
            new GetTeamByIdQuery(id, userId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(
        Guid id,
        [FromBody] RenameTeamRequest body,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var ok = await _mediator.Send(
            new RenameTeamCommand(id, userId, body.Name),
            cancellationToken);

        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var ok = await _mediator.Send(
            new DeleteTeamCommand(id, userId),
            cancellationToken);

        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember(
    Guid id,
    [FromBody] AddTeamMemberRequest body,
    CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _mediator.Send(
            new AddTeamMemberCommand(id, userId, body.CharacterId, body.SlotIndex),
            cancellationToken);

        return result switch
        {
            AddTeamMemberResult.Ok => NoContent(),
            AddTeamMemberResult.TeamNotFound => NotFound(),
            AddTeamMemberResult.CharacterNotFound => NotFound(),
            AddTeamMemberResult.SlotOccupied => BadRequest("Slot is already occupied."),
            AddTeamMemberResult.CharacterAlreadyInTeam => BadRequest("Character is already on this team."),
            AddTeamMemberResult.TeamFull => BadRequest("Team already has 5 members."),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
        //URL’deki id takım kimliğidir; gövdeki characterId ve slotIndex hangi karakterin hangi kutuya gireceğini söyler.
        //TryGetUserId JWT’den sahibi okur; command’a OwnerUserId olarak gider. Handler AddTeamMemberResult döner;
        //switch bu sonucu HTTP’ye çevirir: başarı 204, yokluk 404, iş kuralı ihlali 400 ve kısa İngilizce metin.
        //ASP.NET’te action’ın işi burada biter; SQL ve sahiplik kontrolü MediatR handler’dadır. React düzenleme ekranı ekle / sürükle bitince aynı POST’u atacaktır.
    }

    [HttpDelete("{id:guid}/members/{slotIndex:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(
        Guid id,
        int slotIndex,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var ok = await _mediator.Send(
            new RemoveTeamMemberCommand(id, userId, slotIndex),
            cancellationToken);

        return ok ? NoContent() : NotFound();
    }
    // Burada karakter id’si yoktur; path’teki slotIndex hangi kutunun boşaltılacağını söyler.
    // Handler o takımda o slot satırını siler. Takım senin değilse veya o kutu zaten boşsa false gelir ve 404 dönülür.
    // Sayfa modelindeki “çıkarma o satırı siler” cümlesinin HTTP karşılığı budur. Katalog araması üyelik API’sinin işi değildir;
    // düzenleme sayfası karakterleri yine GET /api/characters?search=... ile bulur.
}