using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.Payments;

public class IndexModel(ApplicationDbContext context, PaymentIntegrityService paymentIntegrityService) : PageModel
{
    public IList<Payment> Payments { get; private set; } = [];

    public decimal TodayPayments { get; private set; }

    public decimal MonthToDatePayments { get; private set; }

    public int PendingPayments { get; private set; }

    public PaymentIntegritySummary IntegritySummary { get; private set; } = new(0, 0, 0, 0, 0, 0);

    public int? SelectedFolioId { get; private set; }

    public async Task OnGetAsync(int? folioId)
    {
        SelectedFolioId = folioId;
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var paymentQuery = context.Payments.AsNoTracking()
            .Where(payment => !folioId.HasValue || payment.FolioId == folioId.Value);
        Payments = await paymentQuery
            .Include(payment => payment.Folio).ThenInclude(folio => folio!.Guest)
            .Include(payment => payment.Folio).ThenInclude(folio => folio!.Reservation).ThenInclude(reservation => reservation!.Room)
            .AsNoTracking()
            .OrderByDescending(payment => payment.PaymentDate)
            .Take(250)
            .ToListAsync();

        TodayPayments = await paymentQuery
            .Where(payment => payment.Status == PaymentStatus.Completed && payment.PaymentDate >= today && payment.PaymentDate < tomorrow)
            .SumAsync(payment => payment.Amount);
        MonthToDatePayments = await paymentQuery
            .Where(payment => payment.Status == PaymentStatus.Completed && payment.PaymentDate >= monthStart && payment.PaymentDate < tomorrow)
            .SumAsync(payment => payment.Amount);
        PendingPayments = await paymentQuery.CountAsync(payment => payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.Authorized);
        IntegritySummary = await paymentIntegrityService.GetSummaryAsync();
    }
}
