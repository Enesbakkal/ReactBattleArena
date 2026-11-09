using MediatR;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed record AddTeamMemberCommand(
    Guid TeamId,
    Guid OwnerUserId,
    Guid CharacterId,
    int SlotIndex) : IRequest<AddTeamMemberResult>;