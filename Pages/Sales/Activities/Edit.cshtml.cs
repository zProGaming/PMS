using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Activities;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public SalesActivity SalesActivity { get; set; } = default!;

    public SelectList AccountOptions { get; set; } = default!;

    public SelectList LeadOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> ActivityTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var activity = await _context.SalesActivities.FindAsync(id);
        if (activity is null)
        {
            return NotFound();
        }

        SalesActivity = activity;
        await LoadOptionsAsync(SalesActivity.SalesAccountId, SalesActivity.SalesLeadId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(SalesActivity.SalesAccountId, SalesActivity.SalesLeadId);
            return Page();
        }

        var activity = await _context.SalesActivities.FindAsync(SalesActivity.Id);
        if (activity is null)
        {
            return NotFound();
        }

        activity.SalesAccountId = SalesActivity.SalesAccountId;
        activity.SalesLeadId = SalesActivity.SalesLeadId;
        activity.ActivityType = SalesActivity.ActivityType;
        activity.ActivityDate = SalesActivity.ActivityDate;
        activity.NextFollowUpDate = SalesActivity.NextFollowUpDate;
        activity.Notes = SalesActivity.Notes;
        activity.CreatedBy = SalesActivity.CreatedBy;

        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task LoadOptionsAsync(object? selectedAccount = null, object? selectedLead = null)
    {
        var accounts = await _context.SalesAccounts
            .AsNoTracking()
            .OrderBy(account => account.AccountName)
            .Select(account => new { account.Id, account.AccountName })
            .ToListAsync();

        var leads = await _context.SalesLeads
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
