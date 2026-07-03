using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Chat.Commands.DeleteMessage;
using KitobdaGimen.Application.Features.Chat.Commands.EditMessage;
using KitobdaGimen.Application.Features.Chat.Commands.GetOrCreateConversation;
using KitobdaGimen.Application.Features.Chat.Commands.MarkMessagesRead;
using KitobdaGimen.Application.Features.Chat.Commands.SendMessage;
using KitobdaGimen.Application.Features.Chat.Commands.ToggleReaction;
using KitobdaGimen.Application.Features.Chat.Queries.GetConversations;
using KitobdaGimen.Application.Features.Chat.Queries.GetMessages;
using KitobdaGimen.Application.Features.Chat.Queries.GetUnreadMessageCount;
using KitobdaGimen.Application.Features.Connections.Commands.CancelConnectionRequest;
using KitobdaGimen.Application.Features.Connections.Commands.RespondToConnection;
using KitobdaGimen.Application.Features.Connections.Commands.SendConnectionRequest;
using KitobdaGimen.Application.Features.Connections.Queries.GetPendingRequests;
using KitobdaGimen.Application.Features.Notifications.Commands.MarkNotificationsRead;
using KitobdaGimen.Application.Features.Users.Queries.SearchUsers;
using KitobdaGimen.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace KitobdaGimen.Web.Controllers;

[Authorize]
[Route("chat")]
public class ChatController : AppController
{
    // Allowed chat image types — re-encoded to WebP regardless of the original format.
    private static readonly HashSet<string> AllowedImageTypes = new()
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxImageBytes = 8 * 1024 * 1024; // 8 MB
    private const int MaxImageDimension = 1600;

    /// <summary>Redis presence service, resolved per request (same pattern as the base mediator).</summary>
    private IPresenceService Presence =>
        HttpContext.RequestServices.GetRequiredService<IPresenceService>();

    /// <summary>DB context, resolved per request (used only for the decorative floating covers).</summary>
    private IAppDbContext Db =>
        HttpContext.RequestServices.GetRequiredService<IAppDbContext>();

    /// <summary>Conversation list (and optionally an open conversation).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(int? conversationId)
    {
        // Opening /chat (where the notification bell points) clears the activity badge:
        // invites and other notifications the user may have missed while offline are seen here.
        await Mediator.Send(new MarkNotificationsReadCommand());

        // Mark the opened conversation read BEFORE loading the list, so its sidebar unread badge is
        // already gone in THIS render. (Previously this ran after GetConversations, so the list was
        // built with the stale unread count and the just-opened chat still showed its old "+N" until
        // a second click re-rendered it with 0.)
        if (conversationId is not null)
        {
            await Mediator.Send(new MarkMessagesReadCommand(conversationId.Value));
        }

        var conversations = await Mediator.Send(new GetConversationsQuery());
        await EnrichOnlineAsync(conversations);

        // Decorative background: real book covers (asaxiy.uz first) that "fly" behind the chat.
        var covers = (await Db.Books
                .Where(b => b.CoverUrl != null && b.CoverUrl != "")
                .OrderByDescending(b => b.Source == "asaxiy.uz")
                .ThenByDescending(b => b.Id)
                .Select(b => b.CoverUrl!)
                .Take(80)
                .ToListAsync())
            .Distinct()
            .Take(40)
            .ToList();

        var model = new ChatPageViewModel
        {
            Conversations = conversations,
            ActiveConversationId = conversationId,
            ActiveConversation = conversationId is null
                ? null
                : conversations.FirstOrDefault(c => c.Id == conversationId),
            Messages = conversationId is null
                ? null
                : await Mediator.Send(new GetMessagesQuery { ConversationId = conversationId.Value }),
            FloatingBookCovers = covers
        };

        return View(model);
    }

