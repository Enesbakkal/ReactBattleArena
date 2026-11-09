using MediatR;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed record RemoveTeamMemberCommand(
    Guid TeamId,
    Guid OwnerUserId,
    int SlotIndex) : IRequest<bool>;