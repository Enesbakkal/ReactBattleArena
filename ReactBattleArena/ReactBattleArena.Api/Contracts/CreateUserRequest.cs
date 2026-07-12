namespace ReactBattleArena.Api.Contracts;

public sealed class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}