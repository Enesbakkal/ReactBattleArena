using FluentValidation;

namespace ReactBattleArena.Application.Users.Commands;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword);
    }
}