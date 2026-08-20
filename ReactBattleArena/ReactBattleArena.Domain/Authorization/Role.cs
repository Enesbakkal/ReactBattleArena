namespace ReactBattleArena.Domain.Authorization;

public sealed class Role
{
    private Role()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public static Role Create(string name)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name
        };
    }
}