using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Commands.SetWinnerGiftBook;

/// <summary>
/// Super admin 1-o'rin g'olibiga kitob sovg'a qiladi. Kitob nomi, muallifi va muqova
/// rasmini kiritadi — g'olibga o'sha kitob rasmi ko'rsatiladi. Bo'sh qiymatlar sovg'ani
/// olib tashlaydi.
/// </summary>
public record SetWinnerGiftBookCommand : IRequest<Unit>
{
    public int Year { get; init; }
    public int Month { get; init; }

    /// <summary>Sovg'a beriladigan o'rin (default 1 — birinchi o'rin).</summary>
    public int Rank { get; init; } = 1;

    public string? GiftBookTitle { get; init; }
    public string? GiftBookAuthor { get; init; }
    public string? GiftBookCoverUrl { get; init; }
}
