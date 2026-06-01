using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.FrontOffice.Guests;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Guest Guest { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public IActionResult OnGetNative()
    {
        return NativePartial();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return NativePartialOrPage();
        }

        Guest.CreatedAtUtc = DateTime.UtcNow;
        _context.Guests.Add(Guest);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
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
            ViewName = "_CreateNative",
            ViewData = new ViewDataDictionary<CreateModel>(ViewData, this)
        };
    }
}
