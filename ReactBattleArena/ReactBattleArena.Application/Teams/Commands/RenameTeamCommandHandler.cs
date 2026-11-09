using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Application.Teams.Commands;

public sealed class RenameTeamCommandHandler : IRequestHandler<RenameTeamCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public RenameTeamCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(RenameTeamCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Teams
            .FirstOrDefaultAsync(
                t => t.Id == request.TeamId && t.OwnerUserId == request.OwnerUserId,
                cancellationToken);

        if (entity is null)
            return false;

        entity.Rename(request.Name);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

// Sorguda hem Id hem OwnerUserId vardır. Yalnız Id ile bulup sonra “sahip sen misin?” demek de mümkündür;
// ikisini birlikte yazınca başkasının takımı hiç yüklenmez ve sonuç yine false olur.
// Böylece istemci rastgele bir Guid ile başkasının adını değiştiremez.
// Entity bulunduysa domain’deki Rename yeni adı yazar; EF change tracker değişikliği görür, SaveChangesAsync UPDATE gönderir.
// ASP.NET eşlemesi: action JWT’den kullanıcıyı okur, MediatR’a command verir, handler DbContext ile işi bitirir — iş kuralı controller’da değildir.