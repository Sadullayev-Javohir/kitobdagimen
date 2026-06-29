using KitobdaGimen.Application.Features.Admin.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Admin.Queries.GetAdminUsers;

/// <summary>All users for the admin panel (Admin/SuperAdmin only), newest first.</summary>
public record GetAdminUsersQuery : IRequest<IReadOnlyList<AdminUserDto>>;
