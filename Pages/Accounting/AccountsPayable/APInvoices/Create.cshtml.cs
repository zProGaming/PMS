using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.AccountsPayable.APInvoices;

public class CreateModel(ApplicationDbContext context, AccountsPayableService accountsPayableService) : PageModel
{
    [BindProperty]
    public APInvoice Input { get; set; } = new();

    [BindProperty]
    public List<LineInput> Lines { get; set; } = [];

    public SelectList SupplierOptions { get; private set; } = default!;
    public SelectList PurchaseOrderOptions { get; private set; } = default!;
    public SelectList ReceivingRecordOptions { get; private set; } = default!;
    public SelectList GLAccountOptions { get; private set; } = default!;
    public SelectList InventoryItemOptions { get; private set; } = default!;

    public async Task OnGetAsync(int? purchaseOrderId, int? receivingRecordId)
    {
        if (purchaseOrderId is not null)
        {
            Input = await accountsPayableService.BuildInvoiceFromPurchaseOrderAsync(purchaseOrderId.Value, User.Identity?.Name ?? "System");
            Lines = Input.Lines.Select(LineInput.FromModel).ToList();
        }
        else if (receivingRecordId is not null)
        {
            Input = await accountsPayableService.BuildInvoiceFromReceivingRecordAsync(receivingRecordId.Value, User.Identity?.Name ?? "System");
            Lines = Input.Lines.Select(LineInput.FromModel).ToList();
        }
        else
        {
            Input.InvoiceNumber = await accountsPayableService.GenerateNumberAsync("APINV");
            Lines = [new LineInput()];
        }

        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Lines = Lines.Where(line => !string.IsNullOrWhiteSpace(line.Description) || line.InventoryItemId is not null || line.GLAccountId is not null || line.Quantity != 0 || line.UnitCost != 0).ToList();
        if (Input.SupplierId <= 0)
        {
            ModelState.AddModelError("Input.SupplierId", "Supplier is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.InvoiceNumber))
        {
            ModelState.AddModelError("Input.InvoiceNumber", "Invoice number is required.");
        }

        if (Input.DueDate < Input.InvoiceDate)
        {
            ModelState.AddModelError("Input.DueDate", "Due date cannot be before invoice date.");
        }

        if (await context.APInvoices.AnyAsync(invoice => invoice.SupplierId == Input.SupplierId && invoice.InvoiceNumber == Input.InvoiceNumber))
        {
            ModelState.AddModelError("Input.InvoiceNumber", "Invoice number already exists for this supplier.");
        }

        if (Lines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "At least one invoice line is required.");
        }

        foreach (var line in Lines)
        {
            if (line.Quantity <= 0)
            {
                ModelState.AddModelError(string.Empty, "Line quantity must be positive.");
            }
            if (line.UnitCost < 0)
            {
                ModelState.AddModelError(string.Empty, "Line unit cost cannot be negative.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        Input.CreatedAt = DateTime.Now;
        Input.CreatedBy = User.Identity?.Name ?? "System";
        Input.Status = APInvoiceStatus.Draft;
        Input.Lines = Lines.Select(line => line.ToModel()).ToList();
        accountsPayableService.RecalculateAPInvoice(Input);

        context.APInvoices.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = Input.Id });
    }

    private async Task LoadOptionsAsync()
    {
        SupplierOptions = new SelectList(await context.Suppliers.AsNoTracking().Where(supplier => supplier.IsActive).OrderBy(supplier => supplier.SupplierName).ToListAsync(), "Id", "SupplierName");
        PurchaseOrderOptions = new SelectList(await context.PurchaseOrders.AsNoTracking().Include(order => order.Supplier).OrderByDescending(order => order.OrderDate).Take(100).Select(order => new { order.Id, Name = order.PONumber + " - " + (order.Supplier != null ? order.Supplier.SupplierName : "Supplier") }).ToListAsync(), "Id", "Name");
        ReceivingRecordOptions = new SelectList(await context.ReceivingRecords.AsNoTracking().Include(record => record.Supplier).OrderByDescending(record => record.ReceivedDate).Take(100).Select(record => new { record.Id, Name = record.ReceivingNumber + " - " + (record.Supplier != null ? record.Supplier.SupplierName : "Supplier") }).ToListAsync(), "Id", "Name");
        GLAccountOptions = new SelectList(await context.GLAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountCode).Select(account => new { account.Id, Name = account.AccountCode + " - " + account.AccountName }).ToListAsync(), "Id", "Name");
        InventoryItemOptions = new SelectList(await context.InventoryItems.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.ItemName).Select(item => new { item.Id, Name = item.ItemCode + " - " + item.ItemName }).ToListAsync(), "Id", "Name");
    }

    public class LineInput
    {
        public int? GLAccountId { get; set; }
        public int? InventoryItemId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public decimal UnitCost { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal WithholdingTaxAmount { get; set; }

        public static LineInput FromModel(APInvoiceLine line) => new()
        {
            GLAccountId = line.GLAccountId,
            InventoryItemId = line.InventoryItemId,
            Description = line.Description,
            Quantity = line.Quantity,
            UnitCost = line.UnitCost,
            TaxAmount = line.TaxAmount,
            WithholdingTaxAmount = line.WithholdingTaxAmount
        };

        public APInvoiceLine ToModel() => new()
        {
            GLAccountId = GLAccountId,
            InventoryItemId = InventoryItemId,
            Description = Description,
            Quantity = Quantity,
            UnitCost = UnitCost,
            TaxAmount = TaxAmount,
            WithholdingTaxAmount = WithholdingTaxAmount
        };
    }
}
