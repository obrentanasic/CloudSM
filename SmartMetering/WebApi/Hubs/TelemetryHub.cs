using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Common;

namespace SmartMetering.WebApi.Hubs;

[Authorize(Roles = "Consumer")]
public sealed class TelemetryHub : Hub
{
    private readonly IPropertyRepository _properties;

    public TelemetryHub(IPropertyRepository properties) => _properties = properties;

    public async Task JoinPropertyGroup(string propertyId)
    {
        if (!Guid.TryParse(propertyId, out var parsedPropertyId))
        {
            throw new HubException("Објекат није пронађен.");
        }

        var rawUserId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");
        if (!Guid.TryParse(rawUserId, out var parsedUserId))
        {
            throw new HubException("Кориснички идентитет није валидан.");
        }

        var property = await _properties.GetByIdAsync(
            EntityId.From(parsedPropertyId),
            Context.ConnectionAborted);
        if (property is null || !property.IsOwnedBy(EntityId.From(parsedUserId)))
        {
            throw new HubException("Објекат није пронађен.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, parsedPropertyId.ToString());
    }

    public Task LeavePropertyGroup(string propertyId) =>
        Guid.TryParse(propertyId, out var parsedPropertyId)
            ? Groups.RemoveFromGroupAsync(Context.ConnectionId, parsedPropertyId.ToString())
            : Task.CompletedTask;
}
