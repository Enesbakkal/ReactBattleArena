using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;
using System.Runtime.Intrinsics.X86;
using static System.Net.WebRequestMethods;

namespace ReactBattleArena.Application.Users.Commands;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (entity is null)
            return false;

        if (!_passwordHasher.Verify(request.CurrentPassword, entity.PasswordHash))
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.CurrentPassword), "Current password is wrong.")
            });

        entity.SetPasswordHash(_passwordHasher.Hash(request.NewPassword));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
    //Verify false ise profildeki "e-posta alınmış" ile aynı yol işler.ValidationException middleware'de 400 ve ValidationProblemDetails olur.
    //Kullanıcı token ile bellidir, bu yüzden yanlış parola burada 400 alan hatasıdır. Login'deki 401, o istekte kimlik henüz yokken kayıtlı olup olmadığını gizler.
    //false dönüşü yalnız token'daki Guid'in Users satırı kalmadığında olur. Controller onu 404 yapar.ConfirmPassword handler'da okunmaz. Eşitliği validator bitirmiştir.
    //Hash yalnız Verify geçince çağrılır.
}