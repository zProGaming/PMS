using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.PromotionCodes;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public PromotionCode PromotionCode { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var promotionCode = await _context.PromotionCodes.FirstOrDefaultAsync(p => p.Id == id);
        if (promotionCode == null)
        {
            return NotFound();
        }

        PromotionCode = promotionCode;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var promotionCode = await _context.PromotionCodes.FindAsync(id);
        if (promotionCode != null)
        {
            _context.PromotionCodes.Remove(promotionCode);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
