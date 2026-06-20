using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Queries.GetUnreadMessageCount;

/// <summary>
/// Total number of unread incoming messages for the current user. Drives the navbar
/// "Xabarlar" badge — re-fetched on every page load so it stays accurate after a refresh.
/// </summary>
public record GetUnreadMessageCountQuery : IRequest<int>;
