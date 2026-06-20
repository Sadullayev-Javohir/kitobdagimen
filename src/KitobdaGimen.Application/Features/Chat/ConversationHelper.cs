using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Chat;

internal static class ConversationHelper
{
    /// <summary>
    /// Finds the conversation between two users (order-independent) or creates it.
    /// Stores participants with the smaller id as User1Id to keep the unique pair canonical.
    /// </summary>
    public static async Task<Conversation> GetOrCreateAsync(
        IAppDbContext db, int userA, int userB, CancellationToken cancellationToken)
    {
        var user1Id = Math.Min(userA, userB);
        var user2Id = Math.Max(userA, userB);

        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id, cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                User1Id = user1Id,
                User2Id = user2Id,
                CreatedAt = DateTime.UtcNow
            };
            db.Conversations.Add(conversation);
            await db.SaveChangesAsync(cancellationToken);
        }

        return conversation;
    }
}
