using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.FoodBeverage.Orders;

public class AddItemModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public POSOrderItem OrderItem { get; set; } = new() { Quantity = 1 };

    public int OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public SelectList MenuItemOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? orderId)
    {
        var loadResult = await LoadAddItemFormAsync(orderId);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return Page();
    }

    public async Task<IActionResult> OnGetNativeAsync(int? orderId)
    {
        var loadResult = await LoadAddItemFormAsync(orderId);
        if (loadResult is not null)
        {
            return loadResult;
        }

        return NativePartial();
    }

    public async Task<IActionResult> OnPostAsync(int? orderId)
    {
        if (orderId is null)
        {
            return NotFound();
        }

        var order = await _context.POSOrders
            .Include(order => order.Items)
                .ThenInclude(item => item.MenuItem)
            .FirstOrDefaultAsync(order => order.Id == orderId);

        if (order is null)
        {
            return NotFound();
        }

        OrderId = order.Id;
        OrderNumber = order.OrderNumber;
        ValidateOrderCanChange(order);
        ValidateOrderItem();

        var menuItem = await _context.MenuItems.FindAsync(OrderItem.MenuItemId);
        if (menuItem is null || !menuItem.IsAvailable)
        {
            ModelState.AddModelError("OrderItem.MenuItemId", "Select an available menu item.");
        }

        if (!ModelState.IsValid)
        {
            await LoadMenuItemsAsync(OrderItem.MenuItemId);
            return NativePartialOrPage();
        }

        OrderItem.POSOrderId = order.Id;
        OrderItem.UnitPrice = menuItem!.Price;
        OrderItem.LineTotal = POSOrderTotalsCalculator.CalculateLineTotal(
            OrderItem.Quantity,
            OrderItem.UnitPrice,
            OrderItem.DiscountAmount);
        OrderItem.ItemStatus = POSOrderItemStatus.New;
        OrderItem.IsVoided = false;
        if (order.OrderStatus != POSOrderStatus.Open)
        {
            OrderItem.SentToKitchenAt = DateTime.Now;
        }

        order.Items.Add(OrderItem);
        OrderItem.MenuItem = menuItem;
        POSOrderTotalsCalculator.Recalculate(order);

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id = order.Id });
    }

    private async Task<IActionResult?> LoadAddItemFormAsync(int? orderId)
    {
        if (orderId is null)
        {
            return NotFound();
        }

        var order = await _context.POSOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.Id == orderId);

        if (order is null)
        {
            return NotFound();
        }

        if (order.OrderStatus is POSOrderStatus.Closed or POSOrderStatus.Cancelled)
        {
            return RedirectToPage("./Details", new { id = order.Id });
        }

        OrderId = order.Id;
        OrderNumber = order.OrderNumber;
        OrderItem.POSOrderId = order.Id;
        await LoadMenuItemsAsync();
        return null;
    }

    private void ValidateOrderCanChange(POSOrder order)
    {
        if (order.OrderStatus is POSOrderStatus.Closed or POSOrderStatus.Cancelled)
        {
            ModelState.AddModelError(string.Empty, "Closed or cancelled orders cannot be changed.");
        }
    }

    private void ValidateOrderItem()
    {
        if (OrderItem.Quantity <= 0)
        {
            ModelState.AddModelError("OrderItem.Quantity", "Quantity must be greater than zero.");
        }

        if (OrderItem.DiscountAmount < 0)
        {
            ModelState.AddModelError("OrderItem.DiscountAmount", "Discount cannot be negative.");
        }
    }

    private async Task LoadMenuItemsAsync(object? selectedMenuItem = null)
    {
        var items = await _context.MenuItems
            .Include(item => item.MenuCategory)
            .AsNoTracking()
            .Where(item => item.IsAvailable && item.MenuCategory != null && item.MenuCategory.IsActive)
            .OrderBy(item => item.MenuCategory!.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync();

        MenuItemOptions = new SelectList(
            items.Select(item => new { item.Id, Name = $"{item.MenuCategory?.Name} - {item.Name} ({item.Price:N2})" }),
            "Id",
            "Name",
            selectedMenuItem);
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
            ViewName = "_AddItemNative",
            ViewData = new ViewDataDictionary<AddItemModel>(ViewData, this)
        };
    }
}
