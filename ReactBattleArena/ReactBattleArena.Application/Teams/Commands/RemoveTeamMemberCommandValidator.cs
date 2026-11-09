using FluentValidation;
using ReactBattleArena.Domain.Teams;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed class RemoveTeamMemberCommandValidator : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.OwnerUserId).NotEmpty();
        RuleFor(x => x.SlotIndex).InclusiveBetween(0, Team.SlotCount - 1);
    }
}