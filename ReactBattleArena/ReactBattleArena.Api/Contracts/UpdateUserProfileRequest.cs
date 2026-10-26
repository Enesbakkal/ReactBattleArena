namespace ReactBattleArena.Api.Contracts;

public sealed class UpdateUserProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsContactPublic { get; set; }
}