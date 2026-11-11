namespace ReactBattleArena.Domain.Battles;

public sealed class BattleEvent
{
    private BattleEvent()
    {
    }

    public Guid Id { get; private set; }
    public Guid BattleId { get; private set; }
    public int TurnNumber { get; private set; }
    public int Sequence { get; private set; }

    public BattleSide AttackerSide { get; private set; }
    public int AttackerSlot { get; private set; }
    public BattleSide TargetSide { get; private set; }
    public int TargetSlot { get; private set; }

    public StrikeKind StrikeKind { get; private set; }
    public HitResult HitResult { get; private set; }
    public int Damage { get; private set; }
    public int TargetRemainingHealth { get; private set; }

    public static BattleEvent Create(
        Guid battleId,
        int turnNumber,
        int sequence,
        BattleSide attackerSide,
        int attackerSlot,
        BattleSide targetSide,
        int targetSlot,
        StrikeKind strikeKind,
        HitResult hitResult,
        int damage,
        int targetRemainingHealth)
    {
        return new BattleEvent
        {
            Id = Guid.NewGuid(),
            BattleId = battleId,
            TurnNumber = turnNumber,
            Sequence = sequence,
            AttackerSide = attackerSide,
            AttackerSlot = attackerSlot,
            TargetSide = targetSide,
            TargetSlot = targetSlot,
            StrikeKind = strikeKind,
            HitResult = hitResult,
            Damage = damage,
            TargetRemainingHealth = targetRemainingHealth,
        };
    }
}

// TurnNumber tur sayacıdır. Sequence aynı tur içindeki sıra numarasıdır. Bir hareket iki vuruş üretebilir; Normal önce, Technique sonra. FE ve geçmiş okuması TurnNumber sonra Sequence ile sıralar.
//Damage uygulanan hasardır.TargetRemainingHealth vuruştan sonra hedefin kalan canıdır.Can çubuğu bu alana bakarak iner.
//Hasar formülü henüz burada değil.Formül Application handler’ında hesaplanır; sonuç bu satıra yazılır.Domain yalnız “ne kaydedildi”yi taşır.