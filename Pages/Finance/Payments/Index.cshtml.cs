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

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        Payments = await context.Payments
            .Include(payment => payment.Folio).ThenInclude(folio => folio!.Guest)
            .Include(payment => payment.Folio).ThenInclude(folio => folio!.Reservation).ThenInclude(reservation => reservation!.Room)
            .AsNoTracking()
            .OrderByDescending(payment => payment.PaymentDate)
            .Take(250)
            .ToListAsync();

        TodayPayments = Payments
            .Where(payment => payment.Status == PaymentStatus.Completed && payment.PaymentDate >= today && payment.PaymentDate < tomorrow)
            .Sum(payment => payment.Amount);
        MonthToDatePayments = Payments
            .Where(payment => payment.Status == PaymentStatus.Completed && payment.PaymentDate >= monthStart && payment.PaymentDate < tomorrow)
            .Sum(payment => payment.Amount);
        PendingPayments = Payments.Count(payment => payment.Status is PaymentStatus.Pending or PaymentStatus.Authorized);
        IntegritySummary = await paymentIntegrityService.GetSummaryAsync();
    }
}
