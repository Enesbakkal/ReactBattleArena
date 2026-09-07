using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Domain.Authorization;
using ReactBattleArena.Domain.Characters;
using ReactBattleArena.Domain.Users;
using ReactBattleArena.Domain.Authentication;
namespace ReactBattleArena.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

//Neden interface? Handler’ın ApplicationDbContext’e (Infrastructure sınıfı) bağlı kalmasını istemedik.
//.csproj içindeki <ProjectReference> şu anlama gelir: bu proje, işaret ettiği projenin public tiplerini kullanabilir.
//Application.csproj yalnızca Domain’e bakıyor; Infrastructure satırı yok.
//Infrastructure.csproj ise Application’a bakıyor — bu yüzden Infrastructure,
//Application’ın IApplicationDbContext’ini görür ve class ApplicationDbContext : IApplicationDbContext yazabilir.