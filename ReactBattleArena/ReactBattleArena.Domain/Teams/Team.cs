namespace ReactBattleArena.Domain.Teams;

public sealed class Team
{
    public const int SlotCount = 5;

    private Team()
    {
    }

    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    public static Team Create(Guid ownerUserId, string name, DateTime utcNow)
    {
        return new Team
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = name,
            CreatedAtUtc = utcNow,
        };
    }

    public void Rename(string name)
    {
        Name = name;
    }
}