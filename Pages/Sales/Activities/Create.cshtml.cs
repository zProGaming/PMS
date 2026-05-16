using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Activities;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public SalesActivity SalesActivity { get; set; } = new()
    {
        ActivityDate = DateTime.Now
    };

    public SelectList AccountOptions { get; set; } = default!;

    public SelectList LeadOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> ActivityTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? leadId, int? accountId)
    {
        SalesActivity.SalesLeadId = leadId;
        SalesActivity.SalesAccountId = accountId;

        if (leadId is not null && accountId is null)
        {
            SalesActivity.SalesAccountId = await _context.SalesLeads
                .AsNoTracking()
                .Where(lead => lead.Id == leadId)
                .Select(lead => lead.SalesAccountId)
                .FirstOrDefaultAsync();
        }

        SalesActivity.CreatedBy = User.Identity?.Name ?? Environment.UserName;
        await LoadOptionsAsync(SalesActivity.SalesAccountId, SalesActivity.SalesLeadId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(SalesActivity.CreatedBy))
        {
            SalesActivity.CreatedBy = User.Identity?.Name ?? Environment.UserName;
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(SalesActivity.SalesAccountId, SalesActivity.SalesLeadId);
            return Page();
        }

        SalesActivity.CreatedAt = DateTime.Now;

        _context.SalesActivities.Add(SalesActivity);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task LoadOptionsAsync(object? selectedAccount = null, object? selectedLead = null)
    {
        var accounts = await _context.SalesAccounts
            .AsNoTracking()
            .Where(account => account.IsActive)
            .OrderBy(account => account.AccountName)
            .Select(account => new { account.Id, account.AccountName })
            .ToListAsync();

        var leads = await _context.SalesLeads
            .Include(lead => lead.SalesAccount)
            .AsNoTracking()
            .OrderBy(lead => lead.LeadName)
            .ToListAsync();

        AccountOptions = new SelectList(accounts, "Id", "AccountName", selectedAccount);
        LeadOptions = new SelectList(
            leads.Select(lead => new { lead.Id, Name = $"{lead.LeadName} ({lead.Status})" }),
            "Id",
            "Name",
            selectedLead);
        ActivityTypeOptions = Enum.GetValues<SalesActivityType>().Select(type => new SelectListItem
        {
            Value = type.ToString(),
            Text = type.ToString(),
            Selected = type == SalesActivity.ActivityType
        });
    }
}
