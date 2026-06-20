using KitobdaGimen.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KitobdaGimen.Web.Controllers;

/// <summary>Base MVC controller exposing the mediator and the current user id.</summary>
public abstract class AppController : Controller
{
    private ISender? _mediator;

    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected int? CurrentUserId =>
        HttpContext.RequestServices.GetRequiredService<ICurrentUserService>().UserId;

    protected bool IsAuthenticatedUser => CurrentUserId is not null;
}
