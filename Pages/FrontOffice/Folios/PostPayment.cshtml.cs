using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.FrontOffice.Folios;

public class PostPaymentModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    [BindProperty]
    public Payment Payment { get; set; } = new();

    public int FolioId { get; set; }

    public string FolioNumber { get; set; } = string.Empty;

    public decimal FolioBalance { get; set; }

    public string GuestName { get; set; } = string.Empty;

    public string? CurrentCashierShiftNumber { get; set; }

    public DateTime? CurrentCashierShiftBusinessDate { get; set; }

    public bool HasOpenCashierShift { get; set; }

    public bool CanPostWithoutOpenShift { get; set; }

    public string CashierShiftMessage { get; set; } = string.Empty;

    public Payment? PostedPayment { get; set; }

    public decimal RemainingBalanceAfterPayment { get; set; }

    public async Task<IActionResult> OnGetAsync(int? folioId)
    {
        var loadResult = await LoadPaymentFormAsync(folioId);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return Page();
    }

    public async Task<IActionResult> OnGetNativeAsync(int? folioId)
    {
        var loadResult = await LoadPaymentFormAsync(folioId);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return NativePartial();
    }

    public async Task<IActionResult> OnPostAsync(int? folioId)
    {
        if (folioId is null)
        {
            return NotFound();
        }

        var folio = await _context.Folios.FindAsync(folioId);
        if (folio is null)
        {
            return NotFound();
        }

        FolioId = folio.Id;
        FolioNumber = folio.FolioNumber;
        await LoadFolioContextAsync(folio.Id);
        var businessDate = await GetBusinessDateAsync();
        var userName = User.Identity?.Name ?? "Cashier";
        await LoadCashierContextAsync(userName);
        Payment.FolioId = folio.Id;
        ValidatePayment();
        ValidatePaymentDate(businessDate);

        if (!ModelState.IsValid)
        {
            return NativePartialOrPage();
        }

        var errors = await _financeService.PostFolioPaymentAsync(Payment, userName, CanPostWithoutOpenShift);
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return NativePartialOrPage();
        }

        if (IsNativeWorkflowRequest())
        {
            await LoadPostedPaymentAsync(Payment.Id);
            return SuccessNativePartial();
        }

        return RedirectToPage("./Details", new { id = folio.Id });
    }

    private async Task<IActionResult?> LoadPaymentFormAsync(int? folioId)
    {
        if (folioId is null)
        {
            return NotFound();
        }

        var folio = await _context.Folios
            .AsNoTracking()
            .Include(folio => folio.Guest)
            .Include(folio => folio.Items)
            .Include(folio => folio.Payments)
            .FirstOrDefaultAsync(folio => folio.Id == folioId);

        if (folio is null)
        {
            return NotFound();
        }

        FolioId = folio.Id;
        FolioNumber = folio.FolioNumber;
        FolioBalance = folio.Balance;
        GuestName = $"{folio.Guest?.FirstName} {folio.Guest?.LastName}".Trim();
        var businessDate = await GetBusinessDateAsync();
        await LoadCashierContextAsync(User.Identity?.Name ?? "Cashier");
        Payment = new Payment
        {
            FolioId = folio.Id,
            PaymentDate = businessDate.Date.Add(DateTime.Now.TimeOfDay),
            Amount = folio.Balance > 0 ? folio.Balance : 0,
            Status = PaymentStatus.Completed
        };

        return null;
    }

    private void ValidatePayment()
    {
        if (Payment.Amount <= 0)
        {
            ModelState.AddModelError("Payment.Amount", "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Payment.PaymentMethod))
        {
            ModelState.AddModelError("Payment.PaymentMethod", "Payment method is required.");
        }
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }

    private void ValidatePaymentDate(DateTime businessDate)
    {
        if (Payment.PaymentDate.Date < businessDate.Date)
        {
            ModelState.AddModelError("Payment.PaymentDate", "Transactions for this business date are locked.");
        }
    }

    private async Task LoadFolioContextAsync(int folioId)
    {
        var folio = await _context.Folios
            .AsNoTracking()
            .Include(item => item.Guest)
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .FirstOrDefaultAsync(item => item.Id == folioId);

        FolioBalance = folio?.Balance ?? 0;
        GuestName = $"{folio?.Guest?.FirstName} {folio?.Guest?.LastName}".Trim();
    }

    private async Task LoadCashierContextAsync(string userName)
    {
        CanPostWithoutOpenShift = User.IsInRole(PmsRoles.FinanceManager) ||
            User.IsInRole(PmsRoles.GeneralManager) ||
            User.IsInRole(PmsRoles.SystemAdmin);

        var shift = await _financeService.GetOpenShiftForUserAsync(userName);
        HasOpenCashierShift = shift is not null;
        CurrentCashierShiftNumber = shift?.ShiftNumber;
        CurrentCashierShiftBusinessDate = shift?.BusinessDate;
        CashierShiftMessage = shift is not null
            ? $"Posting will be traced to cashier shift {shift.ShiftNumber}."
            : CanPostWithoutOpenShift
                ? "No cashier shift is open. This payment will be marked as management-posted without shift trace."
                : "Open a cashier shift before posting this payment.";
    }

    private async Task LoadPostedPaymentAsync(int paymentId)
    {
        PostedPayment = await _context.Payments
            .AsNoTracking()
            .Include(payment => payment.Folio).ThenInclude(folio => folio!.Guest)
            .Include(payment => payment.CashierTransactions).ThenInclude(transaction => transaction.CashierShift)
            .FirstOrDefaultAsync(payment => payment.Id == paymentId);

        if (PostedPayment?.Folio is not null)
        {
            var balance = await _context.Folios
                .AsNoTracking()
                .Where(folio => folio.Id == PostedPayment.Folio.Id)
                .Select(folio =>
                    (_context.FolioItems
                        .Where(item => item.FolioId == folio.Id && !item.IsVoided)
                        .Sum(item => (decimal?)item.Amount) ?? 0) -
                    (_context.Payments
                        .Where(payment => payment.FolioId == folio.Id && payment.Status == PaymentStatus.Completed)
                        .Sum(payment => (decimal?)payment.Amount) ?? 0))
                .FirstOrDefaultAsync();

            FolioId = PostedPayment.Folio.Id;
            FolioNumber = PostedPayment.Folio.FolioNumber;
            FolioBalance = balance;
            RemainingBalanceAfterPayment = balance;
            GuestName = $"{PostedPayment.Folio.Guest?.FirstName} {PostedPayment.Folio.Guest?.LastName}".Trim();
            CurrentCashierShiftNumber = PostedPayment.CashierTransactions
                .OrderByDescending(transaction => transaction.TransactionDate)
                .Select(transaction => transaction.CashierShift?.ShiftNumber)
                .FirstOrDefault(number => !string.IsNullOrWhiteSpace(number));
        }
    }

    private IActionResult NativePartialOrPage()
    {
        return IsNativeWorkflowRequest() ? NativePartial() : Page();
    }

    private bool IsNativeWorkflowRequest()
    {
        return string.Equals(Request.Query["vpmsNative"], "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-VPMS-Native-Dialog"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private PartialViewResult NativePartial()
    {
        return new PartialViewResult
        {
            ViewName = "_PostPaymentNative",
            ViewData = new ViewDataDictionary<PostPaymentModel>(ViewData, this)
        };
    }

    private PartialViewResult SuccessNativePartial()
    {
        return new PartialViewResult
        {
            ViewName = "_PostPaymentSuccessNative",
            ViewData = new ViewDataDictionary<PostPaymentModel>(ViewData, this)
        };
    }
}
