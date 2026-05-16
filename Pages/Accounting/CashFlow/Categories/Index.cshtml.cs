using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.CashFlow.Categories;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<CashFlowCategory> Categories { get; private set; } = [];

    [BindProperty]
    public CashFlowCategory Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        Input.Code = (Input.Code ?? string.Empty).Trim().ToUpperInvariant();
        Input.Name = (Input.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(Input.Code))
        {
            ModelState.AddModelError("Input.Code", "Category code is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError("Input.Name", "Category name is required.");
        }

        if (await context.CashFlowCategories.AnyAsync(category => category.Code == Input.Code))
        {
            ModelState.AddModelError("Input.Code", "Category code must be unique.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.IsActive = true;
        context.CashFlowCategories.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var category = await context.CashFlowCategories.FindAsync(id);
        if (category is not null)
        {
            category.IsActive = !category.IsActive;
            await context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Categories = await context.CashFlowCategories
            .AsNoTracking()
            .OrderBy(category => category.CashFlowSection)
            .ThenBy(category => category.SortOrder)
            .ThenBy(category => category.Code)
            .ToListAsync();
    }
}