    [HttpPost("start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int otherUserId)
    {
        var conversation = await Mediator.Send(new GetOrCreateConversationCommand(otherUserId));
        return RedirectToAction(nameof(Index), new { conversationId = conversation.Id });
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendMessageCommand command)
    {
        var message = await Mediator.Send(command);
        return Json(message);
    }

    /// <summary>Uploads a chat image, re-encodes it as WebP and returns its public URL (JSON).</summary>
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Rasm tanlanmadi." });
        }

        if (file.Length > MaxImageBytes)
        {
            return BadRequest(new { message = "Rasm hajmi 8 MB dan oshmasligi kerak." });
        }

        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        var isAllowedType = AllowedImageTypes.Contains(contentType);
        var hasImageExtension = file.FileName?.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) == true
            || file.FileName?.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) == true
            || file.FileName?.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == true
            || file.FileName?.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) == true
            || file.FileName?.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) == true;

        if (!isAllowedType && !hasImageExtension)
        {
            return BadRequest(new { message = $"Faqat JPG, PNG, WEBP yoki GIF rasm yuklash mumkin. ({contentType})" });
        }

        Image image;
        try
        {
            await using var input = file.OpenReadStream();
            image = await Image.LoadAsync(input);
        }
        catch (UnknownImageFormatException)
        {
            return BadRequest(new { message = "Fayl rasm formatida emas." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Rasmni o'qib bo'lmadi: {ex.Message}" });
        }

        using (image)
        {
            if (image.Width > MaxImageDimension || image.Height > MaxImageDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxImageDimension, MaxImageDimension)
                }));
            }

            var uploadDir = KitobdaGimen.Web.UploadPaths.Dir("chat");
            var fileName = $"{Guid.NewGuid():N}.webp";
            var fullPath = Path.Combine(uploadDir, fileName);

            await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = 80 });

            var url = $"/uploads/chat/{fileName}";
            return Json(new { url });
        }
    }

    /// <summary>Toggles the current user's emoji reaction on a message (Telegram-style).</summary>
    [HttpPost("message/{id:int}/react")]
    public async Task<IActionResult> React(int id, [FromBody] ReactRequest body)
    {
        var message = await Mediator.Send(new ToggleReactionCommand(id, body.Emoji ?? ""));
        return Json(message);
    }

    /// <summary>Edits a message the current user sent.</summary>
    [HttpPost("message/{id:int}/edit")]
    public async Task<IActionResult> EditMessage(int id, [FromBody] EditMessageRequest body)
    {
        var message = await Mediator.Send(new EditMessageCommand(id, body.Text ?? ""));
        return Json(message);
    }

    /// <summary>Deletes a message the current user sent.</summary>
    [HttpPost("message/{id:int}/delete")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        await Mediator.Send(new DeleteMessageCommand(id));
        return NoContent();
    }

    [HttpGet("{conversationId:int}/messages")]
    public async Task<IActionResult> Messages(int conversationId, int page = 1)
    {
        var messages = await Mediator.Send(new GetMessagesQuery { ConversationId = conversationId, Page = page });
        return Json(messages);
    }

    [HttpPost("{conversationId:int}/read")]
    public async Task<IActionResult> MarkRead(int conversationId)
    {
        await Mediator.Send(new MarkMessagesReadCommand(conversationId));
        return NoContent();
    }

    /// <summary>Unread incoming message count for the current user — drives the navbar "Xabarlar"
    /// badge. Fetched on every page load so the badge survives refresh and stays accurate.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await Mediator.Send(new GetUnreadMessageCountQuery());
        return Json(new { count });
    }

    // ── Chat 2.0: people search + connection (invite) endpoints ────────────────────────

    /// <summary>Searches users for the people search; online status filled from Redis presence.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(string? q, int page = 1)
    {
        var result = await Mediator.Send(new SearchUsersQuery { Q = q ?? "", Page = page });

        var online = await Presence.AreOnlineAsync(result.Items.Select(i => i.Id));
        foreach (var item in result.Items)
        {
            item.IsOnline = online.TryGetValue(item.Id, out var isOnline) && isOnline;
        }

        return Json(result);
    }

    /// <summary>Sends a chat invite (auto-accepts a reciprocal pending invite).</summary>
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectRequest body)
    {
        var connection = await Mediator.Send(new SendConnectionRequestCommand(body.AddresseeId));
        return Json(connection);
    }

    /// <summary>Accepts or declines a received invite (only the addressee may respond).</summary>
    [HttpPost("connect/{id:int}/respond")]
    public async Task<IActionResult> Respond(int id, [FromBody] RespondRequest body)
    {
        var connection = await Mediator.Send(new RespondToConnectionCommand(id, body.Accept));
        return Json(connection);
    }

    /// <summary>Cancels a pending invite the current user sent.</summary>
    [HttpPost("connect/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await Mediator.Send(new CancelConnectionRequestCommand(id));
        return NoContent();
    }

    /// <summary>Incoming pending invites (for the owl's invite panel / badge).</summary>
    [HttpGet("requests")]
    public async Task<IActionResult> Requests()
    {
        var requests = await Mediator.Send(new GetPendingRequestsQuery());
        return Json(requests);
    }

    /// <summary>Fills <c>IsOnline</c> on each conversation's other user from Redis presence.</summary>
    private async Task EnrichOnlineAsync(IReadOnlyList<Application.Features.Chat.Dtos.ConversationDto> conversations)
    {
        if (conversations.Count == 0) return;
        var online = await Presence.AreOnlineAsync(conversations.Select(c => c.OtherUser.Id));
        foreach (var c in conversations)
        {
            c.IsOnline = online.TryGetValue(c.OtherUser.Id, out var isOnline) && isOnline;
        }
    }

    /// <summary>JSON body for <see cref="Connect"/> (the people-search "Taklif qilish" button).</summary>
    public record ConnectRequest(int AddresseeId);

    /// <summary>JSON body for <see cref="Respond"/> (accept/decline a received invite).</summary>
    public record RespondRequest(bool Accept);

    /// <summary>JSON body for <see cref="EditMessage"/>.</summary>
    public record EditMessageRequest(string? Text);

    /// <summary>JSON body for <see cref="React"/> (toggle an emoji reaction on a message).</summary>
    public record ReactRequest(string? Emoji);
}
