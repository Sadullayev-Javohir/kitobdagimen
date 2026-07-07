using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteById;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteBySlug;

public class GetQuoteBySlugQueryHandler : IRequestHandler<GetQuoteBySlugQuery, QuoteDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetQuoteBySlugQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<QuoteDetailDto> Handle(GetQuoteBySlugQuery request, CancellationToken cancellationToken)
        => QuoteDetailLoader.LoadAsync(
            _db.Quotes.Where(q => q.Slug == request.Slug),
            _currentUser.UserId,
            _currentUser.Email?.ToLowerInvariant(),
            cancellationToken);
}
