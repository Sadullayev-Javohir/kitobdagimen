using KitobdaGimen.Domain.Common;

namespace KitobdaGimen.Domain.Entities;

/// <summary>
/// Har kuni tong soat 05:30 da kitobxonlarni erta uyg'onishga va kitob o'qishga
/// ilhomlantiruvchi iqtiboslar navbati.
/// Har tong navbatdagi iqtibos e'lon qilinadi va avtomatik ravishda bazadan olib tashlanadi (FIFO).
/// </summary>
public class MorningQuote : BaseEntity
{
    public string Text { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string? Source { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

