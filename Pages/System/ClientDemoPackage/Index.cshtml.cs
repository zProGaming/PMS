using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;
using ClientDemoPackageModel = Vantage.PMS.Models.SystemAdministration.ClientDemoPackage;

namespace Vantage.PMS.Pages.System.ClientDemoPackage;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<ClientDemoPackageModel> Packages { get; private set; } = new List<ClientDemoPackageModel>();
    public ClientDemoPackageModel? ActivePackage { get; private set; }

    [BindProperty]
    public ClientDemoPackageInput Input { get; set; } = new();

    public async Task OnGetAsync(int? id)
    {
        Packages = await context.ClientDemoPackages
            .Include(item => item.Items)
            .AsNoTracking()
            .OrderByDescending(item => item.PreparedDate)
            .ToListAsync();

        ActivePackage = id.HasValue
            ? Packages.FirstOrDefault(item => item.Id == id.Value)
            : Packages.FirstOrDefault();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync(null);
            return Page();
        }

        var package = new ClientDemoPackageModel
        {
            PackageName = Input.PackageName,
            ClientName = Input.ClientName,
            HotelName = Input.HotelName,
            PreparedBy = Input.PreparedBy,
            PreparedDate = Input.PreparedDate,
            Status = DemoPackageStatus.Draft,
            Notes = Input.Notes
        };

        var sort = 10;
        foreach (var item in DefaultItems)
        {
            package.Items.Add(new ClientDemoPackageItem
            {
                ItemTitle = item.Title,
                ModuleName = item.Module,
                Description = item.Description,
                SortOrder = sort,
                IsIncluded = true
            });
            sort += 10;
        }

        context.ClientDemoPackages.Add(package);
        await context.SaveChangesAsync();
        return RedirectToPage(new { id = package.Id });
    }

    private static readonly (string Title, string Module, string Description)[] DefaultItems =
    [
        ("Reservation to check-in", "Front Office", "Show reservation creation, guest lookup, check-in, and room assignment."),
        ("Check-in to folio", "Finance", "Demonstrate folio charges, payments, balances, and payment receipt printout."),
        ("F&B charge to room", "F&B Service POS", "Create a restaurant or room service order and post it to a guest folio."),
        ("Kitchen order update", "F&B Kitchen", "Update food item status through station-based kitchen display."),
        ("Housekeeping room readiness", "Housekeeping", "Move a room through dirty, clean, inspected, and ready states."),
        ("Banquet event to BEO", "Banquet", "Create an event, review charges, and print the Banquet Event Order."),
        ("Rate plan to booking engine", "Revenue", "Show rate plans, restrictions, room content, and public room selection."),
        ("Booking request to reservation", "Booking Engine", "Convert a direct booking request into a PMS reservation."),
        ("Inventory receiving to stock movement", "Inventory and Purchasing", "Show PO receiving, stock movement, and low-stock controls."),
        ("Finance document to AR invoice", "Accounts Receivable", "Review finance document, AR invoice, and aging visibility."),
        ("AI daily summary and insights", "Management AI", "Generate rule-based daily summary and management recommendations.")
    ];
}

public class ClientDemoPackageInput
{
    public string PackageName { get; set; } = "Vantage PMS Client Demo Package";

    public string ClientName { get; set; } = "Sample Hotel Client";

    public string HotelName { get; set; } = "Vantage Grand Hotel";

    public string PreparedBy { get; set; } = "Vantage Innovations PH";

    public DateTime PreparedDate { get; set; } = DateTime.Today;

    public string? Notes { get; set; }
}
