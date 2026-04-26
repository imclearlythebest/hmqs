using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hmqs.Api.Controllers;

[Authorize]
public abstract class AuthorizedApiController : ControllerBase
{
    protected Guid GetCurrentListenerId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var listenerId))
        {
            throw new InvalidOperationException("Authenticated user id is missing or invalid.");
        }

        return listenerId;
    }
}