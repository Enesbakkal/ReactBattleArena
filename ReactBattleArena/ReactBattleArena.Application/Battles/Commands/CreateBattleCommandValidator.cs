using FluentValidation;

namespace ReactBattleArena.Application.Battles.Commands;

public sealed class CreateBattleCommandValidator : AbstractValidator<CreateBattleCommand>
{
    public CreateBattleCommandValidator()
    {
        RuleFor(x => x.InitiatorUserId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
    }
}
//“5 slot dolu mu” FluentValidation’da değil; handler DB’ye bakıp TeamIncomplete döner. Validator yalnız boş Guid’i keser.