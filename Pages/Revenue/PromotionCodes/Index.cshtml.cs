using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.PromotionCodes;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<PromotionCode> PromotionCodes { get; set; } = new List<PromotionCode>();

    public async Task OnGetAsync()
    {
        PromotionCodes = await _context.PromotionCodes
            .Include(p => p.AppliesToRatePlan)
            .Include(p => p.AppliesToRoomType)
            .OrderBy(p => p.Code)
            .ToListAsync();
    }
}
