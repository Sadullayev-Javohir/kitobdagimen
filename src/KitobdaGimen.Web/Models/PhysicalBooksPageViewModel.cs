using KitobdaGimen.Application.Features.PhysicalBooks.Dtos;

namespace KitobdaGimen.Web.Models;

/// <summary>"Almashish" sahifasi uchun ma'lumot: kutubxona (boshqalarning mavjud kitoblari)
/// va joriy foydalanuvchining o'z kitoblari.</summary>
public record PhysicalBooksPageViewModel
{
    public IReadOnlyList<PhysicalBookDto> Library { get; init; } = Array.Empty<PhysicalBookDto>();
    public IReadOnlyList<PhysicalBookDto> Mine { get; init; } = Array.Empty<PhysicalBookDto>();
    public int? CurrentUserId { get; init; }
}
