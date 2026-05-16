using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class BankTransactionsReportModel(ApplicationDbContext context) : PageModel
{
    public IList<BankTransaction> Transactions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Transactions = await context.BankTransactions.AsNoTracking()
            .Include(transaction => transaction.BankAccount)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .Take(500)
            .ToListAsync();
    }
}
