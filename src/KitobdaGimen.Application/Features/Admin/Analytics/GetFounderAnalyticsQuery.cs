using MediatR;

namespace KitobdaGimen.Application.Features.Admin.Analytics;

/// <summary>
/// Founder analytics dashboard data. SuperAdmin only (gated in the handler).
/// </summary>
public record GetFounderAnalyticsQuery : IRequest<FounderAnalyticsDto>;
