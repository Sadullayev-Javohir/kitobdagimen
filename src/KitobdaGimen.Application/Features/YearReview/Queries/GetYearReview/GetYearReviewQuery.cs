using KitobdaGimen.Application.Features.YearReview.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.YearReview.Queries.GetYearReview;

/// <summary>
/// Foydalanuvchining berilgan yil uchun "Yillik Kitob Yakuni" hisobotini yig'adi:
/// o'qilgan kitoblar soni, jami betlar, o'qilgan kitoblar ro'yxati, eng ko'p like
/// yig'gan post va iqtibos, hamda foydalanuvchiga xos noyob motivatsiya.
/// </summary>
public record GetYearReviewQuery(int UserId, int Year) : IRequest<YearReviewDto>;
