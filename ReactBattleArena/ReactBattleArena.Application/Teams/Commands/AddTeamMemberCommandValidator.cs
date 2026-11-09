using FluentValidation;
using ReactBattleArena.Domain.Teams;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.OwnerUserId).NotEmpty();
        RuleFor(x => x.CharacterId).NotEmpty();
        RuleFor(x => x.SlotIndex).InclusiveBetween(0, Team.SlotCount - 1);
        // SlotIndex 0–4 dışı handler’a inmeden 400 olur. Domain Create içindeki ArgumentOutOfRangeException yedek güvenlik; asıl kapı validator’dır.
    }
}