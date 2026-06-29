using MediatR;

namespace KitobdaGimen.Application.Features.Admin.Commands.AdminDeletePost;

/// <summary>Admin/SuperAdmin deletes ANY post (moderation), regardless of author.</summary>
public record AdminDeletePostCommand(int PostId) : IRequest<Unit>;
