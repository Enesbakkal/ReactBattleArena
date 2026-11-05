using MediatR;

namespace ReactBattleArena.Application.Teams.Queries;

public sealed record GetMyTeamsQuery(Guid OwnerUserId) : IRequest<IReadOnlyList<TeamRowDto>>;

public sealed record TeamSlotDto(
    int SlotIndex,
    Guid? CharacterId,
    string? CharacterName,
    string? ImageUrl);

public sealed record TeamRowDto(
    Guid Id,
    string Name,
    DateTime CreatedAtUtc,
    IReadOnlyList<TeamSlotDto> Slots);