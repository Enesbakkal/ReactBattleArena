using FluentValidation;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
        // ValidationBehavior boş gövdede 400 döner. Sayfa boş gövde göndermez. Ham string yoksa istek hiç çıkmaz.
    }
}