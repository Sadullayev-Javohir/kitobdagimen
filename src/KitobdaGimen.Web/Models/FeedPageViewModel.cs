using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Auth.Dtos;
using KitobdaGimen.Application.Features.Challenge.Dtos;
using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;
using KitobdaGimen.Application.Features.Posts.Queries.GetFeed;
using KitobdaGimen.Application.Features.Profile.Dtos;
using KitobdaGimen.Application.Features.ReadingGoals.Dtos;

namespace KitobdaGimen.Web.Models;

public class FeedPageViewModel
{
    public PagedResult<FeedItemDto> Feed { get; init; } = null!;
    public UserDto? CurrentUser { get; init; }
    public ProfileDto? UserProfile { get; init; }
    public ReadingGoalDto? ActiveGoal { get; init; }
    public IReadOnlyList<ChallengeStandingDto> TopReaders { get; init; } = Array.Empty<ChallengeStandingDto>();
    public IReadOnlyList<PhysicalBookDto> ExchangeBooks { get; init; } = Array.Empty<PhysicalBookDto>();
    public string? Search { get; init; }
    public bool IsAdmin { get; init; }
}
