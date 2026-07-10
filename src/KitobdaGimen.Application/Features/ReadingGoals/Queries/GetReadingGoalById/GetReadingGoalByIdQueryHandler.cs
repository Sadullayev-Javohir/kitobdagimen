using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.ReadingGoals.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.ReadingGoals.Queries.GetReadingGoalById;

public class GetReadingGoalByIdQueryHandler : IRequestHandler<GetReadingGoalByIdQuery, ReadingGoalDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetReadingGoalByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReadingGoalDetailDto> Handle(GetReadingGoalByIdQuery request, CancellationToken cancellationToken)
    {
        var today = KitobdaGimen.Application.Common.UzTime.Today;

        // Tugatilgan kitoblar profilda ommaviy ko'rinadi, shuning uchun batafsil sahifa
        // (faqat o'qish — tahrirlash boshqaruvlari yo'q) istalgan kishi uchun ochiq.
        // Faqat mavjudligini tekshiramiz; egalik shart emas.
        var ownerId = await _db.ReadingGoals
            .Where(g => g.Id == request.ReadingGoalId)
            .Select(g => (int?)g.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerId is null)
        {
            throw new NotFoundException("O'qish maqsadi", request.ReadingGoalId);
        }

        var dto = await _db.ReadingGoals
            .Where(g => g.Id == request.ReadingGoalId)
            .ToReadingGoalDto(today)
            .FirstAsync(cancellationToken);

        var history = await _db.ReadingProgress
            .Where(p => p.ReadingGoalId == request.ReadingGoalId)
            .OrderBy(p => p.Date)
            .Select(p => new ReadingProgressDto
            {
                Date = p.Date,
                PagesReadToday = p.PagesReadToday
            })
            .ToListAsync(cancellationToken);

        return new ReadingGoalDetailDto
        {
            Goal = dto,
            History = history,
            IsOwner = _currentUser.UserId is int uid && uid == ownerId.Value
        };
    }
}
