using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

    public FolioStatus FolioStatus { get; set; }

    public bool CanPostCharge => FolioStatus == FolioStatus.Open;

    public string FolioControlMessage => CanPostCharge
        ? "Charges will be posted to the active folio ledger."
        : $"This folio is {FormatFolioStatus(FolioStatus)} and is read-only for charge posting.";

    public async Task<IActionResult> OnGetAsync(int? folioId)
    {
        var loadResult = await LoadChargeFormAsync(folioId);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return Page();
    }

    public async Task<IActionResult> OnGetNativeAsync(int? folioId)
    {
        var loadResult = await LoadChargeFormAsync(folioId);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return NativePartial();
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
        FolioStatus = folio.Status;
        FolioBalance = folio.Balance;
        GuestName = $"{folio.Guest?.FirstName} {folio.Guest?.LastName}".Trim();
        var businessDate = await GetBusinessDateAsync();
        ValidateFolioCanPost();
        ValidateCharge();
        ValidatePostingDate(businessDate);

        if (!ModelState.IsValid)
        {
            await LoadChargeCodesAsync();
            return NativePartialOrPage();
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

    private async Task<IActionResult?> LoadChargeFormAsync(int? folioId)
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
        FolioStatus = folio.Status;
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
        return null;
    }

    private void ValidateFolioCanPost()
    {
        if (!CanPostCharge)
        {
            ModelState.AddModelError(string.Empty, "Charges cannot be posted to closed, voided, or transferred folios.");
        }
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

    private static string FormatFolioStatus(FolioStatus status)
    {
        return status switch
        {
            FolioStatus.Transferred => "transferred to AR",
            _ => status.ToString().ToLowerInvariant()
        };
    }

    private IActionResult NativePartialOrPage()
    {
        return IsNativeWorkflowRequest() ? NativePartial() : Page();
    }

    private bool IsNativeWorkflowRequest()
    {
        return string.Equals(Request.Query["vpmsNative"], "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-VPMS-Native-Dialog"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private PartialViewResult NativePartial()
    {
        return new PartialViewResult
        {
            ViewName = "_PostChargeNative",
            ViewData = new ViewDataDictionary<PostChargeModel>(ViewData, this)
        };
    }
}
