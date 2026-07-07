using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Quotes.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Quotes.Queries.GetQuoteById;

public class GetQuoteByIdQueryHandler : IRequestHandler<GetQuoteByIdQuery, QuoteDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetQuoteByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<QuoteDetailDto> Handle(GetQuoteByIdQuery request, CancellationToken cancellationToken)
        => QuoteDetailLoader.LoadAsync(
            _db.Quotes.Where(q => q.Id == request.QuoteId),
            _currentUser.UserId,
            _currentUser.Email?.ToLowerInvariant(),
            cancellationToken);
}
