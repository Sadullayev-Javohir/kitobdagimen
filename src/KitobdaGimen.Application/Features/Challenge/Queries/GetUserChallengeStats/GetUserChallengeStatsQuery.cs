using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetUserChallengeStats;

/// <summary>
/// Foydalanuvchining o'qish statistikasi: so'nggi 30 kunlik (kunlik betlar) va so'nggi
/// 12 oylik (oylik betlar/kitoblar). three.js 3D grafikada ko'rsatiladi.
/// </summary>
public record GetUserChallengeStatsQuery(int UserId) : IRequest<UserChallengeStatsDto>;
