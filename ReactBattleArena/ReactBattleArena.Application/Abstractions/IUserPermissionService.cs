namespace ReactBattleArena.Application.Abstractions;

public interface IUserPermissionService
{
    Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken = default);
}