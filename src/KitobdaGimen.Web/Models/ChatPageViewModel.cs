using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Chat.Dtos;

namespace KitobdaGimen.Web.Models;

/// <summary>View model for the chat page: the conversation list plus the open conversation's messages.</summary>
public class ChatPageViewModel
{
    public IReadOnlyList<ConversationDto> Conversations { get; init; } = Array.Empty<ConversationDto>();
    public int? ActiveConversationId { get; init; }
    public ConversationDto? ActiveConversation { get; init; }
    public PagedResult<MessageDto>? Messages { get; init; }
}
