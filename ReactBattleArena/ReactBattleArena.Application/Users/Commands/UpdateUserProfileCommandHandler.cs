using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Application.Users.Commands;

public sealed class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, bool>
{
    private readonly IApplicationDbContext _db;

    public UpdateUserProfileCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (entity is null)
            return false;

        var emailTaken = await _db.Users.AnyAsync(
            u => u.Id != request.Id && u.Email == request.Email,
            cancellationToken);

        if (emailTaken)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Email), "Email is already taken.")
            });

        entity.UpdateProfile(
            string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim(),
            string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
            string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            request.IsContactPublic);

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}