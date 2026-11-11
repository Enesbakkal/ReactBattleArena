namespace ReactBattleArena.Domain.Battles;

public sealed class BattleFighter
{
    private BattleFighter()
    {
    }

    public Guid BattleId { get; private set; }
    public BattleSide Side { get; private set; }
    public int SlotIndex { get; private set; }

    public Guid? CharacterId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Universe { get; private set; } = null!;
    public int Rarity { get; private set; }

    public int BaseAttack { get; private set; }
    public int BaseDefense { get; private set; }
    public int BaseSpeed { get; private set; }
    public int BaseHealth { get; private set; }
    public int BaseTechnique { get; private set; }
    public int BaseAccuracy { get; private set; }
    public int BaseForesight { get; private set; }

    public static BattleFighter Create(
        Guid battleId,
        BattleSide side,
        int slotIndex,
        Guid? characterId,
        string name,
        string universe,
        int rarity,
        int baseAttack,
        int baseDefense,
        int baseSpeed,
        int baseHealth,
        int baseTechnique,
        int baseAccuracy,
        int baseForesight)
    {
        if (slotIndex < 0 || slotIndex >= 5)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));

        return new BattleFighter
        {
            BattleId = battleId,
            Side = side,
            SlotIndex = slotIndex,
            CharacterId = characterId,
            Name = name,
            Universe = universe,
            Rarity = rarity,
            BaseAttack = baseAttack,
            BaseDefense = baseDefense,
            BaseSpeed = baseSpeed,
            BaseHealth = baseHealth,
            BaseTechnique = baseTechnique,
            BaseAccuracy = baseAccuracy,
            BaseForesight = baseForesight,
        };
    }
}

//BattleFighter, dövüş başladığı andaki karakter verisinin kopyasını tutar: ad, evren, rarity ve yedi güç.Katalogdaki Character satırı sonra değişse bile eski dövüş aynı sayılarla kalır.
//Yedi sayıyı her BattleEvent satırına yazmak tekrar olur. 
//Bu yüzden kopya fighter tablosunda bir kez durur. Olay satırı yalnız Side ve SlotIndex ile hangi fighter’a ait olduğunu söyler. 
//FE can çubuğunun başlangıcını bu satırdaki BaseHealth’ten alır; vuruştan sonra kalan canı olaydaki TargetRemainingHealth ile günceller.

//CharacterId nullable.Sistem dizilimi katalogdan karakter seçtiyse Guid dolar.Katalog satırı silinse bile Name ve Universe fighter satırında kaldığı için geçmiş okunur.

//EF’de anahtar (BattleId, Side, SlotIndex) olacak. Aynı dövüşte iki tarafta da 0–4 yer vardır; yalnız SlotIndex yetmez, taraf da anahtara girer.
//TeamMember’daki (TeamId, SlotIndex) fikri buraya taşınmış hâli.

//TeamMember “bu takımın şu yerinde hangi karakter var” üyelik bağını tutar.BattleFighter “bu dövüşte o yerde hangi ad ve güçlerle duruyordu” kopyasını tutar. İkisi aynı iş değildir.