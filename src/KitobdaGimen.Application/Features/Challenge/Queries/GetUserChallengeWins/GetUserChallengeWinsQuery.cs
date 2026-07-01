using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetUserChallengeWins;

/// <summary>Foydalanuvchi qaysi oylarda challenge g'olibi bo'lganini qaytaradi (profil uchun).</summary>
public record GetUserChallengeWinsQuery(int UserId) : IRequest<IReadOnlyList<UserChallengeWinDto>>;
