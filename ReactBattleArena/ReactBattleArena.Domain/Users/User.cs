namespace ReactBattleArena.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    public string UserName { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string? DisplayName { get; private set; }

    // Arena / ödül için; Auth sonrası da kullanılacak
    public int Points { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static User Create(
        string userName,
        string email,
        string? displayName,
        DateTime utcNow)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            DisplayName = displayName,
            Points = 0,
            CreatedAtUtc = utcNow
        };
    }

    public void Update(string userName, string email, string? displayName)
    {
        UserName = userName;
        Email = email;
        DisplayName = displayName;
    }

    public void AddPoints(int amount)
    {
        if (amount <= 0)
            return;

        Points += amount;
    }
}