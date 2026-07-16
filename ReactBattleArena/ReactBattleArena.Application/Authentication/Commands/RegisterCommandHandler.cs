using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;
using ReactBattleArena.Domain.Users;

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

        return entity.Id;
    }
}
