using MediatR;

namespace ReactBattleArena.Application.Users.Queries;

public sealed record GetUserByIdQuery(Guid Id, Guid CallerId) : IRequest<UserDetailDto?>;

public sealed record UserDetailDto(
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