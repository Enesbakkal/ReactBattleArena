using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Domain.Characters;
using ReactBattleArena.Domain.Users;
namespace ReactBattleArena.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}