using MediatR;

namespace KitobdaGimen.Application.Features.Admin.Commands.BroadcastNotification;

/// <summary>
/// SuperAdmin: barcha foydalanuvchilarga bir martalik e'lon (sarlavha + matn, ixtiyoriy havola) yuboradi.
/// Bildirishnoma real-time yetkaziladi (onlayn foydalanuvchilar darhol ko'radi) va bazaga saqlanadi
/// (offlayn foydalanuvchilar keyingi kirishida ko'radi). Yuborilgan qabul qiluvchilar soni qaytadi.
/// </summary>
public record BroadcastNotificationCommand(string Title, string Message, string? Url) : IRequest<int>;
