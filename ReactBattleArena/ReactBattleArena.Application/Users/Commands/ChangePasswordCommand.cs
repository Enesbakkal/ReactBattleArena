using MediatR;

namespace ReactBattleArena.Application.Users.Commands;

public sealed record ChangePasswordCommand(
    Guid Id,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest<bool>;