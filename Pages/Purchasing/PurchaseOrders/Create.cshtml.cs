using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Purchasing.PurchaseOrders;

public class CreateModel(ApplicationDbContext context, InventoryService inventoryService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly InventoryService _inventoryService = inventoryService;

    [BindProperty]
    public PurchaseOrder PurchaseOrder { get; set; } = new() { OrderDate = DateTime.Today };

    public SelectList SupplierOptions { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        PurchaseOrder.PONumber = await _inventoryService.GenerateNumberAsync("PO");
        PurchaseOrder.PreparedBy = User.Identity?.Name ?? string.Empty;
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetNativeAsync()
    {
        PurchaseOrder.PONumber = await _inventoryService.GenerateNumberAsync("PO");
        PurchaseOrder.PreparedBy = User.Identity?.Name ?? string.Empty;
        await LoadOptionsAsync();
        return NativePartial();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return NativePartialOrPage();
        }

        if (string.IsNullOrWhiteSpace(PurchaseOrder.PONumber))
        {
            PurchaseOrder.PONumber = await _inventoryService.GenerateNumberAsync("PO");
        }

        PurchaseOrder.Status = PurchaseOrderStatus.Draft;
        _inventoryService.RecalculatePurchaseOrderTotals(PurchaseOrder);
        _context.PurchaseOrders.Add(PurchaseOrder);
        await _context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = PurchaseOrder.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.IsActive)
            .OrderBy(supplier => supplier.SupplierName)
            .ToListAsync();

        SupplierOptions = new SelectList(suppliers, "Id", "SupplierName", PurchaseOrder.SupplierId);
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
