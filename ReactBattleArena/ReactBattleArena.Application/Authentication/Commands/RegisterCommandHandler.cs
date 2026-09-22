using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Authorization;
using ReactBattleArena.Domain.Users;
using System.Numerics;
using static System.Net.WebRequestMethods;

namespace ReactBattleArena.Application.Authentication.Commands;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var userNameTaken = await _db.Users.AnyAsync(u => u.UserName == request.UserName, cancellationToken);

        if (userNameTaken)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.UserName), "UserName is already taken.")
            });

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailTaken)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Email), "Email is already taken.")
            });

        var passwordHash = _passwordHasher.Hash(request.Password);

        var entity = User.Create(
            request.UserName,
            request.Email,
            request.DisplayName,
            passwordHash,
            DateTime.UtcNow);

        _db.Users.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var playerRole = await _db.Roles.SingleAsync(
            r => r.Name == Roles.Player, cancellationToken);
        //SingleAsync Player satırını bulamazsa exception fırlatır. Bu ValidationException değildir.
        //Middleware onu 400 yapmaz. Roles tablosunda Player yoksa kayıt 500 olur.
        //Rol yoksa (seed çalışmamış) sessizce geçme, patlat ki fark edesin.

        _db.UserRoles.Add(UserRole.Create(entity.Id, playerRole.Id));
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;

        //Player'ın hangi fiile sahip olduğu, RolePermissions satırı ve permission kodu bu cevapta yoktur. O bağ karakter ekleme işindedir

        //İlk SaveChangesAsync kullanıcıyı yazar. Ondan sonra handler Roles tablosunda adı Player olan satırı arar.
        //Bulduğu rolün Id değeri ile yeni kullanıcının Id değerinden bir UserRoles satırı kurar.İkinci SaveChangesAsync bu bağı yazar.
        //Dönüş, kullanıcının Guid değeridir.
    }
}
