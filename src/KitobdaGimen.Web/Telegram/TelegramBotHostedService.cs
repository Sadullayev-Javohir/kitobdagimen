using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Features.Challenge.Commands.FinalizeChallengeMonth;
using KitobdaGimen.Application.Features.Challenge.Queries.GetChallengeStandings;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Web.Telegram;

public class TelegramBotHostedService : BackgroundService
{
    private enum SessionStep
    {
        None = 0,
        RegFullName,
        RegUsername,
        EditFullName,
        EditUsername,
        EditBio,
        AwaitAvatarPhoto,
        SearchBookQuery,
        AddBookTitle,
        AddBookAuthor,
        AddBookPages,
        AddBookDailyGoal,
        LogPagesAmount,
        NewQuoteBookTitle,
        NewQuoteBookAuthor,
        NewQuoteText,
        NewQuoteBookCover,
        NewQuoteImage,
        NewPostBookTitle,
        NewPostBookAuthor,
        NewPostText,
        NewPostBookCover,
        NewPostReviewImage,
        CommentOnPost,
        CommentOnQuote,
        CommentOnJackpot,
        NewPhysBookTitle,
        NewPhysBookAuthor,
        NewPhysBookCover,
        NewPhysBookRegion,
        NewPhysBookCondition,
        AdminSetDates,
        AdminSetPrizeTitle,
        AdminSetPrizeAuthor,
        AdminSetPrizeCover,
        AdminSetGroupId
    }

    private sealed class UserBotSession
    {
        public SessionStep Step { get; set; } = SessionStep.None;
        public string? TempFullName { get; set; }
        public string? TempBookTitle { get; set; }
        public string? TempBookAuthor { get; set; }
        public int TempTotalPages { get; set; }
        public int TempGoalId { get; set; }
        public int TempTargetId { get; set; }
        public int TempPrizeRank { get; set; }
        public string? TempRegion { get; set; }
        public string? TempCoverUrl { get; set; }
        public string? TempText { get; set; }
        public string? TargetFlow { get; set; }
        public int? SelectedBookId { get; set; }
        public int SearchMessageId { get; set; }
    }

    private readonly TelegramBotService _bot;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramBotHostedService> _logger;
    private readonly ConcurrentDictionary<long, UserBotSession> _sessions = new();

    private static readonly object CancelInlineMarkup = new
    {
        inline_keyboard = new object[][]
        {
            new object[]
            {
                new { text = "❌ Bekor qilish", callback_data = "cancel_input" }
            }
        }
    };

    private long _offset;
    private DateOnly _lastReminderSentDate;
    private DateOnly _lastReportSentDate;

    public TelegramBotHostedService(
        TelegramBotService bot,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramBotHostedService> logger)
    {
        _bot = bot;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_bot.IsConfigured)
        {
            _logger.LogInformation("Telegram Bot Token sozlanmagan — bot polling ishga tushmadi.");
            return;
        }

        await RegisterBotCommandsAsync(stoppingToken);
        _ = RunScheduledJobsLoopAsync(stoppingToken);

        _logger.LogInformation("Telegram Bot (@{BotUsername}) long-polling ishga tushdi.", _bot.BotUsername);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("TelegramBotPolling");
                var url = $"https://api.telegram.org/bot{_bot.Settings.BotToken}/getUpdates?offset={_offset}&timeout=25";
                using var resp = await client.GetAsync(url, stoppingToken);
                var body = await resp.Content.ReadAsStringAsync(stoppingToken);
                var root = JsonNode.Parse(body);

                if (root?["ok"]?.GetValue<bool>() != true)
                {
                    await Task.Delay(3000, stoppingToken);
                    continue;
                }

                var updates = root["result"]?.AsArray();
                if (updates is null || updates.Count == 0)
                {
                    continue;
                }

                foreach (var upd in updates)
                {
                    if (upd is null) continue;
                    var updateId = upd["update_id"]?.GetValue<long>() ?? 0;
                    if (updateId >= _offset)
                    {
                        _offset = updateId + 1;
                    }

                    try
                    {
                        await HandleUpdateAsync(upd, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Telegram update #{UpdateId} qayta ishlashda xato.", updateId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram polling siklida xatolik — 3 soniyadan so'ng qayta ulanadi.");
                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    private async Task RegisterBotCommandsAsync(CancellationToken ct)
    {
        var commands = new object[]
        {
            new { command = "start", description = "🚀 Botni ishga tushirish va ro'yxatdan o'tish" },
            new { command = "app", description = "🌐 Web ilovani ochish" },
            new { command = "login", description = "🔑 Veb-saytga kirish uchun 2 daqiqalik kod" },
            new { command = "kitoblar", description = "📚 O'qiyotgan kitoblarim va mutolaa rejasi" },
            new { command = "fikr", description = "✍️ Fikr bildirish (Taqriz va Iqtibos yozish)" },
            new { command = "challenge", description = "🏆 Challenge reytingi va sovg'a kitoblar" },
            new { command = "almashish", description = "🔄 Kitob almashish e'lonlari" },
            new { command = "profil", description = "👤 Profilim va rasm o'rnatish" },
            new { command = "bugun", description = "📊 Bugun kim qancha kitob o'qidi" },
            new { command = "admin", description = "🛠 Admin va Challenge boshqaruvi" }
        };

        // Guruhlarda va default holatda hech qanday "/" slash buyruqlar menyusi chiqmasligi uchun tozalaymiz
        await _bot.CallApiAsync("deleteMyCommands", new { scope = new { type = "default" } }, ct);
        await _bot.CallApiAsync("deleteMyCommands", new { scope = new { type = "all_group_chats" } }, ct);
        await _bot.CallApiAsync("deleteMyCommands", new { scope = new { type = "all_chat_administrators" } }, ct);

        // Buyruqlarni FAQAT shaxsiy (private) chatlar uchun o'rnatamiz
        await _bot.CallApiAsync("setMyCommands", new
        {
            commands,
            scope = new { type = "all_private_chats" }
        }, ct);

        // Agar HTTPS WebApp URL mavjud bo'lsa, chat pastki menyu tugmasini Telegram Mini App sifatida o'rnatamiz
        if (_bot.WebAppUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            await _bot.CallApiAsync("setChatMenuButton", new
            {
                menu_button = new
                {
                    type = "web_app",
                    text = "Web ilova",
                    web_app = new { url = _bot.WebAppUrl }
                }
            }, ct);
        }
    }

    // ── Soat 20:00 (Eslatma) va 23:59 (Kunlik guruh hisoboti) Scheduler ──────────

    private async Task RunScheduledJobsLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var now = UzTime.Now;
                var today = DateOnly.FromDateTime(now);

                // 20:00 — Bugun kitob o'qimaganlarga eslatma
                if (now.Hour == 20 && now.Minute == 0 && _lastReminderSentDate != today)
                {
                    _lastReminderSentDate = today;
                    await _bot.SendDailyReadingRemindersAsync(ct);
                }

                // 23:59 — «KitobdagiMen» guruhiga kunlik sarhisob (kim qancha o'qidi)
                if (now.Hour == 23 && now.Minute == 59 && _lastReportSentDate != today)
                {
                    _lastReportSentDate = today;
                    await _bot.SendDailyGroupReportAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram rejalashtirilgan (20:00 / 23:59) vazifasida xato.");
            }

            await Task.Delay(TimeSpan.FromSeconds(25), ct);
        }
    }

    // ── Update Router ────────────────────────────────────────────────────────────

    private async Task HandleUpdateAsync(JsonNode update, CancellationToken ct)
    {
        if (update["callback_query"] is JsonNode cb)
        {
            await HandleCallbackQueryAsync(cb, ct);
            return;
        }

        if (update["message"] is JsonNode msg)
        {
            await HandleMessageAsync(msg, ct);
            return;
        }

        if (update["my_chat_member"] is JsonNode mcm)
        {
            await HandleMyChatMemberAsync(mcm, ct);
            return;
        }
    }

    private async Task HandleMyChatMemberAsync(JsonNode mcm, CancellationToken ct)
    {
        var chat = mcm["chat"];
        var newMember = mcm["new_chat_member"];
        if (chat is null || newMember is null) return;

        var chatId = chat["id"]?.GetValue<long>() ?? 0;
        var chatType = chat["type"]?.GetValue<string>() ?? "";
        var status = newMember["status"]?.GetValue<string>() ?? "";

        if (chatId == 0) return;

        // 1) Shaxsiy chatda botni bloklash / blokdan chiqarish holati
        if (chatType == "private")
        {
            if (status == "kicked")
            {
                await _bot.MarkUserBlockedAsync(chatId, ct);
            }
            else if (status == "member")
            {
                await _bot.MarkUserUnblockedAsync(chatId, ct);
            }
            return;
        }

        if (chatType != "group" && chatType != "supergroup") return;

        // Bot guruhga a'zo yoki admin qilib qo'shilganida
        if (status is "member" or "administrator")
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var configuredTarget = await _bot.GetGroupTargetAsync(db, ct);
            var isTargetGroup = configuredTarget is null ||
                                configuredTarget.Value.ChatId == chatId ||
                                Math.Abs(configuredTarget.Value.ChatId).ToString().EndsWith("5511445336") ||
                                Math.Abs(chatId).ToString().EndsWith("5511445336");

            if (isTargetGroup)
            {
                await SaveBookClubGroupIdAsync(db, chatId.ToString(), ct);
                var welcome =
                    "🎉 <b>«KitobdagiMen» guruhi muvaffaqiyatli bog'landi!</b>\n\n" +
                    "<blockquote>📚 Ushbu guruhga har kuni soat <b>23:59</b> da kitobxonlarning kunlik mutolaa sarhisobi hamda <b>Challenge</b> g'oliblari e'loni avtomatik yuborib boriladi.</blockquote>";
                await _bot.SendHtmlAsync(chatId, welcome, ct: ct);
            }
        }
    }

    private async Task SendMustJoinGroupPromptAsync(long chatId, CancellationToken ct)
    {
        var inviteLink = await _bot.GetGroupInviteLinkAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("👋 <b>Assalomu alaykum!</b>");
        sb.AppendLine();
        sb.AppendLine("⚠️ <b>«KitobdagiMen» guruhiga a'zo bo'ling!</b>");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("Telegram botdan foydalanish, kitoblar mutolaasini qayd etish, Challenge va boshqa xizmatlarga so'rov yuborish uchun rasmiy <b>KitobdagiMen</b> guruhimizga a'zo bo'lishingiz lozim.");
        sb.AppendLine();
        sb.AppendLine("1️⃣ Quyidagi <b>«👥 KitobdagiMen guruhiga qo'shilish»</b> tugmasini bosing;");
        sb.AppendLine("2️⃣ Guruhga a'zo bo'lgach, <b>«✅ A'zo bo'ldim»</b> tugmasini bosing.");

        var markup = new
        {
            inline_keyboard = new object[][]
            {
                new object[]
                {
                    new { text = "👥 KitobdagiMen guruhiga qo'shilish", url = inviteLink }
                },
                new object[]
                {
                    new { text = "✅ A'zo bo'ldim", callback_data = "sub:check" }
                }
            }
        };

        await _bot.SendHtmlAsync(chatId, sb.ToString(), markup, ct: ct);
    }

    // ── Message Handler ──────────────────────────────────────────────────────────

    private async Task HandleMessageAsync(JsonNode msg, CancellationToken ct)
    {
        var chat = msg["chat"];
        var from = msg["from"];
        if (chat is null || from is null) return;

        var chatId = chat["id"]?.GetValue<long>() ?? 0;
        var chatType = chat["type"]?.GetValue<string>() ?? "private";
        var threadId = msg["message_thread_id"]?.GetValue<int>();
        var tgUserId = from["id"]?.GetValue<long>() ?? 0;
        var tgUsername = from["username"]?.GetValue<string>();
        var tgFirstName = from["first_name"]?.GetValue<string>() ?? "";
        var tgLastName = from["last_name"]?.GetValue<string>() ?? "";
        var text = msg["text"]?.GetValue<string>()?.Trim() ?? "";
        var messageId = msg["message_id"]?.GetValue<int>() ?? 0;

        if (chatId == 0 || tgUserId == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        // 1) Guruh xabarlari ("KitobdagiMen" guruhi integratsiyasi)
        if (chatType is "group" or "supergroup")
        {
            await HandleGroupMessageAsync(db, mediator, chatId, threadId, tgUserId, tgUsername, text, ct);
            return;
        }

        // 2) Shaxsiy (private) chat
        var user = await FindAndSyncUserAsync(db, tgUserId, tgUsername, ct);
        var session = _sessions.GetOrAdd(tgUserId, _ => new UserBotSession());

        // Guruh a'zoligini tekshiramiz: KitobdagiMen guruhiga qo'shilishi shart
        if (!IsConfiguredSuperAdmin(tgUserId, tgUsername, user?.Username))
        {
            var isMember = await _bot.IsUserMemberOfBookClubAsync(tgUserId, ct);
            if (!isMember)
            {
                await SendMustJoinGroupPromptAsync(chatId, ct);
                return;
            }
        }

        // Agar foydalanuvchi rasm yuborgan bo'lsa
        var photos = msg["photo"]?.AsArray();
        if (photos is not null && photos.Count > 0)
        {
            var largestFileId = photos[^1]?["file_id"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(largestFileId))
            {
                if (user is not null && session.Step == SessionStep.AdminSetPrizeCover && user.Role >= UserRole.Admin)
                {
                    var coverUrl = await _bot.DownloadTelegramPhotoAsWebpAsync(largestFileId, "covers", 1000, ct);
                    var cfg = await ChallengeConfigStore.GetOrDefaultAsync(db, ct);
                    var prize = cfg.GetPrize(session.TempPrizeRank);
                    prize.Title = session.TempBookTitle;
                    prize.Author = session.TempBookAuthor;
                    if (!string.IsNullOrWhiteSpace(coverUrl))
                    {
                        prize.CoverUrl = coverUrl;
                    }
                    await ChallengeConfigStore.SaveAsync(db, cfg, ct);
                    session.Step = SessionStep.None;

                    await _bot.SendHtmlAsync(chatId,
                        $"✅ <b>{session.TempPrizeRank}-o'rin sovg'a kitobi va muqova rasmi saqlandi!</b>\n\n" +
                        $"<blockquote>🎁 <b>{TelegramBotService.Escape(prize.Title)}</b> — <i>{TelegramBotService.Escape(prize.Author)}</i></blockquote>",
                        BuildMainMenuKeyboard(user.Role >= UserRole.Admin), ct: ct);
                    return;
                }

                if (user is not null && session.Step == SessionStep.NewPhysBookCover)
                {
                    var coverUrl = await _bot.DownloadTelegramPhotoAsWebpAsync(largestFileId, "books", 1024, ct);
                    await SaveNewPhysicalBookAsync(db, chatId, user, session, coverUrl, ct);
                    return;
                }

                if (user is not null && session.Step == SessionStep.NewPostBookCover)
                {
                    var coverUrl = await _bot.DownloadTelegramPhotoAsWebpAsync(largestFileId, "books", 1024, ct);
                    session.TempCoverUrl = coverUrl;
                    session.Step = SessionStep.NewPostReviewImage;

                    await _bot.SendHtmlAsync(chatId,
                        "✅ <b>Kitob rasmi (muqovasi) qabul qilindi!</b>\n\n" +
                        "📸 <b>2-qadam: Taqrizga mos rasm yuboring:</b>\n\n" +
                        "Ushbu taqrizingizga mos estetik fotosurat yoki lavha yuboring (yoki rasmsiz chop etish uchun tugmani bosing):",
                        new
                        {
                            inline_keyboard = new object[][]
                            {
                                new object[] { new { text = "⏭ Taqriz rasmini o'tkazib yuborish", callback_data = "post:skip_review_image" } }
                            }
                        }, ct: ct);
                    return;
                }

                if (user is not null && session.Step == SessionStep.NewPostReviewImage)
                {
                    var postImgUrl = await _bot.DownloadTelegramPhotoAsWebpAsync(largestFileId, "posts", 1024, ct);
                    await SaveNewPostAsync(db, chatId, user, session, session.TempCoverUrl, postImgUrl, ct);
                    return;
                }

                if (user is not null && session.Step == SessionStep.NewQuoteBookCover)
                {
                    var coverUrl = await _bot.DownloadTelegramPhotoAsWebpAsync(largestFileId, "books", 1024, ct);
                    session.TempCoverUrl = coverUrl;
                    session.Step = SessionStep.NewQuoteImage;

                    await _bot.SendHtmlAsync(chatId,
                        "✅ <b>Kitob rasmi (muqovasi) qabul qilindi!</b>\n\n" +
                        "📸 <b>2-qadam: Iqtibosga mos rasm yuboring:</b>\n\n" +
                        "Ushbu iqtibosga mos estetik fotosurat yoki lavha yuboring (yoki rasmsiz chop etish uchun tugmani bosing):",
                        new
                        {
                            inline_keyboard = new object[][]
                            {
                                new object[] { new { text = "⏭ Iqtibos rasmini o'tkazib yuborish", callback_data = "quote:skip_image" } }
                            }
                        }, ct: ct);
                    return;
                }

                if (user is not null && session.Step == SessionStep.NewQuoteImage)
                {
                    var quoteImgUrl = await _bot.DownloadTelegramPhotoAsWebpAsync(largestFileId, "quotes", 1024, ct);
                    await SaveNewQuoteAsync(db, chatId, user, session, session.TempCoverUrl, quoteImgUrl, ct);
                    return;
                }

                if (user is not null && session.Step == SessionStep.AwaitAvatarPhoto)
                {
                    var avatarUrl = await _bot.DownloadTelegramPhotoAsWebpAsync(largestFileId, "avatars", 512, ct);
                    if (!string.IsNullOrWhiteSpace(avatarUrl))
                    {
                        user.AvatarUrl = avatarUrl;
                        await db.SaveChangesAsync(ct);
                        session.Step = SessionStep.None;

                        await _bot.SendHtmlAsync(chatId,
                            "🖼 <b>Profil rasmingiz muvaffaqiyatli yangilandi!</b>",
                            BuildMainMenuKeyboard(user.Role >= UserRole.Admin), ct: ct);
                        return;
                    }
                }
            }
        }

        // Bekor qilish buyrug'i
        if (text.Equals("/cancel", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("❌ Bekor qilish", StringComparison.OrdinalIgnoreCase))
        {
            session.Step = SessionStep.None;
            session.TempCoverUrl = null;
            session.TargetFlow = null;
            session.SelectedBookId = null;
            session.SearchMessageId = 0;
            if (user is not null)
            {
                await _bot.SendHtmlAsync(chatId, "✅ Amal bekor qilindi. Asosiy menyudasiz:", BuildMainMenuKeyboard(user.Role >= UserRole.Admin), ct: ct);
            }
            return;
        }

        // Agar foydalanuvchi hali ro'yxatdan o'tmagan bo'lsa -> Ro'yxatdan o'tish oqimi
        if (user is null)
        {
            await HandleUnregisteredUserFlowAsync(db, chatId, tgUserId, tgUsername, tgFirstName, tgLastName, text, session, ct);
            return;
        }

        // Deep-link bilan /start kelgan bo'lsa (masalan: /start comment_post_12 yoki /start comment_quote_5)
        if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            session.Step = SessionStep.None;
            var arg = text.Length > 6 ? text[6..].Trim() : "";

            if (arg.StartsWith("comment_post_", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(arg["comment_post_".Length..], out var targetPostId))
            {
                await ShowPostCommentsAndPromptAsync(db, chatId, session, targetPostId, ct);
                return;
            }

            if (arg.StartsWith("comment_quote_", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(arg["comment_quote_".Length..], out var targetQuoteId))
            {
                await ShowQuoteCommentsAndPromptAsync(db, chatId, session, targetQuoteId, ct);
                return;
            }

            if (arg.StartsWith("jackpot_comment_", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(arg["jackpot_comment_".Length..], out var targetJackpotId))
            {
                await ShowJackpotCommentsAndPromptAsync(db, chatId, session, targetJackpotId, ct);
                return;
            }

            if (arg.Equals("jackpot", StringComparison.OrdinalIgnoreCase))
            {
                await ShowTodayJackpotInBotAsync(db, chatId, user, ct);
                return;
            }

            if (arg.StartsWith("profile_", StringComparison.OrdinalIgnoreCase))
            {
                var targetSlug = arg["profile_".Length..].Trim();
                var targetUser = await db.Users.FirstOrDefaultAsync(
                    u => (u.Username != null && u.Username.ToLower() == targetSlug.ToLower()) || u.Id.ToString() == targetSlug, ct);

                if (targetUser is not null)
                {
                    var profileSlug = !string.IsNullOrWhiteSpace(targetUser.Username) ? targetUser.Username : targetUser.Id.ToString();
                    var directUrl = $"{_bot.WebAppUrl}/profile/{profileSlug}";
                    await _bot.SendHtmlAsync(chatId,
                        $"👤 <b>Kitobxon:</b> <b>{TelegramBotService.Escape(targetUser.FullName)}</b>\n" +
                        (!string.IsNullOrWhiteSpace(targetUser.Bio) ? $"<i>\"{TelegramBotService.Escape(targetUser.Bio)}\"</i>\n\n" : "\n") +
                        "Foydalanuvchi profilini Mini App orqali ko'rishingiz mumkin:",
                        new
                        {
                            inline_keyboard = new object[][]
                            {
                                new object[]
                                {
                                    new { text = "🌐 Profilni Mini Appda ochish", web_app = new { url = directUrl } }
                                }
                            }
                        }, ct: ct);
                    return;
                }
            }

            if (arg.Equals("challenge", StringComparison.OrdinalIgnoreCase))
            {
                await SendChallengeDashboardAsync(db, mediator, chatId, user, ct);
                return;
            }

            if (arg.Equals("read", StringComparison.OrdinalIgnoreCase))
            {
                await SendReadingDashboardAsync(db, chatId, user, ct);
                return;
            }

            await SendWelcomeAsync(chatId, user, ct);
            return;
        }

        // Asosiy menyu tugmalari va slash buyruqlar
        if (IsMenuCommand(text))
        {
            session.Step = SessionStep.None;
            await RouteMenuCommandAsync(db, mediator, chatId, tgUserId, user, session, text, ct);
            return;
        }

        // Faol qadam (conversational step) bo'lsa
        if (session.Step != SessionStep.None)
        {
            await HandleActiveStepAsync(db, mediator, chatId, tgUserId, tgUsername, user, session, text, messageId, ct);
            return;
        }

        // Standart javob — asosiy menyu
        await SendWelcomeAsync(chatId, user, ct);
    }

    // ── Ro'yxatdan o'tmagan foydalanuvchi oqimi (Ism-familiya -> Username -> 2 daqiqalik OTP) ──

    private async Task HandleUnregisteredUserFlowAsync(
        IAppDbContext db,
        long chatId,
        long tgUserId,
        string? tgUsername,
        string tgFirstName,
        string tgLastName,
        string text,
        UserBotSession session,
        CancellationToken ct)
    {
        if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase) || session.Step == SessionStep.None)
        {
            session.Step = SessionStep.RegFullName;

            var sb = new StringBuilder();
            sb.AppendLine("📚 <b>KitobdagiMen</b> platformasiga xush kelibsiz!\n");
            sb.AppendLine("Iltimos, <b>ism va familiyangizni</b> kiriting:");
            sb.AppendLine("<i>(Masalan: Javohir Sadullayev)</i>");

            await _bot.SendHtmlAsync(chatId, sb.ToString(), ct: ct);
            return;
        }

        if (session.Step == SessionStep.RegFullName)
        {
            if (text.Length < 3 || text.Length > 100 || text.StartsWith('/'))
            {
                await _bot.SendHtmlAsync(chatId,
                    "⚠️ <b>Ism va familiya kamida 3 ta belgidan iborat bo'lsin:</b>",
                    ct: ct);
                return;
            }

            session.TempFullName = text;
            session.Step = SessionStep.RegUsername;

            object? markup = null;
            if (!string.IsNullOrWhiteSpace(tgUsername))
            {
                var cleanTg = Regex.Replace(tgUsername.ToLowerInvariant(), @"[^a-z0-9_]", "");
                if (cleanTg.Length >= 3)
                {
                    markup = new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[]
                            {
                                new { text = $"✨ @{cleanTg} ni tanlash", callback_data = $"reg:user:{cleanTg}" }
                            }
                        }
                    };
                }
            }

            await _bot.SendHtmlAsync(chatId,
                $"✅ Rahmat, <b>{TelegramBotService.Escape(session.TempFullName)}</b>!\n\n" +
                "Endi o'zingizga <b>username</b> tanlang:\n" +
                "<i>(Lotin harflari va raqamlar, 3-28 belgi)</i>",
                markup, ct: ct);
            return;
        }

        if (session.Step == SessionStep.RegUsername)
        {
            await CompleteRegistrationWithUsernameAsync(db, chatId, tgUserId, tgUsername, session, text, ct);
        }
    }

