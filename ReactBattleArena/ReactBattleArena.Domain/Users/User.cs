using static System.Net.WebRequestMethods;

namespace ReactBattleArena.Domain.Users;

public sealed class User
{
    private User()
    {
    }
    // User.Create yeni bir Guid üretir, puanı 0 yapar, PasswordHash alanına BCrypt çıktısını yazar.
    // Entity'nin boş constructor'ı EF içindir.Uygulama kodu Create ile nesne kurar.

    public Guid Id { get; private set; }

    public string UserName { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string? DisplayName { get; private set; }
    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? Phone { get; private set; }

    public string? AvatarUrl { get; private set; }

    public bool IsDisabled { get; private set; }

    public bool IsContactPublic { get; private set; }

    public string PasswordHash { get; private set; } = null!;

    // Arena / ödül için; Auth sonrası da kullanılacak
    public int Points { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static User Create(
        string userName,
        string email,
        string? displayName,
        string passwordHash,
        DateTime utcNow)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            DisplayName = displayName,
            FirstName = null,
            LastName = null,
            Phone = null,
            AvatarUrl = null,
            IsDisabled = false,
            IsContactPublic = false,
            PasswordHash = passwordHash,
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

    public void UpdateProfile(
    string? firstName,
    string? lastName,
    string? phone,
    string email,
    string? avatarUrl,
    bool isContactPublic)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Email = email;
        AvatarUrl = avatarUrl;
        IsContactPublic = isContactPublic;
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
}