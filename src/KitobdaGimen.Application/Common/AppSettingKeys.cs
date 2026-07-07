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

    /// <summary>
    /// Landing sahifa statistikasi (foydalanuvchilar / o'qilgan kitoblar / o'qilgan betlar)
    /// JSON snapshot ko'rinishida. Kuniga bir marta (Toshkent kuni bo'yicha) avtomatik
    /// yangilanadi; super admin /admin dan majburan yangilashi ham mumkin.
    /// </summary>
    public const string LandingStats = "Landing:Stats";

    /// <summary>
    /// Barcha sahifalarda (shu jumladan landing) fon sifatida ko'rsatiladigan video URL manzili
    /// (masalan "/uploads/videos/xxxx.mp4"). Bo'sh/yo'q bo'lsa standart "/videos/book.mp4" ishlatiladi.
    /// Faqat super admin o'rnatadi (/admin dagi video yuklash orqali).
    /// </summary>
    public const string BackgroundVideoUrl = "Landing:BackgroundVideoUrl";
}
