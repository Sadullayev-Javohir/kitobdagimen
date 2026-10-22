namespace KitobdaGimen.Web.Telegram;

public class TelegramBotSettings
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;
    public string BotUsername { get; set; } = "KitobdagiMenBot";
    public string WebAppUrl { get; set; } = "https://kitobdagimen.uz";
    public string BookClubGroupId { get; set; } = string.Empty;
    public string BookClubInviteLink { get; set; } = "https://t.me/+M2zWs1tYAKoxMjli";
    public int? BookClubTopicId { get; set; }
    public List<string> SuperAdminUsernames { get; set; } = new();
    public List<long> SuperAdminTelegramIds { get; set; } = new();
}
