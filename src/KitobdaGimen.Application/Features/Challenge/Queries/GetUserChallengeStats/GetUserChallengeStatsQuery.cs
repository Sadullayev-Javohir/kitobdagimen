using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetUserChallengeStats;

/// <summary>
/// Foydalanuvchining o'qish statistikasi: so'nggi 30 kunlik (kunlik betlar/kitoblar) va
/// joriy yil bo'yicha to'liq kalendar (GitHub uslubidagi heatmap) + mavjud yillar ro'yxati.
/// </summary>
public record GetUserChallengeStatsQuery(int UserId) : IRequest<UserChallengeStatsDto>;
