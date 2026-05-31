using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;

namespace Vantage.PMS.Pages.System.RoleAccessReadiness;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IReadOnlyList<RoleAccessRow> Roles { get; private set; } = [];

    public IReadOnlyList<AccessSurface> Surfaces { get; private set; } = BuildSurfaces();

    public RoleAccessSummary Summary { get; private set; } = new(0, 0, 0, 0, 100);

    public async Task OnGetAsync()
    {
        var existingRoles = await context.Roles.AsNoTracking().Select(role => role.Name ?? string.Empty).ToListAsync();
        var userRoleCounts = await context.UserRoles.AsNoTracking()
            .GroupBy(userRole => userRole.RoleId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync();
        var roleIds = await context.Roles.AsNoTracking().Select(role => new { role.Id, role.Name }).ToListAsync();
        var roleCountByName = roleIds.ToDictionary(
            role => role.Name ?? string.Empty,
            role => userRoleCounts.FirstOrDefault(count => count.Key == role.Id)?.Count ?? 0);

        Roles = PmsRoles.All
            .Select(role => new RoleAccessRow(
                role,
                existingRoles.Contains(role),
                roleCountByName.TryGetValue(role, out var users) ? users : 0,
                Surfaces.Where(surface => surface.Roles.Contains(role)).Select(surface => surface.Name).ToList()))
            .OrderByDescending(row => row.RoleName == PmsRoles.SystemAdmin)
            .ThenBy(row => row.RoleName)
            .ToList();

        var missingRoles = Roles.Count(role => !role.Exists);
        var rolesWithoutUsers = Roles.Count(role => role.Exists && role.UserCount == 0);
        var totalAssignments = Roles.Sum(role => role.SurfaceNames.Count);
        var score = PmsRoles.All.Length == 0 ? 100 : Math.Max(0, 100 - (missingRoles * 10) - (rolesWithoutUsers * 2));
        Summary = new RoleAccessSummary(PmsRoles.All.Length, missingRoles, rolesWithoutUsers, totalAssignments, score);
    }

    private static IReadOnlyList<AccessSurface> BuildSurfaces() =>
    [
        Surface("Admin Setup", "/Admin", PmsRoles.AdminSetup),
        Surface("Front Office", "/FrontOffice", PmsRoles.FrontOffice),
        Surface("Housekeeping", "/Housekeeping", PmsRoles.Housekeeping),
        Surface("Finance", "/Finance", PmsRoles.Finance),
        Surface("Accounting Approvals", "/Accounting", PmsRoles.FinanceApprovals),
        Surface("Accounts Receivable", "/AccountsReceivable", PmsRoles.AccountsReceivable),
        Surface("F&B Service", "/FoodBeverage", PmsRoles.FoodBeverageService),
        Surface("Kitchen", "/FoodBeverageKitchen", PmsRoles.FoodBeverageKitchen),
        Surface("Sales", "/Sales", PmsRoles.Sales),
        Surface("Banquet", "/Banquet", PmsRoles.Banquet),
        Surface("Revenue", "/Revenue", PmsRoles.Revenue),
        Surface("Booking Management", "/BookingManagement", PmsRoles.BookingEngineManagement),
        Surface("Guest Portal Management", "/GuestPortalManagement", PmsRoles.GuestPortalManagement),
        Surface("Inventory & Purchasing", "/Inventory / /Purchasing", PmsRoles.InventoryPurchasing),
        Surface("Reports", "/Reports", PmsRoles.Reports),
        Surface("Report Administration", "/Reports/TemplateSettings", PmsRoles.ReportAdministration),
        Surface("Management AI", "/ManagementAI", PmsRoles.ManagementAI),
        Surface("AI Settings", "/ManagementAI/Settings", PmsRoles.AIIntegrationSettings),
        Surface("System Management", "/System", PmsRoles.SystemManagement),
        Surface("System Administration", "/Admin/UsersAndRoles", PmsRoles.SystemAdministration),
        Surface("Printable Documents", "/Documents", PmsRoles.PrintableDocuments),
        Surface("Client Demo", "/System/DemoPresentation", PmsRoles.ClientDemo),
        Surface("Labor Costing", "/Labor", PmsRoles.LaborCosting),
        Surface("Executive Reporting", "/Executive", PmsRoles.ExecutiveReporting),
        Surface("Executive Management", "/Executive management actions", PmsRoles.ExecutiveManagement),
        Surface("Group Management", "/Groups", PmsRoles.GroupManagement),
        Surface("Group Finance", "/Groups finance actions", PmsRoles.GroupFinance)
    ];

    private static AccessSurface Surface(string name, string routeScope, IReadOnlyCollection<string> roles)
        => new(name, routeScope, roles);

    public static string RoleStatusClass(RoleAccessRow role) => role.Exists switch
    {
        true when role.UserCount > 0 => "vpms-status-pill success",
        true => "vpms-status-pill warning",
        _ => "vpms-status-pill danger"
    };
}

public record AccessSurface(string Name, string RouteScope, IReadOnlyCollection<string> Roles);

public record RoleAccessRow(string RoleName, bool Exists, int UserCount, IReadOnlyList<string> SurfaceNames);

public record RoleAccessSummary(int TotalRoles, int MissingRoles, int RolesWithoutUsers, int TotalSurfaceAssignments, int Score);
