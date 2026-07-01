using KitobdaGimen.Application.Features.YearReview.Dtos;

namespace KitobdaGimen.Web.Models;

/// <summary>
/// "Yillik Kitob Yakuni" to'liq sahifasi ( common ulashish sahifasi va admin oldindan
/// ko'rish) uchun ko'rinish modeli. Modal ichidagi kartochka esa alohida partial
/// (<c>_YearReviewCard</c>) orqali AJAX bilan yuklanadi.
/// </summary>
public class YearReviewPageViewModel
{
    /// <summary>Ko'rsatiladigan yillik hisobot.</summary>
    public YearReviewDto Review { get; init; } = new();

    /// <summary>Ommaviy ulashish sahifasimi (anonim ko'ruvchi).</summary>
    public bool IsShareView { get; init; }

    /// <summary>Admin oldindan ko'rish (sinov) sahifasimi.</summary>
    public bool IsPreview { get; init; }

    /// <summary>Ulashish uchun to'liq (absolyut) URL — mavjud bo'lsa.</summary>
    public string? ShareUrl { get; init; }
}
