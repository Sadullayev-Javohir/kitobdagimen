namespace KitobdaGimen.Web.Models;

/// <summary>
/// Options for the shared <c>_RichEditor</c> partial — kichik matn muharriri
/// (qalin/kursiv/tagchiziq/marker). Composer'da forma maydoni sifatida,
/// tahrirlash panellarida esa <c>data-edit-text</c> bilan ishlatiladi.
/// </summary>
public record RichEditorModel
{
    /// <summary>Oldindan to'ldirilgan, allaqachon sanitize qilingan HTML (b/i/u/mark).</summary>
    public string Html { get; init; } = string.Empty;

    /// <summary>Composer rejimida forma maydoni nomi (masalan, "ReviewText").</summary>
    public string? Name { get; init; }

    /// <summary>True bo'lsa tahrirlash rejimi: chiquvchi maydon <c>data-edit-text</c> oladi.</summary>
    public bool Edit { get; init; }

    public string Placeholder { get; init; } = "Fikringizni yozing...";

    /// <summary>Eng kam belgi soni (client tomon — submitni bloklaydi). 0 bo'lsa cheklov yo'q.</summary>
    public int MinLength { get; init; }

    /// <summary>Eng ko'p belgi soni (client tomon — kiritishni cheklaydi + sanagich ko'rsatadi). 0 bo'lsa cheklov yo'q.</summary>
    public int MaxLength { get; init; }
}
