using KitobdaGimen.Application.Features.Admin.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Admin.Queries.GetSystemStatus;

public record GetSystemStatusQuery : IRequest<SystemStatusDto>;
