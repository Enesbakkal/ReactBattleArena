using Microsoft.AspNetCore.Authorization;

namespace ReactBattleArena.Api.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Code { get; }
    public PermissionRequirement(string code)
    {
        Code = code;
    }
}