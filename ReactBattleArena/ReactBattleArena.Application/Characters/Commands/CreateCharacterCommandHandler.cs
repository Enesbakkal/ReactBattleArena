using MediatR;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Characters;
using System.Reflection.Metadata;
using System.Runtime.ConstrainedExecution;
using static System.Net.WebRequestMethods;

namespace ReactBattleArena.Application.Characters.Commands;

public sealed class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateCharacterCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateCharacterCommand request, CancellationToken cancellationToken)
    {
        var entity = Character.Create(
            request.Name,
            request.Universe,
            request.Biography,
            request.Rarity,
            request.BaseAttack,
            request.BaseDefense,
            request.BaseSpeed,
            request.ImageUrl,
            DateTime.UtcNow);

        _db.Characters.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
//Handler IRequestHandler<CreateCharacterCommand, Guid> uygular: “şu mesaj gelince bu işi yap, Guid dön.”
//Constructor’da IApplicationDbContext ister; ApplicationDbContext sınıf adını yazmaz. Böylece Application,
//SQL Server’a nasıl bağlandığını bilmez.Handle içinde üç iş var.Bir: Character.Create(...) ile entity üret —
//DateTime.UtcNow burada verilir, command’da saat taşımayız.İki: _db.Characters.Add(entity) — bu henüz SQL çalıştırmaz,
//EF’e “bu nesne yeni, INSERT olacak” der.Üç: SaveChangesAsync gerçek INSERT’i gönderir.Dönüş değeri entity.Id;
//7 Temmuz’daki controller bunu 201 Created body’sinde kullanacak.
//CancellationToken isteğin iptal edildiğini handler’a taşır (istemci bağlantıyı kesti, vs.).
//SaveChangesAsync’e iletmezsen iptal SQL’in ortasında kalır.Alışkanlık: token’ı aşağı ver.