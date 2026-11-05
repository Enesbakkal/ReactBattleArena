namespace ReactBattleArena.Domain.Teams;

public sealed class TeamMember
{
    private TeamMember()
    {
    }

    public Guid TeamId { get; private set; }
    public Guid CharacterId { get; private set; }
    public int SlotIndex { get; private set; }

    public static TeamMember Create(Guid teamId, Guid characterId, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Team.SlotCount)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));

        return new TeamMember
        {
            TeamId = teamId,
            CharacterId = characterId,
            SlotIndex = slotIndex,
        };
    }
}