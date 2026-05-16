using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.Finance.ChargeCodes;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<ChargeCode> ChargeCodes { get; set; } = new List<ChargeCode>();

    [BindProperty]
    public ChargeCode ChargeCode { get; set; } = new() { IsActive = true };

    public async Task OnGetAsync(int? id)
    {
        await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(ChargeCode.Id == 0 ? null : ChargeCode.Id);
            return Page();
        }

        if (ChargeCode.Id == 0)
        {
            ChargeCode.CreatedAt = DateTime.Now;
            ChargeCode.CreatedBy = User.Identity?.Name ?? "System";
            _context.ChargeCodes.Add(ChargeCode);
        }
        else
        {
            var existing = await _context.ChargeCodes.FindAsync(ChargeCode.Id);
            if (existing is null)
            {
                return NotFound();
            }

            existing.Code = ChargeCode.Code;
            existing.Name = ChargeCode.Name;
            existing.Description = ChargeCode.Description;
            existing.ChargeCategory = ChargeCode.ChargeCategory;
            existing.IsTaxable = ChargeCode.IsTaxable;
            existing.IsServiceChargeable = ChargeCode.IsServiceChargeable;
            existing.IsActive = ChargeCode.IsActive;
            existing.DefaultAmount = ChargeCode.DefaultAmount;
        }

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        var chargeCode = await _context.ChargeCodes.FindAsync(id);
        if (chargeCode is null)
        {
            return NotFound();
        }

        chargeCode.IsActive = false;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadAsync(int? id)
    {
        ChargeCodes = await _context.ChargeCodes
            .AsNoTracking()
            .OrderBy(item => item.Code)
            .ToListAsync();

        if (id is not null)
        {
            var existing = await _context.ChargeCodes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
            if (existing is not null)
            {
                ChargeCode = existing;
            }
        }
    }
}
