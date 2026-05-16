using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.Leads;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public SalesLead SalesLead { get; set; } = default!;

    public SelectList AccountOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var lead = await _context.SalesLeads.FindAsync(id);
        if (lead is null)
        {
            return NotFound();
        }

        SalesLead = lead;
        await LoadOptionsAsync(SalesLead.SalesAccountId, SalesLead.Status);
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

        var lead = await _context.SalesLeads.FindAsync(SalesLead.Id);
        if (lead is null)
        {
            return NotFound();
        }

        lead.SalesAccountId = SalesLead.SalesAccountId;
        lead.LeadName = SalesLead.LeadName;
        lead.LeadSource = SalesLead.LeadSource;
        lead.EstimatedValue = SalesLead.EstimatedValue;
        lead.Status = SalesLead.Status;
        lead.ExpectedCloseDate = SalesLead.ExpectedCloseDate;
        lead.AssignedTo = SalesLead.AssignedTo;
        lead.Notes = SalesLead.Notes;

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
