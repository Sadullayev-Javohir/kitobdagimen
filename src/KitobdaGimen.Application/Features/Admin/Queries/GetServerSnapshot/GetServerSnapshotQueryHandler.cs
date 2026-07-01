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
        // Server health is visible to Admin and SuperAdmin. The snapshot contains only aggregate
        // numbers and component states — no secrets (connection strings, keys) or PII.
        await AdminGuard.RequireAsync(_db, _currentUser, UserRole.Admin, cancellationToken);
        return _store.Latest;
    }
}
