using MediatR;
namespace ReactBattleArena.Application.Characters.Queries;

public sealed record GetCharacterByIdQuery(Guid Id) : IRequest<CharacterDetailDto?>;
public sealed record CharacterDetailDto(
    Guid Id,
    string Name,
    string Universe,
    string? Biography,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseHealth,
    int BaseTechnique,
    int BaseAccuracy,
    int BaseForesight,
    int BaseSpeed,
    string? ImageUrl,
    DateTime CreatedAtUtc);