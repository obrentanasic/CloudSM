using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SmartMetering.Domain.Common;

namespace SmartMetering.WebApi.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The authenticated user's id, taken from the JWT 'sub' / NameIdentifier claim.</summary>
    protected EntityId CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new InvalidOperationException("User id claim is missing.");
            return EntityId.Parse(value);
        }
    }
}
