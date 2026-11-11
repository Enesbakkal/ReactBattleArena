namespace ReactBattleArena.Domain.Battles;

public enum BattleSide
{
    Initiator = 0,
    Opponent = 1,
}

public enum BattleOutcome
{
    InitiatorWin = 0,
    OpponentWin = 1,
    Draw = 2,
}

public enum StrikeKind
{
    Normal = 0,
    Technique = 1,
}

public enum HitResult
{
    FullHit = 0,
    PartialHit = 1,
    FullDodge = 2,
}

// BattleSide iki cepheyi ayırır: dövüşü başlatan taraf ve rakip. Slot numarası tek başına yetmez; iki tarafta da 0–4 yer vardır.
//BattleOutcome sonucun kimden görüldüğünü sabitler.Başlatan kazandı, rakip kazandı veya berabere.Puan yazımı sonra bu değere bakacak.
//StrikeKind bir hareketin iki vuruşundan hangisi olduğunu söyler. Önce BaseAttack (Normal), sonra BaseTechnique (Technique).
//HitResult isabet sonucudur.Kura yoktur. Accuracy ≥ Foresight ise FullHit.Foresight büyük ve Foresight ≥ 2×Accuracy ise FullDodge (hasar 0). 
//Foresight büyük ama iki katına yetmiyorsa PartialHit(hasar yarı). Ayrı bir Miss değeri yok; kural bu üç hâli kapsar.