using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Infrastructure.Persistence;

public sealed class UserPermissionService : IUserPermissionService
{
    private readonly ApplicationDbContext _db;

    public UserPermissionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> GetCodesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from ur in _db.UserRoles
            join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
            join p in _db.Permissions on rp.PermissionId equals p.Id
            where ur.UserId == userId
            select p.Code
        ).Distinct().ToListAsync(cancellationToken);
    }
}