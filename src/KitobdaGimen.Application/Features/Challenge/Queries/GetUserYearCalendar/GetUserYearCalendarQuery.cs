using KitobdaGimen.Application.Features.Challenge.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Challenge.Queries.GetUserYearCalendar;

/// <summary>
/// Foydalanuvchining berilgan yil uchun to'liq o'qish kalendari (GitHub uslubidagi heatmap).
/// Avvalgi yillarni ko'rish uchun mijozdan AJAX bilan chaqiriladi.
/// </summary>
public record GetUserYearCalendarQuery(int UserId, int Year) : IRequest<YearCalendarDto>;
