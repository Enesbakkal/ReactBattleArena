namespace ReactBattleArena.Domain.Battles;

public sealed class Battle
{
    public const int MaxTurns = 30;

    private Battle()
    {
    }

    public Guid Id { get; private set; }
    public Guid InitiatorUserId { get; private set; }
    public Guid InitiatorTeamId { get; private set; }
    public Guid? OpponentUserId { get; private set; }
    public Guid? OpponentTeamId { get; private set; }
    public BattleOutcome Outcome { get; private set; }
    public int TurnCount { get; private set; }
    public bool IsHighlighted { get; private set; }
    public bool IsSystemOpponent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime FinishedAtUtc { get; private set; }

    public static Battle Create(
        Guid initiatorUserId,
        Guid initiatorTeamId,
        Guid? opponentUserId,
        Guid? opponentTeamId,
        bool isSystemOpponent,
        BattleOutcome outcome,
        int turnCount,
        DateTime utcNow)
    {
        return new Battle
        {
            Id = Guid.NewGuid(),
            InitiatorUserId = initiatorUserId,
            InitiatorTeamId = initiatorTeamId,
            OpponentUserId = opponentUserId,
            OpponentTeamId = opponentTeamId,
            IsSystemOpponent = isSystemOpponent,
            Outcome = outcome,
            TurnCount = turnCount,
            IsHighlighted = false,
            CreatedAtUtc = utcNow,
            FinishedAtUtc = utcNow,
        };
    }
}

// Tek dövüş satırı. Özet buradadır; vuruş listesi ayrı tablodadır.
// Team ile aynı kalıp: private constructor, Create factory, property’ler dışarıdan set edilmez.
// OpponentUserId ve OpponentTeamId nullable. Uygun kullanıcı takımı yoksa katalogdan sistem dizilimi kurulur.
// O durumda takım satırı yoktur; kullanıcı da yoktur. IsSystemOpponent = true olur.
// Sistem dizilimine karşı puan yazılmaz; o kural handler’da bu bayrağa bakacak.
// MaxTurns = 30 süre sayacı değildir. Kimse düşmezse hesap kesilir; kalan can toplamı büyük olan kazanır, eşitse berabere.
// IsHighlighted şimdilik hep false. /arena/highlights sonra bu bayrağı okuyacak; kim işaretler ayrı iş.
// Bu satır POST /api/battles cevabının özet kısmına denk gelir. Liste GET /api/battles aynı tablodan Skip/Take ile okunur.