    private async Task CompleteRegistrationWithUsernameAsync(
        IAppDbContext db,
        long chatId,
        long tgUserId,
        string? tgUsername,
        UserBotSession session,
        string rawUsername,
        CancellationToken ct)
    {
        var candidate = rawUsername.Trim().TrimStart('@').ToLowerInvariant();
        if (!Regex.IsMatch(candidate, @"^[a-z0-9_]{3,28}$"))
        {
            await _bot.SendHtmlAsync(chatId,
                "⚠️ Username noto'g'ri. Faqat lotin harflari va raqamlar (3–28 belgi) kiriting:", ct: ct);
            return;
        }

        var taken = await db.Users.AnyAsync(
            u => u.Username != null && u.Username.ToLower() == candidate && u.GoogleId != $"tg:{tgUserId}", ct);

        if (taken)
        {
            await _bot.SendHtmlAsync(chatId,
                $"⚠️ <b>@{TelegramBotService.Escape(candidate)}</b> band! Boshqa username kiriting:", ct: ct);
            return;
        }

        var avatarUrl = await _bot.FetchUserAvatarFromTelegramAsync(tgUserId, ct);
        var isSuperAdmin = IsConfiguredSuperAdmin(tgUserId, tgUsername, candidate);

        var newUser = new User
        {
            GoogleId = $"tg:{tgUserId}",
            Email = $"{tgUserId}@tg.kitobdagimen.uz",
            FullName = session.TempFullName ?? "Kitobxon",
            Username = candidate,
            AvatarUrl = avatarUrl,
            Role = isSuperAdmin ? UserRole.SuperAdmin : UserRole.User,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync(ct);

        await SaveTelegramUsernameSettingAsync(db, newUser.Id, tgUsername ?? candidate, ct);
        session.Step = SessionStep.None;

        // Faqat birinchi ro'yxatdan o'tganda kirish kodi (OTP) beriladi
        var otp = _bot.GenerateLoginOtp(newUser.Id, tgUserId, newUser.FullName, newUser.Username);

        var welcomeHtml = BuildOtpCardHtml(newUser, otp, isNewRegistration: true);
        await _bot.SendHtmlAsync(chatId, welcomeHtml, BuildOtpInlineMarkup(otp.Code), ct: ct);

        await _bot.SendHtmlAsync(chatId,
            "Bo'limni tanlang:",
            BuildMainMenuKeyboard(newUser.Role >= UserRole.Admin), ct: ct);
    }

    // ── OTP Xabari va Qaytadan Yuborish ──────────────────────────────────────────

    private async Task SendWelcomeAsync(long chatId, User user, CancellationToken ct)
    {
        var html = $"👋 Salom, <b>{TelegramBotService.Escape(user.FullName)}</b>!\n\n" +
                   "Kerakli bo'limni tanlang:";

        await _bot.SendHtmlAsync(chatId, html, BuildMainMenuKeyboard(user.Role >= UserRole.Admin), ct: ct);
    }

    private async Task SendWelcomeWithOtpAsync(long chatId, User user, long tgUserId, CancellationToken ct)
    {
        var otp = _bot.GenerateLoginOtp(user.Id, tgUserId, user.FullName, user.Username);
        var html = BuildOtpCardHtml(user, otp, isNewRegistration: false);

        await _bot.SendHtmlAsync(chatId, html, BuildOtpInlineMarkup(otp.Code), ct: ct);
    }

    private static string BuildOtpCardHtml(User user, TelegramOtpEntry otp, bool isNewRegistration)
    {
        var sb = new StringBuilder();
        if (isNewRegistration)
        {
            sb.AppendLine("🎉 <b>Muvaffaqiyatli ro'yxatdan o'tdingiz!</b>\n");
        }
        else
        {
            sb.AppendLine("🔐 <b>Saytga kirish</b>\n");
        }

        sb.AppendLine($"🔑 <b>Kirish kodi:</b> <code>{otp.Code}</code>");
        sb.AppendLine("⏱ <i>Amal qilish muddati: 2 daqiqa</i>");
        return sb.ToString();
    }

    private object BuildOtpInlineMarkup(string? otpCode = null)
    {
        var rows = new List<object[]>();

        // 1) Agar HTTPS WebApp URL bo'lsa, Telegram Mini App sifatida to'g'ridan-to'g'ri bot ichida ochish tugmasi
        if (_bot.WebAppUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var launchUrl = !string.IsNullOrWhiteSpace(otpCode)
                ? $"{_bot.WebAppUrl.TrimEnd('/')}/auth/tg-magic?code={otpCode}"
                : _bot.WebAppUrl;

            rows.Add(new object[]
            {
                new
                {
                    text = "🌐 Web Ilovani ochish (Mini App)",
                    web_app = new { url = launchUrl }
                }
            });
        }

        // 2) 2 daqiqalik kodni qaytadan yuborish
        rows.Add(new object[]
        {
            new { text = "🔄 Qaytadan yuborish (Yangi kod)", callback_data = "otp:resend" }
        });

        // 3) Profil rasm va Challenge
        rows.Add(new object[]
        {
            new { text = "🖼 Profil rasmini o'rnatish", callback_data = "profile:avatar" },
            new { text = "🏆 Challenge", callback_data = "menu:challenge" }
        });

        return new { inline_keyboard = rows };
    }

    private object BuildMainMenuKeyboard(bool isAdmin)
    {
        var rows = new List<object[]>();

        if (_bot.WebAppUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            rows.Add(new object[]
            {
                new { text = "🌐 Web ilova", web_app = new { url = _bot.WebAppUrl } }
            });
        }

        rows.Add(new object[] { new { text = "📚 Kitoblarim" }, new { text = "✍️ Fikr bildirish" } });
        rows.Add(new object[] { new { text = "🏆 Challenge" }, new { text = "🔄 Kitob almashish" } });
        rows.Add(new object[] { new { text = "👤 Profilim" }, new { text = "🔑 Saytga kirish kodi" } });

        if (isAdmin)
        {
            rows.Add(new object[] { new { text = "🛠 Admin va Challenge boshqaruvi" } });
        }

        return new
        {
            keyboard = rows,
            resize_keyboard = true,
            is_persistent = true
        };
    }

    // ── Asosiy Menyu Buyruqlari ──────────────────────────────────────────────────

    private static bool IsMenuCommand(string text) =>
        text is "📚 Kitoblarim" or "/kitoblar"
            or "📖 Bet kiritish" or "/bet"
            or "🌐 Web ilova" or "Web ilova"
            or "✍️ Fikr bildirish" or "💭 Fikr bildirish" or "Fikr bildirish" or "/fikr"
            or "✍️ Taqriz yozish" or "Taqriz yozish" or "✍️ Taqrizlar" or "/taqriz"
            or "💬 Iqtibos yozish" or "Iqtibos yozish" or "💬 Iqtiboslar" or "/iqtibos"
            or "🏆 Challenge" or "/challenge"
            or "🔄 Kitob almashish" or "/almashish"
            or "👤 Profilim" or "/profil"
            or "🔑 Saytga kirish kodi" or "/login" or "/app"
            or "🛠 Admin va Challenge boshqaruvi" or "/admin"
            or "/bugun" or "/goliblar"
            or "🎰 Jackpot" or "/jackpot" or "👑 Kun qahramoni";

    private async Task RouteMenuCommandAsync(
        IAppDbContext db,
        ISender mediator,
        long chatId,
        long tgUserId,
        User user,
        UserBotSession session,
        string text,
        CancellationToken ct)
    {
        switch (text)
        {
            case "🎰 Jackpot":
            case "/jackpot":
            case "👑 Kun qahramoni":
                await ShowTodayJackpotInBotAsync(db, chatId, user, ct);
                break;
            case "/goliblar":
                if (user.Role < UserRole.Admin)
                {
                    await _bot.SendHtmlAsync(chatId, "⛔ Faqat Admin uchun!", ct: ct);
                    return;
                }
                var (curY, curM) = ChallengeCalendar.CurrentPeriod();
                var countAnnounced = await _bot.BroadcastChallengeWinnersAsync(curY, curM, ct);
                if (countAnnounced > 0)
                {
                    await _bot.SendHtmlAsync(chatId,
                        $"✅ <b>{countAnnounced} nafar Challenge g'olibi e'lon qilindi!</b>\n\nGuruhga, g'oliblarga va bot foydalanuvchilariga xabar yuborildi.", ct: ct);
                }
                else
                {
                    var (prevY, prevM) = ChallengeCalendar.LastCompletedPeriod();
                    var countPrev = await _bot.BroadcastChallengeWinnersAsync(prevY, prevM, ct);
                    if (countPrev > 0)
                    {
                        await _bot.SendHtmlAsync(chatId,
                            $"✅ <b>{ChallengeCalendar.PeriodLabel(prevY, prevM)} davri bo'yicha {countPrev} nafar Challenge g'olibi e'lon qilindi!</b>", ct: ct);
                    }
                    else
                    {
                        await _bot.SendHtmlAsync(chatId, "⚠️ Challenge g'oliblari aniqlanmadi.", ct: ct);
                    }
                }
                break;

            case "🌐 Web ilova":
            case "Web ilova":
            case "🔑 Saytga kirish kodi":
            case "/login":
            case "/app":
                var otp = _bot.GenerateLoginOtp(user.Id, tgUserId, user.FullName, user.Username);
                await _bot.SendHtmlAsync(chatId, BuildOtpCardHtml(user, otp, false), BuildOtpInlineMarkup(otp.Code), ct: ct);
                break;

            case "📚 Kitoblarim":
            case "/kitoblar":
                await SendReadingDashboardAsync(db, chatId, user, ct);
                break;

            case "📖 Bet kiritish":
            case "/bet":
                await PromptSelectBookForLoggingAsync(db, chatId, user, session, ct);
                break;

            case "✍️ Fikr bildirish":
            case "💭 Fikr bildirish":
            case "Fikr bildirish":
            case "/fikr":
                await PromptFeedbackMenuAsync(chatId, ct);
                break;

            case "✍️ Taqriz yozish":
            case "Taqriz yozish":
            case "✍️ Taqrizlar":
            case "/taqriz":
                await StartBookSelectionFlowAsync(db, chatId, user, session, "post", ct);
                break;

            case "💬 Iqtibos yozish":
            case "Iqtibos yozish":
            case "💬 Iqtiboslar":
            case "/iqtibos":
                await StartBookSelectionFlowAsync(db, chatId, user, session, "quote", ct);
                break;

            case "🏆 Challenge":
            case "/challenge":
                await SendChallengeDashboardAsync(db, mediator, chatId, user, ct);
                break;

            case "🔄 Kitob almashish":
            case "/almashish":
                await SendPhysicalBooksDashboardAsync(db, chatId, user, ct);
                break;

            case "👤 Profilim":
            case "/profil":
                await SendProfileDashboardAsync(db, chatId, user, ct);
                break;

            case "/bugun":
                var report = await _bot.BuildDailyReportHtmlAsync(db, UzTime.Today, ct);
                await _bot.SendHtmlAsync(chatId, report, ct: ct);
                break;

            case "🛠 Admin va Challenge boshqaruvi":
            case "/admin":
                if (user.Role < UserRole.Admin)
                {
                    await _bot.SendHtmlAsync(chatId, "⛔ Sizda Admin huquqi mavjud emas.", ct: ct);
                    return;
                }
                await SendAdminDashboardAsync(db, chatId, user, ct);
                break;
        }
    }

    // ── 0) 💭 Fikr bildirish (Taqriz / Iqtibos tanlovi) ──────────────────────────

    private async Task PromptFeedbackMenuAsync(long chatId, CancellationToken ct)
    {
        var html = "💭 <b>Fikr bildirish</b>\n\n" +
                   "Kitob bo'yicha qanday fikr yoki taassurot qoldirmoqchisiz?\n\n" +
                   "✍️ <b>Taqriz yozish</b> — O'qigan kitobingiz haqida to'liq fikr va tavsiyalar\n" +
                   "💬 <b>Iqtibos yozish</b> — Kitobdan sizga yoqqan ibratli parcha va jumlalar";

        var markup = new
        {
            inline_keyboard = new object[][]
            {
                new object[]
                {
                    new { text = "✍️ Taqriz yozish", callback_data = "post:new" },
                    new { text = "💬 Iqtibos yozish", callback_data = "quote:new" }
                }
            }
        };

        await _bot.SendHtmlAsync(chatId, html, markup, ct: ct);
    }

    // ── 1) 📚 Kitoblarim va Mutolaa Rejalari ──────────────────────────────────────

    private async Task SendReadingDashboardAsync(IAppDbContext db, long chatId, User user, CancellationToken ct)
    {
        var goals = await db.ReadingGoals
            .AsNoTracking()
            .Include(g => g.Book)
            .Where(g => g.UserId == user.Id)
            .OrderByDescending(g => g.IsActive)
            .ThenByDescending(g => g.StartDate)
            .ToListAsync(ct);

        var activeGoals = goals.Where(g => g.IsActive).ToList();
        var finishedCount = goals.Count(g => !g.IsActive);

        var today = UzTime.Today;
        var todayPages = await db.ReadingProgress
            .Where(p => p.ReadingGoal.UserId == user.Id && p.Date == today)
            .SumAsync(p => (int?)p.PagesReadToday, ct) ?? 0;

        var sb = new StringBuilder();
        sb.AppendLine("📚 <b>Mening kitoblarim</b>\n");
        sb.AppendLine($"🔥 Bugun: <b>{todayPages} bet</b>");
        sb.AppendLine($"📖 Faol kitoblar: <b>{activeGoals.Count} ta</b> · Tugatilgan: <b>{finishedCount} ta</b>\n");

        var inlineRows = new List<object[]>();

        if (activeGoals.Count == 0)
        {
            sb.AppendLine("<i>Hozircha faol kitobingiz yo'q. Yangi kitob qo'shing:</i>");
        }
        else
        {
            var idx = 0;
            foreach (var g in activeGoals)
            {
                idx++;
                var bar = TelegramBotService.BuildProgressBar(g.CurrentPage, g.Book.TotalPages);
                sb.AppendLine($"<b>{idx}. {TelegramBotService.Escape(g.Book.Title)}</b>");
                sb.AppendLine($"   {bar} {g.CurrentPage}/{g.Book.TotalPages} bet\n");

                inlineRows.Add(new object[]
                {
                    new { text = $"📖 Bet kiritish: {Truncate(g.Book.Title, 22)}", callback_data = $"read:log:{g.Id}" }
                });
            }
        }

        inlineRows.Add(new object[]
        {
            new { text = "➕ Yangi kitob qo'shish", callback_data = "read:add" }
        });

        await _bot.SendHtmlAsync(chatId, sb.ToString(), new { inline_keyboard = inlineRows }, ct: ct);
    }

    private async Task PromptSelectBookForLoggingAsync(
        IAppDbContext db, long chatId, User user, UserBotSession session, CancellationToken ct)
    {
        var activeGoals = await db.ReadingGoals
            .AsNoTracking()
            .Include(g => g.Book)
            .Where(g => g.UserId == user.Id && g.IsActive)
            .ToListAsync(ct);

        if (activeGoals.Count == 0)
        {
            await _bot.SendHtmlAsync(chatId,
                "📚 <b>Sizda hozircha faol kitob yo'q.</b>\nAvval o'qiyotgan kitobingizni qo'shing:",
                new
                {
                    inline_keyboard = new object[][]
                    {
                        new object[] { new { text = "➕ Yangi kitob qo'shish", callback_data = "read:add" } }
                    }
                }, ct: ct);
            return;
        }

        if (activeGoals.Count == 1)
        {
            var g = activeGoals[0];
            session.TempGoalId = g.Id;
            session.Step = SessionStep.LogPagesAmount;

            await _bot.SendHtmlAsync(chatId,
                $"📖 <b>«{TelegramBotService.Escape(g.Book.Title)}»</b>\n" +
                $"<blockquote>Joriy holat: <b>{g.CurrentPage} / {g.Book.TotalPages}</b> bet\n" +
                $"Kunlik maqsad: <b>{g.DailyPageGoal}</b> bet</blockquote>\n\n" +
                "✍️ <b>Bugun necha bet o'qidingiz?</b> <i>(Masalan: <code>25</code>)</i>:", ct: ct);
            return;
        }

        var rows = activeGoals
            .Select(g => new object[]
            {
                new { text = $"📖 {g.Book.Title} ({g.CurrentPage}/{g.Book.TotalPages})", callback_data = $"read:log:{g.Id}" }
            })
            .ToList();

        await _bot.SendHtmlAsync(chatId,
            "📖 <b>Qaysi kitob bo'yicha bugungi o'qilgan betlarni kiritamiz?</b>",
            new { inline_keyboard = rows }, ct: ct);
    }

    // ── 2) 💬 Iqtiboslar va Izohlar ──────────────────────────────────────────────

    private async Task SendQuotesDashboardAsync(IAppDbContext db, long chatId, User user, CancellationToken ct)
    {
        var quotes = await db.Quotes
            .AsNoTracking()
            .Include(q => q.User)
            .Include(q => q.Book)
            .Include(q => q.Likes)
            .Include(q => q.Comments)
            .OrderByDescending(q => q.CreatedAt)
            .Take(3)
            .ToListAsync(ct);

        await _bot.SendHtmlAsync(chatId,
            "💬 <b>KITOBLARDAN IQTIBOSLAR</b>\n" +
            "<blockquote>O'zingizga yoqqan iqtiboslarni ulashing — ular ham veb-saytda, ham <b>KitobdagiMen</b> guruhida e'lon qilinadi! Har bir iqtibosga izoh (comment) qoldirishingiz mumkin.</blockquote>",
            new
            {
                inline_keyboard = new object[][]
                {
                    new object[] { new { text = "✨ Yangi iqtibos yozish", callback_data = "quote:new" } }
                }
            }, ct: ct);

        foreach (var q in quotes)
        {
            var profileSlug = !string.IsNullOrWhiteSpace(q.User.Username) ? q.User.Username : q.User.Id.ToString();
            var profileUrl = $"{_bot.WebAppUrl}/profile/{profileSlug}";
            var profileMiniAppUrl = $"https://t.me/{_bot.BotUsername}?startapp=profile_{profileSlug}";
            var sb = new StringBuilder();
            sb.AppendLine($"\"{TelegramBotService.Escape(q.Text)}\"");
            sb.AppendLine();
            sb.AppendLine($"📚 <b>Asar:</b> \"{TelegramBotService.Escape(q.Book.Title)}\"" +
                          (string.IsNullOrWhiteSpace(q.Book.Author) ? "" : $" — <i>{TelegramBotService.Escape(q.Book.Author)}</i>"));
            var authorLink = TelegramBotService.TryExtractTelegramId(q.User.GoogleId, out var uTgId) && uTgId > 0
                ? $"<a href=\"tg://user?id={uTgId}\">Kitobxon</a>"
                : $"<b>{TelegramBotService.Escape(q.User.FullName)}</b>";
            sb.AppendLine($"✍️ {authorLink}");

            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = $"❤️ Yoqdi ({q.Likes.Count})", callback_data = $"quote:like:{q.Id}" },
                        new { text = $"💬 Izohlar ({q.Comments.Count})", callback_data = $"quote:comments:{q.Id}" },
                        new { text = "✍️ Izoh yozish", callback_data = $"quote:addcomment:{q.Id}" }
                    },
                    new object[]
                    {
                        new { text = "👤 Foydalanuvchi profili", url = profileMiniAppUrl }
                    }
                }
            };

            await _bot.SendHtmlAsync(chatId, sb.ToString(), markup, ct: ct);
        }
    }

    private async Task ShowQuoteCommentsAndPromptAsync(
        IAppDbContext db, long chatId, UserBotSession session, int quoteId, CancellationToken ct)
    {
        var quote = await db.Quotes
            .AsNoTracking()
            .Include(q => q.User)
            .Include(q => q.Book)
            .Include(q => q.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(q => q.Id == quoteId, ct);

        if (quote is null)
        {
            await _bot.SendHtmlAsync(chatId, "⚠️ Iqtibos topilmadi.", ct: ct);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("💬 <b>IQTIBOS VA IZOHLAR</b>");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"<blockquote>«{TelegramBotService.Escape(quote.Text)}»</blockquote>");
        sb.AppendLine($"📚 <b>{TelegramBotService.Escape(quote.Book.Title)}</b>");
        sb.AppendLine();

        var ordered = quote.Comments.OrderBy(c => c.CreatedAt).TakeLast(10).ToList();
        if (ordered.Count == 0)
        {
            sb.AppendLine("<i>Hozircha izohlar yo'q. Birinchi bo'lib fikr bildiring!</i>");
        }
        else
        {
            sb.AppendLine($"💭 <b>Izohlar ({quote.Comments.Count}):</b>");
            foreach (var c in ordered)
            {
                var uHandle = await TelegramBotService.ResolveDisplayHandleAsync(db, c.UserId, c.User.Username, ct);
                sb.AppendLine($"• <b>{TelegramBotService.Escape(c.User.FullName)}</b> ({uHandle}): <i>{TelegramBotService.Escape(c.Text)}</i>");
            }
        }

        session.TempTargetId = quoteId;
        session.Step = SessionStep.CommentOnQuote;
        sb.AppendLine();
        sb.AppendLine("✍️ <b>Ushbu iqtibosga izohingizni yozib yuboring:</b>");

        await _bot.SendHtmlAsync(chatId, sb.ToString(), CancelInlineMarkup, ct: ct);
    }

    private async Task ShowJackpotCommentsAndPromptAsync(
        IAppDbContext db, long chatId, UserBotSession session, int jackpotId, CancellationToken ct)
    {
        var jackpot = await db.DailyJackpots
            .AsNoTracking()
            .Include(j => j.User)
            .Include(j => j.Post!).ThenInclude(p => p.Book)
            .Include(j => j.Quote!).ThenInclude(q => q.Book)
            .Include(j => j.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(j => j.Id == jackpotId, ct);

        if (jackpot is null)
        {
            await _bot.SendHtmlAsync(chatId, "⚠️ Jackpot ma'lumotlari topilmadi.", ct: ct);
            return;
        }

        var isPost = jackpot.Type == "post";
        var book = isPost ? jackpot.Post?.Book : jackpot.Quote?.Book;
        var rawText = isPost ? (jackpot.Post?.ReviewText ?? "") : (jackpot.Quote?.Text ?? "");
        var plain = TelegramBotService.StripHtml(rawText);
        if (plain.Length > 350) plain = plain[..350] + "…";

        var title = isPost ? "Kun taqrizi" : "Kun iqtibosi";

        var sb = new StringBuilder();
        sb.AppendLine($"🎰 <b>{title} (Jackpot) va izohlar</b>");
        sb.AppendLine($"📚 <b>Kitob:</b> «{TelegramBotService.Escape(book?.Title ?? "")}»");
        sb.AppendLine($"👑 <b>Muallif:</b> 🌟 {TelegramBotService.Escape(jackpot.User.FullName)}");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"<blockquote>{TelegramBotService.Escape(plain)}</blockquote>");
        sb.AppendLine();

        var ordered = jackpot.Comments.OrderBy(c => c.CreatedAt).TakeLast(10).ToList();
        if (ordered.Count == 0)
        {
            sb.AppendLine("<i>Hozircha izohlar yo'q. Birinchi bo'lib tabrik yoki fikringizni bildiring!</i>");
        }
        else
        {
            sb.AppendLine($"💭 <b>Izohlar ({jackpot.Comments.Count}):</b>");
            foreach (var c in ordered)
            {
                var uHandle = await TelegramBotService.ResolveDisplayHandleAsync(db, c.UserId, c.User.Username, ct);
                sb.AppendLine($"• <b>{TelegramBotService.Escape(c.User.FullName)}</b> ({uHandle}): <i>{TelegramBotService.Escape(c.Text)}</i>");
            }
        }

        session.TempTargetId = jackpotId;
        session.Step = SessionStep.CommentOnJackpot;
        sb.AppendLine();
        sb.AppendLine("✍️ <b>Kun g'olibiga fikr yoki tabrikingizni yozib yuboring:</b>");

        await _bot.SendHtmlAsync(chatId, sb.ToString(), CancelInlineMarkup, ct: ct);
    }

    private async Task ShowTodayJackpotInBotAsync(
        IAppDbContext db, long chatId, User? viewer, CancellationToken ct)
    {
        var today = UzTime.Today;
        var jackpot = await db.DailyJackpots
            .AsNoTracking()
            .Include(j => j.User)
            .Include(j => j.Post!).ThenInclude(p => p.Book)
            .Include(j => j.Quote!).ThenInclude(q => q.Book)
            .Include(j => j.Likes)
            .Include(j => j.Comments)
            .FirstOrDefaultAsync(j => j.Date == today, ct);

        if (jackpot is null)
        {
            await _bot.SendHtmlAsync(chatId, "🎰 <b>Bugungi jackpot hali aniqlanmoqda...</b>\n\nKuni bilan yozilgan taqriz va iqtiboslar orasidan random tanlanadi!", ct: ct);
            return;
        }

        var isPost = jackpot.Type == "post";
        var book = isPost ? jackpot.Post?.Book : jackpot.Quote?.Book;
        var rawText = isPost ? (jackpot.Post?.ReviewText ?? "") : (jackpot.Quote?.Text ?? "");
        var plain = TelegramBotService.StripHtml(rawText);
        if (plain.Length > 500) plain = plain[..500] + "…";

        var header = isPost ? "🎰 <b>BUGUNGI KUN TAQRIZI (JACKPOT)</b> 👑" : "🎰 <b>BUGUNGI KUN IQTIBOSI (JACKPOT)</b> 👑";
        var authorSuffix = string.IsNullOrWhiteSpace(book?.Author) ? "" : $" — <i>{TelegramBotService.Escape(book.Author)}</i>";

        var sb = new StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine();
        sb.AppendLine($"«{TelegramBotService.Escape(plain)}»");
        sb.AppendLine();
        sb.AppendLine($"📚 <b>Kitob:</b> «{TelegramBotService.Escape(book?.Title ?? "")}»{authorSuffix}");
        sb.AppendLine($"👑 <b>Kun Qahramoni:</b> 🌟 <b>{TelegramBotService.Escape(jackpot.User.FullName)}</b> 👑");
        sb.AppendLine();
        sb.AppendLine($"❤️ Yoqdi: <b>{jackpot.Likes.Count}</b> · 💬 Fikrlar: <b>{jackpot.Comments.Count}</b>");
        sb.AppendLine();
        sb.AppendLine("✨ <i>Ushbu kitobxon bugun platformamizda \"Kun Qahramoni\" premium maqomiga ega!</i>");

        var isLiked = viewer is not null && jackpot.Likes.Any(l => l.UserId == viewer.Id);
        var likeBtnText = isLiked ? $"❤️ Sizga yoqdi ({jackpot.Likes.Count})" : $"🤍 Yoqdi ({jackpot.Likes.Count})";

        var markup = new
        {
            inline_keyboard = new object[][]
            {
                new object[]
                {
                    new { text = likeBtnText, callback_data = $"jackpot:like:{jackpot.Id}" },
                    new { text = "💬 Fikr bildirish", callback_data = $"jackpot:comments:{jackpot.Id}" }
                },
                new object[]
                {
                    new { text = "👑 Saytda ochish (Jackpot Zali)", web_app = new { url = $"{_bot.WebAppUrl}/jackpot" } }
                }
            }
        };

        await _bot.SendHtmlAsync(chatId, sb.ToString(), markup, ct: ct);
    }

    // ── 3) ✍️ Taqrizlar (Postlar) va Izohlar ─────────────────────────────────────

    private async Task SendPostsDashboardAsync(IAppDbContext db, long chatId, User user, CancellationToken ct)
    {
        var posts = await db.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Book)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt)
            .Take(3)
            .ToListAsync(ct);

        await _bot.SendHtmlAsync(chatId,
            "✍️ <b>KITOB TAQRIZLARI VA SHARHLAR</b>\n" +
            "<blockquote>O'qigan kitoblaringiz haqida taqriz yozing — taqrizingiz veb-sayt lentasiga va <b>KitobdagiMen</b> guruhiga joylanadi!</blockquote>",
            new
            {
                inline_keyboard = new object[][]
                {
                    new object[] { new { text = "➕ Yangi taqriz yozish", callback_data = "post:new" } }
                }
            }, ct: ct);

        foreach (var p in posts)
        {
            var profileSlug = !string.IsNullOrWhiteSpace(p.User.Username) ? p.User.Username : p.User.Id.ToString();
            var profileUrl = $"{_bot.WebAppUrl}/profile/{profileSlug}";
            var profileMiniAppUrl = $"https://t.me/{_bot.BotUsername}?startapp=profile_{profileSlug}";
            var plain = TelegramBotService.StripHtml(p.ReviewText);
            if (plain.Length > 500) plain = plain[..500] + "…";

            var sb = new StringBuilder();
            sb.AppendLine($"\"{TelegramBotService.Escape(plain)}\"");
            sb.AppendLine();
            sb.AppendLine($"📚 <b>Asar:</b> \"{TelegramBotService.Escape(p.Book.Title)}\"" +
                          (string.IsNullOrWhiteSpace(p.Book.Author) ? "" : $" — <i>{TelegramBotService.Escape(p.Book.Author)}</i>"));
            var authorLink = TelegramBotService.TryExtractTelegramId(p.User.GoogleId, out var uTgId) && uTgId > 0
                ? $"<a href=\"tg://user?id={uTgId}\">Kitobxon</a>"
                : $"<b>{TelegramBotService.Escape(p.User.FullName)}</b>";
            sb.AppendLine($"✍️ {authorLink}");

            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = $"❤️ Yoqdi ({p.Likes.Count})", callback_data = $"post:like:{p.Id}" },
                        new { text = $"💬 Izohlar ({p.Comments.Count})", callback_data = $"post:comments:{p.Id}" },
                        new { text = "✍️ Izoh yozish", callback_data = $"post:addcomment:{p.Id}" }
                    },
                    new object[]
                    {
                        new { text = "👤 Foydalanuvchi profili", url = profileMiniAppUrl }
                    }
                }
            };

            await _bot.SendHtmlAsync(chatId, sb.ToString(), markup, ct: ct);
        }
    }

    private async Task ShowPostCommentsAndPromptAsync(
        IAppDbContext db, long chatId, UserBotSession session, int postId, CancellationToken ct)
    {
        var post = await db.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Book)
            .Include(p => p.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Id == postId, ct);

        if (post is null)
        {
            await _bot.SendHtmlAsync(chatId, "⚠️ Taqriz topilmadi.", ct: ct);
            return;
        }

        var plain = TelegramBotService.StripHtml(post.ReviewText);
        if (plain.Length > 400) plain = plain[..400] + "…";

        var sb = new StringBuilder();
        sb.AppendLine($"📖 <b>«{TelegramBotService.Escape(post.Book.Title)}» taqrizi va izohlari</b>");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"<blockquote>{TelegramBotService.Escape(plain)}</blockquote>");
        sb.AppendLine();

        var ordered = post.Comments.OrderBy(c => c.CreatedAt).TakeLast(10).ToList();
        if (ordered.Count == 0)
        {
            sb.AppendLine("<i>Hozircha izohlar yo'q. Birinchi bo'lib fikr bildiring!</i>");
        }
        else
        {
            sb.AppendLine($"💭 <b>Izohlar ({post.Comments.Count}):</b>");
            foreach (var c in ordered)
            {
                var uHandle = await TelegramBotService.ResolveDisplayHandleAsync(db, c.UserId, c.User.Username, ct);
                sb.AppendLine($"• <b>{TelegramBotService.Escape(c.User.FullName)}</b> ({uHandle}): <i>{TelegramBotService.Escape(c.Text)}</i>");
            }
        }

        session.TempTargetId = postId;
        session.Step = SessionStep.CommentOnPost;
        sb.AppendLine();
        sb.AppendLine("✍️ <b>Ushbu taqrizga izohingizni yozib yuboring:</b>");

        await _bot.SendHtmlAsync(chatId, sb.ToString(), CancelInlineMarkup, ct: ct);
    }

    // ── 4) 🏆 Challenge va 1-, 2-, 3-o'rin Sovg'a Kitoblar ──────────────────────

    private async Task SendChallengeDashboardAsync(
        IAppDbContext db, ISender mediator, long chatId, User? viewer, CancellationToken ct)
    {
        var cfg = await ChallengeConfigStore.GetOrDefaultAsync(db, ct);
        var (cy, cm) = ChallengeCalendar.CurrentPeriod();

        var standings = await mediator.Send(new GetChallengeStandingsQuery
        {
            Year = cy,
            Month = cm,
            Limit = 10
        }, ct);

        var sb = new StringBuilder();
        sb.AppendLine($"🏆 <b>{TelegramBotService.Escape(cfg.Title)}</b>");
        sb.AppendLine($"📅 {TelegramBotService.Escape(cfg.FormattedRange)} · ⏳ <b>{cfg.DaysRemaining} kun qoldi</b>\n");

        var p1 = cfg.GetPrize(1);
        var p2 = cfg.GetPrize(2);
        var p3 = cfg.GetPrize(3);
        sb.AppendLine("🎁 <b>Sovg'alar:</b>");
        sb.AppendLine($"🥇 1: {TelegramBotService.Escape(p1.Title ?? "—")}");
        sb.AppendLine($"🥈 2: {TelegramBotService.Escape(p2.Title ?? "—")}");
        sb.AppendLine($"🥉 3: {TelegramBotService.Escape(p3.Title ?? "—")}\n");

        sb.AppendLine("🔥 <b>Top-10 reyting:</b>");
        if (standings.Count == 0)
        {
            sb.AppendLine("<i>Hozircha bu davrda hech kim bet kiritmadi. Birinchi bo'lib peshqadamga aylaning!</i>");
        }
        else
        {
            foreach (var s in standings)
            {
                var medal = s.Rank switch
                {
                    1 => "🥇",
                    2 => "🥈",
                    3 => "🥉",
                    _ => $"<b>{s.Rank}.</b>"
                };
                var handle = await TelegramBotService.ResolveDisplayHandleAsync(db, s.UserId, s.Username, ct);
                var meTag = viewer is not null && viewer.Id == s.UserId ? " 👈 <b>(Siz)</b>" : "";
                sb.AppendLine($"{medal} {handle} ({TelegramBotService.Escape(s.FullName)}) — <b>{s.PagesRead} bet</b> · {s.BooksRead} kitob{meTag}");
            }
        }

        var inlineRows = new List<object[]>
        {
            new object[]
            {
                new { text = "📖 Bet kiritish", callback_data = "read:log_menu" },
                new { text = "🔄 Yangilash", callback_data = "menu:challenge" }
            }
        };

        if (viewer is not null && viewer.Role >= UserRole.Admin)
        {
            inlineRows.Add(new object[]
            {
                new { text = "🛠 Challenge'ni boshqarish (Admin)", callback_data = "menu:admin" }
            });
        }

        await _bot.SendHtmlAsync(chatId, sb.ToString(), new { inline_keyboard = inlineRows }, ct: ct);
    }

    // ── 5) 🔄 Kitob Almashish ────────────────────────────────────────────────────

    private async Task SendPhysicalBooksDashboardAsync(IAppDbContext db, long chatId, User user, CancellationToken ct)
    {
        var items = await db.PhysicalBooks
            .AsNoTracking()
            .Include(p => p.Book)
            .Include(p => p.Owner)
            .Where(p => p.Status == PhysicalBookStatus.Mavjud)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("🔄 <b>KITOB ALMASHISH VA HADYA BOZORI</b>");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("<blockquote>O'qib bo'lgan qog'oz kitoblaringizni boshqa kitobxonlar bilan almashing yoki vaqtincha o'qishga bering! Kitob holatini ko'rish uchun fotosurati ham ko'rsatiladi.</blockquote>");
        sb.AppendLine();

        var inlineButtons = new List<object[]>();

        if (items.Count == 0)
        {
            sb.AppendLine("<i>Hozircha bo'sh kitob e'lonlari yo'q. Birinchi bo'lib kitobingizni qo'shing!</i>");
        }
        else
        {
            foreach (var item in items)
            {
                var ownerHandle = await TelegramBotService.ResolveDisplayHandleAsync(db, item.OwnerId, item.Owner.Username, ct);
                var bTitle = item.Book?.Title ?? item.ManualTitle ?? "Kitob";
                var bAuthor = item.Book?.Author ?? item.ManualAuthor ?? "";
                var hasPhoto = !string.IsNullOrWhiteSpace(item.ManualCoverUrl ?? item.Book?.CoverUrl) ? "📸 " : "📕 ";

                sb.AppendLine($"{hasPhoto}<b>{TelegramBotService.Escape(bTitle)}</b>" +
                              (string.IsNullOrWhiteSpace(bAuthor) ? "" : $" — <i>{TelegramBotService.Escape(bAuthor)}</i>"));
                sb.AppendLine($"   👤 Egasi: {TelegramBotService.Escape(item.Owner.FullName)} ({ownerHandle}) · 🟢 Mavjud");
                sb.AppendLine();

                if (item.OwnerId != user.Id)
                {
                    var shortTitle = bTitle.Length > 22 ? bTitle[..22] + "…" : bTitle;
                    inlineButtons.Add(new object[]
                    {
                        new { text = $"📖 O'qimoqchiman: {shortTitle}", callback_data = $"pb:reserve:{item.Id}" }
                    });
                }
            }
        }

        inlineButtons.Add(new object[]
        {
            new { text = "➕ Kitob e'lonini qo'shish", callback_data = "pb:new" }
        });
        inlineButtons.Add(new object[]
        {
            new { text = "🌐 Barcha kitoblar (veb-saytda)", url = $"{_bot.WebAppUrl}/almashish" }
        });

        await _bot.SendHtmlAsync(chatId, sb.ToString(), new { inline_keyboard = inlineButtons }, ct: ct);
    }

    // ── 6) 👤 Profilim va Rasm O'rnatish ─────────────────────────────────────────

    private async Task SendProfileDashboardAsync(IAppDbContext db, long chatId, User user, CancellationToken ct)
    {
        var totalPages = await db.ReadingProgress
            .Where(p => p.ReadingGoal.UserId == user.Id)
            .SumAsync(p => (int?)p.PagesReadToday, ct) ?? 0;

        var booksCount = await db.ReadingGoals.CountAsync(g => g.UserId == user.Id, ct);
        var postsCount = await db.Posts.CountAsync(p => p.UserId == user.Id, ct);
        var quotesCount = await db.Quotes.CountAsync(q => q.UserId == user.Id, ct);
        var followersCount = await db.Follows.CountAsync(f => f.FollowingId == user.Id, ct);

        var sb = new StringBuilder();
        sb.AppendLine($"👤 <b>{TelegramBotService.Escape(user.FullName)}</b> (@{TelegramBotService.Escape(user.Username ?? "kitobxon")})\n");
        if (!string.IsNullOrWhiteSpace(user.Bio))
        {
            sb.AppendLine($"<i>{TelegramBotService.Escape(user.Bio)}</i>\n");
        }
        sb.AppendLine($"📖 O'qilgan: <b>{totalPages} bet</b> · 📚 Kitoblar: <b>{booksCount} ta</b>");
        sb.AppendLine($"✍️ Taqrizlar: <b>{postsCount}</b> · 💬 Iqtiboslar: <b>{quotesCount}</b> · 👥 Kuzatuvchilar: <b>{followersCount}</b>");

        var markup = new
        {
            inline_keyboard = new object[][]
            {
                new object[]
                {
                    new { text = "🖼 Profil rasmini o'rnatish", callback_data = "profile:avatar" }
                },
                new object[]
                {
                    new { text = "✏️ Ismni o'zgartirish", callback_data = "profile:name" },
                    new { text = "🔖 Username o'zgartirish", callback_data = "profile:username" }
                },
                new object[]
                {
                    new { text = "📝 Bio yozish", callback_data = "profile:bio" },
                    new { text = "🔑 Kirish kodi olish", callback_data = "otp:resend" }
                }
            }
        };

        await _bot.SendHtmlAsync(chatId, sb.ToString(), markup, ct: ct);
    }

    // ── 7) 🛠 Admin va Challenge Boshqaruvi ──────────────────────────────────────

    private async Task SendAdminDashboardAsync(IAppDbContext db, long chatId, User user, CancellationToken ct)
    {
        var cfg = await ChallengeConfigStore.GetOrDefaultAsync(db, ct);
        var groupTarget = await _bot.GetGroupTargetAsync(db, ct);
        var groupDisplay = groupTarget is null
            ? "Hali ulanmagan"
            : groupTarget.Value.TopicId.HasValue
                ? $"{groupTarget.Value.ChatId} (Topic #{groupTarget.Value.TopicId.Value})"
                : groupTarget.Value.ChatId.ToString();

        var p1 = cfg.GetPrize(1);
        var p2 = cfg.GetPrize(2);
        var p3 = cfg.GetPrize(3);

        var stats = await _bot.GetBotUserStatsAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("🛠 <b>ADMIN VA BOSHQARUV MARKAZI</b>");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("📊 <b>FOYDALANUVCHILAR VA BOT STATISTIKASI:</b>");
        sb.AppendLine($"<blockquote>👥 <b>Jami foydalanuvchilar:</b> {stats.TotalUsers} nafar\n" +
                      $"🤖 <b>Telegram bot a'zolari:</b> {stats.TelegramUsers} nafar\n" +
                      $"   • 🟢 <b>Faol bot a'zolari:</b> {stats.ActiveBotUsers} nafar ({(100.0 - stats.BlockedPercent):0.#}%)\n" +
                      $"   • 🚫 <b>Botni bloklaganlar:</b> {stats.BlockedBotUsers} nafar ({stats.BlockedPercent:0.#}%)\n" +
                      $"🌐 <b>Sayt orqali:</b> {stats.WebsiteOnlyUsers} nafar\n" +
                      $"📈 <b>Yangi:</b> Bugun +{stats.TodayNewUsers} · 7 kunda +{stats.WeekNewUsers}\n" +
                      $"🔥 <b>Bugun faol:</b> {stats.TodayActiveUsers} nafar</blockquote>");
        sb.AppendLine();
        sb.AppendLine($"📌 <b>Challenge:</b> {TelegramBotService.Escape(cfg.Title)} ({cfg.FormattedRange})");
        sb.AppendLine($"👥 <b>Guruh ID:</b> <code>{TelegramBotService.Escape(groupDisplay)}</code>");
        sb.AppendLine();
        sb.AppendLine("🎁 <b>Sovg'a kitoblar:</b>");
        sb.AppendLine($"<blockquote>🥇 1-o'rin: {TelegramBotService.Escape(p1.Title ?? "—")}\n" +
                      $"🥈 2-o'rin: {TelegramBotService.Escape(p2.Title ?? "—")}\n" +
                      $"🥉 3-o'rin: {TelegramBotService.Escape(p3.Title ?? "—")}</blockquote>");
        sb.AppendLine();
        sb.AppendLine("<i>Quyidagi tugmalar orqali statistika, bot holati va Challenge boshqaruvini amalga oshiring:</i>");

        var markup = new
        {
            inline_keyboard = new object[][]
            {
                new object[]
                {
                    new { text = "📊 Batafsil statistika", callback_data = "admin:user_stats" },
                    new { text = "🔄 Blok holatini tekshirish", callback_data = "admin:sync_blocked" }
                },
                new object[]
                {
                    new { text = "🚫 Bloklaganlar ro'yxati", callback_data = "admin:list_blocked" },
                    new { text = "📅 Challenge sanasi", callback_data = "admin:ch_dates" }
                },
                new object[]
                {
                    new { text = "🥇 1-o'rin", callback_data = "admin:prize:1" },
                    new { text = "🥈 2-o'rin", callback_data = "admin:prize:2" },
                    new { text = "🥉 3-o'rin", callback_data = "admin:prize:3" }
                },
                new object[]
                {
                    new { text = "📣 Challenge e'loni", callback_data = "admin:announce_ch" },
                    new { text = "🏆 G'oliblarni e'lon qilish", callback_data = "admin:announce_winners" }
                },
                new object[]
                {
                    new { text = "👥 KitobdagiMen Guruh ID", callback_data = "admin:set_group" }
                },
                new object[]
                {
                    new { text = "📊 23:59 Hisobotni yuborish", callback_data = "admin:send_report" },
                    new { text = "🔔 20:00 Eslatmani yuborish", callback_data = "admin:send_reminder" }
                }
            }
        };

        await _bot.SendHtmlAsync(chatId, sb.ToString(), markup, ct: ct);
    }

    // ── Callback Query Handler ───────────────────────────────────────────────────

    private async Task HandleCallbackQueryAsync(JsonNode cb, CancellationToken ct)
    {
        var cbId = cb["id"]?.GetValue<string>() ?? "";
        var data = cb["data"]?.GetValue<string>() ?? "";
        var from = cb["from"];
        var msg = cb["message"];
        if (from is null || msg is null) return;

        var tgUserId = from["id"]?.GetValue<long>() ?? 0;
        var tgUsername = from["username"]?.GetValue<string>();
        var chatId = msg["chat"]?["id"]?.GetValue<long>() ?? 0;
        var messageId = msg["message_id"]?.GetValue<int>() ?? 0;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        var session = _sessions.GetOrAdd(tgUserId, _ => new UserBotSession());

        // Guruh a'zoligini tasdiqlash tugmasi
        if (data == "sub:check")
        {
            var isMember = await _bot.IsUserMemberOfBookClubAsync(tgUserId, ct);
            if (!isMember)
            {
                await _bot.AnswerCallbackAsync(cbId, "❌ Siz hali «KitobdagiMen» guruhiga qo'shilmadingiz! Iltimos, avval guruhga a'zo bo'ling.", showAlert: true, ct: ct);
                return;
            }

            await _bot.AnswerCallbackAsync(cbId, "🎉 Guruhga a'zo bo'lganingiz tasdiqlandi!", showAlert: false, ct: ct);
            var memberUser = await FindAndSyncUserAsync(db, tgUserId, tgUsername, ct);
            if (memberUser is null)
            {
                await HandleUnregisteredUserFlowAsync(db, chatId, tgUserId, tgUsername,
                    from["first_name"]?.GetValue<string>() ?? "",
                    from["last_name"]?.GetValue<string>() ?? "",
                    "/start", session, ct);
            }
            else
            {
                var welcome =
                    "🎉 <b>Guruhga a'zo bo'lganingiz tasdiqlandi!</b>\n\n" +
                    "<blockquote>📚 <b>KitobdagiMen</b> platformasiga xush kelibsiz! Endi barcha buyruqlar, mutolaa monitoringi, challenge va xizmatlardan to'liq foydalanishingiz mumkin. 🚀</blockquote>";
                await _bot.SendHtmlAsync(chatId, welcome, BuildMainMenuKeyboard(memberUser.Role >= UserRole.Admin), ct: ct);
            }
            return;
        }

        // Boshqa har qanday so'rov/tugma uchun guruh a'zoligini tekshiramiz
        if (!IsConfiguredSuperAdmin(tgUserId, tgUsername, null))
        {
            var isMember = await _bot.IsUserMemberOfBookClubAsync(tgUserId, ct);
            if (!isMember)
            {
                await _bot.AnswerCallbackAsync(cbId, "⚠️ Botdan foydalanish uchun avval «KitobdagiMen» guruhiga qo'shiling!", showAlert: true, ct: ct);
                await SendMustJoinGroupPromptAsync(chatId, ct);
                return;
            }
        }

        // Bekor qilish tugmasi (cancel)
        if (data == "cancel_input")
        {
            session.Step = SessionStep.None;
            session.TempCoverUrl = null;
            session.TargetFlow = null;
            session.SelectedBookId = null;
            session.SearchMessageId = 0;
            await _bot.AnswerCallbackAsync(cbId, "Amal bekor qilindi.", showAlert: false, ct: ct);
            var memberUser = await FindAndSyncUserAsync(db, tgUserId, tgUsername, ct);
            if (memberUser is not null)
            {
                await _bot.SendHtmlAsync(chatId, "✅ Amal bekor qilindi. Asosiy menyudasiz:", BuildMainMenuKeyboard(memberUser.Role >= UserRole.Admin), ct: ct);
            }
            return;
        }

        // Ro'yxatdan o'tishda tavsiya qilingan username tugmasi
        if (data.StartsWith("reg:user:", StringComparison.Ordinal))
        {
            var chosen = data["reg:user:".Length..];
            await _bot.AnswerCallbackAsync(cbId, $"@{chosen} tanlandi!", ct: ct);
            await CompleteRegistrationWithUsernameAsync(db, chatId, tgUserId, tgUsername, session, chosen, ct);
            return;
        }

        var user = await FindAndSyncUserAsync(db, tgUserId, tgUsername, ct);
        if (user is null)
        {
            await _bot.AnswerCallbackAsync(cbId, "Avval bot orqali ro'yxatdan o'ting!", showAlert: true, ct: ct);
            return;
        }

        // Kitob qidiruv va tanlov (Book search and selection) callbacklari
        if (data.StartsWith("b_search:", StringComparison.Ordinal))
        {
            var flow = data["b_search:".Length..];
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await PromptSearchBookAsync(chatId, session, flow, ct);
            return;
        }

        if (data.StartsWith("b_retry:", StringComparison.Ordinal))
        {
            var flow = data["b_retry:".Length..];
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await PromptSearchBookAsync(chatId, session, flow, ct);
            return;
        }

        if (data.StartsWith("b_new:", StringComparison.Ordinal))
        {
            var flow = data["b_new:".Length..];
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            session.TargetFlow = flow;
            session.SelectedBookId = null;
            session.TempBookTitle = null;
            session.TempBookAuthor = null;
            session.Step = SessionStep.AddBookTitle;
            await _bot.SendHtmlAsync(chatId,
                "📚 <b>Yangi kitob yaratish (1/2):</b>\n\nIltimos, kitobning <b>nomini</b> kiriting <i>(masalan: O'tkan kunlar)</i>:",
                CancelInlineMarkup, ct: ct);
            return;
        }

        if (data.StartsWith("b_sel:", StringComparison.Ordinal))
        {
            var parts = data.Split(':');
            if (parts.Length == 3 && int.TryParse(parts[2], out var selBookId))
            {
                var flow = parts[1];
                await _bot.AnswerCallbackAsync(cbId, ct: ct);
                await HandleBookSelectedAsync(db, chatId, user, session, flow, selBookId, ct);
            }
            return;
        }

        // 1) 🔄 OTP kodni qaytadan yuborish (2 daqiqalik yangi kod)
        if (data == "otp:resend")
        {
            var newOtp = _bot.GenerateLoginOtp(user.Id, tgUserId, user.FullName, user.Username);
            var html = BuildOtpCardHtml(user, newOtp, isNewRegistration: false);
            await _bot.AnswerCallbackAsync(cbId, "✅ Yangi 2 daqiqalik kirish kodi yaratildi!", ct: ct);
            await _bot.EditHtmlAsync(chatId, messageId, html, BuildOtpInlineMarkup(newOtp.Code), ct: ct);
            return;
        }

        // 2) Profil tahrirlash tugmalari
        if (data == "profile:avatar")
        {
            session.Step = SessionStep.AwaitAvatarPhoto;
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await _bot.SendHtmlAsync(chatId,
                "🖼 <b>Profil rasmini o'rnatish:</b>\n\n" +
                "<blockquote>Iltimos, hozir chatga o'zingiz xohlagan <b>rasmni (photo)</b> yuboring — u avtomatik ravishda ham botda, ham veb-saytdagi profilingizda o'rnatiladi!</blockquote>",
                ct: ct);
            return;
        }

        if (data == "profile:name")
        {
            session.Step = SessionStep.EditFullName;
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await _bot.SendHtmlAsync(chatId, "✏️ <b>Yangi Ism va Familiyangizni yozib yuboring:</b>", ct: ct);
            return;
        }

        if (data == "profile:username")
        {
            session.Step = SessionStep.EditUsername;
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await _bot.SendHtmlAsync(chatId, "🔖 <b>Yangi username kiriting</b> <i>(3–28 ta lotin harfi/raqam)</i>:", ct: ct);
            return;
        }

        if (data == "profile:bio")
        {
            session.Step = SessionStep.EditBio;
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await _bot.SendHtmlAsync(chatId, "📝 <b>Profilingiz uchun qisqacha Bio (o'zingiz haqingizda) yozib yuboring:</b>", ct: ct);
            return;
        }

        // 3) Mutolaa va bet kiritish
        if (data == "read:add")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await StartBookSelectionFlowAsync(db, chatId, user, session, "reading", ct);
            return;
        }

        if (data == "read:log_menu")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await PromptSelectBookForLoggingAsync(db, chatId, user, session, ct);
            return;
        }

        if (data.StartsWith("read:log:", StringComparison.Ordinal) &&
            int.TryParse(data["read:log:".Length..], out var goalId))
        {
            var goal = await db.ReadingGoals
                .AsNoTracking()
                .Include(g => g.Book)
                .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == user.Id, ct);

            if (goal is null)
            {
                await _bot.AnswerCallbackAsync(cbId, "Kitob topilmadi.", true, ct);
                return;
            }

            session.TempGoalId = goal.Id;
            session.Step = SessionStep.LogPagesAmount;
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await _bot.SendHtmlAsync(chatId,
                $"📖 <b>«{TelegramBotService.Escape(goal.Book.Title)}»</b>\n" +
                $"<blockquote>Joriy holat: <b>{goal.CurrentPage} / {goal.Book.TotalPages}</b> bet\n" +
                $"Kunlik maqsad: <b>{goal.DailyPageGoal}</b> bet</blockquote>\n\n" +
                "✍️ <b>Bugun necha bet o'qidingiz?</b> <i>(Masalan: <code>30</code>)</i>:", CancelInlineMarkup, ct: ct);
            return;
        }

        // 4) Iqtiboslar (Like, Comment, New)
        if (data is "quote:new" or "feedback:quote")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await StartBookSelectionFlowAsync(db, chatId, user, session, "quote", ct);
            return;
        }

        if (data.StartsWith("quote:like:", StringComparison.Ordinal) &&
            int.TryParse(data["quote:like:".Length..], out var likeQuoteId))
        {
            var existing = await db.QuoteLikes.FirstOrDefaultAsync(l => l.QuoteId == likeQuoteId && l.UserId == user.Id, ct);
            bool isLiked;
            if (existing is null)
            {
                db.QuoteLikes.Add(new QuoteLike { QuoteId = likeQuoteId, UserId = user.Id, CreatedAt = DateTime.UtcNow });
                await db.SaveChangesAsync(ct);
                isLiked = true;
            }
            else
            {
                db.QuoteLikes.Remove(existing);
                await db.SaveChangesAsync(ct);
                isLiked = false;
            }

            var count = await db.QuoteLikes.CountAsync(l => l.QuoteId == likeQuoteId, ct);
            var toastMsg = isLiked ? $"❤️ Yoqdi! Jami: {count}" : $"🤍 Like qaytarib olindi. Jami: {count}";
            await _bot.AnswerCallbackAsync(cbId, toastMsg, ct: ct);
            await UpdateLikeButtonOnMessageAsync(msg, chatId, messageId, data, count, ct);
            _ = _bot.SyncQuoteLikesInGroupAsync(likeQuoteId, count, CancellationToken.None);
            return;
        }

        if ((data.StartsWith("quote:comments:", StringComparison.Ordinal) ||
             data.StartsWith("quote:addcomment:", StringComparison.Ordinal)))
        {
            var idPart = data.Split(':').Last();
            if (int.TryParse(idPart, out var qId))
            {
                await _bot.AnswerCallbackAsync(cbId, ct: ct);
                await ShowQuoteCommentsAndPromptAsync(db, chatId, session, qId, ct);
            }
            return;
        }

        // 5) Taqrizlar / Postlar (Like, Comment, New)
        if (data is "post:new" or "feedback:post")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await StartBookSelectionFlowAsync(db, chatId, user, session, "post", ct);
            return;
        }

        if (data == "feedback:menu")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await PromptFeedbackMenuAsync(chatId, ct);
            return;
        }

        if (data.StartsWith("jackpot:like:", StringComparison.Ordinal) &&
            int.TryParse(data["jackpot:like:".Length..], out var likeJackpotId))
        {
            var existing = await db.DailyJackpotLikes.FirstOrDefaultAsync(l => l.DailyJackpotId == likeJackpotId && l.UserId == user.Id, ct);
            bool isLiked;
            if (existing is null)
            {
                db.DailyJackpotLikes.Add(new DailyJackpotLike { DailyJackpotId = likeJackpotId, UserId = user.Id, CreatedAt = DateTime.UtcNow });
                await db.SaveChangesAsync(ct);
                isLiked = true;
            }
            else
            {
                db.DailyJackpotLikes.Remove(existing);
                await db.SaveChangesAsync(ct);
                isLiked = false;
            }

            var count = await db.DailyJackpotLikes.CountAsync(l => l.DailyJackpotId == likeJackpotId, ct);
            var toastMsg = isLiked ? $"❤️ Sizga ham yoqdi! Jami: {count}" : $"🤍 Like qaytarib olindi. Jami: {count}";
            await _bot.AnswerCallbackAsync(cbId, toastMsg, ct: ct);
            await UpdateLikeButtonOnMessageAsync(msg, chatId, messageId, data, count, ct);
            _ = _bot.SyncJackpotLikesInGroupAsync(likeJackpotId, count, CancellationToken.None);
            return;
        }

        if (data.StartsWith("jackpot:comments:", StringComparison.Ordinal) ||
            data.StartsWith("jackpot:addcomment:", StringComparison.Ordinal))
        {
            var idPart = data.Split(':').Last();
            if (int.TryParse(idPart, out var jId))
            {
                await _bot.AnswerCallbackAsync(cbId, ct: ct);
                await ShowJackpotCommentsAndPromptAsync(db, chatId, session, jId, ct);
            }
            return;
        }

        if (data.StartsWith("post:like:", StringComparison.Ordinal) &&
            int.TryParse(data["post:like:".Length..], out var likePostId))
        {
            var existing = await db.Likes.FirstOrDefaultAsync(l => l.PostId == likePostId && l.UserId == user.Id, ct);
            bool isLiked;
            if (existing is null)
            {
                db.Likes.Add(new Like { PostId = likePostId, UserId = user.Id, CreatedAt = DateTime.UtcNow });
                await db.SaveChangesAsync(ct);
                isLiked = true;
            }
            else
            {
                db.Likes.Remove(existing);
                await db.SaveChangesAsync(ct);
                isLiked = false;
            }

            var count = await db.Likes.CountAsync(l => l.PostId == likePostId, ct);
            var toastMsg = isLiked ? $"❤️ Yoqdi! Jami: {count}" : $"🤍 Like qaytarib olindi. Jami: {count}";
            await _bot.AnswerCallbackAsync(cbId, toastMsg, ct: ct);
            await UpdateLikeButtonOnMessageAsync(msg, chatId, messageId, data, count, ct);
            _ = _bot.SyncPostLikesInGroupAsync(likePostId, count, CancellationToken.None);
            return;
        }

        if ((data.StartsWith("post:comments:", StringComparison.Ordinal) ||
             data.StartsWith("post:addcomment:", StringComparison.Ordinal)))
        {
            var idPart = data.Split(':').Last();
            if (int.TryParse(idPart, out var pId))
            {
                await _bot.AnswerCallbackAsync(cbId, ct: ct);
                await ShowPostCommentsAndPromptAsync(db, chatId, session, pId, ct);
            }
            return;
        }

        // 6) Kitob almashish e'lon qo'shish va amallar
        if (data == "pb:new")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await StartBookSelectionFlowAsync(db, chatId, user, session, "physical", ct);
            return;
        }

        if (data == "pb:skip_cover")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await SaveNewPhysicalBookAsync(db, chatId, user, session, null, ct);
            return;
        }

        if (data == "post:skip_book_cover")
        {
            session.TempCoverUrl = null;
            session.Step = SessionStep.NewPostReviewImage;
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await _bot.SendHtmlAsync(chatId,
                "📸 <b>2-qadam: Taqrizga mos rasm yuboring:</b>\n\n" +
                "Ushbu taqrizingizga mos estetik fotosurat yoki lavha yuboring (yoki rasmsiz chop etish uchun tugmani bosing):",
                new
                {
                    inline_keyboard = new object[][]
                    {
                        new object[] { new { text = "⏭ Taqriz rasmini o'tkazib yuborish", callback_data = "post:skip_review_image" } }
                    }
                }, ct: ct);
            return;
        }

        if (data is "post:skip_review_image" or "post:skip_image")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await SaveNewPostAsync(db, chatId, user, session, session.TempCoverUrl, null, ct);
            return;
        }

        if (data == "quote:skip_book_cover")
        {
            session.TempCoverUrl = null;
            session.Step = SessionStep.NewQuoteImage;
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await _bot.SendHtmlAsync(chatId,
                "📸 <b>2-qadam: Iqtibosga mos rasm yuboring:</b>\n\n" +
                "Ushbu iqtibosga mos estetik fotosurat yoki lavha yuboring (yoki rasmsiz chop etish uchun tugmani bosing):",
                new
                {
                    inline_keyboard = new object[][]
                    {
                        new object[] { new { text = "⏭ Iqtibos rasmini o'tkazib yuborish", callback_data = "quote:skip_image" } }
                    }
                }, ct: ct);
            return;
        }

        if (data is "quote:skip_image" or "quote:skip_quote_image")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await SaveNewQuoteAsync(db, chatId, user, session, session.TempCoverUrl, null, ct);
            return;
        }

        if (data.StartsWith("pb:reserve:", StringComparison.Ordinal))
        {
            var idStr = data["pb:reserve:".Length..];
            if (int.TryParse(idStr, out var pbId))
            {
                var book = await db.PhysicalBooks
                    .Include(p => p.Book)
                    .Include(p => p.Owner)
                    .FirstOrDefaultAsync(p => p.Id == pbId, ct);

                if (book is null)
                {
                    await _bot.AnswerCallbackAsync(cbId, "⚠️ Kitob topilmadi.", showAlert: true, ct: ct);
                    return;
                }

                if (book.OwnerId == user.Id)
                {
                    await _bot.AnswerCallbackAsync(cbId, "O'zingizning kitobingizni band qila olmaysiz!", showAlert: true, ct: ct);
                    return;
                }

                if (book.Status != PhysicalBookStatus.Mavjud)
                {
                    await _bot.AnswerCallbackAsync(cbId, "Ushbu kitob hozir band qilingan yoki o'qilmoqda.", showAlert: true, ct: ct);
                    return;
                }

                var reservation = new PhysicalBookReservation
                {
                    PhysicalBookId = book.Id,
                    ReserverId = user.Id,
                    ReservedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    IsConfirmed = false
                };
                db.PhysicalBookReservations.Add(reservation);
                book.Status = PhysicalBookStatus.BandQilindi;
                await db.SaveChangesAsync(ct);

                await _bot.AnswerCallbackAsync(cbId, "✅ Kitob band qilindi!", showAlert: true, ct: ct);

                var bTitle = book.Book?.Title ?? book.ManualTitle ?? "Kitob";
                await _bot.SendHtmlAsync(chatId,
                    $"📖 <b>«{TelegramBotService.Escape(bTitle)}» kitobini o'qish istagini bildirdingiz!</b>\n\n" +
                    "<blockquote>Kitob egasiga Telegram orqali xabar yuborildi. Egasi <b>«Qabul qilish»</b> tugmasini bosishi bilanoq, sizga uning Telegram aloqasi yuboriladi va kitobni qayerda topshirishni kelishib olishingiz mumkin bo'ladi!</blockquote>",
                    ct: ct);

                // Kitob egasiga Telegram orqali bildirishnoma yuboriladi
                await _bot.NotifyPhysicalBookReservedAsync(book.Id, user.Id, ct);
            }
            return;
        }

        if (data.StartsWith("pb:accept:", StringComparison.Ordinal))
        {
            var parts = data["pb:accept:".Length..].Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var pbId) && int.TryParse(parts[1], out var reserverId))
            {
                var book = await db.PhysicalBooks
                    .Include(p => p.Book)
                    .Include(p => p.Reservations)
                        .ThenInclude(r => r.Reserver)
                    .FirstOrDefaultAsync(p => p.Id == pbId, ct);

                if (book is null)
                {
                    await _bot.AnswerCallbackAsync(cbId, "Kitob topilmadi.", showAlert: true, ct: ct);
                    return;
                }

                if (book.OwnerId != user.Id)
                {
                    await _bot.AnswerCallbackAsync(cbId, "Faqat kitob egasi qabul qila oladi!", showAlert: true, ct: ct);
                    return;
                }

                var reservation = book.Reservations
                    .Where(r => r.ReserverId == reserverId && !r.IsConfirmed)
                    .OrderByDescending(r => r.ReservedAt)
                    .FirstOrDefault();

                if (reservation is null && book.Status == PhysicalBookStatus.OqiyApti)
                {
                    await _bot.AnswerCallbackAsync(cbId, "Bu so'rov allaqachon qabul qilingan!", showAlert: true, ct: ct);
                    return;
                }

                if (reservation != null)
                {
                    reservation.IsConfirmed = true;
                }
                book.Status = PhysicalBookStatus.OqiyApti;
                await db.SaveChangesAsync(ct);

                await _bot.AnswerCallbackAsync(cbId, "✅ Qabul qilindi!", ct: ct);

                // Ikkala tomonga bir-birining Telegram usernamelarini yuborish
                await _bot.NotifyPhysicalBookHandoverConfirmedAsync(book.Id, user.Id, reserverId, ct);

                var bTitle = book.Book?.Title ?? book.ManualTitle ?? "Kitob";
                var reserver = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == reserverId, ct);
                var reserverHandle = await TelegramBotService.ResolveDisplayHandleAsync(db, reserverId, reserver?.Username, ct);

                var updatedText =
                    $"✅ <b>KITOB SO'ROVI QABUL QILINDI!</b>\n━━━━━━━━━━━━━━━━━━━━\n" +
                    $"«<b>{TelegramBotService.Escape(bTitle)}</b>» kitobingizni <b>{TelegramBotService.Escape(reserver?.FullName ?? "Kitobxon")}</b> olib ketishi mumkin.\n\n" +
                    $"👉 <b>Kitobxon Telegram aloqasi:</b> {reserverHandle}\n" +
                    $"<i>Ikkalangiz bir-biringizga yozib kitobni topshirish joyi va vaqtini kelishib olishingiz mumkin!</i>";

                var markup = reserverHandle.StartsWith('@')
                    ? new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = $"💬 Kitobxonga yozish ({reserverHandle})", url = $"https://t.me/{reserverHandle.TrimStart('@')}" } },
                            new object[] { new { text = "🌐 Veb-saytda ko'rish", url = $"{_bot.WebAppUrl}/almashish" } }
                        }
                    }
                    : null;

                await _bot.EditHtmlAsync(chatId, messageId, updatedText, markup, ct: ct);
            }
            return;
        }

        if (data.StartsWith("pb:reject:", StringComparison.Ordinal))
        {
            var parts = data["pb:reject:".Length..].Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var pbId) && int.TryParse(parts[1], out var reserverId))
            {
                var book = await db.PhysicalBooks
                    .Include(p => p.Book)
                    .Include(p => p.Reservations)
                    .FirstOrDefaultAsync(p => p.Id == pbId, ct);

                if (book is null || book.OwnerId != user.Id)
                {
                    await _bot.AnswerCallbackAsync(cbId, "Amal bajarilmadi.", ct: ct);
                    return;
                }

                var reservation = book.Reservations
                    .Where(r => r.ReserverId == reserverId && !r.IsConfirmed)
                    .OrderByDescending(r => r.ReservedAt)
                    .FirstOrDefault();

                if (reservation != null)
                {
                    db.PhysicalBookReservations.Remove(reservation);
                }
                book.Status = PhysicalBookStatus.Mavjud;
                await db.SaveChangesAsync(ct);

                await _bot.AnswerCallbackAsync(cbId, "So'rov rad etildi.", ct: ct);

                var bTitle = book.Book?.Title ?? book.ManualTitle ?? "Kitob";
                await _bot.EditHtmlAsync(chatId, messageId,
                    $"❌ <b>«{TelegramBotService.Escape(bTitle)}» kitobi bo'yicha so'rov rad etildi.</b>\nKitob maqomi yana «Mavjud» holatiga qaytarildi.",
                    null, ct: ct);

                var reserver = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == reserverId, ct);
                if (reserver != null && TelegramBotService.TryExtractTelegramId(reserver.GoogleId, out var rTgId) && rTgId > 0)
                {
                    await _bot.SendHtmlAsync(rTgId,
                        $"ℹ️ <b>«{TelegramBotService.Escape(bTitle)}»</b> kitobi bo'yicha so'rovingiz kitob egasi tomonidan qabul qilinmadi.",
                        ct: ct);
                }
            }
            return;
        }

        // 7) Menyu va Admin Callback'lar
        if (data == "menu:challenge")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await SendChallengeDashboardAsync(db, mediator, chatId, user, ct);
            return;
        }

        if (data == "menu:admin")
        {
            if (user.Role < UserRole.Admin)
            {
                await _bot.AnswerCallbackAsync(cbId, "Faqat Admin uchun!", true, ct);
                return;
            }
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await SendAdminDashboardAsync(db, chatId, user, ct);
            return;
        }

        if (data.StartsWith("admin:", StringComparison.Ordinal))
        {
            if (user.Role < UserRole.Admin)
            {
                await _bot.AnswerCallbackAsync(cbId, "Faqat Admin uchun!", true, ct);
                return;
            }

            await HandleAdminCallbackAsync(db, mediator, cbId, chatId, user, session, data, ct);
        }
    }

    // ── Admin Callback Handler ───────────────────────────────────────────────────

    private async Task HandleAdminCallbackAsync(
        IAppDbContext db,
        ISender mediator,
        string cbId,
        long chatId,
        User user,
        UserBotSession session,
        string data,
        CancellationToken ct)
    {
        if (data == "admin:user_stats")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            var stats = await _bot.GetBotUserStatsAsync(ct);
            var sb = new StringBuilder();
            sb.AppendLine("📊 <b>FOYDALANUVCHILAR VA BOT METRIKALARI</b>");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"👥 <b>Jami foydalanuvchilar:</b> <code>{stats.TotalUsers}</code> nafar");
            sb.AppendLine($"🌐 <b>Sayt orqali:</b> <code>{stats.WebsiteOnlyUsers}</code> nafar");
            sb.AppendLine($"🤖 <b>Telegram bot orqali:</b> <code>{stats.TelegramUsers}</code> nafar");
            sb.AppendLine();
            sb.AppendLine("🤖 <b>Bot holati va auditi:</b>");
            sb.AppendLine($"<blockquote>🟢 <b>Faol bot a'zolari:</b> <code>{stats.ActiveBotUsers}</code> nafar ({(100.0 - stats.BlockedPercent):0.#}%)\n" +
                          $"🚫 <b>Botni bloklaganlar:</b> <code>{stats.BlockedBotUsers}</code> nafar ({stats.BlockedPercent:0.#}%)</blockquote>");
            sb.AppendLine();
            sb.AppendLine("📈 <b>O'sish dinamikasi va faollik:</b>");
            sb.AppendLine($"<blockquote>✨ <b>Bugun ro'yxatdan o'tganlar:</b> +<code>{stats.TodayNewUsers}</code> nafar\n" +
                          $"📅 <b>Oxirgi 7 kunda qo'shilganlar:</b> +<code>{stats.WeekNewUsers}</code> nafar\n" +
                          $"🔥 <b>Bugun faol foydalanuvchilar:</b> <code>{stats.TodayActiveUsers}</code> nafar</blockquote>");

            var statsMarkup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = "🔄 Blok holatini tekshirish", callback_data = "admin:sync_blocked" },
                        new { text = "🚫 Bloklaganlar ro'yxati", callback_data = "admin:list_blocked" }
                    },
                    new object[]
                    {
                        new { text = "🔙 Admin paneliga qaytish", callback_data = "admin:menu" }
                    }
                }
            };

            await _bot.SendHtmlAsync(chatId, sb.ToString(), statsMarkup, ct: ct);
            return;
        }

        if (data == "admin:sync_blocked")
        {
            await _bot.AnswerCallbackAsync(cbId, "⏳ Bot a'zolarining blok holati tekshirilmoqda...", showAlert: false, ct: ct);
            var syncResult = await _bot.SyncBotBlockedStatusAsync(ct);

            var sb = new StringBuilder();
            sb.AppendLine("🔄 <b>BOT BLOK HOLATINI TEKSHIRISH NATIJASI</b>");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"👥 <b>Tekshirilgan bot a'zolari:</b> <code>{syncResult.TotalChecked}</code> nafar");
            sb.AppendLine($"🚫 <b>Yangi aniqlangan bloklar:</b> +<code>{syncResult.NewlyBlocked}</code> nafar");
            sb.AppendLine($"🟢 <b>Qayta ochilganlar (unblock):</b> +<code>{syncResult.NewlyUnblocked}</code> nafar");
            sb.AppendLine($"🛑 <b>Hozirda jami bloklaganlar:</b> <code>{syncResult.CurrentlyBlocked}</code> nafar");
            sb.AppendLine();
            sb.AppendLine("<blockquote>✨ <i>Tekshiruv Telegram API orqali amalga oshirildi. Foydalanuvchilarga hech qanday keraksiz bildirishnoma yuborilmadi.</i></blockquote>");

            var syncMarkup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = "📊 Yangilangan statistika", callback_data = "admin:user_stats" },
                        new { text = "🚫 Bloklaganlar ro'yxati", callback_data = "admin:list_blocked" }
                    },
                    new object[]
                    {
                        new { text = "🔙 Admin paneliga qaytish", callback_data = "admin:menu" }
                    }
                }
            };

            await _bot.SendHtmlAsync(chatId, sb.ToString(), syncMarkup, ct: ct);
            return;
        }

        if (data == "admin:list_blocked")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            var stats = await _bot.GetBotUserStatsAsync(ct);
            var sb = new StringBuilder();
            sb.AppendLine("🚫 <b>BOTNI BLOKLAGAN FOYDALANUVCHILAR</b>");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");

            if (stats.RecentlyBlockedUsers.Count == 0)
            {
                sb.AppendLine("🎉 <i>Ajoyib natija! Hozircha birorta ham foydalanuvchi botni bloklamagan.</i>");
            }
            else
            {
                sb.AppendLine($"Jami bloklaganlar soni: <b>{stats.BlockedBotUsers}</b> nafar\n");
                var idx = 0;
                foreach (var b in stats.RecentlyBlockedUsers)
                {
                    idx++;
                    var usernameStr = !string.IsNullOrWhiteSpace(b.Username) ? $" (@{TelegramBotService.Escape(b.Username)})" : "";
                    var timeStr = b.BlockedAt.HasValue
                        ? UzTime.FromUtc(b.BlockedAt.Value).ToString("yyyy-MM-dd HH:mm")
                        : "Sana noma'lum";
                    sb.AppendLine($"<b>{idx}. {TelegramBotService.Escape(b.FullName)}</b>{usernameStr}");
                    sb.AppendLine($"   🆔 <code>{b.TelegramId}</code> · 📅 {timeStr}");
                }
            }

            var listMarkup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = "🔄 Qaytadan tekshirish", callback_data = "admin:sync_blocked" },
                        new { text = "📊 Statistika", callback_data = "admin:user_stats" }
                    },
                    new object[]
                    {
                        new { text = "🔙 Admin paneliga qaytish", callback_data = "admin:menu" }
                    }
                }
            };

            await _bot.SendHtmlAsync(chatId, sb.ToString(), listMarkup, ct: ct);
            return;
        }

        if (data == "admin:menu")
        {
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await SendAdminDashboardAsync(db, chatId, user, ct);
            return;
        }

        if (data == "admin:ch_dates")
        {
            session.Step = SessionStep.AdminSetDates;
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await _bot.SendHtmlAsync(chatId,
                "📅 <b>Challenge boshlanish va tugash sanasini kiriting:</b>\n\n" +
                "<blockquote>Format: <code>YYYY-MM-DD YYYY-MM-DD | Challenge nomi</code>\n" +
                "Masalan:\n<code>2026-10-21 2026-11-20 | Kuzgi Kitobxonlar Challenge</code></blockquote>",
                ct: ct);
            return;
        }

        if (data.StartsWith("admin:prize:", StringComparison.Ordinal) &&
            int.TryParse(data["admin:prize:".Length..], out var rank))
        {
            var pRank = Math.Clamp(rank, 1, 3);
            session.TempPrizeRank = pRank;
            session.SearchMessageId = 0;
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await PromptSearchBookAsync(chatId, session, $"prize_{pRank}", ct);
            return;
        }

        if (data == "admin:set_group")
        {
            session.Step = SessionStep.AdminSetGroupId;
            await _bot.AnswerCallbackAsync(cbId, ct: ct);
            await _bot.SendHtmlAsync(chatId,
                "👥 <b>«KitobdagiMen» guruh ID'sini kiriting:</b>\n\n" +
                "<blockquote>💡 <i>Maslahat: Botni «KitobdagiMen» guruhiga qo'shib, guruh (yoki kerakli Topic) ichida <code>/bind_group</code> deb yozsangiz, guruh ID avtomatik ulanadi! Yoki guruh ID raqamini (masalan: <code>-1001234567890</code>) shu yerga yozib yuboring:</i></blockquote>",
                ct: ct);
            return;
        }

        if (data == "admin:send_report")
        {
            await _bot.AnswerCallbackAsync(cbId, "📊 Kunlik hisobot yuborilmoqda...", ct: ct);
            var reportHtml = await _bot.BuildDailyReportHtmlAsync(db, UzTime.Today, ct);
            await _bot.SendDailyGroupReportAsync(ct);
            await _bot.SendHtmlAsync(chatId, "✅ <b>Kunlik 23:59 hisoboti yuborildi!</b>\n\n" + reportHtml, ct: ct);
            return;
        }

        if (data == "admin:send_reminder")
        {
            await _bot.AnswerCallbackAsync(cbId, "🔔 20:00 eslatmalari yuborilmoqda...", ct: ct);
            var count = await _bot.SendDailyReadingRemindersAsync(ct);
            await _bot.SendHtmlAsync(chatId, $"✅ <b>Bugun kitob o'qimagan {count} ta foydalanuvchiga eslatma yuborildi!</b>", ct: ct);
            return;
        }

        if (data == "admin:announce_ch")
        {
            var cfg = await ChallengeConfigStore.GetOrDefaultAsync(db, ct);
            var p1 = cfg.GetPrize(1);
            var p2 = cfg.GetPrize(2);
            var p3 = cfg.GetPrize(3);

            var msgHtml =
                $"🔥 <b>YANGI MUTOLAA CHALLENGE E'LON QILINDI!</b>\n" +
                "━━━━━━━━━━━━━━━━━━━━\n" +
                $"🏆 <b>{TelegramBotService.Escape(cfg.Title)}</b>\n" +
                $"📅 <b>Muddat:</b> {TelegramBotService.Escape(cfg.FormattedRange)} ({cfg.TotalDays} kun)\n\n" +
                "🎁 <b>G'oliblar uchun sovg'a kitoblar:</b>\n" +
                $"<blockquote>🥇 <b>1-o'rin:</b> {TelegramBotService.Escape(p1.Title ?? "Maxsus kitob")} — <i>{TelegramBotService.Escape(p1.Author ?? "")}</i>\n" +
                $"🥈 <b>2-o'rin:</b> {TelegramBotService.Escape(p2.Title ?? "Maxsus kitob")} — <i>{TelegramBotService.Escape(p2.Author ?? "")}</i>\n" +
                $"🥉 <b>3-o'rin:</b> {TelegramBotService.Escape(p3.Title ?? "Maxsus kitob")} — <i>{TelegramBotService.Escape(p3.Author ?? "")}</i></blockquote>\n\n" +
                "📚 <i>Har kuni o'qigan betlaringizni bot yoki veb-sayt orqali kiriting va g'olibga aylaning!</i>";

            await _bot.BroadcastChallengeUpdateAsync(msgHtml, ct);
            await _bot.AnswerCallbackAsync(cbId, "📣 Challenge e'loni yuborildi!", ct: ct);
            await _bot.SendHtmlAsync(chatId, "✅ <b>Challenge e'loni tayyorlandi va «KitobdagiMen» guruhiga yuborildi:</b>\n\n" + msgHtml, ct: ct);
            return;
        }

        if (data == "admin:announce_winners")
        {
            await _bot.AnswerCallbackAsync(cbId, "🏆 G'oliblar hisoblanmoqda va e'lon qilinmoqda...", ct: ct);
            var (y, m) = ChallengeCalendar.CurrentPeriod();
            var count = await _bot.BroadcastChallengeWinnersAsync(y, m, ct);
            if (count > 0)
            {
                await _bot.SendHtmlAsync(chatId,
                    $"✅ <b>{count} nafar Challenge g'olibi e'lon qilindi!</b>\n\n" +
                    "<blockquote>📌 Xabar rasmiy guruhga, g'oliblarning shaxsiy chatiga va bot foydalanuvchilariga muvaffaqiyatli yetkazildi.</blockquote>", ct: ct);
            }
            else
            {
                var (ly, lm) = ChallengeCalendar.LastCompletedPeriod();
                var countLast = await _bot.BroadcastChallengeWinnersAsync(ly, lm, ct);
                if (countLast > 0)
                {
                    await _bot.SendHtmlAsync(chatId,
                        $"✅ <b>O'tgan davr ({ChallengeCalendar.PeriodLabel(ly, lm)}) bo'yicha {countLast} nafar Challenge g'olibi e'lon qilindi!</b>", ct: ct);
                }
                else
                {
                    await _bot.SendHtmlAsync(chatId,
                        "⚠️ <b>Challenge g'oliblari aniqlanmadi.</b>\nUshbu davrda hali mutolaa natijalari mavjud emas yoki g'oliblar topilmadi.", ct: ct);
                }
            }
            return;
        }
    }

    // ── Conversational Steps Handler ─────────────────────────────────────────────

    private async Task HandleActiveStepAsync(
        IAppDbContext db,
        ISender mediator,
        long chatId,
        long tgUserId,
        string? tgUsername,
        User user,
        UserBotSession session,
        string text,
        int messageId,
        CancellationToken ct)
    {
        switch (session.Step)
        {
            case SessionStep.SearchBookQuery:
                await HandleBookSearchQueryAsync(db, chatId, user, session, text, messageId, ct);
                break;

            case SessionStep.EditFullName:
                if (text.Length < 3 || text.Length > 100)
                {
                    await _bot.SendHtmlAsync(chatId, "⚠️ Ism-familiya 3–100 belgidan iborat bo'lishi kerak:", ct: ct);
                    return;
                }
                user.FullName = text;
                await db.SaveChangesAsync(ct);
                session.Step = SessionStep.None;
                await _bot.SendHtmlAsync(chatId, $"✅ Ism-familiyangiz <b>{TelegramBotService.Escape(user.FullName)}</b> ga o'zgartirildi!", ct: ct);
                await SendProfileDashboardAsync(db, chatId, user, ct);
                break;

            case SessionStep.EditUsername:
                var cand = text.Trim().TrimStart('@').ToLowerInvariant();
                if (!Regex.IsMatch(cand, @"^[a-z0-9_]{3,28}$"))
                {
                    await _bot.SendHtmlAsync(chatId, "⚠️ Faqat lotin harflari, raqamlar va `_` (3–28 belgi) kiriting:", ct: ct);
                    return;
                }
                var exists = await db.Users.AnyAsync(u => u.Id != user.Id && u.Username != null && u.Username.ToLower() == cand, ct);
                if (exists)
                {
                    await _bot.SendHtmlAsync(chatId, "⚠️ Bu username band. Boshqa kiriting:", ct: ct);
                    return;
                }
                user.Username = cand;
                await db.SaveChangesAsync(ct);
                session.Step = SessionStep.None;
                await _bot.SendHtmlAsync(chatId, $"✅ Username <b>@{TelegramBotService.Escape(user.Username)}</b> ga yangilandi!", ct: ct);
                await SendProfileDashboardAsync(db, chatId, user, ct);
                break;

            case SessionStep.EditBio:
                user.Bio = text.Length > 400 ? text[..400] : text;
                await db.SaveChangesAsync(ct);
                session.Step = SessionStep.None;
                await _bot.SendHtmlAsync(chatId, "✅ Bio ma'lumotingiz saqlandi!", ct: ct);
                await SendProfileDashboardAsync(db, chatId, user, ct);
                break;

            // ── Kitob qo'shish / Yangi kitob yaratish ──
            case SessionStep.AddBookTitle:
                if (string.IsNullOrWhiteSpace(text)) return;
                session.TempBookTitle = text.Trim();
                session.Step = SessionStep.AddBookAuthor;
                await _bot.SendHtmlAsync(chatId, "✍️ <b>Kitob muallifini kiriting:</b> <i>(masalan: Abdulla Qodiriy)</i>:", CancelInlineMarkup, ct: ct);
                break;

            case SessionStep.AddBookAuthor:
                if (string.IsNullOrWhiteSpace(text)) return;
                session.TempBookAuthor = text.Trim();
                if (session.TargetFlow == "quote")
                {
                    session.Step = SessionStep.NewQuoteText;
                    await _bot.SendHtmlAsync(chatId,
                        $"📖 <b>Kitob:</b> «{TelegramBotService.Escape(session.TempBookTitle)}» — <i>{TelegramBotService.Escape(session.TempBookAuthor)}</i>\n\n" +
                        "💬 Endi kitobdan olgan <b>iqtibos matnini</b> yozib yuboring:",
                        CancelInlineMarkup, ct: ct);
                }
                else if (session.TargetFlow == "post")
                {
                    session.Step = SessionStep.NewPostText;
                    await _bot.SendHtmlAsync(chatId,
                        $"📖 <b>Kitob:</b> «{TelegramBotService.Escape(session.TempBookTitle)}» — <i>{TelegramBotService.Escape(session.TempBookAuthor)}</i>\n\n" +
                        "✍️ Endi kitob haqidagi <b>taqriz va fikrlaringizni</b> batafsil yozib yuboring:",
                        CancelInlineMarkup, ct: ct);
                }
                else if (session.TargetFlow == "physical")
                {
                    session.Step = SessionStep.NewPhysBookCover;
                    await _bot.SendHtmlAsync(chatId,
                        $"📖 <b>Kitob:</b> «{TelegramBotService.Escape(session.TempBookTitle)}» — <i>{TelegramBotService.Escape(session.TempBookAuthor)}</i>\n\n" +
                        "📸 <b>Kitob fotosurati:</b>\nBoshqa kitobxonlar kitobingiz qanday holatda ekanligini ko'rishlari uchun uning <b>fotosuratini</b> yuboring:\n" +
                        "<i>(Kitobxonlar kitob fotosuratini ko'rib o'qishga tanlashadi)</i>",
                        new
                        {
                            inline_keyboard = new object[][]
                            {
                                new object[] { new { text = "⏭ Rasmsiz davom etish", callback_data = "pb:skip_cover" } }
                            }
                        }, ct: ct);
                }
                else if (session.TargetFlow?.StartsWith("prize_") == true)
                {
                    var pRank = int.TryParse(session.TargetFlow["prize_".Length..], out var pr) ? pr : session.TempPrizeRank;
                    session.TempPrizeRank = pRank;

                    var cfgPrize = await ChallengeConfigStore.GetOrDefaultAsync(db, ct);
                    var prize = cfgPrize.GetPrize(pRank);
                    prize.Title = session.TempBookTitle;
                    prize.Author = session.TempBookAuthor;
                    await ChallengeConfigStore.SaveAsync(db, cfgPrize, ct);

                    var newBook = new Book
                    {
                        Title = session.TempBookTitle ?? "Kitob",
                        Author = session.TempBookAuthor ?? "Muallif",
                        TotalPages = 200,
                        GenreId = 1
                    };
                    db.Books.Add(newBook);
                    await db.SaveChangesAsync(ct);

                    session.SelectedBookId = newBook.Id;
                    session.Step = SessionStep.AdminSetPrizeCover;

                    await _bot.SendHtmlAsync(chatId,
                        $"✅ <b>{pRank}-o'rin uchun yangi kitob yaratildi va sovg'aga biriktirildi!</b>\n\n" +
                        $"<blockquote>🏆 <b>{pRank}-o'rin:</b> «{TelegramBotService.Escape(newBook.Title)}» — <i>{TelegramBotService.Escape(newBook.Author)}</i></blockquote>\n\n" +
                        "🖼 Agar muqova rasmini ham biriktirmoqchi bo'lsangiz, hozir <b>rasm yuboring</b>:",
                        new
                        {
                            inline_keyboard = new object[][]
                            {
                                new object[] { new { text = "⏭ O'tkazib yuborish (Boshqaruv paneli)", callback_data = "admin:menu" } }
                            }
                        }, ct: ct);
                }
                else
                {
                    session.Step = SessionStep.AddBookPages;
                    await _bot.SendHtmlAsync(chatId, "📄 Kitobning <b>jami betlar sonini</b> kiriting <i>(masalan: 320)</i>:", CancelInlineMarkup, ct: ct);
                }
                break;

            case SessionStep.AddBookPages:
                if (!int.TryParse(text, out var totalPages) || totalPages < 5 || totalPages > 10000)
                {
                    await _bot.SendHtmlAsync(chatId, "⚠️ Iltimos, to'g'ri betlar sonini kiriting (5 dan 10000 gacha):", CancelInlineMarkup, ct: ct);
                    return;
                }
                session.TempTotalPages = totalPages;
                session.Step = SessionStep.AddBookDailyGoal;
                await _bot.SendHtmlAsync(chatId, "🎯 Har kuni ushbu kitobdan <b>necha bet</b> o'qishni reja qilasiz? <i>(Masalan: 25)</i>:", CancelInlineMarkup, ct: ct);
                break;

            case SessionStep.AddBookDailyGoal:
                if (!int.TryParse(text, out var dailyGoal) || dailyGoal < 1 || dailyGoal > 2000)
                {
                    await _bot.SendHtmlAsync(chatId, "⚠️ Kunlik bet maqsadini raqamda kiriting (1..2000):", CancelInlineMarkup, ct: ct);
                    return;
                }

                Book? book = null;
                if (session.SelectedBookId.HasValue)
                {
                    book = await db.Books.FirstOrDefaultAsync(b => b.Id == session.SelectedBookId.Value, ct);
                }

                if (book is null)
                {
                    book = new Book
                    {
                        Title = session.TempBookTitle ?? "Kitob",
                        Author = session.TempBookAuthor ?? "Muallif",
                        TotalPages = session.TempTotalPages > 0 ? session.TempTotalPages : 200,
                        GenreId = 1
                    };
                    db.Books.Add(book);
                    await db.SaveChangesAsync(ct);
                }

                var rg = new ReadingGoal
                {
                    UserId = user.Id,
                    BookId = book.Id,
                    DailyPageGoal = dailyGoal,
                    StartDate = DateTime.UtcNow,
                    CurrentPage = 0,
                    IsActive = true
                };
                db.ReadingGoals.Add(rg);
                await db.SaveChangesAsync(ct);
                session.Step = SessionStep.None;
                session.SelectedBookId = null;

                await _bot.SendHtmlAsync(chatId,
                    $"🎉 <b>«{TelegramBotService.Escape(book.Title)}» mutolaa rejangizga qo'shildi!</b>\n\n" +
                    $"<blockquote>📚 <b>Muallif:</b> {TelegramBotService.Escape(book.Author)}\n" +
                    $"📄 <b>Jami:</b> {book.TotalPages} bet · 🎯 <b>Kunlik maqsad:</b> {dailyGoal} bet</blockquote>\n\n" +
                    "Endi har kuni o'qigan betlaringizni kiritib borishingiz mumkin!",
                    new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = "📖 Hozir bet kiritish", callback_data = $"read:log:{rg.Id}" } }
                        }
                    }, ct: ct);
                break;

            // ── Bugungi o'qilgan betni kiritish ──
            case SessionStep.LogPagesAmount:
                var cleaned = text.Trim().TrimStart('+');
                if (!int.TryParse(cleaned, out var pagesRead) || pagesRead <= 0 || pagesRead > 2000)
                {
                    await _bot.SendHtmlAsync(chatId, "⚠️ Iltimos, bugun o'qigan betlar sonini musbat raqamda kiriting (masalan: <code>25</code>):", ct: ct);
                    return;
                }

                var goal = await db.ReadingGoals
                    .Include(g => g.Book)
                    .FirstOrDefaultAsync(g => g.Id == session.TempGoalId && g.UserId == user.Id, ct);

                if (goal is null)
                {
                    session.Step = SessionStep.None;
                    await _bot.SendHtmlAsync(chatId, "⚠️ Kitob topilmadi.", ct: ct);
                    return;
                }

                var remaining = Math.Max(0, goal.Book.TotalPages - goal.CurrentPage);
                var actualAdded = Math.Min(pagesRead, remaining > 0 ? remaining : pagesRead);
                goal.CurrentPage = Math.Min(goal.Book.TotalPages, goal.CurrentPage + actualAdded);
                if (goal.CurrentPage >= goal.Book.TotalPages)
                {
                    goal.IsActive = false;
                }

                var today = UzTime.Today;
                var prog = await db.ReadingProgress
                    .FirstOrDefaultAsync(p => p.ReadingGoalId == goal.Id && p.Date == today, ct);

                if (prog is null)
                {
                    prog = new ReadingProgress
                    {
                        ReadingGoalId = goal.Id,
                        Date = today,
                        PagesReadToday = actualAdded
                    };
                    db.ReadingProgress.Add(prog);
                }
                else
                {
                    prog.PagesReadToday += actualAdded;
                }

                await db.SaveChangesAsync(ct);
                session.Step = SessionStep.None;

                var bar = TelegramBotService.BuildProgressBar(goal.CurrentPage, goal.Book.TotalPages);
                var finishedMsg = !goal.IsActive
                    ? "\n\n🏆 <b>TABRIKLAYMIZ! Siz ushbu kitobni to'liq o'qib tugatdingiz!</b> 🎉"
                    : "";

                await _bot.SendHtmlAsync(chatId,
                    $"✅ <b>+{actualAdded} bet muvaffaqiyatli yozildi!</b>\n" +
                    "━━━━━━━━━━━━━━━━━━━━\n" +
                    $"📖 <b>Kitob:</b> «{TelegramBotService.Escape(goal.Book.Title)}»\n" +
                    $"📊 <b>Jarayon:</b> {bar} (<code>{goal.CurrentPage}/{goal.Book.TotalPages}</code> bet)\n" +
                    $"🔥 <b>Bugun ushbu kitobdan:</b> {prog.PagesReadToday} bet{finishedMsg}",
                    ct: ct);
                break;

            // ── Yangi Iqtibos yozish ──
            case SessionStep.NewQuoteBookTitle:
                session.TempBookTitle = text;
                session.Step = SessionStep.NewQuoteBookAuthor;
                await _bot.SendHtmlAsync(chatId, "✍️ Kitob <b>muallifini</b> kiriting:", ct: ct);
                break;

            case SessionStep.NewQuoteBookAuthor:
                session.TempBookAuthor = text;
                session.Step = SessionStep.NewQuoteText;
                await _bot.SendHtmlAsync(chatId, "💬 <b>Iqtibos matnini</b> yozib yuboring:", ct: ct);
                break;

            case SessionStep.NewQuoteText:
                if (text.Length < 5)
                {
                    await _bot.SendHtmlAsync(chatId, "⚠️ Iqtibos matni juda qisqa. Qaytadan yozing:", ct: ct);
                    return;
                }

                session.TempText = text;
                if (session.SelectedBookId.HasValue || !string.IsNullOrWhiteSpace(session.TempCoverUrl))
                {
                    // Kitob bazada topilgan / tanlangan — rasm so'ramaymiz, iqtibosni darhol chop etamiz!
                    await SaveNewQuoteAsync(db, chatId, user, session, session.TempCoverUrl, null, ct);
                }
                else
                {
                    session.Step = SessionStep.NewQuoteBookCover;
                    await _bot.SendHtmlAsync(chatId,
                        "📚 <b>1-qadam: Kitob rasmini (muqovasini) yuboring:</b>\n\n" +
                        "Kitobning muqova rasmini yuboring (yoki muqovasiz davom etish uchun tugmani bosing):",
                        new
                        {
                            inline_keyboard = new object[][]
                            {
                                new object[] { new { text = "⏭ Muqovasiz davom etish", callback_data = "quote:skip_book_cover" } }
                            }
                        }, ct: ct);
                }
                break;

            case SessionStep.NewQuoteBookCover:
                // Agar foydalanuvchi foto emas, matn yuborsa muqovasiz 2-qadamga o'tamiz
                session.TempCoverUrl = null;
                session.Step = SessionStep.NewQuoteImage;
                await _bot.SendHtmlAsync(chatId,
                    "📸 <b>2-qadam: Iqtibosga mos rasm yuboring:</b>\n\n" +
                    "Ushbu iqtibosga mos estetik fotosurat yoki lavha yuboring (yoki rasmsiz chop etish uchun tugmani bosing):",
                    new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = "⏭ Iqtibos rasmini o'tkazib yuborish", callback_data = "quote:skip_image" } }
                        }
                    }, ct: ct);
                break;

            case SessionStep.NewQuoteImage:
                // Agar foydalanuvchi foto emas, matn yozsa rasmsiz saqlaymiz
                await SaveNewQuoteAsync(db, chatId, user, session, session.TempCoverUrl, null, ct);
                break;

            // ── Yangi Taqriz (Post) yozish ──
            case SessionStep.NewPostBookTitle:
                session.TempBookTitle = text;
                session.Step = SessionStep.NewPostBookAuthor;
                await _bot.SendHtmlAsync(chatId, "✍️ Kitob <b>muallifini</b> kiriting:", ct: ct);
                break;

            case SessionStep.NewPostBookAuthor:
                session.TempBookAuthor = text;
                session.Step = SessionStep.NewPostText;
                await _bot.SendHtmlAsync(chatId,
                    "✍️ <b>Taqriz matni:</b>\nEndi kitob haqidagi <b>taqriz va xulosalaringizni</b> batafsil yozib yuboring:",
                    ct: ct);
                break;

            case SessionStep.NewPostText:
                if (text.Length < 10)
                {
                    await _bot.SendHtmlAsync(chatId, "⚠️ Taqriz matni kamida 10 ta belgidan iborat bo'lsin:", ct: ct);
                    return;
                }

                session.TempText = text;
                if (session.SelectedBookId.HasValue || !string.IsNullOrWhiteSpace(session.TempCoverUrl))
                {
                    // Kitob bazada topilgan / tanlangan — rasm so'ramaymiz, taqrizni darhol chop etamiz!
                    await SaveNewPostAsync(db, chatId, user, session, session.TempCoverUrl, null, ct);
                }
                else
                {
                    session.Step = SessionStep.NewPostBookCover;
                    await _bot.SendHtmlAsync(chatId,
                        "📚 <b>1-qadam: Kitob rasmini (muqovasini) yuboring:</b>\n\n" +
                        "Kitobning muqova rasmini yuboring (yoki rasmsiz davom etish uchun tugmani bosing):",
                        new
                        {
                            inline_keyboard = new object[][]
                            {
                                new object[] { new { text = "⏭ Muqovasiz davom etish", callback_data = "post:skip_book_cover" } }
                            }
                        }, ct: ct);
                }
                break;

            case SessionStep.NewPostBookCover:
                // Agar foydalanuvchi foto emas, matn yuborsa muqovasiz 2-qadamga o'tamiz
                session.TempCoverUrl = null;
                session.Step = SessionStep.NewPostReviewImage;
                await _bot.SendHtmlAsync(chatId,
                    "📸 <b>2-qadam: Taqrizga mos rasm yuboring:</b>\n\n" +
                    "Ushbu taqrizingiz uchun mos estetik fotosurat yoki lavha yuboring (yoki rasmsiz chop etish uchun tugmani bosing):",
                    new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = "⏭ Taqriz rasmini o'tkazib yuborish", callback_data = "post:skip_review_image" } }
                        }
                    }, ct: ct);
                break;

            case SessionStep.NewPostReviewImage:
                // Agar foydalanuvchi foto emas, matn yozsa rasmsiz saqlaymiz
                await SaveNewPostAsync(db, chatId, user, session, session.TempCoverUrl, null, ct);
                break;

            // ── Taqrizga yoki Iqtibosga Izoh (Comment) bildirish ──
            case SessionStep.CommentOnJackpot:
                if (string.IsNullOrWhiteSpace(text)) return;

                var cJackpot = await db.DailyJackpots.FirstOrDefaultAsync(j => j.Id == session.TempTargetId, ct);
                if (cJackpot is null)
                {
                    session.Step = SessionStep.None;
                    await _bot.SendHtmlAsync(chatId, "⚠️ Tanlov topilmadi.", ct: ct);
                    return;
                }

                db.DailyJackpotComments.Add(new DailyJackpotComment
                {
                    DailyJackpotId = cJackpot.Id,
                    UserId = user.Id,
                    Text = text.Length > 1000 ? text[..1000] : text,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);
                session.Step = SessionStep.None;

                await _bot.SendHtmlAsync(chatId,
                    "🎉 <b>Fikringiz muvaffaqiyatli qabul qilindi!</b>\n\n" +
                    $"<blockquote>💬 <i>{TelegramBotService.Escape(text)}</i></blockquote>\n" +
                    "Fikringiz platformadagi Jackpot zali va guruhda ko'rinadi.",
                    ct: ct);
                break;

            case SessionStep.CommentOnPost:
                if (string.IsNullOrWhiteSpace(text)) return;

                var cPost = await db.Posts.FirstOrDefaultAsync(p => p.Id == session.TempTargetId, ct);
                if (cPost is null)
                {
                    session.Step = SessionStep.None;
                    await _bot.SendHtmlAsync(chatId, "⚠️ Taqriz topilmadi.", ct: ct);
                    return;
                }

                db.Comments.Add(new Comment
                {
                    PostId = cPost.Id,
                    UserId = user.Id,
                    Text = text.Length > 1000 ? text[..1000] : text,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);
                session.Step = SessionStep.None;

                await _bot.SendHtmlAsync(chatId,
                    "✅ <b>Izohingiz taqriz ostiga qo'shildi!</b>\n\n" +
                    $"<blockquote>💬 <i>{TelegramBotService.Escape(text)}</i></blockquote>",
                    ct: ct);
                break;

            case SessionStep.CommentOnQuote:
                if (string.IsNullOrWhiteSpace(text)) return;

                var cQuote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == session.TempTargetId, ct);
                if (cQuote is null)
                {
                    session.Step = SessionStep.None;
                    await _bot.SendHtmlAsync(chatId, "⚠️ Iqtibos topilmadi.", ct: ct);
                    return;
                }

                db.QuoteComments.Add(new QuoteComment
                {
                    QuoteId = cQuote.Id,
                    UserId = user.Id,
                    Text = text.Length > 1000 ? text[..1000] : text,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);
                session.Step = SessionStep.None;

                await _bot.SendHtmlAsync(chatId,
                    "✅ <b>Izohingiz iqtibos ostiga qo'shildi!</b>\n\n" +
                    $"<blockquote>💬 <i>{TelegramBotService.Escape(text)}</i></blockquote>",
                    ct: ct);
                break;

            // ── Kitob almashish e'lonini qo'shish ──
            case SessionStep.NewPhysBookTitle:
                session.TempBookTitle = text;
                session.Step = SessionStep.NewPhysBookAuthor;
                await _bot.SendHtmlAsync(chatId, "✍️ <b>Kitob almashish (2/3):</b>\nKitob <b>muallifini</b> kiriting:", ct: ct);
                break;

            case SessionStep.NewPhysBookAuthor:
                session.TempBookAuthor = text;
                session.Step = SessionStep.NewPhysBookCover;
                await _bot.SendHtmlAsync(chatId,
                    "📸 <b>Kitobingiz fotosurati (3/3):</b>\n\n" +
                    "Boshqa kitobxonlar kitobingiz qanday holatda ekanligini ko'rishlari uchun uning <b>fotosuratini (rasmini)</b> yuboring:\n" +
                    "<i>(Kitobxonlar kitobingiz fotosuratini ko'rib o'qishga tanlashadi)</i>",
                    new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = "⏭ Rasmsiz davom etish", callback_data = "pb:skip_cover" } }
                        }
                    }, ct: ct);
                break;

            case SessionStep.NewPhysBookCover:
                // Agar foydalanuvchi foto emas, matn yozsa rasmsiz saqlaymiz
                await SaveNewPhysicalBookAsync(db, chatId, user, session, null, ct);
                break;

            // ── Admin: Challenge sanalarini va sovg'a kitoblarni o'zgartirish ──
            case SessionStep.AdminSetDates:
                if (user.Role < UserRole.Admin) return;
                // Format: YYYY-MM-DD YYYY-MM-DD | Title
                var splitTitle = text.Split('|', 2, StringSplitOptions.TrimEntries);
                var datesPart = splitTitle[0].Split(new[] { ' ', '—', '-' }, StringSplitOptions.RemoveEmptyEntries);
                // Yoki regex orqali 2 ta YYYY-MM-DD topamiz
                var matches = Regex.Matches(splitTitle[0], @"\d{4}-\d{2}-\d{2}");
                if (matches.Count < 2 ||
                    !DateOnly.TryParse(matches[0].Value, out var startDate) ||
                    !DateOnly.TryParse(matches[1].Value, out var endDate))
                {
                    await _bot.SendHtmlAsync(chatId,
                        "⚠️ Sana formati noto'g'ri!\nIltimos quyidagi ko'rinishda yozing:\n<code>2026-10-21 2026-11-20 | Kuzgi Mutolaa Challenge</code>",
                        ct: ct);
                    return;
                }

                var chCfg = await ChallengeConfigStore.GetOrDefaultAsync(db, ct);
                chCfg.StartDate = startDate;
                chCfg.EndDate = endDate < startDate ? startDate : endDate;
                if (splitTitle.Length > 1 && !string.IsNullOrWhiteSpace(splitTitle[1]))
                {
                    chCfg.Title = splitTitle[1];
                }
                else
                {
                    chCfg.Title = $"{chCfg.FormattedRange} Challenge";
                }

                await ChallengeConfigStore.SaveAsync(db, chCfg, ct);
                session.Step = SessionStep.None;

                await _bot.SendHtmlAsync(chatId,
                    $"✅ <b>Challenge muddati yangilandi!</b>\n\n" +
                    $"<blockquote>📌 <b>Nomi:</b> {TelegramBotService.Escape(chCfg.Title)}\n" +
                    $"📅 <b>Boshlanish:</b> {chCfg.StartDate:yyyy-MM-dd}\n" +
                    $"🏁 <b>Tugash:</b> {chCfg.EndDate:yyyy-MM-dd} ({chCfg.TotalDays} kun)</blockquote>",
                    ct: ct);
                await SendAdminDashboardAsync(db, chatId, user, ct);
                break;

            case SessionStep.AdminSetPrizeTitle:
                if (user.Role < UserRole.Admin) return;
                session.TempBookTitle = text;
                session.Step = SessionStep.AdminSetPrizeAuthor;
                await _bot.SendHtmlAsync(chatId,
                    $"✍️ <b>{session.TempPrizeRank}-o'rin sovg'a kitobining muallifini kiriting:</b>",
                    ct: ct);
                break;

            case SessionStep.AdminSetPrizeAuthor:
                if (user.Role < UserRole.Admin) return;
                session.TempBookAuthor = text;
                session.Step = SessionStep.AdminSetPrizeCover;

                var cfgNow = await ChallengeConfigStore.GetOrDefaultAsync(db, ct);
                var prizeNow = cfgNow.GetPrize(session.TempPrizeRank);
                prizeNow.Title = session.TempBookTitle;
                prizeNow.Author = session.TempBookAuthor;
                await ChallengeConfigStore.SaveAsync(db, cfgNow, ct);

                await _bot.SendHtmlAsync(chatId,
                    $"✅ <b>{session.TempPrizeRank}-o'rin kitobi ({TelegramBotService.Escape(prizeNow.Title)}) saqlandi!</b>\n\n" +
                    "🖼 Agar muqova rasmini ham biriktirmoqchi bo'lsangiz, hozir <b>rasm yuboring</b>:",
                    new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = "⏭ O'tkazib yuborish", callback_data = "admin:menu" } }
                        }
                    },
                    ct: ct);
                break;

            case SessionStep.AdminSetGroupId:
                if (user.Role < UserRole.Admin) return;
                await SaveBookClubGroupIdAsync(db, text.Trim(), ct);
                session.Step = SessionStep.None;
                await _bot.SendHtmlAsync(chatId,
                    $"✅ <b>«KitobdagiMen» guruh ID'si (<code>{TelegramBotService.Escape(text.Trim())}</code>) saqlandi!</b>",
                    ct: ct);
                await SendAdminDashboardAsync(db, chatId, user, ct);
                break;
        }
    }

    // ── "KitobdagiMen" Guruh Xabarlarini Qayta Ishlash ──────────────────────────

    private async Task HandleGroupMessageAsync(
        IAppDbContext db,
        ISender mediator,
        long groupChatId,
        int? threadId,
        long tgUserId,
        string? tgUsername,
        string text,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // Guruh -5511445336 bo'lsa chat_id va topic_id ni avtomatik yangilab borish
        if (Math.Abs(groupChatId).ToString().EndsWith("5511445336"))
        {
            var targetValue = threadId.HasValue ? $"{groupChatId}:{threadId.Value}" : groupChatId.ToString();
            var currentTarget = await _bot.GetGroupTargetAsync(db, ct);
            if (currentTarget is null || currentTarget.Value.ChatId != groupChatId || currentTarget.Value.TopicId != threadId)
            {
                await SaveBookClubGroupIdAsync(db, targetValue, ct);
            }
        }

        var user = await FindAndSyncUserAsync(db, tgUserId, tgUsername, ct);

        // 1) Faqat SuperAdmin guruh (yoki Topic) ichida /bind_group yozsa bog'lashni tasdiqlash uchun
        if (text.StartsWith("/bind_group", StringComparison.OrdinalIgnoreCase))
        {
            if (user is not null && user.Role >= UserRole.Admin || IsConfiguredSuperAdmin(tgUserId, tgUsername, user?.Username))
            {
                var targetValue = threadId.HasValue ? $"{groupChatId}:{threadId.Value}" : groupChatId.ToString();
                await SaveBookClubGroupIdAsync(db, targetValue, ct);
                var topicNote = threadId.HasValue ? $" (Topic #{threadId.Value})" : "";
                await _bot.SendHtmlAsync(groupChatId,
                    $"✅ <b>Ushbu guruh{topicNote} rasmiy «KitobdagiMen» guruhi sifatida ulandi!</b>\n\n" +
                    "<blockquote>📌 Endi har kuni soat <b>23:59</b> da kunlik mutolaa sarhisobi hamda <b>Challenge</b> g'oliblari e'loni shu yerga avtomatik yuboriladi!</blockquote>",
                    messageThreadId: threadId,
                    ct: ct);
            }
            return;
        }

        // Guruhda foydalanuvchilar bot buyruqlaridan (/bugun, /challenge, /iqtibos, /taqriz va h.k.)
        // foydalana olmaydi. Guruhda faqatgina bot 23:59 hisoboti va Challenge g'oliblari e'lonini yuboradi.
        return;
    }

    // ── Foydalanuvchini topish va SuperAdmin huquqini tekshirish ──────────────────

    private async Task<User?> FindAndSyncUserAsync(
        IAppDbContext db, long tgUserId, string? tgUsername, CancellationToken ct)
    {
        var tgKey = $"tg:{tgUserId}";
        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == tgKey, ct);

        if (user is null && !string.IsNullOrWhiteSpace(tgUsername))
        {
            // Agar foydalanuvchi avvaldan shu username bilan bazada bo'lsa va hali tg: ga ulanmagan bo'lsa
            var lower = tgUsername.TrimStart('@').ToLowerInvariant();
            var existingByUsername = await db.Users
                .FirstOrDefaultAsync(u => u.Username != null && u.Username.ToLower() == lower && !u.GoogleId.StartsWith("tg:"), ct);

            if (existingByUsername is not null)
            {
                existingByUsername.GoogleId = tgKey;
                user = existingByUsername;
            }
        }

        if (user is not null)
        {
            var changed = false;
            if (IsConfiguredSuperAdmin(tgUserId, tgUsername, user.Username) && user.Role != UserRole.SuperAdmin)
            {
                user.Role = UserRole.SuperAdmin;
                changed = true;
            }

            if (user.IsBotBlocked)
            {
                user.IsBotBlocked = false;
                user.BotBlockedAt = null;
                changed = true;
            }

            if (changed)
            {
                await db.SaveChangesAsync(ct);
            }

            if (!string.IsNullOrWhiteSpace(tgUsername))
            {
                await SaveTelegramUsernameSettingAsync(db, user.Id, tgUsername, ct);
            }
        }

        return user;
    }

    private bool IsConfiguredSuperAdmin(long tgUserId, string? tgUsername, string? platformUsername)
    {
        var s = _bot.Settings;
        if (s.SuperAdminTelegramIds.Contains(tgUserId))
        {
            return true;
        }

        var list = s.SuperAdminUsernames;
        if (list.Count == 0) return false;

        if (!string.IsNullOrWhiteSpace(tgUsername) &&
            list.Any(u => string.Equals(u.TrimStart('@'), tgUsername.TrimStart('@'), StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(platformUsername) &&
            list.Any(u => string.Equals(u.TrimStart('@'), platformUsername.TrimStart('@'), StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static async Task SaveTelegramUsernameSettingAsync(
        IAppDbContext db, int userId, string tgUsername, CancellationToken ct)
    {
        var clean = tgUsername.TrimStart('@');
        if (string.IsNullOrWhiteSpace(clean)) return;

        var key = $"Telegram:Username:{userId}";
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (row is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = clean,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
        else if (row.Value != clean)
        {
            row.Value = clean;
            row.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task SaveBookClubGroupIdAsync(IAppDbContext db, string groupId, CancellationToken ct)
    {
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == ChallengeConfigStore.BookClubGroupIdKey, ct);
        if (row is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = ChallengeConfigStore.BookClubGroupIdKey,
                Value = groupId,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            row.Value = groupId;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value ?? "";
        return value[..maxLength] + "…";
    }

    private async Task StartBookSelectionFlowAsync(
        IAppDbContext db,
        long chatId,
        User user,
        UserBotSession session,
        string flowType,
        CancellationToken ct)
    {
        session.TargetFlow = flowType;
        session.SelectedBookId = null;
        session.TempCoverUrl = null;
        session.TempText = null;
        session.TempBookTitle = null;
        session.TempBookAuthor = null;

        // Foydalanuvchining "Kitoblarim" bo'limidagi ayni paytda o'qiyotgan faol kitobi
        var activeGoal = await db.ReadingGoals
            .AsNoTracking()
            .Include(g => g.Book)
            .Where(g => g.UserId == user.Id && g.IsActive)
            .OrderByDescending(g => g.StartDate)
            .ThenByDescending(g => g.Id)
            .FirstOrDefaultAsync(ct);

        if (activeGoal is not null && flowType is "quote" or "post" or "physical")
        {
            var bookTitle = activeGoal.Book.Title;
            var bookAuthor = activeGoal.Book.Author;

            string header;
            string actionText;
            if (flowType == "quote")
            {
                header = "💬 <b>Yangi iqtibos yozish</b>";
                actionText = "iqtibos yozish";
            }
            else if (flowType == "post")
            {
                header = "✍️ <b>Yangi kitob taqrizi yozish</b>";
                actionText = "taqriz yozish";
            }
            else
            {
                header = "🔄 <b>Kitob almashish uchun e'lon qo'shish</b>";
                actionText = "almashishga qo'yish";
            }

            var promptMsg =
                $"{header}\n\n" +
                "Siz ayni paytda o'qiyotgan kitob:\n" +
                $"📖 <b>«{TelegramBotService.Escape(bookTitle)}»</b>" +
                (string.IsNullOrWhiteSpace(bookAuthor) ? "" : $" — <i>{TelegramBotService.Escape(bookAuthor)}</i>") + "\n\n" +
                "Ushbu kitob bo'yicha davom etasizmi yoki boshqa kitob qidirasizmi?";

            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = $"✅ «{Truncate(bookTitle, 20)}» dan {actionText}", callback_data = $"b_sel:{flowType}:{activeGoal.BookId}" }
                    },
                    new object[]
                    {
                        new { text = "🔍 Boshqa kitob qidirish", callback_data = $"b_search:{flowType}" }
                    },
                    new object[]
                    {
                        new { text = "❌ Bekor qilish", callback_data = "cancel_input" }
                    }
                }
            };

            var sent = await _bot.SendHtmlAsync(chatId, promptMsg, markup, ct: ct);
            session.SearchMessageId = sent?["result"]?["message_id"]?.GetValue<int>() ?? 0;
            return;
        }

        // Faol kitobi bo'lmasa yoki "Kitoblarim" bo'limida yangi kitob qo'shayotgan bo'lsa
        await PromptSearchBookAsync(chatId, session, flowType, ct);
    }

    private async Task PromptSearchBookAsync(
        long chatId,
        UserBotSession session,
        string flowType,
        CancellationToken ct)
    {
        session.TargetFlow = flowType;
        session.Step = SessionStep.SearchBookQuery;

        var flowTitle = flowType switch
        {
            "quote" => "💬 <b>Iqtibos uchun kitob qidirish:</b>",
            "post" => "✍️ <b>Taqriz uchun kitob qidirish:</b>",
            "physical" => "🔄 <b>Almashish uchun kitob qidirish:</b>",
            "prize_1" => "🥇 <b>1-o'rin sovg'asi uchun kitob qidirish:</b>",
            "prize_2" => "🥈 <b>2-o'rin sovg'asi uchun kitob qidirish:</b>",
            "prize_3" => "🥉 <b>3-o'rin sovg'asi uchun kitob qidirish:</b>",
            _ => "📚 <b>Mutolaa uchun kitob qidirish:</b>"
        };

        var html =
            $"{flowTitle}\n\n" +
            "Kitob nomi yoki muallifidan <b>1-2 ta so'z</b> yozib yuboring <i>(masalan: <code>Atom odatlari</code> yoki <code>Alkimyogar</code>)</i>:";

        var markup = new
        {
            inline_keyboard = new object[][]
            {
                new object[] { new { text = "❌ Bekor qilish", callback_data = "cancel_input" } }
            }
        };

        bool edited = false;
        if (session.SearchMessageId > 0)
        {
            var editRes = await _bot.EditHtmlAsync(chatId, session.SearchMessageId, html, markup, ct);
            edited = editRes?["ok"]?.GetValue<bool>() == true;
        }

        if (!edited)
        {
            var sent = await _bot.SendHtmlAsync(chatId, html, markup, ct: ct);
            session.SearchMessageId = sent?["result"]?["message_id"]?.GetValue<int>() ?? 0;
        }
    }

    private async Task HandleBookSearchQueryAsync(
        IAppDbContext db,
        long chatId,
        User user,
        UserBotSession session,
        string text,
        int messageId,
        CancellationToken ct)
    {
        // 1. Anti-flood / toza chat: foydalanuvchi yozgan xabarni darhol o'chiramiz
        if (messageId > 0)
        {
            await _bot.DeleteMessageAsync(chatId, messageId, ct);
        }

        var query = text.Trim();
        if (string.IsNullOrWhiteSpace(query)) return;

        var flow = session.TargetFlow ?? "reading";

        // So'zlarga ajratamiz (1-2 ta so'zdan qidirish uchun) va tutuq/apostroflarni normallashtiramiz
        var normQuery = TextNormalizer.NormalizeApostrophes(query);
        var words = normQuery.Split(new[] { ' ', ',', '-', '—', '.' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length >= 2)
            .ToArray();

        if (words.Length == 0)
        {
            words = new[] { normQuery };
        }

        // PostgreSQL qidiruvi (barcha tutuq belgilari va apostroflar hisobga olingan holda)
        var baseQuery = db.Books.AsNoTracking().AsQueryable();
        foreach (var word in words)
        {
            var w = word.ToLower();
            baseQuery = baseQuery.Where(b =>
                b.Title.ToLower()
                    .Replace("‘", "'")
                    .Replace("’", "'")
                    .Replace("ʻ", "'")
                    .Replace("ʼ", "'")
                    .Replace("`", "'")
                    .Replace("´", "'")
                    .Replace("ʹ", "'")
                    .Contains(w) ||
                b.Author.ToLower()
                    .Replace("‘", "'")
                    .Replace("’", "'")
                    .Replace("ʻ", "'")
                    .Replace("ʼ", "'")
                    .Replace("`", "'")
                    .Replace("´", "'")
                    .Replace("ʹ", "'")
                    .Contains(w));
        }

        var foundBooks = await baseQuery
            .OrderByDescending(b => b.ReadingGoals.Count + b.Posts.Count)
            .Take(5)
            .ToListAsync(ct);

        // Agar barcha so'zlar bo'yicha topilmasa va 2+ so'z kiritilgan bo'lsa, birinchi so'z bo'yicha qidirib ko'ramiz
        if (foundBooks.Count == 0 && words.Length > 1)
        {
            var firstWord = words[0].ToLower();
            foundBooks = await db.Books.AsNoTracking()
                .Where(b =>
                    b.Title.ToLower()
                        .Replace("‘", "'")
                        .Replace("’", "'")
                        .Replace("ʻ", "'")
                        .Replace("ʼ", "'")
                        .Replace("`", "'")
                        .Replace("´", "'")
                        .Replace("ʹ", "'")
                        .Contains(firstWord) ||
                    b.Author.ToLower()
                        .Replace("‘", "'")
                        .Replace("’", "'")
                        .Replace("ʻ", "'")
                        .Replace("ʼ", "'")
                        .Replace("`", "'")
                        .Replace("´", "'")
                        .Replace("ʹ", "'")
                        .Contains(firstWord))
                .OrderByDescending(b => b.ReadingGoals.Count + b.Posts.Count)
                .Take(5)
                .ToListAsync(ct);
        }

        string resultHtml;
        object markup;

        if (foundBooks.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🔍 <b>«{TelegramBotService.Escape(query)}» bo'yicha topilgan kitoblar:</b>\n");
            sb.AppendLine("Kerakli kitobni tanlash uchun ustiga bosing:\n");

            var rows = new List<object[]>();
            foreach (var b in foundBooks)
            {
                var label = $"📖 {b.Title}" + (string.IsNullOrWhiteSpace(b.Author) ? "" : $" — {b.Author}");
                rows.Add(new object[]
                {
                    new { text = Truncate(label, 36), callback_data = $"b_sel:{flow}:{b.Id}" }
                });
            }

            rows.Add(new object[]
            {
                new { text = "🔍 Qaytadan qidirish", callback_data = $"b_retry:{flow}" },
                new { text = "➕ Yangi qo'shish", callback_data = $"b_new:{flow}" }
            });
            rows.Add(new object[]
            {
                new { text = "❌ Bekor qilish", callback_data = "cancel_input" }
            });

            resultHtml = sb.ToString();
            markup = new { inline_keyboard = rows };
        }
        else
        {
            resultHtml =
                $"⚠️ <b>Bazamizda «{TelegramBotService.Escape(query)}» kitobi topilmadi.</b>\n\n" +
                "Bizda bu kitob hozircha yo'q. Uni <b>o'zingiz yangi qo'shishingiz</b> mumkin:";

            markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = "➕ Yangi kitob qo'shish", callback_data = $"b_new:{flow}" }
                    },
                    new object[]
                    {
                        new { text = "🔍 Qaytadan qidirish", callback_data = $"b_retry:{flow}" },
                        new { text = "❌ Bekor qilish", callback_data = "cancel_input" }
                    }
                }
            };
        }

        // Chat to'lib ketmasligi uchun mavjud bot xabarini tahrirlaymiz (editMessageText)
        bool edited = false;
        if (session.SearchMessageId > 0)
        {
            var editRes = await _bot.EditHtmlAsync(chatId, session.SearchMessageId, resultHtml, markup, ct);
            edited = editRes?["ok"]?.GetValue<bool>() == true;
        }

        if (!edited)
        {
            var sent = await _bot.SendHtmlAsync(chatId, resultHtml, markup, ct: ct);
            session.SearchMessageId = sent?["result"]?["message_id"]?.GetValue<int>() ?? 0;
        }
    }

    private async Task HandleBookSelectedAsync(
        IAppDbContext db,
        long chatId,
        User user,
        UserBotSession session,
        string flow,
        int bookId,
        CancellationToken ct)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId, ct);
        if (book is null)
        {
            await _bot.SendHtmlAsync(chatId, "⚠️ Kitob topilmadi.", ct: ct);
            return;
        }

        session.SelectedBookId = book.Id;
        session.TempBookTitle = book.Title;
        session.TempBookAuthor = book.Author;
        session.TempCoverUrl = book.CoverUrl;
        session.TempTotalPages = book.TotalPages;

        switch (flow)
        {
            case "prize_1":
            case "prize_2":
            case "prize_3":
                var rank = int.Parse(flow["prize_".Length..]);
                session.TempPrizeRank = rank;

                var chCfg = await ChallengeConfigStore.GetOrDefaultAsync(db, ct);
                var prize = chCfg.GetPrize(rank);
                prize.Title = book.Title;
                prize.Author = book.Author;
                if (!string.IsNullOrWhiteSpace(book.CoverUrl))
                {
                    prize.CoverUrl = book.CoverUrl;
                }
                await ChallengeConfigStore.SaveAsync(db, chCfg, ct);

                session.Step = SessionStep.AdminSetPrizeCover;
                await _bot.SendHtmlAsync(chatId,
                    $"✅ <b>{rank}-o'rin sovg'asi sifatida tanlandi!</b>\n\n" +
                    $"<blockquote>🏆 <b>{rank}-o'rin:</b> «{TelegramBotService.Escape(book.Title)}» — <i>{TelegramBotService.Escape(book.Author)}</i>\n" +
                    (!string.IsNullOrWhiteSpace(book.CoverUrl) ? "🖼 Muqova: biriktirildi" : "🖼 Muqova: hozircha yo'q") +
                    "</blockquote>\n\n" +
                    "🖼 Agar boshqa muqova rasmini yuklamoqchi bo'lsangiz, hozir <b>rasm yuboring</b> yoki menyuga qayting:",
                    new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = "🔙 Boshqaruv paneli", callback_data = "admin:menu" } }
                        }
                    }, ct: ct);
                break;

            case "quote":
                session.Step = SessionStep.NewQuoteText;
                await _bot.SendHtmlAsync(chatId,
                    $"📖 <b>Tanlangan kitob:</b> «{TelegramBotService.Escape(book.Title)}» — <i>{TelegramBotService.Escape(book.Author)}</i>\n\n" +
                    "💬 Endi kitobdan olgan <b>iqtibos matnini</b> yozib yuboring:",
                    CancelInlineMarkup, ct: ct);
                break;

            case "post":
                session.Step = SessionStep.NewPostText;
                await _bot.SendHtmlAsync(chatId,
                    $"📖 <b>Tanlangan kitob:</b> «{TelegramBotService.Escape(book.Title)}» — <i>{TelegramBotService.Escape(book.Author)}</i>\n\n" +
                    "✍️ Endi kitob haqidagi <b>taqriz va fikrlaringizni</b> batafsil yozib yuboring:",
                    CancelInlineMarkup, ct: ct);
                break;

            case "physical":
                // Kitob bazada topilgan / tanlangan — rasm so'ramaymiz, darhol almashish kutubxonasiga joylaymiz!
                await SaveNewPhysicalBookAsync(db, chatId, user, session, book.CoverUrl, ct);
                break;

            case "reading":
            default:
                if (book.TotalPages > 0)
                {
                    session.Step = SessionStep.AddBookDailyGoal;
                    await _bot.SendHtmlAsync(chatId,
                        $"📖 <b>Tanlangan kitob:</b> «{TelegramBotService.Escape(book.Title)}» — <i>{TelegramBotService.Escape(book.Author)}</i>\n" +
                        $"📄 Jami betlar: <b>{book.TotalPages} bet</b>\n\n" +
                        "🎯 Har kuni ushbu kitobdan <b>necha bet</b> o'qishni reja qilasiz? <i>(Masalan: 25)</i>:",
                        CancelInlineMarkup, ct: ct);
                }
                else
                {
                    session.Step = SessionStep.AddBookPages;
                    await _bot.SendHtmlAsync(chatId,
                        $"📖 <b>Tanlangan kitob:</b> «{TelegramBotService.Escape(book.Title)}» — <i>{TelegramBotService.Escape(book.Author)}</i>\n\n" +
                        "📄 Kitobning <b>jami betlar sonini</b> kiriting <i>(masalan: 320)</i>:",
                        CancelInlineMarkup, ct: ct);
                }
                break;
        }
    }

    private async Task SaveNewPhysicalBookAsync(
        IAppDbContext db, long chatId, User user, UserBotSession session, string? coverUrl, CancellationToken ct)
    {
        Book? pbBook = null;
        if (session.SelectedBookId.HasValue)
        {
            pbBook = await db.Books.FirstOrDefaultAsync(b => b.Id == session.SelectedBookId.Value, ct);
        }

        if (pbBook is null)
        {
            var normTitle = TextNormalizer.NormalizeApostrophes(session.TempBookTitle).ToLower();
            pbBook = await db.Books.FirstOrDefaultAsync(b =>
                b.Title.ToLower()
                    .Replace("‘", "'")
                    .Replace("’", "'")
                    .Replace("ʻ", "'")
                    .Replace("ʼ", "'")
                    .Replace("`", "'")
                    .Replace("´", "'")
                    .Replace("ʹ", "'") == normTitle, ct);
        }

        if (pbBook is null)
        {
            pbBook = new Book
            {
                Title = session.TempBookTitle ?? "Kitob",
                Author = session.TempBookAuthor ?? "Muallif",
                CoverUrl = coverUrl,
                TotalPages = 200,
                GenreId = 1
            };
            db.Books.Add(pbBook);
            await db.SaveChangesAsync(ct);
        }

        var pb = new PhysicalBook
        {
            OwnerId = user.Id,
            BookId = pbBook.Id,
            ManualTitle = pbBook.Title,
            ManualAuthor = pbBook.Author,
            ManualCoverUrl = coverUrl ?? pbBook.CoverUrl,
            Status = PhysicalBookStatus.Mavjud,
            CreatedAt = DateTime.UtcNow
        };
        db.PhysicalBooks.Add(pb);
        await db.SaveChangesAsync(ct);
        session.Step = SessionStep.None;
        session.SelectedBookId = null;
        session.TempBookTitle = null;
        session.TempBookAuthor = null;
        session.TempCoverUrl = null;
        session.TempText = null;
        session.TargetFlow = null;
        session.SearchMessageId = 0;

        await _bot.SendHtmlAsync(chatId,
            $"✅ <b>Kitobingiz almashish kutubxonasiga muvaffaqiyatli joylandi!</b>\n\n" +
            $"📕 <b>{TelegramBotService.Escape(pb.ManualTitle)}</b> — <i>{TelegramBotService.Escape(pb.ManualAuthor)}</i>",
            BuildMainMenuKeyboard(user.Role >= UserRole.Admin), ct: ct);
    }

    private async Task SaveNewPostAsync(
        IAppDbContext db, long chatId, User user, UserBotSession session, string? bookCoverUrl, string? reviewImageUrl, CancellationToken ct)
    {
        Book? postBook = null;
        if (session.SelectedBookId.HasValue)
        {
            postBook = await db.Books.FirstOrDefaultAsync(b => b.Id == session.SelectedBookId.Value, ct);
        }

        if (postBook is null)
        {
            var normTitle = TextNormalizer.NormalizeApostrophes(session.TempBookTitle).ToLower();
            postBook = await db.Books.FirstOrDefaultAsync(b =>
                b.Title.ToLower()
                    .Replace("‘", "'")
                    .Replace("’", "'")
                    .Replace("ʻ", "'")
                    .Replace("ʼ", "'")
                    .Replace("`", "'")
                    .Replace("´", "'")
                    .Replace("ʹ", "'") == normTitle, ct);
        }

        var cover = !string.IsNullOrWhiteSpace(bookCoverUrl) ? bookCoverUrl : "/img/default-book-cover.webp";

        if (postBook is null)
        {
            postBook = new Book
            {
                Title = session.TempBookTitle ?? "Kitob",
                Author = session.TempBookAuthor ?? "Muallif",
                CoverUrl = cover,
                TotalPages = 250,
                GenreId = 1
            };
            db.Books.Add(postBook);
            await db.SaveChangesAsync(ct);
        }
        else if (string.IsNullOrWhiteSpace(postBook.CoverUrl))
        {
            postBook.CoverUrl = cover;
            await db.SaveChangesAsync(ct);
        }

        var text = session.TempText ?? "";
        var newPost = new Post
        {
            UserId = user.Id,
            BookId = postBook.Id,
            ReviewText = $"<p>{TelegramBotService.Escape(text)}</p>",
            ImageUrl = reviewImageUrl,
            Slug = Guid.NewGuid().ToString("N")[..12],
            CreatedAt = DateTime.UtcNow
        };
        db.Posts.Add(newPost);
        await db.SaveChangesAsync(ct);
        session.Step = SessionStep.None;
        session.SelectedBookId = null;
        session.TempBookTitle = null;
        session.TempBookAuthor = null;
        session.TempCoverUrl = null;
        session.TempText = null;
        session.TargetFlow = null;
        session.SearchMessageId = 0;

        await _bot.BroadcastPostToGroupAsync(newPost.Id, ct);

        await _bot.SendHtmlAsync(chatId,
            $"✅ <b>Taqrizingiz muvaffaqiyatli chop etildi!</b>\n\n" +
            $"📖 «{TelegramBotService.Escape(postBook.Title)}» — <i>{TelegramBotService.Escape(postBook.Author)}</i>",
            BuildMainMenuKeyboard(user.Role >= UserRole.Admin), ct: ct);
    }

    private async Task SaveNewQuoteAsync(
        IAppDbContext db, long chatId, User user, UserBotSession session, string? bookCoverUrl, string? quoteImageUrl, CancellationToken ct)
    {
        Book? quoteBook = null;
        if (session.SelectedBookId.HasValue)
        {
            quoteBook = await db.Books.FirstOrDefaultAsync(b => b.Id == session.SelectedBookId.Value, ct);
        }

        if (quoteBook is null)
        {
            var normTitle = TextNormalizer.NormalizeApostrophes(session.TempBookTitle).ToLower();
            quoteBook = await db.Books.FirstOrDefaultAsync(b =>
                b.Title.ToLower()
                    .Replace("‘", "'")
                    .Replace("’", "'")
                    .Replace("ʻ", "'")
                    .Replace("ʼ", "'")
                    .Replace("`", "'")
                    .Replace("´", "'")
                    .Replace("ʹ", "'") == normTitle, ct);
        }

        var cover = !string.IsNullOrWhiteSpace(bookCoverUrl) ? bookCoverUrl : "/img/default-book-cover.webp";

        if (quoteBook is null)
        {
            quoteBook = new Book
            {
                Title = session.TempBookTitle ?? "Kitob",
                Author = session.TempBookAuthor ?? "Muallif",
                CoverUrl = cover,
                TotalPages = 250,
                GenreId = 1
            };
            db.Books.Add(quoteBook);
            await db.SaveChangesAsync(ct);
        }
        else if (!string.IsNullOrWhiteSpace(bookCoverUrl) &&
                 (string.IsNullOrWhiteSpace(quoteBook.CoverUrl) || quoteBook.CoverUrl.Contains("default-book-cover", StringComparison.OrdinalIgnoreCase)))
        {
            quoteBook.CoverUrl = bookCoverUrl;
            await db.SaveChangesAsync(ct);
        }

        var text = session.TempText ?? "";
        var quote = new Quote
        {
            UserId = user.Id,
            BookId = quoteBook.Id,
            Text = text,
            ImageUrl = quoteImageUrl,
            Slug = Guid.NewGuid().ToString("N")[..12],
            CreatedAt = DateTime.UtcNow
        };
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(ct);
        session.Step = SessionStep.None;
        session.SelectedBookId = null;
        session.TempBookTitle = null;
        session.TempBookAuthor = null;
        session.TempCoverUrl = null;
        session.TempText = null;
        session.TargetFlow = null;
        session.SearchMessageId = 0;

        await _bot.BroadcastQuoteToGroupAsync(quote.Id, ct);

        await _bot.SendHtmlAsync(chatId,
            $"✅ <b>Iqtibosingiz chop etildi!</b>\n\n" +
            $"«{TelegramBotService.Escape(quote.Text)}»\n" +
            $"📚 <i>{TelegramBotService.Escape(quoteBook.Title)}</i>",
            BuildMainMenuKeyboard(user.Role >= UserRole.Admin), ct: ct);
    }

    private async Task UpdateLikeButtonOnMessageAsync(
        JsonNode msg,
        long chatId,
        int messageId,
        string callbackData,
        int newCount,
        CancellationToken ct)
    {
        try
        {
            var replyMarkup = msg["reply_markup"];
            var inlineKeyboard = replyMarkup?["inline_keyboard"]?.AsArray();
            if (inlineKeyboard is null || inlineKeyboard.Count == 0) return;

            var updated = false;
            foreach (var rowNode in inlineKeyboard)
            {
                var row = rowNode?.AsArray();
                if (row is null) continue;

                foreach (var btnNode in row)
                {
                    if (btnNode is null) continue;
                    var btnCb = btnNode["callback_data"]?.GetValue<string>();
                    if (btnCb == callbackData)
                    {
                        btnNode["text"] = newCount > 0 ? $"❤️ Yoqdi ({newCount})" : "❤️ Yoqdi";
                        updated = true;
                        break;
                    }
                }
                if (updated) break;
            }

            if (updated)
            {
                await _bot.EditReplyMarkupAsync(chatId, messageId, replyMarkup, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tugmadagi like sonini yangilashda xato: ChatId={ChatId}, MessageId={MessageId}", chatId, messageId);
        }
    }
}
