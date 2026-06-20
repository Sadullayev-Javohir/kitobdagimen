using KitobdaGimen.Application.Features.Profile.Dtos;
using MediatR;

namespace KitobdaGimen.Application.Features.Profile.Queries.GetUserProfile;

/// <summary>Returns the profile for the given user id, or throws if not found.</summary>
public record GetUserProfileQuery(int UserId) : IRequest<ProfileDto>;
