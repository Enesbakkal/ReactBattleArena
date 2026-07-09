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
    string? ImageUrl) : IRequest<bool>;
//IRequest<bool> → bulundu ve güncellendiyse true, yoksa false.