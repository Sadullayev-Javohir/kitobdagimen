using MediatR;

namespace KitobdaGimen.Application.Features.Home.Queries.GetLandingStats;

/// <summary>
/// Landing sahifa statistikasi. Odatda kunlik snapshot'dan o'qiydi; snapshot eskirgan
/// (boshqa kunga tegishli) yoki <paramref name="ForceRefresh"/> bo'lsa qayta hisoblab saqlaydi.
/// <c>ForceRefresh</c> — /admin dagi super admin "yangilash" tugmasi uchun.
/// </summary>
public record GetLandingStatsQuery(bool ForceRefresh = false) : IRequest<LandingStatsDto>;
