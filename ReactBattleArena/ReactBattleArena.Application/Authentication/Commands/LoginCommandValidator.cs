using FluentValidation;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        //Handler'dan önce LoginCommandValidator çalışır. Kullanıcı adı veya e-posta boş olamaz. Parola boş olamaz, 6 ile 100 karakter arasındadır.
        RuleFor(x => x.UserNameOrEmail).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        //Kural bozulursa ValidationBehavior ValidationException atar. Middleware bunu 400 ve ValidationProblemDetails yapar.
        //Handler çalışmaz. Ne JWT ne RefreshTokens satırı oluşur.
    }
}