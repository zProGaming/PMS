using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class CashPositionModel(ApplicationDbContext context) : PageModel
{
    public IList<CashPositionRow> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Rows = await context.BankAccounts.AsNoTracking()
            .Select(account => new CashPositionRow
            {
                AccountName = account.AccountName,
                BankName = account.BankName,
                Currency = account.Currency,
                IsActive = account.IsActive,
                Balance = account.OpeningBalance +
                    account.BankTransactions.Sum(transaction => transaction.DebitAmount - transaction.CreditAmount)
            })
            .OrderBy(row => row.AccountName)
            .ToListAsync();
    }

    public class CashPositionRow
    {
        public string AccountName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Currency { get; set; } = "PHP";
        public bool IsActive { get; set; }
        public decimal Balance { get; set; }
    }
}
