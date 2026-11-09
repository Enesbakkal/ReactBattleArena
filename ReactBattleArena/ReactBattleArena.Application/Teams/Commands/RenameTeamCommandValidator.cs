using FluentValidation;
namespace ReactBattleArena.Application.Teams.Commands;

public sealed class RenameTeamCommandValidator : AbstractValidator<RenameTeamCommand>
{
    public RenameTeamCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.OwnerUserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
    // Pipeline’daki ValidationBehavior bu kuralları handler’dan önce çalıştırır.
}