namespace ReactBattleArena.Api.Contracts;

public sealed class AddTeamMemberRequest
{
    public Guid CharacterId { get; set; }
    public int SlotIndex { get; set; }
}