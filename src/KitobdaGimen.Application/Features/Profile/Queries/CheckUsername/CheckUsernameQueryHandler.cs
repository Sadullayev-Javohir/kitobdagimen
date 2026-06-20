using System.Text.RegularExpressions;
using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KitobdaGimen.Application.Features.Profile.Queries.CheckUsername;

public partial class CheckUsernameQueryHandler : IRequestHandler<CheckUsernameQuery, UsernameCheckDto>
{
    // Same rule as UpdateProfileCommandValidator: 3-32 chars, letters/digits/underscore.
    [GeneratedRegex("^[a-zA-Z0-9_]{3,32}$")]
    private static partial Regex UsernameRegex();

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CheckUsernameQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<UsernameCheckDto> Handle(CheckUsernameQuery request, CancellationToken cancellationToken)
    {
        var raw = (request.Username ?? string.Empty).Trim();

        if (!UsernameRegex().IsMatch(raw))
        {
            return new UsernameCheckDto
            {
                IsValid = false,
                IsAvailable = false,
                Message = "Username 3-32 belgi: harf, raqam va pastki chiziq (_)."
            };
        }

        var username = raw.ToLowerInvariant();
        var currentUserId = _currentUser.UserId;

        var isTaken = await _db.Users.AnyAsync(
            u => u.Id != currentUserId && u.Username == username, cancellationToken);

        return new UsernameCheckDto
        {
            IsValid = true,
            IsAvailable = !isTaken,
            Message = isTaken ? "Bu username band — boshqasini tanlang." : "Username bo'sh ✓"
        };
    }
}
