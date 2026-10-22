using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using KitobdaGimen.Application.Common;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Challenge.Commands.FinalizeChallengeMonth;
using KitobdaGimen.Domain.Entities;
using KitobdaGimen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace KitobdaGimen.Web.Telegram;

public class TelegramBotService : ITelegramBotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<TelegramBotSettings> _settingsMonitor;
    private readonly ILogger<TelegramBotService> _logger;

    // 2 daqiqalik OTP kodlar: code -> entry
    private readonly ConcurrentDictionary<string, TelegramOtpEntry> _otpByCode = new();
    // telegramId -> oxirgi yaratilgan kod (yangi kod so'ralganda eskisini o'chirish uchun)
    private readonly ConcurrentDictionary<long, string> _lastCodeByTelegramId = new();

    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(2);

    public TelegramBotService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<TelegramBotSettings> settingsMonitor,
        ILogger<TelegramBotService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _settingsMonitor = settingsMonitor;
        _logger = logger;
    }

    public TelegramBotSettings Settings => _settingsMonitor.CurrentValue;

    public string BotUsername =>
        string.IsNullOrWhiteSpace(Settings.BotUsername)
            ? "KitobdagiMenBot"
            : Settings.BotUsername.TrimStart('@');

    public string WebAppUrl =>
        string.IsNullOrWhiteSpace(Settings.WebAppUrl)
            ? "https://kitobdagimen.uz"
            : Settings.WebAppUrl.TrimEnd('/');

    private string ApiBaseUrl => $"https://api.telegram.org/bot{Settings.BotToken}";
    private string FileBaseUrl => $"https://api.telegram.org/file/bot{Settings.BotToken}";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Settings.BotToken);

    // ── 2 daqiqalik OTP boshqaruvi ──────────────────────────────────────────────

    public TelegramOtpEntry GenerateLoginOtp(int userId, long telegramId, string fullName, string? username)
    {
        CleanupExpiredOtps();

        if (_lastCodeByTelegramId.TryRemove(telegramId, out var oldCode))
        {
            _otpByCode.TryRemove(oldCode, out _);
        }

        string code;
        do
        {
            code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        } while (_otpByCode.ContainsKey(code));

        var entry = new TelegramOtpEntry(
            Code: code,
            UserId: userId,
            TelegramId: telegramId,
            FullName: fullName,
            Username: username,
            ExpiresAtUtc: DateTime.UtcNow.Add(OtpLifetime));

        _otpByCode[code] = entry;
        _lastCodeByTelegramId[telegramId] = code;
        return entry;
    }

    public TelegramOtpEntry? VerifyLoginOtp(string code)
    {
        CleanupExpiredOtps();
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = Regex.Replace(code, @"\s+", "");
        if (!_otpByCode.TryRemove(normalized, out var entry))
        {
            return null;
        }

        _lastCodeByTelegramId.TryRemove(entry.TelegramId, out _);

        if (entry.ExpiresAtUtc < DateTime.UtcNow)
        {
            return null;
        }

        return entry;
    }

    private void CleanupExpiredOtps()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _otpByCode)
        {
            if (kv.Value.ExpiresAtUtc < now)
            {
                _otpByCode.TryRemove(kv.Key, out _);
            }
        }
    }

    // ── Telegram Mini App (WebApp) initData HMAC-SHA256 tekshiruvi ──────────────

    public TelegramWebAppUser? ValidateWebAppInitData(string? initData)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(initData))
        {
            return null;
        }

        try
        {
            var pairs = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var segment in initData.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = segment.IndexOf('=');
                if (idx <= 0) continue;
                var k = Uri.UnescapeDataString(segment[..idx]);
                var v = Uri.UnescapeDataString(segment[(idx + 1)..]);
                pairs[k] = v;
            }

            if (!pairs.TryGetValue("hash", out var receivedHash) ||
                !pairs.TryGetValue("user", out var userJson))
            {
                return null;
            }

            var dataCheckString = string.Join("\n",
                pairs.Where(kv => kv.Key != "hash")
                     .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                     .Select(kv => $"{kv.Key}={kv.Value}"));

            var secretKey = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes("WebAppData"),
                Encoding.UTF8.GetBytes(Settings.BotToken));

            var computedBytes = HMACSHA256.HashData(
                secretKey,
                Encoding.UTF8.GetBytes(dataCheckString));

            var computedHex = Convert.ToHexString(computedBytes).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(computedHex),
                    Encoding.ASCII.GetBytes(receivedHash.ToLowerInvariant())))
            {
                return null;
            }

            var node = JsonNode.Parse(userJson);
            if (node is null) return null;

            var tgId = node["id"]?.GetValue<long>() ?? 0;
            if (tgId == 0) return null;

            return new TelegramWebAppUser(
                TelegramId: tgId,
                FirstName: node["first_name"]?.GetValue<string>() ?? "Kitobxon",
                LastName: node["last_name"]?.GetValue<string>() ?? "",
                Username: node["username"]?.GetValue<string>(),
                PhotoUrl: node["photo_url"]?.GetValue<string>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram WebApp initData tekshiruvida xato.");
            return null;
        }
    }

    // ── Telegram HTTP API helperlar ─────────────────────────────────────────────

    public async Task<JsonNode?> CallApiAsync(string method, object payload, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        try
        {
            var client = _httpClientFactory.CreateClient("TelegramBot");
            using var resp = await client.PostAsJsonAsync($"{ApiBaseUrl}/{method}", payload, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            var json = JsonNode.Parse(body);
            if (json?["ok"]?.GetValue<bool>() != true)
            {
                var desc = json?["description"]?.GetValue<string>() ?? body;
                if (!desc.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Telegram API {Method} xato qaytardi: {Body}", method, body);
                }

                // Agar foydalanuvchi botni bloklagan bo'lsa yoki akkaunti o'chirilgan bo'lsa
                if (desc.Contains("bot was blocked by the user", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("user is deactivated", StringComparison.OrdinalIgnoreCase))
                {
                    long targetChatId = 0;
                    if (payload is IDictionary<string, object?> dict && dict.TryGetValue("chat_id", out var cVal) && cVal is long cid)
                    {
                        targetChatId = cid;
                    }
                    else
                    {
                        var prop = payload.GetType().GetProperty("chat_id");
                        if (prop != null && prop.GetValue(payload) is long pCid)
                        {
                            targetChatId = pCid;
                        }
                    }

                    if (targetChatId > 0)
                    {
                        _ = MarkUserBlockedAsync(targetChatId, CancellationToken.None);
                    }
                }
            }
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram API {Method} chaqiruvida xatolik.", method);
            return null;
        }
    }

    public async Task<JsonNode?> SendHtmlAsync(
        long chatId,
        string html,
        object? replyMarkup = null,
        bool disableWebPreview = true,
        int? messageThreadId = null,
        object? linkPreviewOptions = null,
        CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            ["text"] = html,
            ["parse_mode"] = "HTML"
        };
        if (linkPreviewOptions != null)
        {
            payload["link_preview_options"] = linkPreviewOptions;
        }
        else
        {
            payload["disable_web_page_preview"] = disableWebPreview;
        }
        if (messageThreadId is int threadId && threadId > 0)
        {
            payload["message_thread_id"] = threadId;
        }
        if (replyMarkup is not null)
        {
            payload["reply_markup"] = replyMarkup;
        }

        var res = await CallApiAsync("sendMessage", payload, ct);
        if (res?["ok"]?.GetValue<bool>() != true && chatId < 0)
        {
            var strId = chatId.ToString();
            long? altChatId = null;
            if (strId.StartsWith("-100"))
            {
                if (long.TryParse("-" + strId[4..], out var stripped))
                {
                    altChatId = stripped;
                }
            }
            else
            {
                if (long.TryParse($"-100{Math.Abs(chatId)}", out var prefixed))
                {
                    altChatId = prefixed;
                }
            }

            if (altChatId.HasValue && altChatId.Value != chatId)
            {
                payload["chat_id"] = altChatId.Value;
                var retry = await CallApiAsync("sendMessage", payload, ct);
                if (retry?["ok"]?.GetValue<bool>() == true)
                {
                    return retry;
                }
            }
        }

        return res;
    }

    public async Task<JsonNode?> EditHtmlAsync(
        long chatId,
        int messageId,
        string html,
        object? replyMarkup = null,
        CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            ["message_id"] = messageId,
            ["text"] = html,
            ["parse_mode"] = "HTML",
            ["disable_web_page_preview"] = true
        };
        if (replyMarkup is not null)
        {
            payload["reply_markup"] = replyMarkup;
        }
        var res = await CallApiAsync("editMessageText", payload, ct);
        if (res?["ok"]?.GetValue<bool>() != true)
        {
            var desc = res?["description"]?.GetValue<string>() ?? "";
            if (desc.Contains("there is no text in the message to edit", StringComparison.OrdinalIgnoreCase))
            {
                var captionPayload = new Dictionary<string, object?>
                {
                    ["chat_id"] = chatId,
                    ["message_id"] = messageId,
                    ["caption"] = html,
                    ["parse_mode"] = "HTML"
                };
                if (replyMarkup is not null)
                {
                    captionPayload["reply_markup"] = replyMarkup;
                }
                return await CallApiAsync("editMessageCaption", captionPayload, ct);
            }
        }
        return res;
    }

    public async Task<JsonNode?> EditReplyMarkupAsync(
        long chatId,
        int messageId,
        object replyMarkup,
        CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            ["message_id"] = messageId,
            ["reply_markup"] = replyMarkup
        };

        var res = await CallApiAsync("editMessageReplyMarkup", payload, ct);
        if (res?["ok"]?.GetValue<bool>() != true && chatId < 0)
        {
            var strId = chatId.ToString();
            long? altChatId = null;
            if (strId.StartsWith("-100"))
            {
                if (long.TryParse("-" + strId[4..], out var stripped))
                    altChatId = stripped;
            }
            else
            {
                if (long.TryParse($"-100{Math.Abs(chatId)}", out var prefixed))
                    altChatId = prefixed;
            }

            if (altChatId.HasValue && altChatId.Value != chatId)
            {
                payload["chat_id"] = altChatId.Value;
                var retry = await CallApiAsync("editMessageReplyMarkup", payload, ct);
                if (retry?["ok"]?.GetValue<bool>() == true)
                {
                    return retry;
                }
            }
        }

        return res;
    }

    public Task AnswerCallbackAsync(
        string callbackQueryId,
        string? text = null,
        bool showAlert = false,
        CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["callback_query_id"] = callbackQueryId,
            ["show_alert"] = showAlert
        };
        if (!string.IsNullOrWhiteSpace(text))
        {
            payload["text"] = text;
        }
        return CallApiAsync("answerCallbackQuery", payload, ct);
    }

    public async Task<JsonNode?> SendPhotoAsync(
        long chatId,
        string photoUrlOrPath,
        string captionHtml,
        object? replyMarkup = null,
        int? messageThreadId = null,
        CancellationToken ct = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(photoUrlOrPath))
        {
            return await SendHtmlAsync(chatId, captionHtml, replyMarkup, messageThreadId: messageThreadId, ct: ct);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("TelegramBot");

            // 1. Diskdagi lokal fayl bo'lsa (/uploads/...)
            string? localFilePath = null;
            if (photoUrlOrPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var rel = photoUrlOrPath["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
                var full = Path.Combine(UploadPaths.Root, rel);
                if (File.Exists(full))
                {
                    localFilePath = full;
                }
            }
            else if (File.Exists(photoUrlOrPath))
            {
                localFilePath = photoUrlOrPath;
            }

            if (localFilePath != null)
            {
                byte[]? fileBytes = null;
                var sendFileName = Path.GetFileName(localFilePath);
                try
                {
                    using var img = await Image.LoadAsync(localFilePath, ct);
                    const int maxDim = 900;
                    if (img.Width > maxDim || img.Height > maxDim)
                    {
                        img.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxDim, maxDim)
                        }));
                    }
                    using var ms = new MemoryStream();
                    await img.SaveAsJpegAsync(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 85 }, ct);
                    fileBytes = ms.ToArray();
                    sendFileName = Path.GetFileNameWithoutExtension(localFilePath) + ".jpg";
                }
                catch
                {
                    fileBytes = null;
                }

                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(chatId.ToString()), "chat_id");
                form.Add(new StringContent(captionHtml), "caption");
                form.Add(new StringContent("HTML"), "parse_mode");
                form.Add(new StringContent("true"), "show_caption_above_media");

                if (messageThreadId is int threadId && threadId > 0)
                {
                    form.Add(new StringContent(threadId.ToString()), "message_thread_id");
                }

                if (replyMarkup != null)
                {
                    form.Add(new StringContent(JsonSerializer.Serialize(replyMarkup)), "reply_markup");
                }

                if (fileBytes != null)
                {
                    form.Add(new ByteArrayContent(fileBytes), "photo", sendFileName);
                }
                else
                {
                    await using var stream = File.OpenRead(localFilePath);
                    var fileContent = new StreamContent(stream);
                    form.Add(fileContent, "photo", sendFileName);
                }

                using var resp = await client.PostAsync($"{ApiBaseUrl}/sendPhoto", form, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);
                var json = JsonNode.Parse(body);
                if (json?["ok"]?.GetValue<bool>() == true)
                {
                    return json;
                }
                _logger.LogWarning("sendPhoto (local file) xato qaytardi: {Body}", body);
            }
            else
            {
                // URL bo'lsa
                var targetPhotoUrl = photoUrlOrPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? photoUrlOrPath
                    : $"{WebAppUrl}/{photoUrlOrPath.TrimStart('/')}";

                var payload = new Dictionary<string, object?>
                {
                    ["chat_id"] = chatId,
                    ["photo"] = targetPhotoUrl,
                    ["caption"] = captionHtml,
                    ["parse_mode"] = "HTML",
                    ["show_caption_above_media"] = true
                };
                if (messageThreadId is int tid && tid > 0) payload["message_thread_id"] = tid;
                if (replyMarkup != null) payload["reply_markup"] = replyMarkup;

                var res = await CallApiAsync("sendPhoto", payload, ct);
                if (res?["ok"]?.GetValue<bool>() == true)
                {
                    return res;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "sendPhoto chaqiruvida xatolik: ChatId={ChatId}", chatId);
        }

        // Agar rasm yuborishda xato bo'lsa fallback qilib oddiy matn yuboramiz
        return await SendHtmlAsync(chatId, captionHtml, replyMarkup, messageThreadId: messageThreadId, ct: ct);
    }

    // ── Rasm yuklab olish va WebP qilib saqlash ──────────────────────────────────

    public async Task<string?> DownloadTelegramPhotoAsWebpAsync(
        string fileId, string targetFolder, int maxDimension = 800, CancellationToken ct = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(fileId)) return null;

        try
        {
            var fileInfo = await CallApiAsync("getFile", new { file_id = fileId }, ct);
            var filePath = fileInfo?["result"]?["file_path"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(filePath)) return null;

            var client = _httpClientFactory.CreateClient("TelegramBot");
            await using var stream = await client.GetStreamAsync($"{FileBaseUrl}/{filePath}", ct);
            using var image = await Image.LoadAsync(stream, ct);

            if (image.Width > maxDimension || image.Height > maxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxDimension, maxDimension)
                }));
            }

            var dir = UploadPaths.Dir(targetFolder);
            var fileName = $"{Guid.NewGuid():N}.webp";
            var fullPath = Path.Combine(dir, fileName);
            await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = 84 }, ct);
            return $"/uploads/{targetFolder}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram rasmini yuklab olishda xato: {FileId}", fileId);
            return null;
        }
    }

    public async Task<string?> FetchUserAvatarFromTelegramAsync(long telegramUserId, CancellationToken ct = default)
    {
        try
        {
            var res = await CallApiAsync("getUserProfilePhotos", new { user_id = telegramUserId, limit = 1 }, ct);
            var photos = res?["result"]?["photos"]?.AsArray();
            if (photos is null || photos.Count == 0) return null;

            var sizes = photos[0]?.AsArray();
            if (sizes is null || sizes.Count == 0) return null;

            var largestFileId = sizes[^1]?["file_id"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(largestFileId)) return null;

            return await DownloadTelegramPhotoAsWebpAsync(largestFileId, "avatars", 512, ct);
        }
        catch
        {
            return null;
        }
    }

    // ── Veb-saytga kirganlik haqida xabar ────────────────────────────────────────

    public async Task NotifyWebLoginSuccessAsync(long telegramId, CancellationToken ct = default)
    {
        if (telegramId <= 0) return;

        var html = "✅ <b>«KitobdagiMen» veb-platformasiga muvaffaqiyatli kirdingiz!</b>";

        await SendHtmlAsync(telegramId, html, ct: ct);
    }

    // ── "KitobdagiMen" Guruh ID va Topic ID sini olish ──────────────────────────

    public async Task<(long ChatId, int? TopicId)?> GetGroupTargetAsync(IAppDbContext db, CancellationToken ct = default)
    {
        var row = await db.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == ChallengeConfigStore.BookClubGroupIdKey, ct);

        var raw = !string.IsNullOrWhiteSpace(row?.Value)
            ? row.Value
            : Settings.BookClubGroupId;

        if (string.IsNullOrWhiteSpace(raw)) return null;

        // Format: "-1001234567890" yoki "-1001234567890:42" (42 = Topic / message_thread_id)
        var parts = raw.Trim().Split(':', 2, StringSplitOptions.TrimEntries);
        if (long.TryParse(parts[0], out var chatId) && chatId != 0)
        {
            int? topicId = Settings.BookClubTopicId;
            if (parts.Length > 1 && int.TryParse(parts[1], out var parsedTopic) && parsedTopic > 0)
            {
                topicId = parsedTopic;
            }
            return (chatId, topicId);
        }
        return null;
    }

    public async Task<long?> GetBookClubGroupIdAsync(IAppDbContext db, CancellationToken ct = default)
    {
        var target = await GetGroupTargetAsync(db, ct);
        return target?.ChatId;
    }

    // ── Guruh integratsiyasi: Taqriz va Iqtiboslarni guruhga uzatish ──────────

    public async Task<bool> IsUserInGroupAsync(long groupChatId, long telegramUserId, CancellationToken ct = default)
    {
        if (groupChatId == 0 || telegramUserId == 0) return false;

        try
        {
            var res = await CallApiAsync("getChatMember", new
            {
                chat_id = groupChatId,
                user_id = telegramUserId
            }, ct);

            if (res?["ok"]?.GetValue<bool>() != true)
            {
                return false;
            }

            var status = res["result"]?["status"]?.GetValue<string>()?.ToLowerInvariant();
            return status is "creator" or "administrator" or "member" or "restricted";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "getChatMember tekshiruvida xatolik: ChatId={ChatId}, UserId={UserId}", groupChatId, telegramUserId);
            return false;
        }
    }

    private string? _cachedGroupInviteLink;

    public async Task<bool> IsUserMemberOfBookClubAsync(long telegramUserId, CancellationToken ct = default)
    {
        if (telegramUserId <= 0) return false;

        if (Settings.SuperAdminTelegramIds.Contains(telegramUserId))
        {
            return true;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var target = await GetGroupTargetAsync(db, ct);
        if (target is null || target.Value.ChatId == 0)
        {
            return true;
        }

        return await IsUserInGroupAsync(target.Value.ChatId, telegramUserId, ct);
    }

    public async Task<string> GetGroupInviteLinkAsync(CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedGroupInviteLink))
        {
            return _cachedGroupInviteLink;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var row = await db.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == ChallengeConfigStore.BookClubInviteLinkKey, ct);
            if (!string.IsNullOrWhiteSpace(row?.Value))
            {
                _cachedGroupInviteLink = row.Value.Trim();
                return _cachedGroupInviteLink;
            }

            var target = await GetGroupTargetAsync(db, ct);
            if (target.HasValue && target.Value.ChatId != 0)
            {
                var chatRes = await CallApiAsync("getChat", new { chat_id = target.Value.ChatId }, ct);
                var invite = chatRes?["result"]?["invite_link"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(invite))
                {
                    _cachedGroupInviteLink = invite.Trim();
                    return _cachedGroupInviteLink;
                }

                var username = chatRes?["result"]?["username"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(username))
                {
                    _cachedGroupInviteLink = $"https://t.me/{username.TrimStart('@')}";
                    return _cachedGroupInviteLink;
                }

                var exportRes = await CallApiAsync("exportChatInviteLink", new { chat_id = target.Value.ChatId }, ct);
                var exportedLink = exportRes?["result"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(exportedLink))
                {
                    _cachedGroupInviteLink = exportedLink.Trim();
                    return _cachedGroupInviteLink;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Guruh invite linkini olishda xatolik.");
        }

        if (!string.IsNullOrWhiteSpace(Settings.BookClubInviteLink))
        {
            _cachedGroupInviteLink = Settings.BookClubInviteLink.Trim();
            return _cachedGroupInviteLink;
        }

        _cachedGroupInviteLink = "https://t.me/+M2zWs1tYAKoxMjli";
        return _cachedGroupInviteLink;
    }

    public async Task BroadcastPostToGroupAsync(int postId, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var target = await GetGroupTargetAsync(db, ct);
            if (target is null) return;

            var post = await db.Posts
                .Include(p => p.User)
                .Include(p => p.Book)
                .FirstOrDefaultAsync(p => p.Id == postId, ct);

            if (post is null) return;

            // 1) Foydalanuvchi Telegram ID sini olamiz
            if (!TryExtractTelegramId(post.User.GoogleId, out var userTgId))
            {
                _logger.LogInformation("Post #{PostId}: Muallif {UserId} Telegram profiliga ulanmagan — guruhga yuborilmadi.", postId, post.UserId);
                return;
            }

            // 2) Aynan KitobdagiMen guruhida bor-yo'qligini tekshiramiz
            var memberInfo = await CallApiAsync("getChatMember", new
            {
                chat_id = target.Value.ChatId,
                user_id = userTgId
            }, ct);

            var status = memberInfo?["result"]?["status"]?.GetValue<string>()?.ToLowerInvariant();
            var isInGroup = status is "creator" or "administrator" or "member" or "restricted";
            if (!isInGroup)
            {
                _logger.LogInformation("Post #{PostId}: Muallif {UserId} (tg:{TgId}) KitobdagiMen guruhida mavjud emas — guruhga yuborilmadi.", postId, post.UserId, userTgId);
                return;
            }

            // 3) Muallif Telegram username'ini aniqlash va saqlash
            var fetchedUsername = memberInfo?["result"]?["user"]?["username"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(fetchedUsername))
            {
                await SaveTelegramUsernameSettingAsync(db, post.UserId, fetchedUsername, ct);
            }
            else
            {
                var savedTg = await db.AppSettings.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == $"Telegram:Username:{post.UserId}", ct);
                if (!string.IsNullOrWhiteSpace(savedTg?.Value))
                {
                    fetchedUsername = savedTg.Value;
                }
            }

            var cleanUsername = fetchedUsername?.Trim().TrimStart('@');
            if (string.IsNullOrWhiteSpace(cleanUsername) && !string.IsNullOrWhiteSpace(post.User.Username))
            {
                cleanUsername = post.User.Username.Trim().TrimStart('@');
            }

            var authorText = !string.IsNullOrWhiteSpace(cleanUsername)
                ? $"✍️ <b>Kitobxon:</b> @{cleanUsername}"
                : (userTgId > 0
                    ? $"✍️ <b>Kitobxon:</b> <a href=\"tg://user?id={userTgId}\">{Escape(post.User.FullName)}</a>"
                    : $"✍️ <b>Kitobxon:</b> <b>{Escape(post.User.FullName)}</b>");

            var cleanReview = TruncateText(StripHtml(post.ReviewText), 3500);

            var profileSlug = !string.IsNullOrWhiteSpace(post.User.Username) ? post.User.Username : post.User.Id.ToString();
            var postDetailUrl = $"{WebAppUrl}/post/{profileSlug}/{post.Slug}";
            var authorSuffix = string.IsNullOrWhiteSpace(post.Book.Author) ? "" : $" — <i>{Escape(post.Book.Author)}</i>";

            var html = new StringBuilder();
            html.AppendLine("📝 <b>YANGI TAQRIZ</b>");
            html.AppendLine();
            html.AppendLine($"«{Escape(cleanReview)}»");
            html.AppendLine();
            html.AppendLine($"📚 <b>Kitob:</b> «{Escape(post.Book.Title)}»{authorSuffix}");
            html.AppendLine(authorText);
            html.AppendLine($"🔗 <b>Taqriz:</b> <a href=\"{postDetailUrl}\">Saytda to'liq o'qish</a>");

            var likeCount = await db.Likes.CountAsync(l => l.PostId == post.Id, ct);
            var likeBtnText = likeCount > 0 ? $"❤️ Yoqdi ({likeCount})" : "❤️ Yoqdi";

            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = likeBtnText, callback_data = $"post:like:{post.Id}" },
                        new { text = "💬 Fikr bildirish", url = $"https://t.me/{BotUsername}?start=comment_post_{post.Id}" }
                    }
                }
            };

            // Guruhga hech qanday rasm yuborilmaydi (rasmlar faqat saytda ko'rinadi).
            // disableWebPreview: true orqali link preview ham chiqmaydi, toza matnli yagona xabar boradi.
            var res = await SendHtmlAsync(
                target.Value.ChatId,
                html.ToString(),
                markup,
                disableWebPreview: true,
                messageThreadId: target.Value.TopicId,
                ct: ct);

            var sentMsgId = res?["result"]?["message_id"]?.GetValue<int>();
            if (sentMsgId.HasValue && sentMsgId.Value > 0)
            {
                var realChatId = res?["result"]?["chat"]?["id"]?.GetValue<long>() ?? target.Value.ChatId;
                post.TelegramChatId = realChatId != 0 ? realChatId : target.Value.ChatId;
                post.TelegramMessageId = sentMsgId.Value;
                await db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Post #{PostId} muallifi {UserId} guruhda borligi tasdiqlanib, toza matn bilan xabar guruhga yuborildi (TgMsgId={MsgId}).", postId, post.UserId, sentMsgId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Taqrizni guruhga yuborishda xatolik: PostId={PostId}", postId);
        }
    }

    public async Task SyncPostLikesInGroupAsync(int postId, int likeCount, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var post = await db.Posts
                .AsNoTracking()
                .Where(p => p.Id == postId)
                .Select(p => new { p.TelegramChatId, p.TelegramMessageId })
                .FirstOrDefaultAsync(ct);

            if (post?.TelegramChatId is not long chatId || post.TelegramMessageId is not int msgId || msgId <= 0)
            {
                return;
            }

            var likeText = likeCount > 0 ? $"❤️ Yoqdi ({likeCount})" : "❤️ Yoqdi";
            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = likeText, callback_data = $"post:like:{postId}" },
                        new { text = "💬 Fikr bildirish", url = $"https://t.me/{BotUsername}?start=comment_post_{postId}" }
                    }
                }
            };

            await EditReplyMarkupAsync(chatId, msgId, markup, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Guruhdagi post like tugmasini yangilashda xatolik: PostId={PostId}", postId);
        }
    }

    public async Task DeletePostBroadcastAsync(int postId, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var post = await db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post?.TelegramChatId is long chatId && post.TelegramMessageId is int messageId && messageId > 0)
            {
                await DeleteMessageAsync(chatId, messageId, ct);
                _logger.LogInformation("Post #{PostId} Telegram xabari (#{MessageId}) guruhdan o'chirildi.", postId, messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post #{PostId} Telegram xabarini o'chirishda xatolik.", postId);
        }
    }

    public async Task BroadcastQuoteToGroupAsync(int quoteId, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var target = await GetGroupTargetAsync(db, ct);
            if (target is null) return;

            var quote = await db.Quotes
                .Include(q => q.User)
                .Include(q => q.Book)
                .FirstOrDefaultAsync(q => q.Id == quoteId, ct);

            if (quote is null) return;

            // 1) Foydalanuvchi Telegram ID sini olamiz
            if (!TryExtractTelegramId(quote.User.GoogleId, out var userTgId))
            {
                _logger.LogInformation("Quote #{QuoteId}: Muallif {UserId} Telegram profiliga ulanmagan — guruhga yuborilmadi.", quoteId, quote.UserId);
                return;
            }

            // 2) Aynan KitobdagiMen guruhida bor-yo'qligini tekshiramiz
            var memberInfo = await CallApiAsync("getChatMember", new
            {
                chat_id = target.Value.ChatId,
                user_id = userTgId
            }, ct);

            var status = memberInfo?["result"]?["status"]?.GetValue<string>()?.ToLowerInvariant();
            var isInGroup = status is "creator" or "administrator" or "member" or "restricted";
            if (!isInGroup)
            {
                _logger.LogInformation("Quote #{QuoteId}: Muallif {UserId} (tg:{TgId}) KitobdagiMen guruhida mavjud emas — guruhga yuborilmadi.", quoteId, quote.UserId, userTgId);
                return;
            }

            // 3) Muallif username'ini aniqlash va saqlash
            var fetchedUsername = memberInfo?["result"]?["user"]?["username"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(fetchedUsername))
            {
                await SaveTelegramUsernameSettingAsync(db, quote.UserId, fetchedUsername, ct);
            }
            else
            {
                var savedTg = await db.AppSettings.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == $"Telegram:Username:{quote.UserId}", ct);
                if (!string.IsNullOrWhiteSpace(savedTg?.Value))
                {
                    fetchedUsername = savedTg.Value;
                }
            }

            var cleanUsername = fetchedUsername?.Trim().TrimStart('@');
            if (string.IsNullOrWhiteSpace(cleanUsername) && !string.IsNullOrWhiteSpace(quote.User.Username))
            {
                cleanUsername = quote.User.Username.Trim().TrimStart('@');
            }

            var authorText = !string.IsNullOrWhiteSpace(cleanUsername)
                ? $"✍️ <b>Kitobxon:</b> @{cleanUsername}"
                : (userTgId > 0
                    ? $"✍️ <b>Kitobxon:</b> <a href=\"tg://user?id={userTgId}\">{Escape(quote.User.FullName)}</a>"
                    : $"✍️ <b>Kitobxon:</b> <b>{Escape(quote.User.FullName)}</b>");

            var cleanText = TruncateText(StripHtml(quote.Text), 3500);

            var profileSlug = !string.IsNullOrWhiteSpace(quote.User.Username) ? quote.User.Username : quote.User.Id.ToString();
            var quoteDetailUrl = $"{WebAppUrl}/iqtibos/{profileSlug}/{quote.Slug}";
            var authorSuffix = string.IsNullOrWhiteSpace(quote.Book.Author) ? "" : $" — <i>{Escape(quote.Book.Author)}</i>";

            var html = new StringBuilder();
            html.AppendLine("💬 <b>KITOBLARDAN IQTIBOS</b>");
            html.AppendLine();
            html.AppendLine($"«{Escape(cleanText)}»");
            html.AppendLine();
            html.AppendLine($"📚 <b>Kitob:</b> «{Escape(quote.Book.Title)}»{authorSuffix}");
            html.AppendLine(authorText);
            html.AppendLine($"🔗 <b>Iqtibos:</b> <a href=\"{quoteDetailUrl}\">Saytda ko'rish</a>");

            var likeCount = await db.QuoteLikes.CountAsync(l => l.QuoteId == quote.Id, ct);
            var likeBtnText = likeCount > 0 ? $"❤️ Yoqdi ({likeCount})" : "❤️ Yoqdi";

            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = likeBtnText, callback_data = $"quote:like:{quote.Id}" },
                        new { text = "💬 Fikr bildirish", url = $"https://t.me/{BotUsername}?start=comment_quote_{quote.Id}" }
                    }
                }
            };

            // Guruhga hech qanday rasm yuborilmaydi (rasmlar faqat saytda ko'rinadi).
            // disableWebPreview: true orqali link preview ham chiqmaydi, toza matnli yagona xabar boradi.
            var res = await SendHtmlAsync(
                target.Value.ChatId,
                html.ToString(),
                markup,
                disableWebPreview: true,
                messageThreadId: target.Value.TopicId,
                ct: ct);

            var sentMsgId = res?["result"]?["message_id"]?.GetValue<int>();
            if (sentMsgId.HasValue && sentMsgId.Value > 0)
            {
                var realChatId = res?["result"]?["chat"]?["id"]?.GetValue<long>() ?? target.Value.ChatId;
                quote.TelegramChatId = realChatId != 0 ? realChatId : target.Value.ChatId;
                quote.TelegramMessageId = sentMsgId.Value;
                await db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Quote #{QuoteId} muallifi {UserId} guruhda borligi tasdiqlanib, toza matn bilan xabar guruhga yuborildi (TgMsgId={MsgId}).", quoteId, quote.UserId, sentMsgId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Iqtibosni guruhga yuborishda xatolik: QuoteId={QuoteId}", quoteId);
        }
    }

    public async Task SyncQuoteLikesInGroupAsync(int quoteId, int likeCount, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var quote = await db.Quotes
                .AsNoTracking()
                .Where(q => q.Id == quoteId)
                .Select(q => new { q.TelegramChatId, q.TelegramMessageId })
                .FirstOrDefaultAsync(ct);

            if (quote?.TelegramChatId is not long chatId || quote.TelegramMessageId is not int msgId || msgId <= 0)
            {
                return;
            }

            var likeText = likeCount > 0 ? $"❤️ Yoqdi ({likeCount})" : "❤️ Yoqdi";
            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = likeText, callback_data = $"quote:like:{quoteId}" },
                        new { text = "💬 Fikr bildirish", url = $"https://t.me/{BotUsername}?start=comment_quote_{quoteId}" }
                    }
                }
            };

            await EditReplyMarkupAsync(chatId, msgId, markup, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Guruhdagi iqtibos like tugmasini yangilashda xatolik: QuoteId={QuoteId}", quoteId);
        }
    }

    public async Task DeleteQuoteBroadcastAsync(int quoteId, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var quote = await db.Quotes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == quoteId, ct);
            if (quote?.TelegramChatId is long chatId && quote.TelegramMessageId is int messageId && messageId > 0)
            {
                await DeleteMessageAsync(chatId, messageId, ct);
                _logger.LogInformation("Quote #{QuoteId} Telegram xabari (#{MessageId}) guruhdan o'chirildi.", quoteId, messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Quote #{QuoteId} Telegram xabarini o'chirishda xatolik.", quoteId);
        }
    }

    public async Task<bool> DeleteMessageAsync(long chatId, int messageId, CancellationToken ct = default)
    {
        if (!IsConfigured || chatId == 0 || messageId == 0) return false;
        try
        {
            var res = await CallApiAsync("deleteMessage", new
            {
                chat_id = chatId,
                message_id = messageId
            }, ct);
            var ok = res?["ok"]?.GetValue<bool>() == true;
            if (ok)
            {
                _logger.LogInformation("Telegram xabari muvaffaqiyatli o'chirildi: ChatId={ChatId}, MessageId={MessageId}", chatId, messageId);
            }
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram xabarini o'chirishda xatolik: ChatId={ChatId}, MessageId={MessageId}", chatId, messageId);
            return false;
        }
    }

    // ── Kitob almashish (Physical Book) bildirishnomalari ───────────────────────

    public async Task NotifyPhysicalBookReservedAsync(int physicalBookId, int reserverUserId, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var book = await db.PhysicalBooks
                .AsNoTracking()
                .Include(p => p.Owner)
                .Include(p => p.Book)
                .FirstOrDefaultAsync(p => p.Id == physicalBookId, ct);

            if (book == null) return;

            var reserver = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == reserverUserId, ct);

            if (reserver == null) return;

            if (!TryExtractTelegramId(book.Owner.GoogleId, out var ownerTgId) || ownerTgId <= 0)
            {
                return;
            }

            var reserverHandle = await ResolveDisplayHandleAsync(db, reserver.Id, reserver.Username, ct);
            var bookTitle = book.Book?.Title ?? book.ManualTitle ?? "kitobingiz";
            var bookAuthor = book.Book?.Author ?? book.ManualAuthor ?? "";

            var html = new StringBuilder();
            html.AppendLine("📬 <b>YANGI KITOB SO'ROVI! · #kitob_almashish</b>");
            html.AppendLine("━━━━━━━━━━━━━━━━━━━━");
            html.AppendLine($"👤 <b>{Escape(reserver.FullName)}</b> ({reserverHandle}) sizning «<b>{Escape(bookTitle)}</b>» kitobingizni o'qimoqchi!");
            if (!string.IsNullOrWhiteSpace(bookAuthor))
            {
                html.AppendLine($"✍️ Muallif: <i>{Escape(bookAuthor)}</i>");
            }
            html.AppendLine();
            html.AppendLine("<blockquote>Kitobni ushbu kitobxonga berishga rozi bo'lsangiz, quyidagi <b>«✅ Qabul qilish»</b> tugmasini bosing. Shundan so'ng ikkalangiz bir-biringizga Telegram orqali yozib, kitobni topshirish joyi va vaqtini kelishib olishingiz mumkin bo'ladi!</blockquote>");

            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = "✅ Qabul qilish", callback_data = $"pb:accept:{book.Id}:{reserver.Id}" },
                        new { text = "❌ Rad etish", callback_data = $"pb:reject:{book.Id}:{reserver.Id}" }
                    },
                    new object[]
                    {
                        new { text = "🌐 Veb-saytda ko'rish", url = $"{WebAppUrl}/almashish" }
                    }
                }
            };

            var coverUrl = book.ManualCoverUrl ?? book.Book?.CoverUrl;
            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                await SendPhotoAsync(ownerTgId, coverUrl, html.ToString(), markup, ct: ct);
            }
            else
            {
                await SendHtmlAsync(ownerTgId, html.ToString(), markup, ct: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kitob band qilinganligi haqida egasiga Telegram xabari yuborishda xato: BookId={BookId}", physicalBookId);
        }
    }

    public async Task NotifyPhysicalBookHandoverConfirmedAsync(
        int physicalBookId, int ownerUserId, int reserverUserId, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var book = await db.PhysicalBooks
                .AsNoTracking()
                .Include(p => p.Book)
                .FirstOrDefaultAsync(p => p.Id == physicalBookId, ct);

            var owner = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ownerUserId, ct);
            var reserver = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == reserverUserId, ct);

            if (book == null || owner == null || reserver == null) return;

            var bookTitle = book.Book?.Title ?? book.ManualTitle ?? "kitob";
            var coverUrl = book.ManualCoverUrl ?? book.Book?.CoverUrl;

            var ownerHandle = await ResolveDisplayHandleAsync(db, owner.Id, owner.Username, ct);
            var reserverHandle = await ResolveDisplayHandleAsync(db, reserver.Id, reserver.Username, ct);

            var ownerContact = ownerHandle.StartsWith('@')
                ? $"<a href=\"https://t.me/{ownerHandle.TrimStart('@')}\"><b>{ownerHandle}</b></a>"
                : $"<b>{ownerHandle}</b>";

            var reserverContact = reserverHandle.StartsWith('@')
                ? $"<a href=\"https://t.me/{reserverHandle.TrimStart('@')}\"><b>{reserverHandle}</b></a>"
                : $"<b>{reserverHandle}</b>";

            // 1) O'qimoqchi bo'lgan foydalanuvchiga (Reserver) xabar
            if (TryExtractTelegramId(reserver.GoogleId, out var reserverTgId) && reserverTgId > 0)
            {
                var sbReserver = new StringBuilder();
                sbReserver.AppendLine("🎉 <b>KITOB SO'ROVINGIZ QABUL QILINDI!</b>");
                sbReserver.AppendLine("━━━━━━━━━━━━━━━━━━━━");
                sbReserver.AppendLine($"«<b>{Escape(bookTitle)}</b>» kitobi egasi <b>{Escape(owner.FullName)}</b> so'rovingizni qabul qildi!");
                sbReserver.AppendLine();
                sbReserver.AppendLine("📍 <b>Endi kitobni qayerda va qachon olib ketishni kelishib olish uchun kitob egasiga yozing:</b>");
                sbReserver.AppendLine($"👉 <b>Telegram:</b> {ownerContact}");
                sbReserver.AppendLine($"👤 <b>Kitob egasi:</b> {Escape(owner.FullName)} ({ownerHandle})");
                sbReserver.AppendLine();
                sbReserver.AppendLine("📖 <i>Ikkalangiz kelishib, kitobni olib ketishingiz mumkin. Yoqimli mutolaa!</i>");

                var markupReserver = ownerHandle.StartsWith('@')
                    ? new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = $"💬 Egasiga yozish ({ownerHandle})", url = $"https://t.me/{ownerHandle.TrimStart('@')}" } },
                            new object[] { new { text = "🌐 Veb-saytda ko'rish", url = $"{WebAppUrl}/almashish" } }
                        }
                    }
                    : new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = "🌐 Veb-saytda ko'rish", url = $"{WebAppUrl}/almashish" } }
                        }
                    };

                if (!string.IsNullOrWhiteSpace(coverUrl))
                    await SendPhotoAsync(reserverTgId, coverUrl, sbReserver.ToString(), markupReserver, ct: ct);
                else
                    await SendHtmlAsync(reserverTgId, sbReserver.ToString(), markupReserver, ct: ct);
            }

            // 2) Kitob egasiga (Owner) xabar
            if (TryExtractTelegramId(owner.GoogleId, out var ownerTgId) && ownerTgId > 0)
            {
                var sbOwner = new StringBuilder();
                sbOwner.AppendLine("✅ <b>KITOB SO'ROVINI QABUL QILDINGIZ!</b>");
                sbOwner.AppendLine("━━━━━━━━━━━━━━━━━━━━");
                sbOwner.AppendLine($"«<b>{Escape(bookTitle)}</b>» kitobingizni <b>{Escape(reserver.FullName)}</b> olib ketishi mumkin.");
                sbOwner.AppendLine();
                sbOwner.AppendLine("📍 <b>Kitobni qayerda va qachon topshirishni kelishib olish uchun kitobxonga yozing:</b>");
                sbOwner.AppendLine($"👉 <b>Telegram:</b> {reserverContact}");
                sbOwner.AppendLine($"👤 <b>Kitobxon:</b> {Escape(reserver.FullName)} ({reserverHandle})");
                sbOwner.AppendLine();
                sbOwner.AppendLine("🤝 <i>Iltimos, kitobxon bilan bog'lanib, kitobni topshirish joyi va vaqtini kelishib oling.</i>");

                var markupOwner = reserverHandle.StartsWith('@')
                    ? new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = $"💬 Kitobxonga yozish ({reserverHandle})", url = $"https://t.me/{reserverHandle.TrimStart('@')}" } },
                            new object[] { new { text = "🌐 Veb-saytda ko'rish", url = $"{WebAppUrl}/almashish" } }
                        }
                    }
                    : new
                    {
                        inline_keyboard = new object[][]
                        {
                            new object[] { new { text = "🌐 Veb-saytda ko'rish", url = $"{WebAppUrl}/almashish" } }
                        }
                    };

                if (!string.IsNullOrWhiteSpace(coverUrl))
                    await SendPhotoAsync(ownerTgId, coverUrl, sbOwner.ToString(), markupOwner, ct: ct);
                else
                    await SendHtmlAsync(ownerTgId, sbOwner.ToString(), markupOwner, ct: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kitob topshirilishi tasdiqlanganda foydalanuvchilarga xabar berishda xato: BookId={BookId}", physicalBookId);
        }
    }

    public async Task BroadcastChallengeUpdateAsync(string messageHtml, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var target = await GetGroupTargetAsync(db, ct);
            if (target is null) return;

            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = "🏆 Challenge'da qatnashish", url = $"https://t.me/{BotUsername}?start=challenge" }
                    }
                }
            };

            await SendHtmlAsync(target.Value.ChatId, messageHtml, markup, messageThreadId: target.Value.TopicId, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Challenge yangiligini guruhga yuborishda xatolik.");
        }
    }

    public async Task<int> BroadcastChallengeWinnersAsync(int year, int month, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

            var winners = await db.ChallengeWinners
                .AsNoTracking()
                .Include(w => w.User)
                .Where(w => w.Year == year && w.Month == month)
                .OrderBy(w => w.Rank)
                .ToListAsync(ct);

            if (winners.Count == 0)
            {
                await mediator.Send(new FinalizeChallengeMonthCommand(year, month) { BypassAdminCheck = true }, ct);
                winners = await db.ChallengeWinners
                    .AsNoTracking()
                    .Include(w => w.User)
                    .Where(w => w.Year == year && w.Month == month)
                    .OrderBy(w => w.Rank)
                    .ToListAsync(ct);
            }

            if (winners.Count == 0)
            {
                _logger.LogWarning("Challenge g'oliblari topilmadi: {Year}-{Month}", year, month);
                return 0;
            }

            var periodLabel = ChallengeCalendar.PeriodLabel(year, month);
            var customCfg = await ChallengeConfigStore.GetStoredOrNullAsync(db, ct);
            var challengeTitle = customCfg?.Title ?? $"{ChallengeCalendar.MonthName(month)} Challenge";

            var sb = new StringBuilder();
            sb.AppendLine("🏆 <b>«KITOBDAGIMEN» MUTOLAA CHALLENGE G'OLIBLARI!</b>");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"📌 <b>Bellashuv:</b> {Escape(challengeTitle)} ({periodLabel})");
            sb.AppendLine("🎉 <i>Eng ko'p kitob o'qigan va eng faol kitobxonlarimiz aniqlandi:</i>");
            sb.AppendLine();

            foreach (var w in winners)
            {
                var medal = w.Rank switch
                {
                    1 => "🥇",
                    2 => "🥈",
                    3 => "🥉",
                    _ => $"<b>{w.Rank}.</b>"
                };

                var handle = await ResolveDisplayHandleAsync(db, w.UserId, w.User.Username, ct);
                var giftTitle = w.GiftBookTitle ?? customCfg?.GetPrize(w.Rank)?.Title ?? "Maxsus sovg'a kitob";
                var giftAuthor = w.GiftBookAuthor ?? customCfg?.GetPrize(w.Rank)?.Author ?? "";
                var giftAuthorText = string.IsNullOrWhiteSpace(giftAuthor) ? "" : $" — <i>{Escape(giftAuthor)}</i>";

                sb.AppendLine($"{medal} <b>{w.Rank}-O'RIN: {handle}</b> ({Escape(w.User.FullName)})");
                sb.AppendLine($"   📖 O'qilgan betlar: <b>{w.PagesRead} bet</b> · Kitoblar: <b>{w.BooksRead} ta</b> · Faol kunlar: <b>{w.ActiveDays} kun</b>");
                sb.AppendLine($"   🎁 Sovg'a kitob: <b>«{Escape(giftTitle)}»</b>{giftAuthorText}");
                sb.AppendLine();
            }

            sb.AppendLine("👏 <i>Barcha g'oliblarni yuksak natijalar bilan tabriklaymiz! Mutolaadan aslo to'xtamang!</i>");

            var groupMarkup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = "🏆 Challenge reytingi", url = $"https://t.me/{BotUsername}?start=challenge" }
                    }
                }
            };

            // 1) Linked Telegram guruhiga yuborish
            var target = await GetGroupTargetAsync(db, ct);
            if (target is not null)
            {
                await SendHtmlAsync(
                    target.Value.ChatId, sb.ToString(), groupMarkup, messageThreadId: target.Value.TopicId, ct: ct);
            }

            // 2) G'oliblarga bot orqali shaxsiy tabriknoma va sovg'ani olish ko'rsatmasi
            foreach (var w in winners)
            {
                if (TryExtractTelegramId(w.User.GoogleId, out var winnerTgId))
                {
                    var giftTitle = w.GiftBookTitle ?? customCfg?.GetPrize(w.Rank)?.Title ?? "Maxsus sovg'a kitob";
                    var giftAuthor = w.GiftBookAuthor ?? customCfg?.GetPrize(w.Rank)?.Author ?? "";
                    var giftAuthorText = string.IsNullOrWhiteSpace(giftAuthor) ? "" : $" — <i>{Escape(giftAuthor)}</i>";

                    var winnerDm =
                        "🎉 <b>TABRIKLAYMIZ! SIZ CHALLENGE G'OLIBI BO'LDINGIZ!</b> 🏆\n" +
                        "━━━━━━━━━━━━━━━━━━━━\n" +
                        $"Siz «KitobdagiMen» mutolaa bellashuvida faxrli <b>{w.Rank}-o'rinni</b> egalladingiz!\n\n" +
                        $"<blockquote>📖 <b>O'qigan betlaringiz:</b> {w.PagesRead} bet ({w.BooksRead} ta kitob)\n" +
                        $"🎁 <b>Sizning sovg'angiz:</b> «{Escape(giftTitle)}»{giftAuthorText}</blockquote>\n\n" +
                        "📦 <i>Sovg'angizni yetkazib berish (yoki qabul qilib olish) uchun platforma ma'muriyati bilan bog'laning:</i>\n" +
                        "👤 @javohirsadullayev\n\n" +
                        "🔥 <i>Kitob mutolaasidagi intilishingiz va g'alabangiz bardavom bo'lsin!</i>";

                    await SendHtmlAsync(winnerTgId, winnerDm, ct: ct);
                }
            }

            // 3) Telegram botdagi boshqa barcha faol kitobxonlarga ham g'oliblar e'lonini yetkazish
            var botUsers = await db.Users
                .AsNoTracking()
                .Where(u => u.GoogleId.StartsWith("tg:"))
                .Select(u => new { u.Id, u.GoogleId })
                .ToListAsync(ct);

            var winnerUserIds = winners.Select(w => w.UserId).ToHashSet();
            foreach (var bu in botUsers)
            {
                if (winnerUserIds.Contains(bu.Id)) continue;
                if (TryExtractTelegramId(bu.GoogleId, out var tgId))
                {
                    await SendHtmlAsync(tgId, sb.ToString(), groupMarkup, ct: ct);
                }
            }

            _logger.LogInformation("{Year}-{Month} Challenge g'oliblari guruh va botda e'lon qilindi ({Count} ta g'olib).", year, month, winners.Count);
            return winners.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Challenge g'oliblarini e'lon qilishda xatolik: {Year}-{Month}", year, month);
            return 0;
        }
    }

    // ── Soat 20:00 — Bugun kitob o'qimaganlarga shaxsiy eslatma ──────────────────

    public async Task<int> SendDailyReadingRemindersAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = UzTime.Today;

        var readTodayUserIds = await db.ReadingProgress
            .Where(p => p.Date == today && p.PagesReadToday > 0)
            .Select(p => p.ReadingGoal.UserId)
            .Distinct()
            .ToListAsync(ct);

        var candidates = await db.Users
            .AsNoTracking()
            .Where(u => !readTodayUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Username, u.GoogleId })
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            return 0;
        }

        var webNotification = new NotificationDto
        {
            Type = "reading_reminder",
            ActorName = "KitobdagiMen",
            Message = "Soat 20:00 bo'ldi! Bugun hali kitob o'qimadingiz — mutolaaga vaqt ajrating! 📖",
            Url = "/reading-books",
            ActorAvatarUrl = "/img/icons/icon-192.png"
        };
        await notifications.NotifyManyAsync(candidates.Select(c => c.Id).ToList(), webNotification, ct);

        var sentCount = 0;
        foreach (var user in candidates)
        {
            if (!TryExtractTelegramId(user.GoogleId, out var tgId))
            {
                continue;
            }

            var html =
                "🕗 <b>KECHKI MUTOLAA ESLATMASI (20:00)</b>\n" +
                "━━━━━━━━━━━━━━━━━━━━\n" +
                $"👋 Salom, <b>{Escape(user.FullName)}</b>!\n\n" +
                "<blockquote>«Bugun o'qilgan 20 bet — ertangi g'alabangizning poydevori.»</blockquote>\n\n" +
                "📌 Siz bugun hali o'qilgan betlaringizni kiritmadingiz. Soat <b>23:59</b> da <b>«KitobdagiMen»</b> guruhida e'lon qilinadigan kunlik sarhisobda va <b>Challenge</b> reytingida ismingiz chiqishi uchun hoziroq mutolaa qiling! 📖🔥";

            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = "📖 Hozir bet kiritish", callback_data = "read:log_menu" },
                        new { text = "🏆 Challenge reytingi", callback_data = "menu:challenge" }
                    }
                }
            };

            var res = await SendHtmlAsync(tgId, html, markup, ct: ct);
            if (res?["ok"]?.GetValue<bool>() == true)
            {
                sentCount++;
            }
        }

        _logger.LogInformation("20:00 kunlik mutolaa eslatmasi {Count} ta Telegram foydalanuvchisiga yuborildi.", sentCount);
        return sentCount;
    }

    // ── Soat 23:59 — "KitobdagiMen" guruhiga kunlik mutolaa hisoboti ─────────────

    public async Task<bool> SendDailyGroupReportAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var reportHtml = await BuildDailyReportHtmlAsync(db, UzTime.Today, ct);

        var target = await GetGroupTargetAsync(db, ct);
        var markup = new
        {
            inline_keyboard = new object[][]
            {
                new object[]
                {
                    new { text = "📖 Bet kiritish", url = $"https://t.me/{BotUsername}?start=read" },
                    new { text = "🏆 Challenge reytingi", url = $"https://t.me/{BotUsername}?start=challenge" }
                }
            }
        };

        var delivered = false;
        if (target is not null)
        {
            var res = await SendHtmlAsync(
                target.Value.ChatId, reportHtml, markup, messageThreadId: target.Value.TopicId, ct: ct);
            delivered = res?["ok"]?.GetValue<bool>() == true;
        }

        if (!delivered)
        {
            var admins = await db.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.SuperAdmin && u.GoogleId.StartsWith("tg:"))
                .ToListAsync(ct);

            foreach (var admin in admins)
            {
                if (TryExtractTelegramId(admin.GoogleId, out var adminTgId))
                {
                    var res = await SendHtmlAsync(adminTgId, reportHtml, markup, ct: ct);
                    if (res?["ok"]?.GetValue<bool>() == true)
                    {
                        delivered = true;
                    }
                }
            }
        }

        return delivered;
    }

    public async Task<string> BuildDailyReportHtmlAsync(IAppDbContext db, DateOnly date, CancellationToken ct = default)
    {
        var entries = await db.ReadingProgress
            .AsNoTracking()
            .Where(p => p.Date == date && p.PagesReadToday > 0)
            .Include(p => p.ReadingGoal)
                .ThenInclude(g => g.User)
            .Include(p => p.ReadingGoal)
                .ThenInclude(g => g.Book)
            .ToListAsync(ct);

        var grouped = entries
            .GroupBy(p => p.ReadingGoal.UserId)
            .Select(g =>
            {
                var first = g.First().ReadingGoal.User;
                var totalPages = g.Sum(x => x.PagesReadToday);
                var books = g.Select(x => x.ReadingGoal.Book.Title).Distinct().ToList();
                return new
                {
                    UserId = g.Key,
                    FullName = first.FullName,
                    PlatformUsername = first.Username,
                    TotalPages = totalPages,
                    Books = books
                };
            })
            .OrderByDescending(x => x.TotalPages)
            .ThenBy(x => x.UserId)
            .ToList();

        var totalAllPages = grouped.Sum(x => x.TotalPages);
        var dateLabel = $"{date.Day}-{ChallengeCalendar.MonthName(date.Month)}, {date.Year}";

        var sb = new StringBuilder();
        sb.AppendLine("🌙 <b>«KITOBDAGIMEN» KUNLIK SARHISOBI (23:59)</b>");
        sb.AppendLine($"📅 <b>Sana:</b> {dateLabel}");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");

        if (grouped.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("<blockquote>📚 Bugun hali hech kim o'qilgan betlarni kiritmadi. Ertaga yangi kuch bilan mutolaani davom ettiramiz!</blockquote>");
            return sb.ToString();
        }

        var topReader = grouped[0];
        var topHandle = await ResolveDisplayHandleAsync(db, topReader.UserId, topReader.PlatformUsername, ct);

        sb.AppendLine($"<blockquote>👑 <b>BUGUNGI KUN QAHRAMONI:</b> {topHandle} — <b>{topReader.TotalPages} bet!</b>\n" +
                      $"👥 <b>Faol kitobxonlar:</b> {grouped.Count} nafar\n" +
                      $"📖 <b>Jami o'qilgan sahifalar:</b> {totalAllPages} bet</blockquote>");
        sb.AppendLine();
        sb.AppendLine("🏆 <b>Bugun kim qancha kitob o'qidi:</b>");

        var rank = 0;
        foreach (var item in grouped)
        {
            rank++;
            var medal = rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"<b>{rank}.</b>"
            };

            var handle = await ResolveDisplayHandleAsync(db, item.UserId, item.PlatformUsername, ct);
            var booksText = item.Books.Count > 0
                ? $" <i>(«{Escape(string.Join("», «", item.Books.Take(2)))}»)</i>"
                : "";

            sb.AppendLine($"{medal} {handle} ({Escape(item.FullName)}) — <b>{item.TotalPages} bet</b>{booksText}");
        }

        sb.AppendLine();
        sb.AppendLine("🔥 <i>Barakalla! Har kungi kichik mutolaa katta g'alabalarga olib boradi!</i>");
        return sb.ToString();
    }

    // ── Yordamchi funksiyalar ────────────────────────────────────────────────────

    public static async Task<string> ResolveDisplayHandleAsync(
        IAppDbContext db, int userId, string? platformUsername, CancellationToken ct = default)
    {
        var tgRow = await db.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == $"Telegram:Username:{userId}", ct);

        if (!string.IsNullOrWhiteSpace(tgRow?.Value))
        {
            return "@" + tgRow.Value.TrimStart('@');
        }

        if (!string.IsNullOrWhiteSpace(platformUsername))
        {
            return "@" + platformUsername.TrimStart('@');
        }

        return $"#kitobxon_{userId}";
    }

    public static bool TryExtractTelegramId(string? googleId, out long telegramId)
    {
        telegramId = 0;
        if (string.IsNullOrWhiteSpace(googleId) || !googleId.StartsWith("tg:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return long.TryParse(googleId.AsSpan(3), out telegramId);
    }

    public static string Escape(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return WebUtility.HtmlEncode(text);
    }

    public static string StripHtml(string? html)
        => RichTextSanitizer.StripHtml(html);

    public static string TruncateText(string text, int maxLength = 3500)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
            return text;

        var truncated = text[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > maxLength * 0.7)
        {
            truncated = truncated[..lastSpace];
        }
        return truncated.TrimEnd() + "…";
    }

    public static string BuildProgressBar(int current, int total, int segments = 10)
    {
        if (total <= 0) total = 1;
        var ratio = Math.Clamp((double)current / total, 0.0, 1.0);
        var filled = (int)Math.Round(ratio * segments);
        var empty = Math.Max(0, segments - filled);
        var pct = (int)Math.Round(ratio * 100);
        return $"{new string('▰', filled)}{new string('▱', empty)} <b>{pct}%</b>";
    }

    public static async Task SaveTelegramUsernameSettingAsync(
        IAppDbContext db, int userId, string? username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username)) return;
        var clean = username.Trim().TrimStart('@');
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
        }
        else if (!string.Equals(row.Value, clean, StringComparison.OrdinalIgnoreCase))
        {
            row.Value = clean;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task NotifyUserFollowedAsync(int followerUserId, int followingUserId, CancellationToken ct = default)
    {
        if (!IsConfigured) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var followingUser = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == followingUserId)
                .Select(u => new { u.Id, u.GoogleId, u.IsBotBlocked })
                .FirstOrDefaultAsync(ct);

            if (followingUser is null || !followingUser.GoogleId.StartsWith("tg:") || followingUser.IsBotBlocked)
            {
                return;
            }

            if (!long.TryParse(followingUser.GoogleId[3..], out var recipientTgId) || recipientTgId <= 0)
            {
                return;
            }

            var follower = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == followerUserId)
                .Select(u => new { u.FullName, u.Username, u.Id })
                .FirstOrDefaultAsync(ct);

            if (follower is null) return;

            var profileSlug = !string.IsNullOrWhiteSpace(follower.Username) ? follower.Username : follower.Id.ToString();
            var profileUrl = $"{WebAppUrl}/profile/{profileSlug}";

            var sb = new StringBuilder();
            sb.AppendLine($"👤 <b>{Escape(follower.FullName)}</b> sizni kuzata boshladi!");

            var markup = new
            {
                inline_keyboard = new object[][]
                {
                    new object[]
                    {
                        new { text = "👤 Profilni ko'rish", url = profileUrl }
                    }
                }
            };

            await SendHtmlAsync(recipientTgId, sb.ToString(), markup, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NotifyUserFollowedAsync da xatolik: follower={FollowerId}, following={FollowingId}", followerUserId, followingUserId);
        }
    }

    public async Task MarkUserBlockedAsync(long telegramId, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var tgKey = $"tg:{telegramId}";
            var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == tgKey, ct);
            if (user is not null && !user.IsBotBlocked)
            {
                user.IsBotBlocked = true;
                user.BotBlockedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Foydalanuvchi #{UserId} (tg:{TelegramId}) botni bloklagani belgilandi.", user.Id, telegramId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MarkUserBlockedAsync da xato: tg:{TelegramId}", telegramId);
        }
    }

    public async Task MarkUserUnblockedAsync(long telegramId, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var tgKey = $"tg:{telegramId}";
            var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == tgKey, ct);
            if (user is not null && user.IsBotBlocked)
            {
                user.IsBotBlocked = false;
                user.BotBlockedAt = null;
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Foydalanuvchi #{UserId} (tg:{TelegramId}) botni blokdan chiqargani belgilandi.", user.Id, telegramId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MarkUserUnblockedAsync da xato: tg:{TelegramId}", telegramId);
        }
    }

    public async Task<TelegramBotUserStatsDto> GetBotUserStatsAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var totalUsers = await db.Users.CountAsync(ct);
        var tgUsers = await db.Users.Where(u => u.GoogleId.StartsWith("tg:")).CountAsync(ct);
        var webUsers = Math.Max(0, totalUsers - tgUsers);

        var blockedUsers = await db.Users
            .Where(u => u.GoogleId.StartsWith("tg:") && u.IsBotBlocked)
            .CountAsync(ct);

        var activeBotUsers = Math.Max(0, tgUsers - blockedUsers);
        var blockedPercent = tgUsers > 0 ? Math.Round((double)blockedUsers / tgUsers * 100.0, 1) : 0;

        var today = UzTime.Today;
        var todayStartUtc = today.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
        var weekStartUtc = today.AddDays(-7).ToDateTime(TimeOnly.MinValue).ToUniversalTime();

        var todayNew = await db.Users.CountAsync(u => u.CreatedAt >= todayStartUtc, ct);
        var weekNew = await db.Users.CountAsync(u => u.CreatedAt >= weekStartUtc, ct);

        // Bugun faol foydalanuvchilar (saytga kirgan yoki kitob o'qigan)
        var todayActiveUsers = await db.Users
            .CountAsync(u => (u.LastSeenAt != null && u.LastSeenAt >= todayStartUtc) ||
                             db.ReadingProgress.Any(p => p.ReadingGoal.UserId == u.Id && p.Date == today), ct);

        var recentBlockedUsers = await db.Users
            .Where(u => u.GoogleId.StartsWith("tg:") && u.IsBotBlocked)
            .OrderByDescending(u => u.BotBlockedAt ?? u.CreatedAt)
            .Take(15)
            .Select(u => new
            {
                u.Id,
                u.GoogleId,
                u.FullName,
                u.Username,
                u.BotBlockedAt
            })
            .ToListAsync(ct);

        var blockedItems = recentBlockedUsers.Select(u =>
        {
            var rawId = u.GoogleId.StartsWith("tg:") ? u.GoogleId[3..] : "0";
            _ = long.TryParse(rawId, out var tid);
            return new BotBlockedUserItemDto(u.Id, tid, u.FullName, u.Username, u.BotBlockedAt);
        }).ToList();

        return new TelegramBotUserStatsDto(
            TotalUsers: totalUsers,
            TelegramUsers: tgUsers,
            WebsiteOnlyUsers: webUsers,
            ActiveBotUsers: activeBotUsers,
            BlockedBotUsers: blockedUsers,
            BlockedPercent: blockedPercent,
            TodayNewUsers: todayNew,
            WeekNewUsers: weekNew,
            TodayActiveUsers: todayActiveUsers,
            RecentlyBlockedUsers: blockedItems);
    }

    public async Task<BotSyncResultDto> SyncBotBlockedStatusAsync(CancellationToken ct = default)
    {
        if (!IsConfigured) return new BotSyncResultDto(0, 0, 0, 0);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var tgUsers = await db.Users
            .Where(u => u.GoogleId.StartsWith("tg:"))
            .Select(u => new { u.Id, u.GoogleId, u.IsBotBlocked })
            .ToListAsync(ct);

        var totalChecked = 0;
        var newlyBlocked = 0;
        var newlyUnblocked = 0;

        foreach (var u in tgUsers)
        {
            if (ct.IsCancellationRequested) break;

            var rawId = u.GoogleId[3..];
            if (!long.TryParse(rawId, out var tgId) || tgId <= 0) continue;

            totalChecked++;

            // sendChatAction orqali foydalanuvchiga bildirishnomasiz tekshiramiz
            var client = _httpClientFactory.CreateClient("TelegramBot");
            var url = $"{ApiBaseUrl}/sendChatAction";
            try
            {
                using var resp = await client.PostAsJsonAsync(url, new { chat_id = tgId, action = "typing" }, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);
                var json = JsonNode.Parse(body);
                var isOk = json?["ok"]?.GetValue<bool>() == true;
                var desc = json?["description"]?.GetValue<string>() ?? "";

                var isBlocked = !isOk && (
                    desc.Contains("bot was blocked by the user", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("user is deactivated", StringComparison.OrdinalIgnoreCase));

                if (isBlocked && !u.IsBotBlocked)
                {
                    await db.Users
                        .Where(x => x.Id == u.Id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.IsBotBlocked, true)
                            .SetProperty(x => x.BotBlockedAt, DateTime.UtcNow), ct);
                    newlyBlocked++;
                }
                else if (isOk && u.IsBotBlocked)
                {
                    await db.Users
                        .Where(x => x.Id == u.Id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.IsBotBlocked, false)
                            .SetProperty(x => x.BotBlockedAt, (DateTime?)null), ct);
                    newlyUnblocked++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BotSync: foydalanuvchini tekshirishda xato (tg:{TgId})", tgId);
            }

            // Telegram API rate-limit dan himoya (30ms kechikish)
            await Task.Delay(35, ct);
        }

        var currentlyBlocked = await db.Users
            .Where(u => u.GoogleId.StartsWith("tg:") && u.IsBotBlocked)
            .CountAsync(ct);

        return new BotSyncResultDto(totalChecked, newlyBlocked, newlyUnblocked, currentlyBlocked);
    }
}
