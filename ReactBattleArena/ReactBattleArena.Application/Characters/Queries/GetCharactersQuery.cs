using MediatR;

namespace ReactBattleArena.Application.Characters.Queries;

public sealed record GetCharactersQuery(int Page, int PageSize, string? Search)
    : IRequest<PagedCharacterRowsResult>;

public sealed record CharacterRowDto(
    Guid Id,
    string Name,
    string Universe,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    int BaseHealth,
    int BaseTechnique,
    int BaseAccuracy,
    int BaseForesight,
    string? ImageUrl,
    DateTime CreatedAtUtc);

public sealed record PagedCharacterRowsResult(
    IReadOnlyList<CharacterRowDto> Items,
    int TotalCount);