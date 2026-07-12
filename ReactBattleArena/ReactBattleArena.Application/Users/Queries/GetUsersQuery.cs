using MediatR;

namespace ReactBattleArena.Application.Users.Queries;

public sealed record GetUsersQuery(int Page, int PageSize)
    : IRequest<PagedUserRowsResult>;

public sealed record UserRowDto(
    Guid Id,
    string UserName,
    string Email,
    string? DisplayName,
    int Points,
    DateTime CreatedAtUtc);

public sealed record PagedUserRowsResult(
    IReadOnlyList<UserRowDto> Items,
    int TotalCount);