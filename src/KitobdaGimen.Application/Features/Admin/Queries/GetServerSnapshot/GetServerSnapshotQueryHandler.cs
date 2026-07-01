using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Admin.Monitoring;
using KitobdaGimen.Domain.Enums;
using MediatR;

namespace KitobdaGimen.Application.Features.Admin.Queries.GetServerSnapshot;

public class GetServerSnapshotQueryHandler : IRequestHandler<GetServerSnapshotQuery, ServerSnapshot?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IServerMetricsStore _store;

    public GetServerSnapshotQueryHandler(
        IAppDbContext db, ICurrentUserService currentUser, IServerMetricsStore store)
    {
        _db = db;
        _currentUser = currentUser;
        _store = store;
    }

    public async Task<ServerSnapshot?> Handle(GetServerSnapshotQuery request, CancellationToken cancellationToken)
    {
        // Server internals are SuperAdmin-only (more sensitive than the user list).
        await AdminGuard.RequireAsync(_db, _currentUser, UserRole.SuperAdmin, cancellationToken);
        return _store.Latest;
    }
}
