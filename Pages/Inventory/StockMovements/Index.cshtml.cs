using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.StockMovements;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty(SupportsGet = true)]
    public int? InventoryItemId { get; set; }

    [BindProperty(SupportsGet = true)]
    public StockMovementType? MovementType { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? DepartmentId { get; set; }

    public IList<StockMovement> Movements { get; set; } = new List<StockMovement>();
    public SelectList InventoryItemOptions { get; set; } = null!;
    public SelectList DepartmentOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        var query = _context.StockMovements
            .AsNoTracking()
            .Include(movement => movement.InventoryItem)
            .Include(movement => movement.Department)
            .AsQueryable();

        if (InventoryItemId is not null)
        {
            query = query.Where(movement => movement.InventoryItemId == InventoryItemId);
        }

        if (MovementType is not null)
        {
            query = query.Where(movement => movement.MovementType == MovementType);
        }

        if (FromDate is not null)
        {
            query = query.Where(movement => movement.MovementDate.Date >= FromDate.Value.Date);
        }

        if (ToDate is not null)
        {
            query = query.Where(movement => movement.MovementDate.Date <= ToDate.Value.Date);
        }

        if (DepartmentId is not null)
        {
            query = query.Where(movement => movement.DepartmentId == DepartmentId);
        }

        Movements = await query
            .OrderByDescending(movement => movement.MovementDate)
            .Take(300)
            .ToListAsync();

        await LoadOptionsAsync();
    }

    private async Task LoadOptionsAsync()
    {
        var items = await _context.InventoryItems
            .AsNoTracking()
            .OrderBy(item => item.ItemCode)
            .Select(item => new { item.Id, Name = item.ItemCode + " - " + item.ItemName })
            .ToListAsync();

        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ToListAsync();

        InventoryItemOptions = new SelectList(items, "Id", "Name", InventoryItemId);
        DepartmentOptions = new SelectList(departments, "Id", "Name", DepartmentId);
    }
}
