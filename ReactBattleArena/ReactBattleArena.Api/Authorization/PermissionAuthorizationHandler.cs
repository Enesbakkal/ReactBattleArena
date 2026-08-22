using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserPermissionService _permissions;

    public PermissionAuthorizationHandler(IUserPermissionService permissions)
    {
        _permissions = permissions;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var idValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idValue, out var userId))
            return;

        var codes = await _permissions.GetCodesAsync(userId);
        if (codes.Contains(requirement.Code))
            context.Succeed(requirement);
    }
}