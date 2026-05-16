using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Data;

public static class AccountingSeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedUsaliDepartmentsAsync(context);
        await SeedUsaliReportLinesAsync(context);
        await SeedChartOfAccountsAsync(context);
        await SeedTaxCodesAsync(context);
        await SeedServiceChargeSettingsAsync(context);
        await SeedBankAccountsAsync(context);
        await SeedPhilippineReportLinesAsync(context);
        await SeedAccountingPeriodAsync(context);
        await SeedPostingRulesAsync(context);
    }

    private static async Task SeedUsaliDepartmentsAsync(ApplicationDbContext context)
    {
        var defaults = new[]
        {
            ("ROOMS", "Rooms", USALIDepartmentType.OperatedDepartment),
            ("FB", "Food and Beverage", USALIDepartmentType.OperatedDepartment),
            ("BNQ", "Banquet", USALIDepartmentType.OperatedDepartment),
            ("OOD", "Other Operated Departments", USALIDepartmentType.OperatedDepartment),
            ("A&G", "Administrative and General", USALIDepartmentType.UndistributedOperatingExpense),
            ("S&M", "Sales and Marketing", USALIDepartmentType.UndistributedOperatingExpense),
            ("POM", "Property Operations and Maintenance", USALIDepartmentType.UndistributedOperatingExpense),
            ("UTIL", "Utilities", USALIDepartmentType.UndistributedOperatingExpense),
            ("IT", "Information and Telecommunications", USALIDepartmentType.UndistributedOperatingExpense),
            ("MGMT", "Management Fees", USALIDepartmentType.Management),
            ("FIXED", "Fixed Charges", USALIDepartmentType.FixedCharge),
            ("NOI", "Non-Operating Income/Expenses", USALIDepartmentType.NonOperating)
        };

        var sort = 10;
        foreach (var (code, name, type) in defaults)
        {
            if (!await context.USALIDepartments.AnyAsync(department => department.Code == code))
            {
                context.USALIDepartments.Add(new USALIDepartment
                {
                    Code = code,
                    Name = name,
                    DepartmentType = type,
                    SortOrder = sort,
                    IsActive = true
                });
            }

            sort += 10;
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedUsaliReportLinesAsync(ApplicationDbContext context)
    {
        var defaults = new[]
        {
            ("ROOM_REV", "Rooms Revenue", USALIReportSection.Rooms, false),
            ("ROOM_EXP", "Rooms Department Expenses", USALIReportSection.Rooms, false),
            ("ROOM_PROFIT", "Rooms Department Profit", USALIReportSection.Rooms, true),
            ("FOOD_REV", "Food Revenue", USALIReportSection.FoodAndBeverage, false),
            ("BEV_REV", "Beverage Revenue", USALIReportSection.FoodAndBeverage, false),
            ("FB_COS", "F&B Cost of Sales", USALIReportSection.FoodAndBeverage, false),
            ("FB_EXP", "F&B Department Expenses", USALIReportSection.FoodAndBeverage, false),
            ("FB_PROFIT", "F&B Department Profit", USALIReportSection.FoodAndBeverage, true),
            ("BNQ_REV", "Banquet Revenue", USALIReportSection.Banquet, false),
            ("BNQ_COS", "Banquet Cost of Sales", USALIReportSection.Banquet, false),
            ("BNQ_EXP", "Banquet Department Expenses", USALIReportSection.Banquet, false),
            ("BNQ_PROFIT", "Banquet Department Profit", USALIReportSection.Banquet, true),
            ("OOD_REV", "Other Operated Departments Revenue", USALIReportSection.OtherOperatedDepartments, false),
            ("OOD_EXP", "Other Operated Departments Expenses", USALIReportSection.OtherOperatedDepartments, false),
            ("TOT_OP_REV", "Total Operating Revenue", USALIReportSection.GrossOperatingProfit, true),
            ("TOT_DEPT_EXP", "Total Departmental Expenses", USALIReportSection.GrossOperatingProfit, true),
            ("TOT_DEPT_PROFIT", "Total Departmental Profit", USALIReportSection.GrossOperatingProfit, true),
            ("AG", "Administrative and General", USALIReportSection.AdministrativeAndGeneral, false),
            ("SM", "Sales and Marketing", USALIReportSection.SalesAndMarketing, false),
            ("POM", "Property Operations and Maintenance", USALIReportSection.PropertyOperationsAndMaintenance, false),
            ("UTIL", "Utilities", USALIReportSection.Utilities, false),
            ("IT", "Information and Telecommunications", USALIReportSection.InformationAndTelecommunications, false),
            ("GOP", "Gross Operating Profit", USALIReportSection.GrossOperatingProfit, true),
            ("MGMT_FEE", "Management Fees", USALIReportSection.ManagementFees, false),
            ("NOI", "Non-Operating Income/Expenses", USALIReportSection.NonOperatingIncomeExpense, false),
            ("FIXED", "Fixed Charges", USALIReportSection.FixedCharges, false),
            ("EBITDA_MGMT", "EBITDA-style Management Result", USALIReportSection.EBITDA, true),
            ("NIBT", "Net Income Before Tax", USALIReportSection.NetIncome, true),
            ("TAX_EXP", "Income Tax Expense", USALIReportSection.NetIncome, false),
            ("NET_INC", "Net Income", USALIReportSection.NetIncome, true)
        };

        var sort = 10;
        foreach (var (code, name, section, isSubtotal) in defaults)
        {
            if (!await context.USALIReportLines.AnyAsync(line => line.Code == code))
            {
                context.USALIReportLines.Add(new USALIReportLine
                {
                    Code = code,
                    Name = name,
                    ReportSection = section,
                    SortOrder = sort,
                    IsSubtotal = isSubtotal,
                    IsActive = true
                });
            }

            sort += 10;
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedChartOfAccountsAsync(ApplicationDbContext context)
    {
        var roomsDepartmentId = await GetUsaliDepartmentIdAsync(context, "ROOMS");
        var fbDepartmentId = await GetUsaliDepartmentIdAsync(context, "FB");
        var banquetDepartmentId = await GetUsaliDepartmentIdAsync(context, "BNQ");
        var agDepartmentId = await GetUsaliDepartmentIdAsync(context, "A&G");
        var smDepartmentId = await GetUsaliDepartmentIdAsync(context, "S&M");
        var pomDepartmentId = await GetUsaliDepartmentIdAsync(context, "POM");
        var utilitiesDepartmentId = await GetUsaliDepartmentIdAsync(context, "UTIL");
        var itDepartmentId = await GetUsaliDepartmentIdAsync(context, "IT");
        var fixedDepartmentId = await GetUsaliDepartmentIdAsync(context, "FIXED");
        var nonOperatingDepartmentId = await GetUsaliDepartmentIdAsync(context, "NOI");

        var defaults = new[]
        {
            Account("1000", "Cash on Hand", GLAccountType.Asset, "Cash Receipts Journal", true),
            Account("1010", "Cash in Bank", GLAccountType.Asset, "Cash Receipts Journal", true),
            Account("1020", "E-Wallet Clearing", GLAccountType.Asset, "Cash Receipts Journal", true),
            Account("1100", "Guest Ledger / Guest Receivables", GLAccountType.Asset, "Accounts Receivable Subsidiary Ledger", true),
            Account("1110", "Accounts Receivable - City Ledger", GLAccountType.Asset, "Accounts Receivable Subsidiary Ledger", true),
            Account("1200", "Inventory - F&B", GLAccountType.Asset, "Inventory", true),
            Account("1210", "Inventory - Housekeeping", GLAccountType.Asset, "Inventory", true),
            Account("1220", "Inventory - Maintenance", GLAccountType.Asset, "Inventory", true),
            Account("1300", "Prepaid Expenses", GLAccountType.Asset, "Balance Sheet", false),
            Account("2000", "Accounts Payable", GLAccountType.Liability, "Accounts Payable Subsidiary Ledger", true),
            Account("2010", "Output VAT Payable", GLAccountType.Liability, "VAT Payable Summary", true),
            Account("2020", "Input VAT Clearing", GLAccountType.Asset, "VAT Input Summary", true),
            Account("2030", "Withholding Tax Payable", GLAccountType.Liability, "Withholding Tax Summary", true),
            Account("2040", "Service Charge Payable", GLAccountType.Liability, "Service Charge", true),
            Account("2050", "Guest Deposits", GLAccountType.Liability, "Balance Sheet", true),
            Account("2060", "Unearned Revenue", GLAccountType.Liability, "Balance Sheet", true),
            Account("2070", "Payroll Payable", GLAccountType.Liability, "Payroll", true),
            Account("2080", "Service Charge Distribution Payable", GLAccountType.Liability, "Service Charge", true),
            Account("3000", "Owner's Equity", GLAccountType.Equity, "Balance Sheet", false),
            Account("3100", "Retained Earnings", GLAccountType.Equity, "Balance Sheet", false),
            Account("4000", "Rooms Revenue", GLAccountType.Revenue, "Sales Journal", false, roomsDepartmentId, "ROOM_REV"),
            Account("4100", "Food Revenue", GLAccountType.Revenue, "Sales Journal", false, fbDepartmentId, "FOOD_REV"),
            Account("4110", "Beverage Revenue", GLAccountType.Revenue, "Sales Journal", false, fbDepartmentId, "BEV_REV"),
            Account("4200", "Banquet Revenue", GLAccountType.Revenue, "Sales Journal", false, banquetDepartmentId, "BNQ_REV"),
            Account("4300", "Other Revenue", GLAccountType.Revenue, "Sales Journal", false),
            Account("4400", "Miscellaneous Income", GLAccountType.OtherIncome, "Sales Journal", false),
            Account("5000", "Food Cost", GLAccountType.CostOfSales, "Purchase Journal", false, fbDepartmentId, "FB_COS"),
            Account("5010", "Beverage Cost", GLAccountType.CostOfSales, "Purchase Journal", false, fbDepartmentId, "FB_COS"),
            Account("5020", "Banquet Cost", GLAccountType.CostOfSales, "Purchase Journal", false, banquetDepartmentId, "BNQ_COS"),
            Account("5090", "Inventory Adjustment Expense", GLAccountType.CostOfSales, "Purchase Journal", false),
            Account("6000", "Rooms Payroll and Related Expenses", GLAccountType.Expense, "Profit and Loss", false, roomsDepartmentId, "ROOM_EXP"),
            Account("6100", "F&B Payroll and Related Expenses", GLAccountType.Expense, "Profit and Loss", false, fbDepartmentId, "FB_EXP"),
            Account("6150", "Banquet Payroll and Related Expenses", GLAccountType.Expense, "Profit and Loss", false, banquetDepartmentId, "BNQ_EXP"),
            Account("6200", "Administrative and General Expenses", GLAccountType.Expense, "Profit and Loss", false, agDepartmentId, "AG"),
            Account("6300", "Sales and Marketing Expenses", GLAccountType.Expense, "Profit and Loss", false, smDepartmentId, "SM"),
            Account("6400", "Property Operations and Maintenance", GLAccountType.Expense, "Profit and Loss", false, pomDepartmentId, "POM"),
            Account("6500", "Utilities", GLAccountType.Expense, "Profit and Loss", false, utilitiesDepartmentId, "UTIL"),
            Account("6600", "Information and Telecommunications", GLAccountType.Expense, "Profit and Loss", false, itDepartmentId, "IT"),
            Account("6700", "Rent", GLAccountType.Expense, "Profit and Loss", false, fixedDepartmentId, "FIXED"),
            Account("6710", "Insurance", GLAccountType.Expense, "Profit and Loss", false, fixedDepartmentId, "FIXED"),
            Account("6720", "Taxes and Licenses", GLAccountType.Expense, "Profit and Loss", false, fixedDepartmentId, "FIXED"),
            Account("6800", "Depreciation and Amortization", GLAccountType.Expense, "Profit and Loss", false, fixedDepartmentId, "FIXED"),
            Account("7000", "Interest Expense", GLAccountType.OtherExpense, "Profit and Loss", false, nonOperatingDepartmentId, "NOI"),
            Account("7100", "Income Tax Expense", GLAccountType.OtherExpense, "Profit and Loss", false, nonOperatingDepartmentId, "TAX_EXP"),
            Account("7200", "Inventory Adjustment Income", GLAccountType.OtherIncome, "Profit and Loss", false)
        };

        foreach (var account in defaults)
        {
            if (account.UsaliReportLineId is null && account.UsaliReportLineCode is not null)
            {
                account.Account.UsaliReportLineId = await GetUsaliReportLineIdAsync(context, account.UsaliReportLineCode);
            }

            if (!await context.GLAccounts.AnyAsync(existing => existing.AccountCode == account.Account.AccountCode))
            {
                context.GLAccounts.Add(account.Account);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedTaxCodesAsync(ApplicationDbContext context)
    {
        var outputVatAccountId = await GetAccountIdAsync(context, "2010");
        var inputVatAccountId = await GetAccountIdAsync(context, "2020");
        var withholdingAccountId = await GetAccountIdAsync(context, "2030");

        var defaults = new[]
        {
            new TaxCode { Code = "VAT12-OUT", Name = "Output VAT 12%", TaxType = TaxType.VATOutput, Rate = 12, GLAccountId = outputVatAccountId, IsActive = true },
            new TaxCode { Code = "VAT12-IN", Name = "Input VAT 12%", TaxType = TaxType.VATInput, Rate = 12, GLAccountId = inputVatAccountId, IsActive = true },
            new TaxCode { Code = "EWT", Name = "Expanded Withholding Tax", TaxType = TaxType.WithholdingTax, Rate = 2, GLAccountId = withholdingAccountId, IsActive = true }
        };

        foreach (var taxCode in defaults)
        {
            if (!await context.TaxCodes.AnyAsync(existing => existing.Code == taxCode.Code))
            {
                context.TaxCodes.Add(taxCode);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedServiceChargeSettingsAsync(ApplicationDbContext context)
    {
        if (!await context.ServiceChargeSettings.AnyAsync(setting => setting.Name == "Default Service Charge"))
        {
            context.ServiceChargeSettings.Add(new ServiceChargeSetting
            {
                Name = "Default Service Charge",
                Rate = 10,
                LiabilityGLAccountId = await GetAccountIdAsync(context, "2040"),
                IsActive = true,
                Notes = "Configurable MVP default. Validate local service-charge treatment before production use."
            });
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedBankAccountsAsync(ApplicationDbContext context)
    {
        if (!await context.BankAccounts.AnyAsync(account => account.AccountName == "Operating Bank Account"))
        {
            context.BankAccounts.Add(new BankAccount
            {
                AccountName = "Operating Bank Account",
                BankName = "Default Bank",
                AccountNumber = "CONFIGURE",
                GLAccountId = await GetAccountIdAsync(context, "1010"),
                Currency = "PHP",
                OpeningBalance = 0,
                IsActive = true,
                Notes = "Seeded MVP bank account. Replace with actual bank details before production use."
            });
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedPhilippineReportLinesAsync(ApplicationDbContext context)
    {
        var defaults = new[]
        {
            (PhilippineReportType.VATOutputSummary, "OUTPUT_VAT", "Output VAT", "2010", "VAT12-OUT"),
            (PhilippineReportType.VATInputSummary, "INPUT_VAT", "Input VAT", "2020", "VAT12-IN"),
            (PhilippineReportType.VATPayableSummary, "VAT_PAYABLE", "Net VAT Payable", "2010", "VAT12-OUT"),
            (PhilippineReportType.SalesJournal, "ROOM_SALES", "Rooms Revenue", "4000", null),
            (PhilippineReportType.SalesJournal, "FB_SALES", "F&B Revenue", "4100", null),
            (PhilippineReportType.SalesJournal, "BNQ_SALES", "Banquet Revenue", "4200", null),
            (PhilippineReportType.PurchaseJournal, "AP", "Accounts Payable", "2000", null),
            (PhilippineReportType.CashReceiptsJournal, "CASH", "Cash on Hand", "1000", null),
            (PhilippineReportType.CashReceiptsJournal, "BANK", "Cash in Bank", "1010", null),
            (PhilippineReportType.ExpandedWithholdingTaxSummary, "EWT", "Expanded Withholding Tax", "2030", "EWT")
        };

        var sort = 10;
        foreach (var (reportType, code, name, accountCode, taxCode) in defaults)
        {
            if (!await context.PhilippineTaxReportLines.AnyAsync(line => line.ReportType == reportType && line.LineCode == code))
            {
                context.PhilippineTaxReportLines.Add(new PhilippineTaxReportLine
                {
                    ReportType = reportType,
                    LineCode = code,
                    LineName = name,
                    GLAccountId = await GetAccountIdAsync(context, accountCode),
                    TaxCodeId = taxCode is null ? null : await GetTaxCodeIdAsync(context, taxCode),
                    SortOrder = sort,
                    IsActive = true
                });
            }

            sort += 10;
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedAccountingPeriodAsync(ApplicationDbContext context)
    {
        var today = DateTime.Today;
        var firstDay = new DateTime(today.Year, today.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var periodName = firstDay.ToString("yyyy-MM");

        if (!await context.AccountingPeriods.AnyAsync(period => period.PeriodName == periodName))
        {
            context.AccountingPeriods.Add(new AccountingPeriod
            {
                PeriodName = periodName,
                StartDate = firstDay,
                EndDate = lastDay,
                Status = AccountingPeriodStatus.Open,
                Notes = "Seeded current accounting period."
            });
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedPostingRulesAsync(ApplicationDbContext context)
    {
        var roomsDepartmentId = await GetUsaliDepartmentIdAsync(context, "ROOMS");
        var fbDepartmentId = await GetUsaliDepartmentIdAsync(context, "FB");
        var banquetDepartmentId = await GetUsaliDepartmentIdAsync(context, "BNQ");

        var roomChargeCodeId = await GetChargeCodeIdAsync(context, "ROOM");
        var fbChargeCodeId = await GetChargeCodeIdAsync(context, "FB");
        var banquetChargeCodeId = await GetChargeCodeIdAsync(context, "BNQ");

        var defaults = new[]
        {
            Rule("Room Charge", SourceModule.FrontOffice, SourceTransactionType.RoomCharge, "1100", "4000", roomChargeCodeId, null, roomsDepartmentId, "2010", "2040", null),
            Rule("Folio Charge", SourceModule.Finance, SourceTransactionType.FolioCharge, "1100", "4300", null, null, null, "2010", "2040", null),
            Rule("F&B Charge to Room", SourceModule.FoodBeverage, SourceTransactionType.POSChargeToRoom, "1100", "4100", fbChargeCodeId, null, fbDepartmentId, "2010", "2040", null),
            Rule("F&B POS Payment", SourceModule.FoodBeverage, SourceTransactionType.POSPayment, "1000", "4100", null, "Cash", fbDepartmentId, "2010", "2040", null),
            Rule("Banquet Charge", SourceModule.Banquet, SourceTransactionType.BanquetCharge, "1100", "4200", banquetChargeCodeId, null, banquetDepartmentId, "2010", "2040", null),
            Rule("Folio Payment Cash", SourceModule.Finance, SourceTransactionType.FolioPayment, "1000", "1100", null, "Cash", null, null, null, null),
            Rule("Folio Payment Card", SourceModule.Finance, SourceTransactionType.FolioPayment, "1010", "1100", null, "Card", null, null, null, null),
            Rule("Folio Payment E-Wallet", SourceModule.Finance, SourceTransactionType.FolioPayment, "1020", "1100", null, "EWallet", null, null, null, null),
            Rule("AR Invoice", SourceModule.AccountsReceivable, SourceTransactionType.ARInvoice, "1110", "4300", null, null, null, "2010", null, null),
            Rule("AR Payment Cash", SourceModule.AccountsReceivable, SourceTransactionType.ARPayment, "1000", "1110", null, "Cash", null, null, null, null),
            Rule("AR Payment Bank", SourceModule.AccountsReceivable, SourceTransactionType.ARPayment, "1010", "1110", null, "BankTransfer", null, null, null, null),
            Rule("Purchase Receiving", SourceModule.Purchasing, SourceTransactionType.PurchaseReceiving, "1200", "2000", null, null, null, "2020", null, null),
            Rule("Stock Issue", SourceModule.Inventory, SourceTransactionType.StockIssue, "5000", "1200", null, null, fbDepartmentId, null, null, null),
            Rule("Stock Adjustment Increase", SourceModule.Inventory, SourceTransactionType.StockAdjustmentIncrease, "1200", "7200", null, null, null, null, null, null),
            Rule("Stock Adjustment Decrease", SourceModule.Inventory, SourceTransactionType.StockAdjustmentDecrease, "5090", "1200", null, null, null, null, null, null),
            Rule("Cash Drop", SourceModule.Finance, SourceTransactionType.CashDrop, "1010", "1000", null, "Cash", null, null, null, null),
            Rule("AP Invoice", SourceModule.Purchasing, SourceTransactionType.APInvoice, "6200", "2000", null, null, null, "2020", null, null),
            Rule("Payment Voucher Cash", SourceModule.Finance, SourceTransactionType.PaymentVoucher, "2000", "1000", null, "Cash", null, null, null, null),
            Rule("Payment Voucher Bank", SourceModule.Finance, SourceTransactionType.PaymentVoucher, "2000", "1010", null, "BankTransfer", null, null, null, null),
            Rule("Payment Voucher E-Wallet", SourceModule.Finance, SourceTransactionType.PaymentVoucher, "2000", "1020", null, "EWallet", null, null, null, null),
            Rule("Accrual Entry", SourceModule.Finance, SourceTransactionType.Accrual, "6200", "2000", null, null, null, null, null, null),
            Rule("Payroll Cost", SourceModule.Finance, SourceTransactionType.PayrollCost, "6200", "2070", null, null, null, null, null, null),
            Rule("Service Charge Distribution", SourceModule.Finance, SourceTransactionType.ServiceChargeDistribution, "2080", "2070", null, null, null, null, null, null)
        };

        foreach (var ruleTemplate in defaults)
        {
            if (!await context.PostingRules.AnyAsync(rule =>
                    rule.RuleName == ruleTemplate.RuleName &&
                    rule.SourceModule == ruleTemplate.SourceModule &&
                    rule.TransactionType == ruleTemplate.TransactionType &&
                    rule.PaymentMethod == ruleTemplate.PaymentMethod))
            {
                context.PostingRules.Add(new PostingRule
                {
                    RuleName = ruleTemplate.RuleName,
                    SourceModule = ruleTemplate.SourceModule,
                    TransactionType = ruleTemplate.TransactionType,
                    ChargeCodeId = ruleTemplate.ChargeCodeId,
                    PaymentMethod = ruleTemplate.PaymentMethod,
                    USALIDepartmentId = ruleTemplate.USALIDepartmentId,
                    DebitGLAccountId = await GetRequiredAccountIdAsync(context, ruleTemplate.DebitCode),
                    CreditGLAccountId = await GetRequiredAccountIdAsync(context, ruleTemplate.CreditCode),
                    TaxGLAccountId = ruleTemplate.TaxCode is null ? null : await GetAccountIdAsync(context, ruleTemplate.TaxCode),
                    ServiceChargeGLAccountId = ruleTemplate.ServiceChargeCode is null ? null : await GetAccountIdAsync(context, ruleTemplate.ServiceChargeCode),
                    DiscountGLAccountId = ruleTemplate.DiscountCode is null ? null : await GetAccountIdAsync(context, ruleTemplate.DiscountCode),
                    IsActive = true,
                    Notes = "Seeded configurable MVP posting rule."
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static AccountSeed Account(
        string code,
        string name,
        GLAccountType type,
        string? philippineCategory,
        bool controlAccount,
        int? usaliDepartmentId = null,
        string? usaliReportLineCode = null)
    {
        return new AccountSeed(new GLAccount
        {
            AccountCode = code,
            AccountName = name,
            AccountType = type,
            NormalBalance = type is GLAccountType.Asset or GLAccountType.CostOfSales or GLAccountType.Expense or GLAccountType.OtherExpense
                ? NormalBalance.Debit
                : NormalBalance.Credit,
            UsaliDepartmentId = usaliDepartmentId,
            PhilippineReportCategory = philippineCategory,
            IsControlAccount = controlAccount,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "Seed"
        }, usaliReportLineCode);
    }

    private static PostingRuleSeed Rule(
        string name,
        SourceModule module,
        SourceTransactionType transactionType,
        string debitCode,
        string creditCode,
        int? chargeCodeId,
        string? paymentMethod,
        int? usaliDepartmentId,
        string? taxCode,
        string? serviceChargeCode,
        string? discountCode)
    {
        return new PostingRuleSeed(name, module, transactionType, debitCode, creditCode, chargeCodeId, paymentMethod, usaliDepartmentId, taxCode, serviceChargeCode, discountCode);
    }

    private static async Task<int?> GetAccountIdAsync(ApplicationDbContext context, string code)
    {
        return await context.GLAccounts
            .Where(account => account.AccountCode == code)
            .Select(account => (int?)account.Id)
            .FirstOrDefaultAsync();
    }

    private static async Task<int> GetRequiredAccountIdAsync(ApplicationDbContext context, string code)
    {
        var id = await GetAccountIdAsync(context, code);
        if (id is null)
        {
            throw new InvalidOperationException($"Required GL account {code} was not seeded.");
        }

        return id.Value;
    }

    private static async Task<int?> GetUsaliDepartmentIdAsync(ApplicationDbContext context, string code)
    {
        return await context.USALIDepartments
            .Where(department => department.Code == code)
            .Select(department => (int?)department.Id)
            .FirstOrDefaultAsync();
    }

    private static async Task<int?> GetUsaliReportLineIdAsync(ApplicationDbContext context, string code)
    {
        return await context.USALIReportLines
            .Where(line => line.Code == code)
            .Select(line => (int?)line.Id)
            .FirstOrDefaultAsync();
    }

    private static async Task<int?> GetChargeCodeIdAsync(ApplicationDbContext context, string code)
    {
        return await context.ChargeCodes
            .Where(chargeCode => chargeCode.Code == code)
            .Select(chargeCode => (int?)chargeCode.Id)
            .FirstOrDefaultAsync();
    }

    private static async Task<int?> GetTaxCodeIdAsync(ApplicationDbContext context, string code)
    {
        return await context.TaxCodes
            .Where(taxCode => taxCode.Code == code)
            .Select(taxCode => (int?)taxCode.Id)
            .FirstOrDefaultAsync();
    }

    private sealed record AccountSeed(GLAccount Account, string? UsaliReportLineCode)
    {
        public int? UsaliReportLineId => Account.UsaliReportLineId;
    }

    private sealed record PostingRuleSeed(
        string RuleName,
        SourceModule SourceModule,
        SourceTransactionType TransactionType,
        string DebitCode,
        string CreditCode,
        int? ChargeCodeId,
        string? PaymentMethod,
        int? USALIDepartmentId,
        string? TaxCode,
        string? ServiceChargeCode,
        string? DiscountCode);
}
