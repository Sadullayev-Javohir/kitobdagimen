using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Users.Dtos;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Users.Queries.SearchUsers;

public class SearchUsersQueryHandler
    : IRequestHandler<SearchUsersQuery, PagedResult<UserSearchResultDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SearchUsersQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<UserSearchResultDto>> Handle(
        SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 50 ? 20 : request.PageSize;
        var term = (request.Q ?? "").Trim();

        var query = _db.Users.Where(u => u.Id != userId);

        if (term.Length > 0)
        {
            var lowered = term.ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(lowered)
                || (u.Username != null && u.Username.ToLower().Contains(lowered)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var now = DateTime.UtcNow;

        // Project users plus the raw connection info needed to derive the relationship state.
        var rows = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.FullName,
                u.AvatarUrl,
                u.Bio,
                u.LastSeenAt,
                HasStory = u.Stories.Any(s => s.ExpiresAt > now),
                Connection = _db.Connections
                    .Where(c => (c.RequesterId == userId && c.AddresseeId == u.Id)
                                || (c.RequesterId == u.Id && c.AddresseeId == userId))
                    .Select(c => new { c.Id, c.Status, c.RequesterId })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r =>
        {
            var state = ConnectionState.None;
            int? connId = null;
            if (r.Connection is not null)
            {
                connId = r.Connection.Id;
                state = r.Connection.Status switch
                {
                    ConnectionStatus.Accepted => ConnectionState.Connected,
                    ConnectionStatus.Pending => r.Connection.RequesterId == userId
                        ? ConnectionState.PendingOutgoing
                        : ConnectionState.PendingIncoming,
                    _ => ConnectionState.None // Declined → can invite again
                };
                if (state == ConnectionState.None) connId = null;
            }

            return new UserSearchResultDto
            {
                Id = r.Id,
                Username = r.Username,
                FullName = r.FullName,
                AvatarUrl = r.AvatarUrl,
                Bio = r.Bio,
                HasStory = r.HasStory,
                LastSeenAt = r.LastSeenAt,
                ConnectionState = state,
                ConnectionId = connId
            };
        }).ToList();

        return PagedResult<UserSearchResultDto>.Create(items, page, pageSize, totalCount);
    }
}
