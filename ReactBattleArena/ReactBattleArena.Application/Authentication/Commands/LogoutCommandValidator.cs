using FluentValidation;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}