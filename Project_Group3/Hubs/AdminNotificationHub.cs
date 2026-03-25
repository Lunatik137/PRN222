using Microsoft.AspNetCore.SignalR;

namespace Project_Group3.Hubs;

public class AdminNotificationHub : Hub
{
    public const string AdminGroupName = "admin-users";

    private static readonly HashSet<string> AdminRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin",
        "monitor",
        "support"
    };

    public override async Task OnConnectedAsync()
    {
        var role = Context.GetHttpContext()?.Session.GetString("Role");
        if (role is not null && AdminRoles.Contains(role))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroupName);
        }

        await base.OnConnectedAsync();
    }
}
