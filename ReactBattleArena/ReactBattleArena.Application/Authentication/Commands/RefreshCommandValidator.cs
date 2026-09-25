using FluentValidation;
using static System.Net.WebRequestMethods;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
        //Bu sınıfı Program.cs'e kaydetmeyeceksin; AddValidatorsFromAssembly zaten assembly'yi tarıyor.
        //Boş gövde gelirse 400 döner, 401 değil — "fiş yanlış" ile "fiş hiç yok" ayrı şeyler.
        // Boş gövde 400 olur. 401 olmaz. 401 satır bulunamadı, iptal edildi veya süresi doldu demektir.
    }
}