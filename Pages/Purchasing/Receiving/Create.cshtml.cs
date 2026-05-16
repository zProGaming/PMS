using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Purchasing.Receiving;

public class CreateModel(ApplicationDbContext context, InventoryService inventoryService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly InventoryService _inventoryService = inventoryService;

    [BindProperty]
    public ReceivingRecord ReceivingRecord { get; set; } = new() { ReceivedDate = DateTime.Today };

    public SelectList PurchaseOrderOptions { get; set; } = null!;
    public SelectList SupplierOptions { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int? purchaseOrderId)
    {
        ReceivingRecord.ReceivingNumber = await _inventoryService.GenerateNumberAsync("RR");
        ReceivingRecord.ReceivedBy = User.Identity?.Name ?? string.Empty;
        ReceivingRecord.PurchaseOrderId = purchaseOrderId;
        if (purchaseOrderId is not null)
        {
            var order = await _context.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(po => po.Id == purchaseOrderId);
            ReceivingRecord.SupplierId = order?.SupplierId;
        }

        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ReceivingRecord.PurchaseOrderId is not null)
        {
            var order = await _context.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(po => po.Id == ReceivingRecord.PurchaseOrderId);
            if (order is null)
            {
                ModelState.AddModelError(nameof(ReceivingRecord.PurchaseOrderId), "Purchase order was not found.");
            }
            else if (order.Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived))
            {
                ModelState.AddModelError(nameof(ReceivingRecord.PurchaseOrderId), "Receiving is allowed only for approved or partially received purchase orders.");
            }
            else
            {
                ReceivingRecord.SupplierId = order.SupplierId;
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(ReceivingRecord.ReceivingNumber))
        {
            ReceivingRecord.ReceivingNumber = await _inventoryService.GenerateNumberAsync("RR");
        }

        ReceivingRecord.Status = ReceivingStatus.Draft;
        _context.ReceivingRecords.Add(ReceivingRecord);
        await _context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = ReceivingRecord.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var orders = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(order => order.Status == PurchaseOrderStatus.Approved || order.Status == PurchaseOrderStatus.PartiallyReceived)
            .OrderByDescending(order => order.OrderDate)
            .Select(order => new { order.Id, Name = order.PONumber + " - " + order.Supplier!.SupplierName })
            .ToListAsync();

        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.IsActive)
            .OrderBy(supplier => supplier.SupplierName)
            .ToListAsync();

        PurchaseOrderOptions = new SelectList(orders, "Id", "Name", ReceivingRecord.PurchaseOrderId);
        SupplierOptions = new SelectList(suppliers, "Id", "SupplierName", ReceivingRecord.SupplierId);
    }
}
