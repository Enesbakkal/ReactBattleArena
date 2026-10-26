using FluentValidation;

namespace ReactBattleArena.Application.Users.Commands;

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.AvatarUrl).MaximumLength(500);
    }
    // Uzunluklar UserConfiguration ile aynıdır. AddValidatorsFromAssembly bu sınıfı kendisi bulur.
}