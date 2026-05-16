using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.Events;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public BanquetEvent BanquetEvent { get; set; } = default!;

    public decimal TotalCharges { get; set; }

    public decimal PackageEstimate { get; set; }

    public decimal TotalEstimatedCharges { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var banquetEvent = await _context.BanquetEvents
            .Include(banquetEvent => banquetEvent.FunctionRoom)
            .Include(banquetEvent => banquetEvent.BanquetPackage)
            .Include(banquetEvent => banquetEvent.SalesAccount)
            .Include(banquetEvent => banquetEvent.SalesLead)
            .Include(banquetEvent => banquetEvent.BanquetEventOrder)
            .Include(banquetEvent => banquetEvent.Charges)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(banquetEvent => banquetEvent.Id == id);

        if (banquetEvent is null)
        {
            return NotFound();
        }

        BanquetEvent = banquetEvent;
        TotalCharges = banquetEvent.Charges
            .Where(charge => !charge.IsVoided)
            .Sum(charge => charge.Amount);
        PackageEstimate = (banquetEvent.BanquetPackage?.PricePerPax ?? 0) * Math.Max(banquetEvent.GuaranteedPax, banquetEvent.BanquetPackage?.MinimumPax ?? 0);
        TotalEstimatedCharges = (banquetEvent.FunctionRoom?.Rate ?? 0) + PackageEstimate + TotalCharges;
        return Page();
    }
}
