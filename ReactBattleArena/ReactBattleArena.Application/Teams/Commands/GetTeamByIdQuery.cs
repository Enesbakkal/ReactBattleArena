using MediatR;

namespace ReactBattleArena.Application.Teams.Queries;

public sealed record GetTeamByIdQuery(Guid TeamId, Guid OwnerUserId)
    : IRequest<TeamRowDto?>;

// Liste GetMyTeamsQuery ile birçok satır döner. Düzenleme sayfası adres değişince tek takımı ister;
// bu yüzden ayrı query vardır. Cevap tipi TeamRowDto? olur: bulunduysa aynı satır şekli (id, ad, tarih, beş slot), bulunamadıysa veya senin değilse null. 