using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.BEOs;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public BanquetEvent BanquetEvent { get; set; } = default!;

    [BindProperty]
    public BanquetEventOrder BanquetEventOrder { get; set; } = new();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? eventId)
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
        BanquetEventOrder = banquetEvent.BanquetEventOrder ?? new BanquetEventOrder
        {
            BanquetEventId = banquetEvent.Id,
            BEODate = DateTime.Today,
            PreparedBy = User.Identity?.Name ?? Environment.UserName,
            Status = BanquetEventOrderStatus.Draft,
            CreatedAt = DateTime.Now
        };
        LoadStatusOptions(BanquetEventOrder.Status);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? eventId)
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
        BanquetEventOrder.BanquetEventId = banquetEvent.Id;

        if (!ModelState.IsValid)
        {
            LoadStatusOptions(BanquetEventOrder.Status);
            return Page();
        }

        var existingOrder = await _context.BanquetEventOrders
            .FirstOrDefaultAsync(order => order.BanquetEventId == banquetEvent.Id);

        if (existingOrder is null)
        {
            BanquetEventOrder.CreatedAt = DateTime.Now;
            _context.BanquetEventOrders.Add(BanquetEventOrder);
        }
        else
        {
            existingOrder.BEODate = BanquetEventOrder.BEODate;
            existingOrder.MenuDetails = BanquetEventOrder.MenuDetails;
            existingOrder.SetupInstructions = BanquetEventOrder.SetupInstructions;
            existingOrder.EquipmentRequirements = BanquetEventOrder.EquipmentRequirements;
            existingOrder.ServiceInstructions = BanquetEventOrder.ServiceInstructions;
            existingOrder.KitchenInstructions = BanquetEventOrder.KitchenInstructions;
            existingOrder.BillingInstructions = BanquetEventOrder.BillingInstructions;
            existingOrder.SpecialInstructions = BanquetEventOrder.SpecialInstructions;
            existingOrder.PreparedBy = BanquetEventOrder.PreparedBy;
            existingOrder.ApprovedBy = BanquetEventOrder.ApprovedBy;
            existingOrder.Status = BanquetEventOrder.Status;
        }

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { eventId = banquetEvent.Id });
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int? eventId, BanquetEventOrderStatus status)
    {
        if (eventId is null)
        {
            return NotFound();
        }

        var banquetEvent = await LoadEventAsync(eventId.Value, asTracking: true);
        if (banquetEvent is null)
        {
            return NotFound();
        }

        var order = banquetEvent.BanquetEventOrder;
        if (order is null)
        {
            order = new BanquetEventOrder
            {
                BanquetEventId = banquetEvent.Id,
                BEODate = DateTime.Today,
                PreparedBy = User.Identity?.Name ?? Environment.UserName,
                CreatedAt = DateTime.Now
            };
            _context.BanquetEventOrders.Add(order);
        }

        order.Status = status;
        if (status == BanquetEventOrderStatus.Approved && string.IsNullOrWhiteSpace(order.ApprovedBy))
        {
            order.ApprovedBy = User.Identity?.Name ?? Environment.UserName;
        }

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { eventId = banquetEvent.Id });
    }

    private async Task<BanquetEvent?> LoadEventAsync(int eventId, bool asTracking)
    {
        var query = _context.BanquetEvents
            .Include(banquetEvent => banquetEvent.FunctionRoom)
            .Include(banquetEvent => banquetEvent.BanquetPackage)
            .Include(banquetEvent => banquetEvent.SalesAccount)
            .Include(banquetEvent => banquetEvent.SalesLead)
            .Include(banquetEvent => banquetEvent.Charges)
            .Include(banquetEvent => banquetEvent.BanquetEventOrder)
            .Where(banquetEvent => banquetEvent.Id == eventId);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.AsSplitQuery().FirstOrDefaultAsync();
    }

    private void LoadStatusOptions(BanquetEventOrderStatus selectedStatus)
    {
        StatusOptions = Enum.GetValues<BanquetEventOrderStatus>().Select(status => new SelectListItem
        {
            Value = status.ToString(),
            Text = status.ToString(),
            Selected = status == selectedStatus
        });
    }
}
