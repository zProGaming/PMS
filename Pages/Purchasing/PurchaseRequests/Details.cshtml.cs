using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Purchasing.PurchaseRequests;

public class DetailsModel(ApplicationDbContext context, InventoryService inventoryService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly InventoryService _inventoryService = inventoryService;

    public PurchaseRequest PurchaseRequest { get; set; } = new();

    [BindProperty]
    public PurchaseRequestItem NewItem { get; set; } = new();

    [BindProperty]
    public int SupplierId { get; set; }

    public SelectList InventoryItemOptions { get; set; } = null!;
    public SelectList SupplierOptions { get; set; } = null!;

    public bool CanEditItems => PurchaseRequest.Status == PurchaseRequestStatus.Draft;

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
            ModelState.AddModelError(string.Empty, "Items can be edited only while the request is draft.");
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

        NewItem.PurchaseRequestId = id;
        _inventoryService.RecalculatePurchaseRequestItem(NewItem);
        _context.PurchaseRequestItems.Add(NewItem);
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteItemAsync(int id, int itemId)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != PurchaseRequestStatus.Draft)
        {
            TempData["ErrorMessage"] = "Items can be removed only while the request is draft.";
            return RedirectToPage(new { id });
        }

        var item = await _context.PurchaseRequestItems.FirstOrDefaultAsync(line => line.Id == itemId && line.PurchaseRequestId == id);
        if (item is not null)
        {
            _context.PurchaseRequestItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        var request = await _context.PurchaseRequests
            .Include(purchaseRequest => purchaseRequest.Items)
            .FirstOrDefaultAsync(purchaseRequest => purchaseRequest.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != PurchaseRequestStatus.Draft)
        {
            TempData["ErrorMessage"] = "Only draft requests can be submitted.";
            return RedirectToPage(new { id });
        }

        if (request.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "Add at least one item before submitting.";
            return RedirectToPage(new { id });
        }

        request.Status = PurchaseRequestStatus.Submitted;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != PurchaseRequestStatus.Submitted)
        {
            TempData["ErrorMessage"] = "Only submitted requests can be approved.";
            return RedirectToPage(new { id });
        }

        request.Status = PurchaseRequestStatus.Approved;
        request.ApprovedBy = User.Identity?.Name ?? "System";
        request.ApprovedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != PurchaseRequestStatus.Submitted)
        {
            TempData["ErrorMessage"] = "Only submitted requests can be rejected.";
            return RedirectToPage(new { id });
        }

        request.Status = PurchaseRequestStatus.Rejected;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var request = await _context.PurchaseRequests.FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status is PurchaseRequestStatus.ConvertedToPO or PurchaseRequestStatus.Cancelled)
        {
            TempData["ErrorMessage"] = "This request cannot be cancelled.";
            return RedirectToPage(new { id });
        }

        request.Status = PurchaseRequestStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostConvertToPOAsync(int id)
    {
        if (SupplierId <= 0)
        {
            TempData["ErrorMessage"] = "Select a supplier before converting to purchase order.";
            return RedirectToPage(new { id });
        }

        var order = await _inventoryService.ConvertPurchaseRequestToPurchaseOrderAsync(id, SupplierId, User.Identity?.Name ?? "System");
        if (order is null)
        {
            TempData["ErrorMessage"] = "Only approved requests can be converted to a purchase order.";
            return RedirectToPage(new { id });
        }

        return RedirectToPage("/Purchasing/PurchaseOrders/Details", new { id = order.Id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var request = await _context.PurchaseRequests
            .AsNoTracking()
            .Include(purchaseRequest => purchaseRequest.Department)
            .Include(purchaseRequest => purchaseRequest.Items)
                .ThenInclude(item => item.InventoryItem)
            .FirstOrDefaultAsync(purchaseRequest => purchaseRequest.Id == id);

        if (request is null)
        {
            return false;
        }

        PurchaseRequest = request;
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

        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.IsActive)
            .OrderBy(supplier => supplier.SupplierName)
            .ToListAsync();

        InventoryItemOptions = new SelectList(items, "Id", "Name");
        SupplierOptions = new SelectList(suppliers, "Id", "SupplierName");
    }
}
