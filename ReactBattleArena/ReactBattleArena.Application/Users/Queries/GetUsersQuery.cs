using MediatR;

namespace ReactBattleArena.Application.Users.Queries;

public sealed record GetUsersQuery(int Page, int PageSize, Guid CallerId)
    : IRequest<PagedUserRowsResult>;

public sealed record UserRowDto(
    Guid Id,
    string UserName,
    string? Email,
    string? DisplayName,
    int Points,
    DateTime CreatedAtUtc,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? AvatarUrl,
    bool IsDisabled,
    bool IsContactPublic,
    IReadOnlyList<string> Roles);

public sealed record PagedUserRowsResult(
    IReadOnlyList<UserRowDto> Items,
    int TotalCount);