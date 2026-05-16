using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.System.DemoPresentation;

public class IndexModel(DemoDataSeederService demoDataSeeder) : PageModel
{
    public IList<DemoReadinessItem> ReadinessItems { get; private set; } = new List<DemoReadinessItem>();
    public DemoDataStatus Status { get; private set; } = new();

    public bool HasDemoData =>
        Status.Hotels + Status.Rooms + Status.Guests + Status.Reservations + Status.PosOrders + Status.BanquetEvents + Status.InventoryItems + Status.ARInvoices > 0;

    public async Task OnGetAsync()
    {
        Status = await demoDataSeeder.GetStatusAsync();
        ReadinessItems = await demoDataSeeder.GetReadinessAsync();
    }

    public static string BadgeClass(string status) => status switch
    {
        "Ready" => "vpms-status-badge status-approved",
        "Needs Review" => "vpms-status-badge status-pending",
        _ => "vpms-status-badge status-draft"
    };
}
