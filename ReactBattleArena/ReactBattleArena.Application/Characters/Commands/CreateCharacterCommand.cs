using MediatR;

namespace ReactBattleArena.Application.Characters.Commands;

public sealed record CreateCharacterCommand(
    string Name,
    string Universe,
    string? Biography,
    int Rarity,
    int BaseAttack,
    int BaseDefense,
    int BaseSpeed,
    string? ImageUrl) : IRequest<Guid>;
//IRequest<Guid>
//“Bu istek işlenince Guid döner” (yeni karakterin Id'si)
//Character.Create() ile aynı alanlar(Id ve CreatedAt handler'da eklenir)
// record immutable bir veri taşıyıcısıdır: oluşturulunca alanlar değişmez.
// Entity class’tır çünkü kimliği (Id) vardır ve Update ile değişir.
// Command’da Id ve CreatedAtUtc yok; onları handler Character.Create içinde üretir.
// İstemci “şu Guid ile kaydet” diyemesin diye.
//IRequest<Guid> MediatR sözleşmesi: “bu mesaj işlenince Guid döner.”
//Handler’daki Task<Guid> Handle(...) imzası bunu karşılar. Eski MVC’de karşılığı ActionResult<Guid> gibi bir dönüş tipi olurdu;
//fark şu ki command HTTP bilmez. Aynı command’ı testte, ileride bir background job’da da Send edebilirsin.