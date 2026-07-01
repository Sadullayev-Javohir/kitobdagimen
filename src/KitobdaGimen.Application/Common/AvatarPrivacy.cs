namespace KitobdaGimen.Application.Common;

/// <summary>
/// Bitta foydalanuvchi (<see cref="RestrictedEmail"/>) profil rasmini boshqalardan yashirish
/// qoidasi. Uning avatari faqat quyidagilarga ko'rinadi:
///   • ruxsat etilgan super admin (<see cref="AllowedViewerEmail"/>);
///   • foydalanuvchining o'ziga (o'z avatarini ko'radi).
/// Qolgan barcha foydalanuvchilarga avatar <c>null</c> qilib beriladi (UI bosh harfga tushadi).
/// </summary>
public static class AvatarPrivacy
{
    /// <summary>Avatari yashiriladigan foydalanuvchi email'i (kichik harflarda).</summary>
    public const string RestrictedEmail = "kushakovadilbarxon@gmail.com";

    /// <summary>Yashirilgan avatarni ko'ra oladigan yagona (super admin) email — kichik harflarda.</summary>
    public const string AllowedViewerEmail = "javohirsadullayev836@gmail.com";

    /// <summary>
    /// Berilgan (maqsad) email egasining avatari joriy ko'ruvchiga (viewer) ko'rinadimi.
    /// EF so'rovlari ichida translatsiya qilinmaydigan joylarda (in-memory) ishlatiladi.
    /// </summary>
    public static bool CanView(string? targetEmail, string? viewerEmail)
    {
        if (!string.Equals(targetEmail, RestrictedEmail, System.StringComparison.OrdinalIgnoreCase))
        {
            return true; // Bu foydalanuvchi cheklanmagan — avatari hammaga ko'rinadi.
        }

        return string.Equals(viewerEmail, AllowedViewerEmail, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(viewerEmail, RestrictedEmail, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Qoidani qo'llagan holda ko'rinadigan avatar URL'ini qaytaradi (in-memory).</summary>
    public static string? Resolve(string? targetEmail, string? avatarUrl, string? viewerEmail)
        => CanView(targetEmail, viewerEmail) ? avatarUrl : null;

    /// <summary>
    /// Bildirishnoma / real-time "actor" (amalni bajaruvchi) avatari uchun. Actor cheklangan
    /// foydalanuvchi bo'lsa, avatar hech bir qabul qiluvchiga ko'rsatilmaydi (null) — chunki
    /// bildirishnoma boshqa foydalanuvchilarga boradi.
    /// </summary>
    public static string? ForActor(string? actorEmail, string? avatarUrl)
        => string.Equals(actorEmail, RestrictedEmail, System.StringComparison.OrdinalIgnoreCase)
            ? null
            : avatarUrl;
}
