namespace KitobdaGimen.Application.Common.Interfaces;

public record TelegramOtpEntry(
    string Code,
    int UserId,
    long TelegramId,
    string FullName,
    string? Username,
    DateTime ExpiresAtUtc);

public record TelegramWebAppUser(
    long TelegramId,
    string FirstName,
    string LastName,
    string? Username,
    string? PhotoUrl);

public record BotBlockedUserItemDto(
    int UserId,
    long TelegramId,
    string FullName,
    string? Username,
    DateTime? BlockedAt);

public record TelegramBotUserStatsDto(
    int TotalUsers,
    int TelegramUsers,
    int WebsiteOnlyUsers,
    int ActiveBotUsers,
    int BlockedBotUsers,
    double BlockedPercent,
    int TodayNewUsers,
    int WeekNewUsers,
    int TodayActiveUsers,
    IReadOnlyList<BotBlockedUserItemDto> RecentlyBlockedUsers);

public record BotSyncResultDto(
    int TotalChecked,
    int NewlyBlocked,
    int NewlyUnblocked,
    int CurrentlyBlocked);

public interface ITelegramBotService
{
    string BotUsername { get; }
    string WebAppUrl { get; }

    /// <summary>Generates a 6-digit OTP valid for 2 minutes for the given user.</summary>
    TelegramOtpEntry GenerateLoginOtp(int userId, long telegramId, string fullName, string? username);

    /// <summary>Verifies and consumes a 6-digit OTP code. Returns null if invalid or expired.</summary>
    TelegramOtpEntry? VerifyLoginOtp(string code);

    /// <summary>Validates Telegram Mini App (WebApp) initData signature using HMAC-SHA256.</summary>
    TelegramWebAppUser? ValidateWebAppInitData(string? initData);

    /// <summary>Notifies the user in Telegram that they have logged into the website.</summary>
    Task NotifyWebLoginSuccessAsync(long telegramId, CancellationToken ct = default);

    /// <summary>Shares a review (Post) to the linked KitobdagiMen Telegram group.</summary>
    Task BroadcastPostToGroupAsync(int postId, CancellationToken ct = default);

    /// <summary>Updates the like button count on the Telegram group message for a post.</summary>
    Task SyncPostLikesInGroupAsync(int postId, int likeCount, CancellationToken ct = default);

    /// <summary>Deletes the Telegram group message associated with a post if it exists.</summary>
    Task DeletePostBroadcastAsync(int postId, CancellationToken ct = default);

    /// <summary>Shares a quote (Quote) to the linked KitobdagiMen Telegram group.</summary>
    Task BroadcastQuoteToGroupAsync(int quoteId, CancellationToken ct = default);

    /// <summary>Updates the like button count on the Telegram group message for a quote.</summary>
    Task SyncQuoteLikesInGroupAsync(int quoteId, int likeCount, CancellationToken ct = default);

    /// <summary>Deletes the Telegram group message associated with a quote if it exists.</summary>
    Task DeleteQuoteBroadcastAsync(int quoteId, CancellationToken ct = default);

    /// <summary>Edits inline reply markup of an existing Telegram message.</summary>
    Task<System.Text.Json.Nodes.JsonNode?> EditReplyMarkupAsync(
        long chatId,
        int messageId,
        object replyMarkup,
        CancellationToken ct = default);

    /// <summary>Deletes a message from a chat or group by its message id.</summary>
    Task<bool> DeleteMessageAsync(long chatId, int messageId, CancellationToken ct = default);

    /// <summary>Sends a photo with HTML caption and optional inline buttons to a chat.</summary>
    Task<System.Text.Json.Nodes.JsonNode?> SendPhotoAsync(
        long chatId,
        string photoUrlOrPath,
        string captionHtml,
        object? replyMarkup = null,
        int? messageThreadId = null,
        CancellationToken ct = default);

    /// <summary>Kitob egasiga Telegram orqali kimdir kitobini o'qimoqchi (band qilgan) ekanligi haqida xabar beradi.</summary>
    Task NotifyPhysicalBookReservedAsync(int physicalBookId, int reserverUserId, CancellationToken ct = default);

    /// <summary>Kitob egasi topshirishni qabul qilganda har ikkala tomonga (ega va oluvchi) bir-birining Telegram aloqasini yuboradi.</summary>
    Task NotifyPhysicalBookHandoverConfirmedAsync(int physicalBookId, int ownerUserId, int reserverUserId, CancellationToken ct = default);

    /// <summary>Foydalanuvchi boshqa birovni kuzata boshlaganda, kuzatilgan shaxsga Telegram bot orqali xabar yuboradi.</summary>
    Task NotifyUserFollowedAsync(int followerUserId, int followingUserId, CancellationToken ct = default);

    /// <summary>Sends 20:00 personal reading reminders via Telegram DM to users who haven't read today.</summary>
    Task<int> SendDailyReadingRemindersAsync(CancellationToken ct = default);

    /// <summary>Sends 23:59 daily reading summary with @usernames to the KitobdagiMen Telegram group.</summary>
    Task<bool> SendDailyGroupReportAsync(CancellationToken ct = default);

    /// <summary>Broadcasts Challenge winners and 1st/2nd/3rd prize books to the KitobdagiMen group.</summary>
    Task BroadcastChallengeUpdateAsync(string messageHtml, CancellationToken ct = default);

    /// <summary>Broadcasts Challenge winners to the linked Telegram group and sends congratulations in the bot.</summary>
    Task<int> BroadcastChallengeWinnersAsync(int year, int month, CancellationToken ct = default);

    /// <summary>Checks if a Telegram user is an active member or admin in the group chat.</summary>
    Task<bool> IsUserInGroupAsync(long groupChatId, long telegramUserId, CancellationToken ct = default);

    /// <summary>Checks if a Telegram user is a member of the official KitobdagiMen book club group.</summary>
    Task<bool> IsUserMemberOfBookClubAsync(long telegramUserId, CancellationToken ct = default);

    /// <summary>Returns the official KitobdagiMen group invite link (e.g. https://t.me/+M2zWs1tYAKoxMjli).</summary>
    Task<string> GetGroupInviteLinkAsync(CancellationToken ct = default);

    /// <summary>Foydalanuvchilar va Telegram bot statistikasi (jami, bot a'zolari, bloklaganlar, faollar).</summary>
    Task<TelegramBotUserStatsDto> GetBotUserStatsAsync(CancellationToken ct = default);

    /// <summary>Barcha bot foydalanuvchilarining bloklanganlik holatini Telegram API orqali tekshirib yangilaydi.</summary>
    Task<BotSyncResultDto> SyncBotBlockedStatusAsync(CancellationToken ct = default);

    /// <summary>Foydalanuvchi botni bloklaganini qayd qiladi.</summary>
    Task MarkUserBlockedAsync(long telegramId, CancellationToken ct = default);

    /// <summary>Foydalanuvchi botni qayta ochganini qayd qiladi.</summary>
    Task MarkUserUnblockedAsync(long telegramId, CancellationToken ct = default);
}
