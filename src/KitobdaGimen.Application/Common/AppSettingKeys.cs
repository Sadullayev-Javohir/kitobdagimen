namespace KitobdaGimen.Application.Common;

/// <summary>
/// <see cref="Domain.Entities.AppSetting"/> uchun kalit nomlari (yagona manba).
/// </summary>
public static class AppSettingKeys
{
    /// <summary>
    /// Yillik yakun hisoboti "e'lon qilingan" (foydalanuvchilarga yuborilgan) yil.
    /// Qiymat — yil raqami (masalan "2026"). Yozuv yo'q/bo'sh bo'lsa hisobot ko'rsatilmaydi.
    /// Faqat super admin o'rnatadi (/admin dagi tugma orqali).
    /// </summary>
    public const string YearReviewPublishedYear = "YearReview:PublishedYear";
}
