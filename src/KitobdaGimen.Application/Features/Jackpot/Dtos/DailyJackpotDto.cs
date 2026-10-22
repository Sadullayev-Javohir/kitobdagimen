namespace KitobdaGimen.Application.Features.Jackpot.Dtos;

public class DailyJackpotDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string FormattedDate { get; set; } = null!;
    public string Type { get; set; } = null!; // "post" or "quote"
    public string Title => Type == "post" ? "Kun taqrizi" : "Kun iqtibosi";
    public string BadgeIcon => Type == "post" ? "rate_review" : "format_quote";

    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string UserFullName { get; set; } = null!;
    public string? UserAvatarUrl { get; set; }

    public int BookId { get; set; }
    public string BookTitle { get; set; } = null!;
    public string? BookAuthor { get; set; }
    public string? BookCoverUrl { get; set; }

    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string TargetUrl { get; set; } = null!;

    public int LikeCount { get; set; }
    public bool IsLikedByMe { get; set; }
    public int CommentCount { get; set; }

    public IReadOnlyList<JackpotCommentDto> Comments { get; set; } = Array.Empty<JackpotCommentDto>();
    public DateTime SelectedAt { get; set; }
}

public class JackpotCommentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string UserFullName { get; set; } = null!;
    public string? UserAvatarUrl { get; set; }
    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = null!;
}

public class JackpotLikerDto
{
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string UserFullName { get; set; } = null!;
    public string? UserAvatarUrl { get; set; }
    public DateTime LikedAt { get; set; }
}
