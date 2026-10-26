using MediatR;

namespace ReactBattleArena.Application.Users.Commands;

public sealed record UpdateUserProfileCommand(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? Phone,
    string Email,
    string? AvatarUrl,
    bool IsContactPublic) : IRequest<bool>;
// Id controller’da token’dan doldurulur. İstek gövdesinden gelmez.