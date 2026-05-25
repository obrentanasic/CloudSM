using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartMetering.WebApi.Hubs;

[Authorize]
public sealed class TelemetryHub : Hub
{
    /// <summary>Subscribe this connection to live updates for one property (call on tab open).</summary>
    public Task JoinPropertyGroup(string propertyId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, propertyId);

    /// <summary>Unsubscribe when leaving a tab, so the client only receives the active property's updates.</summary>
    public Task LeavePropertyGroup(string propertyId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, propertyId);
}
