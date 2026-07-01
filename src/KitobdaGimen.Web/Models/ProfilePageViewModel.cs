using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Posts.Dtos;
using KitobdaGimen.Application.Features.Profile.Dtos;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using KitobdaGimen.Application.Features.Stories.Dtos;

namespace KitobdaGimen.Web.Models;

/// <summary>View model for the profile page: the profile, the user's paged posts, finished books and story history.</summary>
public class ProfilePageViewModel
{
    public ProfileDto Profile { get; init; } = null!;
    public PagedResult<PostDto> Posts { get; init; } = null!;
    public IReadOnlyList<ReadingGoalDto> FinishedBooks { get; init; } = Array.Empty<ReadingGoalDto>();

    /// <summary>
    /// All of the profile owner's active reading goals — the same "Faol kitoblarim" list shown on
    /// /reading-books. Rendered in full in the profile "Hozir o'qiyapti" section.
    /// </summary>
    public IReadOnlyList<ReadingGoalDto> CurrentBooks { get; init; } = Array.Empty<ReadingGoalDto>();

    /// <summary>All of the profile owner's stories (newest first), including expired ones.</summary>
    public IReadOnlyList<StoryDto> Stories { get; init; } = Array.Empty<StoryDto>();

    /// <summary>The profile owner's quotes (public — shown on both /profile and /u/{username}).</summary>
    public IReadOnlyList<QuoteDto> MyQuotes { get; init; } = Array.Empty<QuoteDto>();

    /// <summary>Challenge g'oliblik tarixi (qaysi oylarda g'olib bo'lgan) — profilda ko'rsatiladi.</summary>
    public IReadOnlyList<KitobdaGimen.Application.Features.Challenge.Dtos.UserChallengeWinDto> ChallengeWins { get; init; }
        = Array.Empty<KitobdaGimen.Application.Features.Challenge.Dtos.UserChallengeWinDto>();
}
