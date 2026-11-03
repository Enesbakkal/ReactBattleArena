using MediatR;

namespace ReactBattleArena.Application.Characters.Commands;

public sealed record UpdateCharacterCommand(
    Guid Id,
    string Name,
    string Universe,
    string? Biography,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    int BaseHealth,
    int BaseTechnique,
    int BaseAccuracy,
    int BaseForesight,
    string? ImageUrl) : IRequest<bool>;
//IRequest<bool> → bulundu ve güncellendiyse true, yoksa false.