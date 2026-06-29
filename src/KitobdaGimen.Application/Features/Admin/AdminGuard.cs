using KitobdaGimen.Application.Common.Exceptions;
using KitobdaGimen.Application.Common.Interfaces;
using KitobdaGimen.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Admin;

/// <summary>Shared role-gating for admin operations (role is read from the DB, not the JWT,
/// so newly granted roles take effect immediately without re-login).</summary>
internal static class AdminGuard
{
    /// <summary>Ensures the current user has at least <paramref name="min"/> role; returns the
    /// caller's id and role. Throws if anonymous or under-privileged.</summary>
    public static async Task<(int UserId, UserRole Role)> RequireAsync(
        IAppDbContext db, ICurrentUserService currentUser, UserRole min, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Avval tizimga kiring.");

        var role = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Role)
            .FirstOrDefaultAsync(ct);

        if (role < min)
        {
            throw new ForbiddenAccessException("Bu amalni bajarishga ruxsatingiz yo'q.");
        }

        return (userId, role);
    }
}
