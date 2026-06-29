using MediatR;

namespace KitobdaGimen.Application.Features.Admin.Commands.SetUserRole;

/// <summary>SuperAdmin promotes a user to Admin (<c>MakeAdmin=true</c>) or demotes back to User.</summary>
public record SetUserRoleCommand(int TargetUserId, bool MakeAdmin) : IRequest<Unit>;
