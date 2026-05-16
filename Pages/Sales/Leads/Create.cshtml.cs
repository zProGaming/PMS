using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Leads;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public SalesLead SalesLead { get; set; } = new() { Status = SalesLeadStatus.New };

    public SelectList AccountOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateLead();

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(SalesLead.SalesAccountId, SalesLead.Status);
            return Page();
        }

        SalesLead.CreatedAt = DateTime.Now;
        SalesLead.CreatedBy = User.Identity?.Name ?? Environment.UserName;

        _context.SalesLeads.Add(SalesLead);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private void ValidateLead()
    {
        if (SalesLead.EstimatedValue < 0)
        {
            ModelState.AddModelError("SalesLead.EstimatedValue", "Estimated value cannot be negative.");
        }
    }

    private async Task LoadOptionsAsync(object? selectedAccount = null, SalesLeadStatus selectedStatus = SalesLeadStatus.New)
    {
        var accounts = await _context.SalesAccounts
            .AsNoTracking()
            .Where(account => account.IsActive)
            .OrderBy(account => account.AccountName)
            .Select(account => new { account.Id, account.AccountName })
            .ToListAsync();

        AccountOptions = new SelectList(accounts, "Id", "AccountName", selectedAccount);
        StatusOptions = Enum.GetValues<SalesLeadStatus>().Select(status => new SelectListItem
        {
            Value = status.ToString(),
            Text = status.ToString(),
            Selected = status == selectedStatus
        });
    }
}
