using Microsoft.AspNetCore.Authorization;

namespace ReactBattleArena.Api.Authorization;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = "Permission:" + permission;
    }
}