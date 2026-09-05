using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.FrontOffice.Reservations;

public class CheckOutModel(CheckoutService checkout) : PageModel
{
    public Reservation Reservation { get; private set; } = default!;
    public CheckoutReview Review { get; private set; } = default!;
    [BindProperty] public string ReviewToken { get; set; } = string.Empty;
    [BindProperty] public bool ManagerOverrideRequested { get; set; }
    [BindProperty] public string? OverrideReason { get; set; }
    [BindProperty] public bool CreditsAcknowledged { get; set; }

    public bool CanUseManagerOverride => CheckoutReview.CanOverride(User);
    public bool CanSubmitCheckOut => Reservation.Status == ReservationStatus.CheckedIn && Reservation.Room is not null;
    public bool CanCheckOut => CanSubmitCheckOut && Review.Validate(Input(), User).Count == 0;
    private CheckoutRequest Input() => new(ReviewToken, ManagerOverrideRequested, OverrideReason, CreditsAcknowledged);

    public async Task<IActionResult> OnGetAsync(int? id) =>
        await LoadAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnGetNativeAsync(int? id) =>
        await LoadAsync(id) ? NativePartial() : NotFound();

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null) return NotFound();
        if (!ModelState.IsValid)
        {
            if (!await LoadAsync(id)) return NotFound();
            ModelState.Remove(nameof(ReviewToken));
            return IsNative() ? NativePartial() : Page();
        }
        // Only decision fields are bound; no posted reservation or ledger state.
        var result = await checkout.CompleteAsync(id.Value, Input(), User);
        if (!result.Found) return NotFound();
        if (result.Completed) return RedirectToPage("./Details", new { id });
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error);
        if (!await LoadAsync(id)) return NotFound();
        // Render the refreshed review, not the stale hidden POST value.
        ModelState.Remove(nameof(ReviewToken));
        return IsNative() ? NativePartial() : Page();
    }

    private async Task<bool> LoadAsync(int? id)
    {
        if (id is null || await checkout.LoadAsync(id.Value) is not { } reservation) return false;
        Reservation = reservation;
        Review = new CheckoutReview(reservation);
        ReviewToken = Review.Token;
        return true;
    }

    private bool IsNative() => Request.Query["vpmsNative"] == "1" || Request.Headers["X-VPMS-Native-Dialog"] == "1";
    private PartialViewResult NativePartial() => new()
    {
        ViewName = "_CheckOutNative", ViewData = new ViewDataDictionary<CheckOutModel>(ViewData, this)
    };
}
