using KitobdaGimen.Application.Features.Chat.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Chat.Commands.SendMessage;

/// <summary>
/// Sends a message. Provide either an existing <see cref="ConversationId"/> or a
/// <see cref="RecipientId"/> (a conversation is created on demand). At least one of
/// <see cref="Text"/> or <see cref="SharedPostId"/> must be supplied.
/// </summary>
public record SendMessageCommand : IRequest<MessageDto>
{
    public int? ConversationId { get; init; }
    public int? RecipientId { get; init; }
    public string? Text { get; init; }
    public int? SharedPostId { get; init; }

    /// <summary>Public URL of an uploaded image attachment (from <c>/chat/upload-image</c>).</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Key of a built-in app sticker (e.g. "book", "quill").</summary>
    public string? StickerKey { get; init; }

    /// <summary>Public URL of an uploaded voice message (from <c>/chat/upload-voice</c>).</summary>
    public string? VoiceUrl { get; init; }

    /// <summary>Duration of the voice message in seconds.</summary>
    public int? VoiceDurationSeconds { get; init; }

    /// <summary>Id of the message this one replies to (Telegram-style quote); null if not a reply.</summary>
    public int? ReplyToMessageId { get; init; }
}
