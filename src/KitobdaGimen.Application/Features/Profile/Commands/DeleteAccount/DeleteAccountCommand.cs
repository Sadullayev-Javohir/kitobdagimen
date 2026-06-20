using MediatR;

namespace KitobdaGimen.Application.Features.Profile.Commands.DeleteAccount;

/// <summary>
/// Permanently deletes the current user's account and all of their data. The caller must
/// re-type their own account email as a confirmation; it must match the account on file.
/// </summary>
public record DeleteAccountCommand(string? Email) : IRequest<Unit>;
