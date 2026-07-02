using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Domain.Characters;
namespace ReactBattleArena.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}