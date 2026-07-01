using KitobdaGimen.Application.Features.Admin.Monitoring;
using MediatR;

namespace KitobdaGimen.Application.Features.Admin.Queries.GetServerSnapshot;

/// <summary>Latest server-health snapshot for the monitoring dashboard (SuperAdmin only).</summary>
public record GetServerSnapshotQuery : IRequest<ServerSnapshot?>;
