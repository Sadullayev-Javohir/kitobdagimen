using KitobdaGimen.Application.Common.Models;
using KitobdaGimen.Application.Features.Users.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Users.Queries.SearchUsers;

/// <summary>Searches users by full name or username for the /chat people search.</summary>
public record SearchUsersQuery : IRequest<PagedResult<UserSearchResultDto>>
{
    public string Q { get; init; } = "";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
