using MediatR;

namespace ReactBattleArena.Application.Characters.Commands;

public sealed record CreateCharacterCommand(
    string Name,
    string Universe,
    string? Biography,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    string? ImageUrl) : IRequest<Guid>;
//IRequest<Guid>
//“Bu istek işlenince Guid döner” (yeni karakterin Id'si)
//Character.Create() ile aynı alanlar(Id ve CreatedAt handler'da eklenir)