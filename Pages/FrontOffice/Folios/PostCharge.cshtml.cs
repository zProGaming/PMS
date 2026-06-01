using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.FrontOffice.Folios;

public class PostChargeModel(ApplicationDbContext context, GroupManagementService groupManagementService, AuditLogService auditLogService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly GroupManagementService _groupManagementService = groupManagementService;
    private readonly AuditLogService _auditLogService = auditLogService;

    [BindProperty]
    public FolioItem Charge { get; set; } = new();

    public int FolioId { get; set; }

    public string FolioNumber { get; set; } = string.Empty;

    public decimal FolioBalance { get; set; }

    public string GuestName { get; set; } = string.Empty;

    public SelectList ChargeCodeOptions { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int? folioId)
    {
        if (folioId is null)
        {
            return NotFound();
        }

        var folio = await _context.Folios
            .AsNoTracking()
            .Include(folio => folio.Guest)
            .Include(folio => folio.Items)
            .Include(folio => folio.Payments)
            .FirstOrDefaultAsync(folio => folio.Id == folioId);

        if (folio is null)
        {
            return NotFound();
        }

        FolioId = folio.Id;
        FolioNumber = folio.FolioNumber;
        FolioBalance = folio.Balance;
        GuestName = $"{folio.Guest?.FirstName} {folio.Guest?.LastName}".Trim();
        var businessDate = await GetBusinessDateAsync();
        Charge = new FolioItem
        {
            FolioId = folio.Id,
            Quantity = 1,
            PostingDate = businessDate.Date.Add(DateTime.Now.TimeOfDay),
            PostedBy = User.Identity?.Name ?? "Front Desk"
        };

        await LoadChargeCodesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? folioId)
    {
        if (folioId is null)
        {
            return NotFound();
        }

        var folio = await _context.Folios
            .AsNoTracking()
            .Include(item => item.Guest)
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .FirstOrDefaultAsync(item => item.Id == folioId);
        if (folio is null)
        {
            return NotFound();
        }

        FolioId = folio.Id;
        FolioNumber = folio.FolioNumber;
        FolioBalance = folio.Balance;
        GuestName = $"{folio.Guest?.FirstName} {folio.Guest?.LastName}".Trim();
        var businessDate = await GetBusinessDateAsync();
        ValidateCharge();
        ValidatePostingDate(businessDate);

        if (!ModelState.IsValid)
        {
            await LoadChargeCodesAsync();
            return Page();
        }

        Charge.FolioId = folio.Id;
        if (Charge.ChargeCodeId is not null)
        {
            var chargeCode = await _context.ChargeCodes.AsNoTracking().FirstOrDefaultAsync(code => code.Id == Charge.ChargeCodeId);
            if (chargeCode is not null)
            {
                Charge.ChargeCode = chargeCode.Code;
                if (string.IsNullOrWhiteSpace(Charge.Description))
                {
                    Charge.Description = chargeCode.Name;
                }

                if (Charge.UnitPrice == 0 && chargeCode.DefaultAmount is not null)
                {
                    Charge.UnitPrice = chargeCode.DefaultAmount.Value;
                }
            }
        }

        Charge.Amount = Charge.Quantity * Charge.UnitPrice;
        Charge.IsVoided = false;

        var routing = await _groupManagementService.ResolveChargeRoutingAsync(folio, Charge);
        if (routing.IsRouted && routing.TargetFolioId is not null)
        {
            Charge.FolioId = routing.TargetFolioId.Value;
            Charge.Description = $"{Charge.Description} (Routed from folio {folio.FolioNumber} by routing rule {routing.RuleId})";
        }

        _context.FolioItems.Add(Charge);
        await _context.SaveChangesAsync();
        if (routing.IsRouted)
        {
            await _auditLogService.LogAsync(AuditActionType.Update, "Group Management", nameof(FolioItem), Charge.Id.ToString(), null, new { SourceFolioId = folio.Id, Charge.FolioId, routing.RuleId, routing.TargetFolioNumber });
        }

        return RedirectToPage("./Details", new { id = folio.Id });
    }

    private void ValidateCharge()
    {
        if (string.IsNullOrWhiteSpace(Charge.Description) && Charge.ChargeCodeId is null)
        {
            ModelState.AddModelError("Charge.Description", "Description is required.");
        }

        if (string.IsNullOrWhiteSpace(Charge.ChargeCode) && Charge.ChargeCodeId is null)
        {
            ModelState.AddModelError("Charge.ChargeCode", "Charge code is required.");
        }

        if (Charge.Quantity <= 0)
        {
            ModelState.AddModelError("Charge.Quantity", "Quantity must be greater than zero.");
        }

        if (Charge.UnitPrice < 0)
        {
            ModelState.AddModelError("Charge.UnitPrice", "Unit price cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(Charge.PostedBy))
        {
            ModelState.AddModelError("Charge.PostedBy", "Posted by is required.");
        }
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }

    private void ValidatePostingDate(DateTime businessDate)
    {
        if (Charge.PostingDate.Date < businessDate.Date)
        {
            ModelState.AddModelError("Charge.PostingDate", "Transactions for this business date are locked.");
        }
    }

    private async Task LoadChargeCodesAsync()
    {
        var chargeCodes = await _context.ChargeCodes
            .AsNoTracking()
            .Where(code => code.IsActive)
            .OrderBy(code => code.Code)
            .Select(code => new { code.Id, Name = code.Code + " - " + code.Name })
            .ToListAsync();

        ChargeCodeOptions = new SelectList(chargeCodes, "Id", "Name", Charge.ChargeCodeId);
    }
}
