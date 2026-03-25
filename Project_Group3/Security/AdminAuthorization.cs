using Microsoft.AspNetCore.Http;

namespace Project_Group3.Security;

public static class AdminRoles
{
    public const string SuperAdmin = "superadmin";
    public const string Monitor = "monitor";
    public const string Support = "support";

    private static readonly HashSet<string> AdminRoleSet = new(StringComparer.OrdinalIgnoreCase)
    {
        SuperAdmin,
        Monitor,
        Support
    };

    public static bool IsAdminRole(string? role)
        => !string.IsNullOrWhiteSpace(role) && AdminRoleSet.Contains(role);
}

public static class AdminPermissions
{
    private static readonly string[] SuperAdminEmailTargets =
    [
        "All", "Buyer", "Seller", "SuperAdmin", "Monitor", "Support", "Ops"
    ];

    private static readonly string[] SupportEmailTargets =
    [
        "Buyer", "Seller"
    ];

    public static bool IsAdminRole(string? role)
        => AdminRoles.IsAdminRole(role);

    public static bool CanAccessDashboard(string? role)
        => AdminRoles.IsAdminRole(role);

    public static bool CanAccessUserManagement(string? role)
        => AdminRoles.IsAdminRole(role);

    public static bool CanEditUsers(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool CanAccessProductModeration(string? role)
        => AdminRoles.IsAdminRole(role);

    public static bool CanProcessProductModeration(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AdminRoles.Support, StringComparison.OrdinalIgnoreCase);

    public static bool CanPerformFullProductModeration(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool CanAccessOrders(string? role)
        => AdminRoles.IsAdminRole(role);

    public static bool CanProcessOrders(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AdminRoles.Support, StringComparison.OrdinalIgnoreCase);

    public static bool CanAccessAnalytics(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AdminRoles.Monitor, StringComparison.OrdinalIgnoreCase);

    public static bool CanExportAnalytics(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool CanAccessReviewsFeedback(string? role)
        => AdminRoles.IsAdminRole(role);

    public static bool CanProcessReviewsFeedback(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AdminRoles.Support, StringComparison.OrdinalIgnoreCase);

    public static bool CanDeleteReviewsFeedback(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool CanAccessDisputes(string? role)
        => AdminRoles.IsAdminRole(role);

    public static bool CanProcessDisputes(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AdminRoles.Support, StringComparison.OrdinalIgnoreCase);

    public static bool CanFinalizeDisputes(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool CanAccessRefunds(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AdminRoles.Support, StringComparison.OrdinalIgnoreCase);

    public static bool CanProcessRefunds(string? role)
        => CanAccessRefunds(role);

    public static bool CanAccessEmailSystem(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AdminRoles.Support, StringComparison.OrdinalIgnoreCase);

    public static bool CanBroadcastEmailSystem(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool CanUseSystemSettings(string? role)
        => AdminRoles.IsAdminRole(role);

    public static IReadOnlyList<string> GetAllowedEmailTargets(string? role)
        => string.Equals(role, AdminRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            ? SuperAdminEmailTargets
            : string.Equals(role, AdminRoles.Support, StringComparison.OrdinalIgnoreCase)
                ? SupportEmailTargets
                : [];
}

public static class AdminAuthorizationExtensions
{
    public static string GetCurrentRole(this HttpContext httpContext)
        => httpContext.Session.GetString("Role") ?? string.Empty;

    public static bool IsAdminAuthenticated(this HttpContext httpContext)
    {
        var userId = httpContext.Session.GetInt32("UserId");
        var isAdminTwoFactorVerified = httpContext.Session.GetString("IsAdmin2FAVerified");
        var role = httpContext.GetCurrentRole();

        return userId is not null
            && AdminPermissions.IsAdminRole(role)
            && string.Equals(isAdminTwoFactorVerified, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasAdminPermission(this HttpContext httpContext, Func<string?, bool> permissionCheck)
        => httpContext.IsAdminAuthenticated() && permissionCheck(httpContext.GetCurrentRole());
}
