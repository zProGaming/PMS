using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Data;

public static class CashFlowSeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedCategoriesAsync(context);
        await SeedCashAccountSettingsAsync(context);
        await SeedMappingRulesAsync(context);
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        var defaults = new[]
        {
            Category("OP_GUEST_RECEIPTS", "Cash Receipts from Guests", CashFlowSection.Operating, 10),
            Category("OP_AR_RECEIPTS", "Cash Receipts from Accounts Receivable", CashFlowSection.Operating, 20),
            Category("OP_FB_RECEIPTS", "Cash Receipts from F&B Sales", CashFlowSection.Operating, 30),
            Category("OP_BANQUET_RECEIPTS", "Cash Receipts from Banquet Sales", CashFlowSection.Operating, 40),
            Category("OP_SUPPLIER_PAYMENTS", "Cash Paid to Suppliers", CashFlowSection.Operating, 50),
            Category("OP_EMPLOYEE_PAYMENTS", "Cash Paid to Employees", CashFlowSection.Operating, 60),
            Category("OP_UTILITIES", "Cash Paid for Utilities", CashFlowSection.Operating, 70),
            Category("OP_OPERATING_EXPENSES", "Cash Paid for Operating Expenses", CashFlowSection.Operating, 80),
            Category("OP_TAXES", "Cash Paid for Taxes", CashFlowSection.Operating, 90),
            Category("OP_INTEREST", "Cash Paid for Interest", CashFlowSection.Operating, 100),
            Category("OP_OTHER", "Other Operating Cash Flows", CashFlowSection.Operating, 110),
            Category("OP_NET", "Net Cash Provided by Operating Activities", CashFlowSection.Operating, 900, true),

            Category("INV_PPE", "Purchase of Property and Equipment", CashFlowSection.Investing, 10),
            Category("INV_CAPEX", "Renovation and Capital Expenditures", CashFlowSection.Investing, 20),
            Category("INV_LONGTERM", "Purchase of Long-Term Assets", CashFlowSection.Investing, 30),
            Category("INV_ASSET_SALE", "Proceeds from Sale of Assets", CashFlowSection.Investing, 40),
            Category("INV_OTHER", "Other Investing Cash Flows", CashFlowSection.Investing, 50),
            Category("INV_NET", "Net Cash Used in Investing Activities", CashFlowSection.Investing, 900, true),

            Category("FIN_OWNER_CAPITAL", "Owner Capital Contributions", CashFlowSection.Financing, 10),
            Category("FIN_LOAN_PROCEEDS", "Loan Proceeds", CashFlowSection.Financing, 20),
            Category("FIN_LOAN_REPAYMENTS", "Loan Repayments", CashFlowSection.Financing, 30),
            Category("FIN_OWNER_WITHDRAWALS", "Owner Withdrawals or Dividends", CashFlowSection.Financing, 40),
            Category("FIN_COSTS", "Financing Costs", CashFlowSection.Financing, 50),
            Category("FIN_OTHER", "Other Financing Cash Flows", CashFlowSection.Financing, 60),
            Category("FIN_NET", "Net Cash Provided by Financing Activities", CashFlowSection.Financing, 900, true),

            Category("REC_BEGIN", "Beginning Cash and Cash Equivalents", CashFlowSection.BeginningCash, 10, true),
            Category("REC_CHANGE", "Net Increase or Decrease in Cash", CashFlowSection.Reconciliation, 20, true),
            Category("REC_END", "Ending Cash and Cash Equivalents", CashFlowSection.EndingCash, 30, true)
        };

        foreach (var category in defaults)
        {
            if (!await context.CashFlowCategories.AnyAsync(existing => existing.Code == category.Code))
            {
                context.CashFlowCategories.Add(category);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedCashAccountSettingsAsync(ApplicationDbContext context)
    {
        var defaults = new[]
        {
            ("1000", true, false, false, "Cash on hand for cashier and petty cash operations."),
            ("1010", false, true, false, "Operating bank cash account."),
            ("1020", false, false, true, "E-wallet and payment clearing treated as a cash equivalent.")
        };

        foreach (var (accountCode, isCashOnHand, isCashInBank, isEWallet, notes) in defaults)
        {
            var account = await context.GLAccounts.FirstOrDefaultAsync(gl => gl.AccountCode == accountCode);
            if (account is null || await context.CashAccountSettings.AnyAsync(setting => setting.GLAccountId == account.Id))
            {
                continue;
            }

            context.CashAccountSettings.Add(new CashAccountSetting
            {
                GLAccountId = account.Id,
                AccountName = $"{account.AccountCode} - {account.AccountName}",
                IsCashOnHand = isCashOnHand,
                IsCashInBank = isCashInBank,
                IsEWallet = isEWallet,
                IsCashEquivalent = true,
                IsActive = true,
                Notes = notes
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedMappingRulesAsync(ApplicationDbContext context)
    {
        var categories = await context.CashFlowCategories.ToDictionaryAsync(category => category.Code, category => category);
        var accounts = await context.GLAccounts.ToDictionaryAsync(account => account.AccountCode, account => account.Id);

        var defaults = new List<CashFlowMappingRule>();

        AddSource(defaults, categories, "Guest payments", SourceModule.Finance, SourceTransactionType.FolioPayment, "OP_GUEST_RECEIPTS");
        AddSource(defaults, categories, "AR payments", SourceModule.AccountsReceivable, SourceTransactionType.ARPayment, "OP_AR_RECEIPTS");
        AddSource(defaults, categories, "POS payments", SourceModule.FoodBeverage, SourceTransactionType.POSPayment, "OP_FB_RECEIPTS");
        AddSource(defaults, categories, "Banquet payments", SourceModule.Banquet, SourceTransactionType.BanquetPayment, "OP_BANQUET_RECEIPTS");
        AddSource(defaults, categories, "AP payment vouchers", SourceModule.Finance, SourceTransactionType.PaymentVoucher, "OP_SUPPLIER_PAYMENTS");
        AddSource(defaults, categories, "Disbursements", SourceModule.Finance, SourceTransactionType.Disbursement, "OP_SUPPLIER_PAYMENTS");
        AddSource(defaults, categories, "Payroll cost payments", SourceModule.Finance, SourceTransactionType.PayrollCost, "OP_EMPLOYEE_PAYMENTS");

        AddAccount(defaults, categories, accounts, "Utilities payments", "6500", "OP_UTILITIES");
        AddAccount(defaults, categories, accounts, "Taxes paid", "7100", "OP_TAXES");
        AddAccount(defaults, categories, accounts, "Interest paid", "7000", "OP_INTEREST");
        AddAccount(defaults, categories, accounts, "Administrative operating expenses", "6200", "OP_OPERATING_EXPENSES");
        AddAccount(defaults, categories, accounts, "Sales and marketing operating expenses", "6300", "OP_OPERATING_EXPENSES");
        AddAccount(defaults, categories, accounts, "Property maintenance operating expenses", "6400", "OP_OPERATING_EXPENSES");
        AddAccount(defaults, categories, accounts, "Information technology operating expenses", "6600", "OP_OPERATING_EXPENSES");
        AddAccount(defaults, categories, accounts, "Rent payments", "6700", "OP_OPERATING_EXPENSES");
        AddAccount(defaults, categories, accounts, "Insurance payments", "6710", "OP_OPERATING_EXPENSES");
        AddAccount(defaults, categories, accounts, "Taxes and licenses payments", "6720", "OP_TAXES");
        AddAccount(defaults, categories, accounts, "Owner capital movement", "3000", "FIN_OWNER_CAPITAL");
        AddAccount(defaults, categories, accounts, "Retained earnings financing movement", "3100", "FIN_OTHER");

        foreach (var rule in defaults)
        {
            var exists = await context.CashFlowMappingRules.AnyAsync(existing =>
                existing.RuleName == rule.RuleName ||
                (existing.GLAccountId == rule.GLAccountId &&
                 existing.SourceModule == rule.SourceModule &&
                 existing.SourceTransactionType == rule.SourceTransactionType &&
                 existing.CashFlowCategoryId == rule.CashFlowCategoryId));

            if (!exists)
            {
                context.CashFlowMappingRules.Add(rule);
            }
        }

        await context.SaveChangesAsync();
    }

    private static CashFlowCategory Category(string code, string name, CashFlowSection section, int sortOrder, bool isSubtotal = false)
    {
        return new CashFlowCategory
        {
            Code = code,
            Name = name,
            CashFlowSection = section,
            Description = "Seeded configurable cash-flow category. Review mappings with hotel finance before production use.",
            SortOrder = sortOrder,
            IsSubtotal = isSubtotal,
            IsActive = true
        };
    }

    private static void AddSource(
        ICollection<CashFlowMappingRule> rules,
        IReadOnlyDictionary<string, CashFlowCategory> categories,
        string name,
        SourceModule module,
        SourceTransactionType transactionType,
        string categoryCode)
    {
        if (!categories.TryGetValue(categoryCode, out var category))
        {
            return;
        }

        rules.Add(new CashFlowMappingRule
        {
            RuleName = name,
            SourceModule = module,
            SourceTransactionType = transactionType,
            CashFlowCategoryId = category.Id,
            CashFlowSection = category.CashFlowSection,
            IsActive = true,
            CreatedBy = "Seed",
            Notes = "Seeded source transaction cash-flow mapping. Configure before production reporting."
        });
    }

    private static void AddAccount(
        ICollection<CashFlowMappingRule> rules,
        IReadOnlyDictionary<string, CashFlowCategory> categories,
        IReadOnlyDictionary<string, int> accounts,
        string name,
        string accountCode,
        string categoryCode)
    {
        if (!categories.TryGetValue(categoryCode, out var category) || !accounts.TryGetValue(accountCode, out var accountId))
        {
            return;
        }

        rules.Add(new CashFlowMappingRule
        {
            RuleName = name,
            GLAccountId = accountId,
            CashFlowCategoryId = category.Id,
            CashFlowSection = category.CashFlowSection,
            IsActive = true,
            CreatedBy = "Seed",
            Notes = "Seeded offset-account cash-flow mapping. Configure before production reporting."
        });
    }
}
