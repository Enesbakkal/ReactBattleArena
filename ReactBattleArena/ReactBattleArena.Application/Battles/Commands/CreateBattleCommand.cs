using MediatR;
using ReactBattleArena.Domain.Teams;
namespace ReactBattleArena.Application.Battles.Commands;

public sealed record CreateBattleCommand(Guid InitiatorUserId, Guid TeamId)
    : IRequest<CreateBattleResult>;
//Body’de yalnız teamId gelir.Kullanıcı JWT’den okunur. Rakip command’da yoktur; handler seçer.