using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Application.Users.Queries;

public sealed class GetUserByIdQueryHandler
    : IRequestHandler<GetUserByIdQuery, UserDetailDto?>
{
    private readonly IApplicationDbContext _db;

    public GetUserByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserDetailDto?> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Users
        .AsNoTracking()
        .Where(u => u.Id == request.Id)
        .Select(u => new UserDetailDto(
                u.Id,
                u.UserName,
                (u.IsContactPublic || u.Id == request.CallerId) ? u.Email : null,
                u.DisplayName,
                u.Points,
                u.CreatedAtUtc,
                u.FirstName,
                u.LastName,
                (u.IsContactPublic || u.Id == request.CallerId) ? u.Phone : null,
                u.AvatarUrl,
                u.IsDisabled,
                u.IsContactPublic,
                _db.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(
                        _db.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => r.Name)
                    .OrderBy(name => name)
                    .ToList()))
        .FirstOrDefaultAsync(cancellationToken);
    }
}