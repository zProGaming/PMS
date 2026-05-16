using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.Charges;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public BanquetEvent? BanquetEvent { get; set; }

    public IList<BanquetCharge> AllCharges { get; set; } = new List<BanquetCharge>();

    [BindProperty]
    public BanquetCharge NewCharge { get; set; } = new() { Quantity = 1, ChargeDate = DateTime.Today };

    public decimal TotalCharges => (BanquetEvent?.Charges ?? AllCharges).Where(charge => !charge.IsVoided).Sum(charge => charge.Amount);

    public async Task<IActionResult> OnGetAsync(int? eventId)
    {
        if (eventId is null)
        {
            AllCharges = await _context.BanquetCharges
                .Include(charge => charge.BanquetEvent)
                    .ThenInclude(banquetEvent => banquetEvent!.FunctionRoom)
                .AsNoTracking()
                .OrderByDescending(charge => charge.ChargeDate)
                .ThenBy(charge => charge.BanquetEvent!.EventName)
                .ToListAsync();

            return Page();
        }

        var banquetEvent = await LoadEventAsync(eventId.Value, asTracking: false);
        if (banquetEvent is null)
        {
            return NotFound();
        }

        BanquetEvent = banquetEvent;
        NewCharge.BanquetEventId = banquetEvent.Id;
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(int? eventId)
    {
        if (eventId is null)
        {
            return NotFound();
        }

        var banquetEvent = await LoadEventAsync(eventId.Value, asTracking: false);
        if (banquetEvent is null)
        {
            return NotFound();
        }

        BanquetEvent = banquetEvent;
        ValidateCharge();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        NewCharge.BanquetEventId = banquetEvent.Id;
        NewCharge.Amount = NewCharge.Quantity * NewCharge.UnitPrice;
        NewCharge.ChargeDate = NewCharge.ChargeDate == default ? DateTime.Today : NewCharge.ChargeDate;
        NewCharge.IsVoided = false;

        _context.BanquetCharges.Add(NewCharge);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index", new { eventId = banquetEvent.Id });
    }

    public async Task<IActionResult> OnPostVoidAsync(int? eventId, int? chargeId)
    {
        if (eventId is null || chargeId is null)
        {
            return NotFound();
        }

        var charge = await _context.BanquetCharges.FirstOrDefaultAsync(charge =>
            charge.Id == chargeId &&
            charge.BanquetEventId == eventId);

        if (charge is not null)
        {
            charge.IsVoided = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index", new { eventId });
    }

    private void ValidateCharge()
    {
        if (string.IsNullOrWhiteSpace(NewCharge.Description))
        {
            ModelState.AddModelError("NewCharge.Description", "Description is required.");
        }

        if (NewCharge.Quantity <= 0)
        {
            ModelState.AddModelError("NewCharge.Quantity", "Quantity must be greater than zero.");
        }

        if (NewCharge.UnitPrice < 0)
        {
            ModelState.AddModelError("NewCharge.UnitPrice", "Unit price cannot be negative.");
        }
    }

    private async Task<BanquetEvent?> LoadEventAsync(int eventId, bool asTracking)
    {
        var query = _context.BanquetEvents
            .Include(banquetEvent => banquetEvent.FunctionRoom)
            .Include(banquetEvent => banquetEvent.Charges)
            .Where(banquetEvent => banquetEvent.Id == eventId);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.AsSplitQuery().FirstOrDefaultAsync();
    }
}
