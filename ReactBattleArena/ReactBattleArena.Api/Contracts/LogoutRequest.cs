namespace ReactBattleArena.Api.Contracts;

public sealed class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}