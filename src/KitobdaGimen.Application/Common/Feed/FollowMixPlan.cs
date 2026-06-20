namespace KitobdaGimen.Application.Common.Feed;

/// <summary>
/// Plans how a single feed page is blended from two recency-ordered buckets —
/// posts/quotes by people the user follows (plus their own) and posts/quotes by
/// everyone else. The blend follows a 3-followed : 2-non-followed cadence (~60/40),
/// so each page mixes recent items from many users while still leaning on follows.
/// When one bucket runs out, its slots are backfilled from the other so pages never
/// truncate early. The plan is fully deterministic from (page, totals) — paging deeper
/// shows older items with no duplicates or gaps across pages.
/// </summary>
internal static class FollowMixPlan
{
    /// <param name="FollowedSkip">Items to skip in the followed bucket before this page.</param>
    /// <param name="FollowedTake">Items to take from the followed bucket for this page.</param>
    /// <param name="NonFollowedSkip">Items to skip in the non-followed bucket before this page.</param>
    /// <param name="NonFollowedTake">Items to take from the non-followed bucket for this page.</param>
    /// <param name="Order">One entry per slot on the page: <c>true</c> = pull the next followed
    /// item, <c>false</c> = pull the next non-followed item.</param>
    public record Plan(
        int FollowedSkip,
        int FollowedTake,
        int NonFollowedSkip,
        int NonFollowedTake,
        IReadOnlyList<bool> Order);

    public static Plan Build(int page, int pageSize, int followedTotal, int nonFollowedTotal)
    {
        var start = (page - 1) * pageSize;
        var end = start + pageSize;

        int followedUsed = 0, nonFollowedUsed = 0;
        int followedSkip = 0, nonFollowedSkip = 0;
        var order = new List<bool>(pageSize);

        for (var i = 0; i < end && (followedUsed < followedTotal || nonFollowedUsed < nonFollowedTotal); i++)
        {
            var followedLeft = followedUsed < followedTotal;
            var nonFollowedLeft = nonFollowedUsed < nonFollowedTotal;

            // 3 followed : 2 non-followed cadence while both buckets still have items;
            // once one is empty, every remaining slot comes from the other.
            var pickFollowed = followedLeft && nonFollowedLeft ? i % 5 < 3 : followedLeft;

            if (i < start)
            {
                if (pickFollowed) followedSkip++; else nonFollowedSkip++;
            }
            else
            {
                order.Add(pickFollowed);
            }

            if (pickFollowed) followedUsed++; else nonFollowedUsed++;
        }

        var followedTake = order.Count(x => x);
        return new Plan(followedSkip, followedTake, nonFollowedSkip, order.Count - followedTake, order);
    }
}
