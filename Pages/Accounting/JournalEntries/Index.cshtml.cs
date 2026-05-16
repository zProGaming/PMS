using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.JournalEntries;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<JournalEntry> JournalEntries { get; private set; } = [];
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    public async Task OnGetAsync(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
        var query = context.JournalEntries.AsNoTracking().Include(entry => entry.Lines).AsQueryable();
        if (startDate is not null) query = query.Where(entry => entry.JournalDate >= startDate.Value);
        if (endDate is not null) query = query.Where(entry => entry.JournalDate <= endDate.Value);
        JournalEntries = await query.OrderByDescending(entry => entry.JournalDate).ThenByDescending(entry => entry.Id).Take(250).ToListAsync();
    }
}
