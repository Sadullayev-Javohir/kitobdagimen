using MediatR;

namespace KitobdaGimen.Application.Features.Admin.Commands.AdminDeleteUser;

/// <summary>SuperAdmin deletes a user entirely, with all their content.</summary>
public record AdminDeleteUserCommand(int TargetUserId) : IRequest<Unit>;
