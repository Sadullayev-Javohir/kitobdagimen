using KitobdaGimen.Application.Features.Leaderboard.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Leaderboard.Queries.GetReadingLeaderboard;

/// <summary>
/// Kitob o'qish bo'yicha leaderboard - foydalanuvchilar o'qigan betlar soni bo'yicha.
/// Period: Daily (kunlik), Weekly (haftalik), Monthly (oylik), AllTime (umrlik).
/// </summary>
public record GetReadingLeaderboardQuery : IRequest<IReadOnlyList<LeaderboardUserDto>>
{
    public LeaderboardPeriod Period { get; init; } = LeaderboardPeriod.Weekly;
    
    /// <summary>Top nechta foydalanuvchini qaytarish (default: 50).</summary>
    public int Limit { get; init; } = 50;
}

/// <summary>Leaderboard davri.</summary>
public enum LeaderboardPeriod
{
    Daily,     // Bugun
    Weekly,    // So'nggi 7 kun
    Monthly,   // So'nggi 30 kun
    AllTime    // Barchasi
}
