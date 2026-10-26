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
                u.IsContactPublic || u.Id == request.CallerId ? u.Email : null,
                //request.CallerId EF için bir SQL parametresidir.Koşul satırın içindedir.u.Id == request.CallerId kendi satırındır.
                //Kendi e - postan bayrak false olsa da gelir.Başkasında IsContactPublic true değilse iki alan null olur.
                u.DisplayName,
                u.Points,
                u.CreatedAtUtc,
                u.FirstName,
                u.LastName,
                u.IsContactPublic || u.Id == request.CallerId ? u.Phone : null,
                u.AvatarUrl,
                u.IsDisabled,
                u.IsContactPublic,
                _db.UserRoles //Bu, Selectin içinde ikinci bir sorgudur. EF bunu SQL’de alt sorguya çevirir. Döngü içinde ayrıca GET atılmaz. page ve pageSize aynı kalır. UsersController.GetPaged yine Ok(result) döner.
                    .Where(ur => ur.UserId == u.Id)
                    .Join(
                        _db.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => r.Name)
                    .OrderBy(name => name)
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PagedUserRowsResult(items, total);
    }
}