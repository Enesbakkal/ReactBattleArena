using MediatR;
using Microsoft.EntityFrameworkCore;
using ReactBattleArena.Application.Abstractions;

namespace ReactBattleArena.Application.Users.Queries;

public sealed class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, PagedUserRowsResult>
{
    private readonly IApplicationDbContext _db;

    public GetUsersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedUserRowsResult> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAtUtc);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserRowDto(
                u.Id,
                u.UserName,
                u.Email,
                u.DisplayName,
                u.Points,
                u.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedUserRowsResult(items, total);
    }
}