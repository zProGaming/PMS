using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Services;
using Xunit;

namespace Vantage.PMS.Tests;

public class FunctionalModuleTests
{
    [Fact]
    public void POSOrderTotalsCalculator_CalculatesCorrectSubTotalAndTaxAndServiceCharge()
    {
        var order = new POSOrder
        {
            Items = new List<POSOrderItem>
            {
                new POSOrderItem
                {
                    Quantity = 2,
                    UnitPrice = 500m,
                    DiscountAmount = 50m,
                    MenuItem = new MenuItem { IsServiceChargeable = true, IsTaxable = true }
                },
                new POSOrderItem
                {
                    Quantity = 1,
                    UnitPrice = 200m,
                    DiscountAmount = 0m,
                    MenuItem = new MenuItem { IsServiceChargeable = false, IsTaxable = true }
                }
            }
        };

        // For item 1: LineTotal = 2 * 500 - 50 = 950.
        // For item 2: LineTotal = 1 * 200 - 0 = 200.
        // SubTotal = 2*500 + 1*200 = 1200.
        // DiscountAmount = 50.
        // ServiceCharge = 950 * 0.10 = 95.
        // TaxAmount = (950 + 200) * 0.12 = 138.
        // TotalAmount = (950 + 200) + 95 + 138 = 1383.

        foreach (var item in order.Items)
        {
            item.LineTotal = POSOrderTotalsCalculator.CalculateLineTotal(item.Quantity, item.UnitPrice, item.DiscountAmount);
        }

        POSOrderTotalsCalculator.Recalculate(order);

        Assert.Equal(1200m, order.SubTotal);
        Assert.Equal(50m, order.DiscountAmount);
        Assert.Equal(95m, order.ServiceCharge);
        Assert.Equal(138m, order.TaxAmount);
        Assert.Equal(1383m, order.TotalAmount);
    }

    [Fact]
    public void POSOrderTotalsCalculator_IgnoresVoidedAndCancelledItems()
    {
        var order = new POSOrder
        {
            Items = new List<POSOrderItem>
            {
                new POSOrderItem
                {
                    Quantity = 1,
                    UnitPrice = 1000m,
                    DiscountAmount = 0m,
                    ItemStatus = POSOrderItemStatus.New,
                    MenuItem = new MenuItem { IsServiceChargeable = true, IsTaxable = true }
                },
                new POSOrderItem
                {
                    Quantity = 1,
                    UnitPrice = 500m,
                    DiscountAmount = 0m,
                    IsVoided = true,
                    MenuItem = new MenuItem { IsServiceChargeable = true, IsTaxable = true }
                },
                new POSOrderItem
                {
                    Quantity = 1,
                    UnitPrice = 300m,
                    DiscountAmount = 0m,
                    ItemStatus = POSOrderItemStatus.Cancelled,
                    MenuItem = new MenuItem { IsServiceChargeable = true, IsTaxable = true }
                }
            }
        };

        foreach (var item in order.Items)
        {
            item.LineTotal = POSOrderTotalsCalculator.CalculateLineTotal(item.Quantity, item.UnitPrice, item.DiscountAmount);
        }

        POSOrderTotalsCalculator.Recalculate(order);

        Assert.Equal(1000m, order.SubTotal);
        Assert.Equal(100m, order.ServiceCharge);
        Assert.Equal(120m, order.TaxAmount);
        Assert.Equal(1220m, order.TotalAmount);
    }

    [Fact]
    public void FinanceService_MapPaymentMethod_CorrectlyMapsStringsToEnum()
    {
        var service = new FinanceService(null!);

        Assert.Equal(FinancePaymentMethod.Cash, service.MapPaymentMethod("CASH"));
        Assert.Equal(FinancePaymentMethod.Cash, service.MapPaymentMethod("cash"));
        Assert.Equal(FinancePaymentMethod.CreditCard, service.MapPaymentMethod("CREDIT CARD"));
        Assert.Equal(FinancePaymentMethod.CreditCard, service.MapPaymentMethod("Card"));
        Assert.Equal(FinancePaymentMethod.EWallet, service.MapPaymentMethod("GCASH"));
        Assert.Equal(FinancePaymentMethod.EWallet, service.MapPaymentMethod("MAYA"));
        Assert.Equal(FinancePaymentMethod.CompanyCharge, service.MapPaymentMethod("CITY LEDGER"));
        Assert.Equal(FinancePaymentMethod.Other, service.MapPaymentMethod("UNKNOWN_TYPE"));
    }

    [Fact]
    public void FinanceService_CalculateExpectedCash_CalculatesShiftCashFloatAccurately()
    {
        var service = new FinanceService(null!);
        var shift = new CashierShift
        {
            OpeningCashFloat = 5000m,
            Transactions = new List<CashierTransaction>
            {
                new CashierTransaction { TransactionType = CashierTransactionType.Payment, PaymentMethod = FinancePaymentMethod.Cash, Amount = 1500m, IsVoided = false },
                new CashierTransaction { TransactionType = CashierTransactionType.Payment, PaymentMethod = FinancePaymentMethod.CreditCard, Amount = 3000m, IsVoided = false }, // Non-cash ignored
                new CashierTransaction { TransactionType = CashierTransactionType.Refund, PaymentMethod = FinancePaymentMethod.Cash, Amount = 200m, IsVoided = false },
                new CashierTransaction { TransactionType = CashierTransactionType.Payment, PaymentMethod = FinancePaymentMethod.Cash, Amount = 400m, IsVoided = true } // Voided ignored
            },
            CashDrops = new List<CashDrop>
            {
                new CashDrop { Amount = 2000m }
            }
        };

        // Expected Cash = OpeningCashFloat (5000) + CashPayments (1500) - CashRefunds (200) - CashDrops (2000) = 4300.
        var expectedCash = service.CalculateExpectedCash(shift);

        Assert.Equal(4300m, expectedCash);
    }

    [Fact]
    public void FinanceService_RecalculateFinanceDocument_UpdatesTotalsAndBalance()
    {
        var service = new FinanceService(null!);
        var document = new FinanceDocument
        {
            AmountPaid = 500m,
            Lines = new List<FinanceDocumentLine>
            {
                new FinanceDocumentLine
                {
                    Quantity = 2,
                    UnitPrice = 1000m,
                    TaxAmount = 240m,
                    ServiceCharge = 200m,
                    DiscountAmount = 100m
                },
                new FinanceDocumentLine
                {
                    Quantity = 1,
                    UnitPrice = 500m,
                    TaxAmount = 60m,
                    ServiceCharge = 50m,
                    DiscountAmount = 0m
                }
            }
        };

        service.RecalculateFinanceDocument(document);

        // SubTotal = 2*1000 + 1*500 = 2500.
        // TaxAmount = 240 + 60 = 300.
        // ServiceCharge = 200 + 50 = 250.
        // DiscountAmount = 100 + 0 = 100.
        // TotalAmount = 2500 + 300 + 250 - 100 = 2950.
        // Balance = 2950 - 500 = 2450.

        Assert.Equal(2500m, document.SubTotal);
        Assert.Equal(300m, document.TaxAmount);
        Assert.Equal(250m, document.ServiceCharge);
        Assert.Equal(100m, document.DiscountAmount);
        Assert.Equal(2950m, document.TotalAmount);
        Assert.Equal(2450m, document.Balance);
    }
}
