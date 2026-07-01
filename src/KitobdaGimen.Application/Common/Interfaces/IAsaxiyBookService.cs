namespace KitobdaGimen.Application.Common.Interfaces;

/// <summary>
/// asaxiy.uz kitoblar katalogidan ma'lumot oluvchi servis. asaxiy ochiq API bermaydi,
/// shuning uchun servis ularning common (JSON-LD) sahifalarini o'qib, strukturali
/// natijaga aylantiradi. Faqat o'qish — hech narsa yozilmaydi.
/// </summary>
public interface IAsaxiyBookService
{
    /// <summary>Kitoblar bo'limidan matn bo'yicha qidiradi (live).</summary>
    Task<IReadOnlyList<AsaxiyBookResult>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>Bitta kitob sahifasidan to'liq ma'lumot (nom, muallif, betlar soni, muqova) oladi.</summary>
    Task<AsaxiyBookDetails?> GetDetailsAsync(string productUrl, CancellationToken ct = default);

    /// <summary>Muqova rasmini asaxiy CDN'dan yuklab, baytlarini qaytaradi (null = topilmadi).</summary>
    Task<byte[]?> DownloadCoverAsync(string coverUrl, CancellationToken ct = default);

    /// <summary>
    /// asaxiy qidiruvini "qayta ishga tushiradi": transport tanlash holatini tiklaydi va
    /// jonli sinov qidiruvi bajarib, qaysi yo'l orqali ishlayotganini qaytaradi. "Kitoblarni
    /// yangilash" tugmasi shu metodni chaqiradi — qidiruv o'chib qolgan bo'lsa qayta tiklaydi.
    /// </summary>
    Task<AsaxiyHealthResult> RefreshAsync(CancellationToken ct = default);
}

/// <summary>"Kitoblarni yangilash" natijasi — qidiruv holati va qaysi transport ishlagani.</summary>
public record AsaxiyHealthResult
{
    /// <summary>Qidiruv hozir ishlayaptimi.</summary>
    public bool Healthy { get; init; }

    /// <summary>Ishlagan transport nomi (worker / proxy / direct / jina) yoki bo'sh.</summary>
    public string Transport { get; init; } = string.Empty;

    /// <summary>Sinov qidiruvida topilgan kitoblar soni.</summary>
    public int Count { get; init; }

    /// <summary>Foydalanuvchiga ko'rsatiladigan xabar (o'zbekcha).</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>Qidiruv natijasidagi bitta kitob (ro'yxat uchun yengil model).</summary>
public record AsaxiyBookResult
{
    public string Title { get; init; } = null!;
    public string Author { get; init; } = null!;
    public string? CoverUrl { get; init; }
    /// <summary>asaxiy.uz dagi kitob sahifasi manzili — import uchun ishlatiladi.</summary>
    public string Url { get; init; } = null!;
}

/// <summary>Bitta kitobning to'liq import qilinadigan ma'lumoti.</summary>
public record AsaxiyBookDetails
{
    public string Title { get; init; } = null!;
    public string Author { get; init; } = null!;
    public int TotalPages { get; init; }
    public string? CoverUrl { get; init; }
}
