using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Quotes.Dtos;

namespace KitobdaGimen.Web.Models;

public class QuotesPageViewModel
{
    public PagedResult<QuoteDto> Quotes { get; init; } = null!;
    public string? Search { get; init; }
    public string Sort { get; init; } = "latest";
    public bool IsAdmin { get; init; }
}
