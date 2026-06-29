using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Push.Commands.DeletePushSubscription;

public class DeletePushSubscriptionCommandHandler : IRequestHandler<DeletePushSubscriptionCommand, Unit>
{
    private readonly IAppDbContext _db;

    public DeletePushSubscriptionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(DeletePushSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
        {
            return Unit.Value;
        }

        var subs = await _db.PushSubscriptions
            .Where(s => s.Endpoint == request.Endpoint)
            .ToListAsync(cancellationToken);

        if (subs.Count > 0)
        {
            _db.PushSubscriptions.RemoveRange(subs);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
