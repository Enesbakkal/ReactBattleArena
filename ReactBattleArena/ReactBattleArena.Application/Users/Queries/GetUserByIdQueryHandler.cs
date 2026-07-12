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
                u.Email,
                u.DisplayName,
                u.Points,
                u.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }
}