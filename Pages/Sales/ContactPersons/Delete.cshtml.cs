using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Pages.Sales.ContactPersons;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public ContactPerson ContactPerson { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var contact = await _context.ContactPersons
            .Include(contact => contact.SalesAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(contact => contact.Id == id);

        if (contact is null)
        {
            return NotFound();
        }

        ContactPerson = contact;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var contact = await _context.ContactPersons.FindAsync(id);
        var accountId = contact?.SalesAccountId;
        if (contact is not null)
        {
            _context.ContactPersons.Remove(contact);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index", new { accountId });
    }
}
