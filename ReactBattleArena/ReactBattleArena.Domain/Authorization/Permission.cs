namespace ReactBattleArena.Domain.Authorization;

public sealed class Permission
{
    private Permission()
    {
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public static Permission Create(string code)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Code = code
        };
    }
}