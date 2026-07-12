using MediatR;

namespace ReactBattleArena.Application.Users.Queries;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDetailDto?>;

public sealed record UserDetailDto(
    Guid Id,
    string UserName,
    string Email,
    string? DisplayName,
    int Points,
    DateTime CreatedAtUtc);