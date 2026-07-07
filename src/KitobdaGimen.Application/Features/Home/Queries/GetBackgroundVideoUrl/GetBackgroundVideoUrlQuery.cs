using MediatR;

namespace KitobdaGimen.Application.Features.Home.Queries.GetBackgroundVideoUrl;

/// <summary>Joriy fon video URL manzilini qaytaradi (super admin tomonidan yuklangan bo'lsa); aks holda null.</summary>
public record GetBackgroundVideoUrlQuery : IRequest<string?>;
