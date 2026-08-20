using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Domain.Authorization;
using ReactBattleArena.Domain.Characters;
using ReactBattleArena.Domain.Users;
namespace ReactBattleArena.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}