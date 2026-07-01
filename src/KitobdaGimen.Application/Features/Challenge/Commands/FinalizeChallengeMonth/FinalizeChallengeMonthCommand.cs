using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Commands.FinalizeChallengeMonth;

/// <summary>
/// Berilgan oy uchun challenge g'oliblarini hisoblab, e'lon qiladi (top 3 saqlaydi).
/// Idempotent: o'sha oy allaqachon e'lon qilingan bo'lsa, hech narsa o'zgartirmaydi.
/// Odatda admin qo'lda yoki Hangfire jobi (oy boshida) chaqiradi.
/// </summary>
public record FinalizeChallengeMonthCommand(int Year, int Month) : IRequest<int>
{
    /// <summary>
    /// true bo'lsa admin tekshiruvi o'tkazib yuboriladi — faqat server ichidan (Hangfire jobi)
    /// ishlatiladi. Controller hech qachon buni foydalanuvchi kiritmasidan olmaydi.
    /// </summary>
    public bool BypassAdminCheck { get; init; }
}
