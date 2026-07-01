using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Umumiy kalit-qiymat runtime sozlamasi (DB'da saqlanadi). Masalan, yillik yakun
/// hisoboti qaysi yil uchun "e'lon qilingan" (super admin tomonidan yuborilgan)ligini
/// saqlaydi. Kod tarqatmasdan super admin boshqaradigan bayroqlar uchun.
/// </summary>
public class AppSetting : BaseEntity
{
    /// <summary>Sozlama kaliti (noyob). Masalan "YearReview:PublishedYear".</summary>
    public string Key { get; set; } = null!;

    /// <summary>Sozlama qiymati (matn ko'rinishida).</summary>
    public string? Value { get; set; }

    /// <summary>Oxirgi o'zgartirilgan vaqt (UTC).</summary>
    public DateTime UpdatedAt { get; set; }
}
