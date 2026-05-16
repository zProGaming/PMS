using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Purchasing.PurchaseOrders;

public class DetailsModel(ApplicationDbContext context, InventoryService inventoryService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly InventoryService _inventoryService = inventoryService;

    public PurchaseOrder PurchaseOrder { get; set; } = new();

    [BindProperty]
    public PurchaseOrderItem NewItem { get; set; } = new();

    [BindProperty]
    public decimal TaxAmount { get; set; }

    [BindProperty]
    public decimal DiscountAmount { get; set; }

    public SelectList InventoryItemOptions { get; set; } = null!;

    public bool CanEditItems => PurchaseOrder.Status == PurchaseOrderStatus.Draft;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var found = await LoadAsync(id);
        return found ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostAddItemAsync(int id)
    {
        var found = await LoadAsync(id);
        if (!found)
        {
            return NotFound();
        }

        if (!CanEditItems)
        {
            ModelState.AddModelError(string.Empty, "Items can be edited only while the PO is draft.");
        }

        if (NewItem.Quantity <= 0)
        {
            ModelState.AddModelError(nameof(NewItem.Quantity), "Quantity must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        NewItem.PurchaseOrderId = id;
        _inventoryService.RecalculatePurchaseOrderItem(NewItem);
        _context.PurchaseOrderItems.Add(NewItem);
        await _context.SaveChangesAsync();
        await _inventoryService.RecalculatePurchaseOrderTotalsAsync(id);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteItemAsync(int id, int itemId)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            TempData["ErrorMessage"] = "Items can be removed only while the PO is draft.";
            return RedirectToPage(new { id });
        }

        var item = await _context.PurchaseOrderItems.FirstOrDefaultAsync(line => line.Id == itemId && line.PurchaseOrderId == id);
        if (item is not null)
        {
            _context.PurchaseOrderItems.Remove(item);
            await _context.SaveChangesAsync();
            await _inventoryService.RecalculatePurchaseOrderTotalsAsync(id);
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateTotalsAsync(int id)
    {
        var order = await _context.PurchaseOrders
            .Include(purchaseOrder => purchaseOrder.Items)
            .FirstOrDefaultAsync(purchaseOrder => purchaseOrder.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        if (order.Status is PurchaseOrderStatus.Closed or PurchaseOrderStatus.Cancelled)
        {
            TempData["ErrorMessage"] = "Closed or cancelled purchase orders cannot be updated.";
            return RedirectToPage(new { id });
        }

        order.TaxAmount = TaxAmount;
        order.DiscountAmount = DiscountAmount;
        _inventoryService.RecalculatePurchaseOrderTotals(order);
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostForApprovalAsync(int id)
    {
        var order = await _context.PurchaseOrders.Include(purchaseOrder => purchaseOrder.Items).FirstOrDefaultAsync(purchaseOrder => purchaseOrder.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            TempData["ErrorMessage"] = "Only draft purchase orders can be submitted for approval.";
            return RedirectToPage(new { id });
        }

        if (order.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "Add at least one item before submitting for approval.";
            return RedirectToPage(new { id });
        }

        order.Status = PurchaseOrderStatus.ForApproval;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != PurchaseOrderStatus.ForApproval)
        {
            TempData["ErrorMessage"] = "Only purchase orders for approval can be approved.";
            return RedirectToPage(new { id });
        }

        order.Status = PurchaseOrderStatus.Approved;
        order.ApprovedBy = User.Identity?.Name ?? "System";
        order.ApprovedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostMarkFullyReceivedAsync(int id)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived))
        {
            TempData["ErrorMessage"] = "Only approved or partially received POs can be marked fully received.";
            return RedirectToPage(new { id });
        }

        order.Status = PurchaseOrderStatus.FullyReceived;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != PurchaseOrderStatus.FullyReceived)
        {
            TempData["ErrorMessage"] = "Only fully received purchase orders can be closed.";
            return RedirectToPage(new { id });
        }

        order.Status = PurchaseOrderStatus.Closed;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status is PurchaseOrderStatus.Closed or PurchaseOrderStatus.Cancelled)
        {
            TempData["ErrorMessage"] = "This purchase order cannot be cancelled.";
            return RedirectToPage(new { id });
        }

        order.Status = PurchaseOrderStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var order = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(purchaseOrder => purchaseOrder.Supplier)
            .Include(purchaseOrder => purchaseOrder.PurchaseRequest)
            .Include(purchaseOrder => purchaseOrder.Items)
                .ThenInclude(item => item.InventoryItem)
            .FirstOrDefaultAsync(purchaseOrder => purchaseOrder.Id == id);

        if (order is null)
        {
            return false;
        }

        PurchaseOrder = order;
        TaxAmount = order.TaxAmount;
        DiscountAmount = order.DiscountAmount;
        await LoadOptionsAsync();
        return true;
    }

    private async Task LoadOptionsAsync()
    {
        var items = await _context.InventoryItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.ItemCode)
            .Select(item => new { item.Id, Name = item.ItemCode + " - " + item.ItemName })
            .ToListAsync();

        InventoryItemOptions = new SelectList(items, "Id", "Name");
    }
}
