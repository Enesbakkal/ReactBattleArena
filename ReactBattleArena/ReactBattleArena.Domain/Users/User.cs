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

    public string PasswordHash { get; private set; } = null!;

    // Arena / ödül için; Auth sonrası da kullanılacak
    public int Points { get; private set; }

    public string Role { get; private set; } = null!;
    //= null!; = “derleyiciye: başlangıçta null görünebilir ama runtime’da asla null kalmayacak” demek.
    //null-forgiving (!) işareti

    public DateTime CreatedAtUtc { get; private set; }

    public static User Create(
        string userName,
        string email,
        string? displayName,
        string passwordHash,
        string role,
        DateTime utcNow)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Role = role,
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

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void AddPoints(int amount)
    {
        if (amount <= 0)
            return;

        Points += amount;
    }

    public void SetRole(string role)
    {
        Role = role;
    }
}