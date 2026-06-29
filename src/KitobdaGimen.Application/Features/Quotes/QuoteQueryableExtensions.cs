using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using KitobdaGimen.Domain.Entities;

namespace KitobdaGimen.Application.Features.Quotes;

internal static class QuoteQueryableExtensions
{
    /// <summary>Projects quotes to <see cref="QuoteDto"/>, computing save count and save state.</summary>
    public static IQueryable<QuoteDto> ToQuoteDto(this IQueryable<Quote> query, int? currentUserId)
    {
        return query.Select(q => new QuoteDto
        {
            Id = q.Id,
            Text = q.Text,
            CreatedAt = q.CreatedAt,
            Author = new UserSummaryDto
            {
                Id = q.User.Id,
                Username = q.User.Username,
                FullName = q.User.FullName,
                AvatarUrl = q.User.AvatarUrl
            },
            Book = new BookSummaryDto
            {
                Id = q.Book.Id,
                Title = q.Book.Title,
                Author = q.Book.Author,
                CoverUrl = q.Book.CoverUrl,
                Source = q.Book.Source
            },
            SaveCount = q.SavedBy.Count,
            IsSavedByCurrentUser = currentUserId != null && q.SavedBy.Any(s => s.UserId == currentUserId)
        });
    }
}
