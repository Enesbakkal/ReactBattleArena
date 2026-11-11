namespace ReactBattleArena.Application.Battles.Commands;

public enum CreateBattleStatus
{
    Ok,
    TeamNotFound,
    TeamIncomplete,
    CatalogTooSmall,
}

public sealed record CreateBattleResult(
    CreateBattleStatus Status,
    BattleDetailDto? Battle);

public sealed record BattleDetailDto(
    Guid Id,
    BattleOutcomeDto Outcome,
    int TurnCount,
    bool IsSystemOpponent,
    IReadOnlyList<BattleFighterDto> Fighters,
    IReadOnlyList<BattleEventDto> Events);

public sealed record BattleFighterDto(
    string Side,
    int SlotIndex,
    Guid? CharacterId,
    string Name,
    string Universe,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    int BaseHealth,
    int BaseTechnique,
    int BaseAccuracy,
    int BaseForesight);

public sealed record BattleEventDto(
    int TurnNumber,
    int Sequence,
    string AttackerSide,
    int AttackerSlot,
    string TargetSide,
    int TargetSlot,
    string StrikeKind,
    string HitResult,
    int Damage,
    int TargetRemainingHealth);

public enum BattleOutcomeDto
{
    InitiatorWin = 0,
    OpponentWin = 1,
    Draw = 2,
}

//FE JSON’da Side vb. string okusun diye DTO’da "Initiator" / "Opponent" yazacağız(ToString()). Domain enum’ları Application cevabına böyle taşınır.

//CatalogTooSmall: sistem dizilimi için katalogda 5 karakter yoksa.