using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Services;

public class InventoryService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<string> GenerateNumberAsync(string prefix)
    {
        var todayPrefix = $"{prefix}-{DateTime.Today:yyyyMMdd}";
        var existingCount = prefix switch
        {
            "PR" => await _context.PurchaseRequests.CountAsync(request => request.RequestNumber.StartsWith(todayPrefix)),
            "PO" => await _context.PurchaseOrders.CountAsync(order => order.PONumber.StartsWith(todayPrefix)),
            "RR" => await _context.ReceivingRecords.CountAsync(record => record.ReceivingNumber.StartsWith(todayPrefix)),
            "ADJ" => await _context.StockAdjustments.CountAsync(adjustment => adjustment.AdjustmentNumber.StartsWith(todayPrefix)),
            _ => 0
        };

        return $"{todayPrefix}-{existingCount + 1:0000}";
    }

    public async Task<PurchaseOrder?> ConvertPurchaseRequestToPurchaseOrderAsync(int purchaseRequestId, int supplierId, string preparedBy)
    {
        var request = await _context.PurchaseRequests
            .Include(purchaseRequest => purchaseRequest.Items)
            .FirstOrDefaultAsync(purchaseRequest => purchaseRequest.Id == purchaseRequestId);

        if (request is null || request.Status != PurchaseRequestStatus.Approved)
        {
            return null;
        }

        var purchaseOrder = new PurchaseOrder
        {
            PONumber = await GenerateNumberAsync("PO"),
            SupplierId = supplierId,
            PurchaseRequestId = request.Id,
            OrderDate = DateTime.Today,
            Status = PurchaseOrderStatus.Draft,
            PreparedBy = preparedBy,
            Notes = request.Purpose
        };

        foreach (var requestItem in request.Items)
        {
            var amount = requestItem.Quantity * requestItem.EstimatedUnitCost;
            purchaseOrder.Items.Add(new PurchaseOrderItem
            {
                InventoryItemId = requestItem.InventoryItemId,
                Quantity = requestItem.Quantity,
                UnitCost = requestItem.EstimatedUnitCost,
                Amount = amount,
                Notes = requestItem.Notes
            });
        }

        RecalculatePurchaseOrderTotals(purchaseOrder);
        request.Status = PurchaseRequestStatus.ConvertedToPO;
        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();

        return purchaseOrder;
    }

    public async Task<IList<string>> PostReceivingAsync(int receivingRecordId, string createdBy)
    {
        var errors = new List<string>();
        var receiving = await _context.ReceivingRecords
            .Include(record => record.Items)
                .ThenInclude(item => item.InventoryItem)
            .Include(record => record.PurchaseOrder)
                .ThenInclude(order => order!.Items)
            .FirstOrDefaultAsync(record => record.Id == receivingRecordId);

        if (receiving is null)
        {
            errors.Add("Receiving record was not found.");
            return errors;
        }

        if (receiving.Status != ReceivingStatus.Draft)
        {
            errors.Add("Only draft receiving records can be posted.");
            return errors;
        }

        if (receiving.PurchaseOrder is not null &&
            receiving.PurchaseOrder.Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived))
        {
            errors.Add("Receiving can be posted only against approved or partially received purchase orders.");
            return errors;
        }

        if (receiving.Items.Count == 0)
        {
            errors.Add("Add at least one item before posting receiving.");
            return errors;
        }

        foreach (var item in receiving.Items)
        {
            item.Amount = item.QuantityReceived * item.UnitCost;
            if (item.QuantityReceived <= 0)
            {
                errors.Add("Received quantity must be greater than zero.");
                continue;
            }

            if (item.InventoryItem is null)
            {
                errors.Add("Receiving item inventory record was not found.");
                continue;
            }

            item.InventoryItem.CurrentStock += item.QuantityReceived;
            item.InventoryItem.StandardCost = item.UnitCost;
            if (item.ExpiryDate is not null)
            {
                item.InventoryItem.ExpiryDate = item.ExpiryDate;
            }

            _context.StockMovements.Add(new StockMovement
            {
                InventoryItemId = item.InventoryItemId,
                MovementDate = receiving.ReceivedDate,
                MovementType = StockMovementType.PurchaseReceiving,
                Quantity = item.QuantityReceived,
                UnitCost = item.UnitCost,
                ReferenceType = nameof(ReceivingRecord),
                ReferenceId = receiving.Id,
                Remarks = receiving.Notes,
                CreatedBy = createdBy
            });
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        receiving.Status = ReceivingStatus.Posted;
        await _context.SaveChangesAsync();

        if (receiving.PurchaseOrder is not null)
        {
            await UpdatePurchaseOrderReceivingStatusAsync(receiving.PurchaseOrder);
            await _context.SaveChangesAsync();
        }

        return errors;
    }

    public async Task<IList<string>> PostStockAdjustmentAsync(int adjustmentId, string createdBy)
    {
        var errors = new List<string>();
        var adjustment = await _context.StockAdjustments
            .Include(stockAdjustment => stockAdjustment.Items)
                .ThenInclude(item => item.InventoryItem)
            .FirstOrDefaultAsync(stockAdjustment => stockAdjustment.Id == adjustmentId);

        if (adjustment is null)
        {
            errors.Add("Stock adjustment was not found.");
            return errors;
        }

        if (adjustment.Status != StockAdjustmentStatus.Approved)
        {
            errors.Add("Only approved stock adjustments can be posted.");
            return errors;
        }

        if (adjustment.Items.Count == 0)
        {
            errors.Add("Add at least one item before posting adjustment.");
            return errors;
        }

        foreach (var item in adjustment.Items)
        {
            if (item.InventoryItem is null)
            {
                errors.Add("Adjustment item inventory record was not found.");
                continue;
            }

            item.SystemQuantity = item.InventoryItem.CurrentStock;
            item.UnitCost = item.InventoryItem.StandardCost;
            item.VarianceQuantity = item.ActualQuantity - item.SystemQuantity;
            item.VarianceAmount = item.VarianceQuantity * item.UnitCost;

            if (item.VarianceQuantity != 0)
            {
                _context.StockMovements.Add(new StockMovement
                {
                    InventoryItemId = item.InventoryItemId,
                    MovementDate = adjustment.AdjustmentDate,
                    MovementType = item.VarianceQuantity > 0
                        ? StockMovementType.AdjustmentIncrease
                        : StockMovementType.AdjustmentDecrease,
                    Quantity = Math.Abs(item.VarianceQuantity),
                    UnitCost = item.UnitCost,
                    ReferenceType = nameof(StockAdjustment),
                    ReferenceId = adjustment.Id,
                    Remarks = adjustment.Reason,
                    CreatedBy = createdBy
                });
            }

            item.InventoryItem.CurrentStock = item.ActualQuantity;
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        adjustment.Status = StockAdjustmentStatus.Posted;
        await _context.SaveChangesAsync();
        return errors;
    }

    public async Task<IList<string>> IssueStockAsync(int inventoryItemId, int? departmentId, decimal quantity, string? remarks, string createdBy)
    {
        var errors = new List<string>();
        var item = await _context.InventoryItems.FirstOrDefaultAsync(item => item.Id == inventoryItemId);
        if (item is null)
        {
            errors.Add("Inventory item was not found.");
            return errors;
        }

        if (quantity <= 0)
        {
            errors.Add("Issue quantity must be greater than zero.");
        }

        if (item.CurrentStock < quantity)
        {
            errors.Add("Issue quantity cannot exceed current stock.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        item.CurrentStock -= quantity;
        _context.StockMovements.Add(new StockMovement
        {
            InventoryItemId = item.Id,
            MovementDate = DateTime.Now,
            MovementType = StockMovementType.StockIssue,
            Quantity = quantity,
            UnitCost = item.StandardCost,
            ReferenceType = "StockIssue",
            DepartmentId = departmentId,
            Remarks = remarks,
            CreatedBy = createdBy
        });

        await _context.SaveChangesAsync();
        return errors;
    }

    public void RecalculatePurchaseRequestItem(PurchaseRequestItem item)
    {
        item.EstimatedAmount = item.Quantity * item.EstimatedUnitCost;
    }

    public void RecalculatePurchaseOrderItem(PurchaseOrderItem item)
    {
        item.Amount = item.Quantity * item.UnitCost;
    }

    public void RecalculateReceivingItem(ReceivingRecordItem item)
    {
        item.Amount = item.QuantityReceived * item.UnitCost;
    }

    public async Task RecalculatePurchaseOrderTotalsAsync(int purchaseOrderId)
    {
        var order = await _context.PurchaseOrders
            .Include(purchaseOrder => purchaseOrder.Items)
            .FirstOrDefaultAsync(purchaseOrder => purchaseOrder.Id == purchaseOrderId);

        if (order is null)
        {
            return;
        }

        RecalculatePurchaseOrderTotals(order);
        await _context.SaveChangesAsync();
    }

    public void RecalculatePurchaseOrderTotals(PurchaseOrder order)
    {
        order.SubTotal = order.Items.Sum(item => item.Amount);
        order.TotalAmount = order.SubTotal + order.TaxAmount - order.DiscountAmount;
    }

    private async Task UpdatePurchaseOrderReceivingStatusAsync(PurchaseOrder purchaseOrder)
    {
        var receivedByItem = await _context.ReceivingRecordItems
            .Include(item => item.ReceivingRecord)
            .Where(item =>
                item.ReceivingRecord != null &&
                item.ReceivingRecord.PurchaseOrderId == purchaseOrder.Id &&
                item.ReceivingRecord.Status == ReceivingStatus.Posted)
            .GroupBy(item => item.InventoryItemId)
            .Select(group => new { InventoryItemId = group.Key, Quantity = group.Sum(item => item.QuantityReceived) })
            .ToListAsync();

        var allReceived = true;
        var anyReceived = false;
        foreach (var orderItem in purchaseOrder.Items)
        {
            var receivedQuantity = receivedByItem
                .Where(item => item.InventoryItemId == orderItem.InventoryItemId)
                .Sum(item => item.Quantity);

            if (receivedQuantity > 0)
            {
                anyReceived = true;
            }

            if (receivedQuantity < orderItem.Quantity)
            {
                allReceived = false;
            }
        }

        purchaseOrder.Status = allReceived
            ? PurchaseOrderStatus.FullyReceived
            : anyReceived ? PurchaseOrderStatus.PartiallyReceived : PurchaseOrderStatus.Approved;
    }
}
