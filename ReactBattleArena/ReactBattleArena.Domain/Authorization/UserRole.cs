namespace ReactBattleArena .Domain.Authorization;

public sealed class UserRole
{
    private UserRole()
    {

    }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
    }
}