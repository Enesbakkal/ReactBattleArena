using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Domain.Authorization;

namespace ReactBattleArena.Infrastructure.Persistence;

public static class AuthSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(db, Roles.Admin, cancellationToken);
        await EnsureRoleAsync(db, Roles.Player, cancellationToken);
        await EnsureRoleAsync(db, Roles.ShopOwner, cancellationToken);

        await EnsurePermissionAsync(db, PermissionCodes.CharactersCreate, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.CharactersUpdate, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.CharactersDelete, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.ShopItemsCreate, cancellationToken);
        await EnsurePermissionAsync(db, PermissionCodes.UsersDelete, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersCreate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersUpdate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.CharactersDelete, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.ShopItemsCreate, cancellationToken);
        await EnsureRolePermissionAsync(db, Roles.Admin, PermissionCodes.UsersDelete, cancellationToken);

        await EnsureRolePermissionAsync(db, Roles.ShopOwner, PermissionCodes.ShopItemsCreate, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureRoleAsync(
        ApplicationDbContext db,
        string name,
        CancellationToken cancellationToken)
    {
        if (!await db.Roles.AnyAsync(r => r.Name == name, cancellationToken))
            db.Roles.Add(Role.Create(name));
    }

    private static async Task EnsurePermissionAsync(
        ApplicationDbContext db,
        string code,
        CancellationToken cancellationToken)
    {
        if (!await db.Permissions.AnyAsync(p => p.Code == code, cancellationToken))
            db.Permissions.Add(Permission.Create(code));
    }

    private static async Task EnsureRolePermissionAsync(
        ApplicationDbContext db,
        string roleName,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleAsync(r => r.Name == roleName, cancellationToken);
        var permission = await db.Permissions.SingleAsync(p => p.Code == permissionCode, cancellationToken);

        var exists = await db.RolePermissions.AnyAsync(
            x => x.RoleId == role.Id && x.PermissionId == permission.Id,
            cancellationToken);

        if (!exists)
            db.RolePermissions.Add(RolePermission.Create(role.Id, permission.Id));
    }
}