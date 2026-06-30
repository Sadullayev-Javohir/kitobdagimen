namespace KitobdaGimen.Application.Features.Leaderboard.Dtos;

/// <summary>Leaderboard'dagi foydalanuvchi ma'lumotlari.</summary>
public record LeaderboardUserDto
{
    public int UserId { get; init; }
    public string FullName { get; init; } = null!;
    public string? Username { get; init; }
    public string? AvatarUrl { get; init; }
    
    /// <summary>Foydalanuvchining reytingdagi o'rni (1-birinchi, 2-ikkinchi...)</summary>
    public int Rank { get; init; }
    
    /// <summary>Statistika - betlar soni, kitoblar soni yoki postlar/iqtiboslar soni.</summary>
    public int Score { get; init; }
    
    /// <summary>Qo'shimcha ma'lumot (masalan, "5 kitob" yoki "12 post").</summary>
    public string? Detail { get; init; }
}
