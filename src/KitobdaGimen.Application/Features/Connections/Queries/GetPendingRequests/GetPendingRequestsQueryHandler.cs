using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Connections.Dtos;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Connections.Queries.GetPendingRequests;

public class GetPendingRequestsQueryHandler
    : IRequestHandler<GetPendingRequestsQuery, IReadOnlyList<ConnectionDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPendingRequestsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ConnectionDto>> Handle(
        GetPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        return await _db.Connections
            .Where(c => c.AddresseeId == userId && c.Status == ConnectionStatus.Pending)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ConnectionDto
            {
                Id = c.Id,
                Status = c.Status,
                IamRequester = false,
                CreatedAt = c.CreatedAt,
                RespondedAt = c.RespondedAt,
                OtherUser = new UserSummaryDto
                {
                    Id = c.Requester.Id,
                    Username = c.Requester.Username,
                    FullName = c.Requester.FullName,
                    AvatarUrl = c.Requester.AvatarUrl
                }
            })
            .ToListAsync(cancellationToken);
    }
}
