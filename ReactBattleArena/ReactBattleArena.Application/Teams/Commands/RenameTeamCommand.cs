using MediatR;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed record RenameTeamCommand(Guid TeamId, Guid OwnerUserId, string Name)
    : IRequest<bool>;
// Dönüş bool olmasının nedeni controller’ın “bulundu ve senin” ile “yok veya senin değil” ayrımını 204 / 404’e çevirmesidir.
// Character silmede de aynı bool kalıbı vardı; burada da MediatR cevabı ince tutuluyor.