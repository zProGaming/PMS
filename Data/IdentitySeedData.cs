using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Models.Core;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.ManagementAI;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Data;

public static class IdentitySeedData
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        foreach (var roleName in PmsRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(BuildIdentityErrorMessage($"Failed to create role {roleName}.", roleResult.Errors));
                }
            }
        }

        await SeedDefaultDataAsync(context);

        if (await userManager.Users.AnyAsync())
        {
            return;
        }

        var adminEmail = configuration["DefaultAdmin:Email"];
        var adminPassword = configuration["DefaultAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "No users exist and DefaultAdmin:Email / DefaultAdmin:Password are not configured. " +
                "Provision the first SystemAdmin through secure environment configuration, then rotate the password after first sign-in.");
        }

        var adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(BuildIdentityErrorMessage("Failed to create the default admin user.", createResult.Errors));
        }

        var roleAssignmentResult = await userManager.AddToRoleAsync(adminUser, PmsRoles.SystemAdmin);
        if (!roleAssignmentResult.Succeeded)
        {
            throw new InvalidOperationException(BuildIdentityErrorMessage("Failed to assign SystemAdmin role to the default admin user.", roleAssignmentResult.Errors));
        }
    }

    private static async Task SeedDefaultDataAsync(ApplicationDbContext context)
    {
        await SeedChargeCodesAsync(context);
        await SeedDocumentNumberSequencesAsync(context);
        await SeedDepartmentsAsync(context);
        await SeedSystemSettingsAsync(context);
        await SeedAIRecommendationRulesAsync(context);
        await SeedQAChecklistAsync(context);
        await AccountingSeedData.SeedAsync(context);
        await CashFlowSeedData.SeedAsync(context);
        await ExecutiveSeedData.SeedAsync(context);
        await ReportSeedData.SeedAsync(context);
        await context.SaveChangesAsync();
    }

    private static async Task SeedChargeCodesAsync(ApplicationDbContext context)
    {
        var defaults = new[]
        {
            new ChargeCode { Code = "ROOM", Name = "Room Charge", ChargeCategory = ChargeCategory.Room, IsTaxable = true, IsServiceChargeable = true, IsActive = true, CreatedBy = "Seed" },
            new ChargeCode { Code = "FB", Name = "Food and Beverage", ChargeCategory = ChargeCategory.FoodBeverage, IsTaxable = true, IsServiceChargeable = true, IsActive = true, CreatedBy = "Seed" },
            new ChargeCode { Code = "BNQ", Name = "Banquet Charge", ChargeCategory = ChargeCategory.Banquet, IsTaxable = true, IsServiceChargeable = true, IsActive = true, CreatedBy = "Seed" },
            new ChargeCode { Code = "MISC", Name = "Miscellaneous", ChargeCategory = ChargeCategory.Miscellaneous, IsTaxable = true, IsServiceChargeable = false, IsActive = true, CreatedBy = "Seed" },
            new ChargeCode { Code = "TAX", Name = "Tax", ChargeCategory = ChargeCategory.Tax, IsTaxable = false, IsServiceChargeable = false, IsActive = true, CreatedBy = "Seed" },
            new ChargeCode { Code = "SVC", Name = "Service Charge", ChargeCategory = ChargeCategory.ServiceCharge, IsTaxable = false, IsServiceChargeable = false, IsActive = true, CreatedBy = "Seed" },
            new ChargeCode { Code = "DISC", Name = "Discount", ChargeCategory = ChargeCategory.Discount, IsTaxable = false, IsServiceChargeable = false, IsActive = true, CreatedBy = "Seed" }
        };

        foreach (var chargeCode in defaults)
        {
            if (!await context.ChargeCodes.AnyAsync(existing => existing.Code == chargeCode.Code))
            {
                context.ChargeCodes.Add(chargeCode);
            }
        }
    }

    private static async Task SeedDocumentNumberSequencesAsync(ApplicationDbContext context)
    {
        var defaults = new Dictionary<FinanceDocumentType, string>
        {
            [FinanceDocumentType.ProFormaInvoice] = "PF",
            [FinanceDocumentType.StatementOfAccount] = "SOA",
            [FinanceDocumentType.OfficialInvoice] = "OI",
            [FinanceDocumentType.AcknowledgementReceipt] = "AR",
            [FinanceDocumentType.PaymentReceipt] = "PR",
            [FinanceDocumentType.CreditMemo] = "CM",
            [FinanceDocumentType.DebitMemo] = "DM",
            [FinanceDocumentType.ChargeSlip] = "CS"
        };

        foreach (var sequence in defaults)
        {
            if (!await context.DocumentNumberSequences.AnyAsync(existing => existing.DocumentType == sequence.Key))
            {
                context.DocumentNumberSequences.Add(new DocumentNumberSequence
                {
                    DocumentType = sequence.Key,
                    Prefix = sequence.Value,
                    NextNumber = 1,
                    PaddingLength = 6,
                    IsActive = true
                });
            }
        }
    }

    private static async Task SeedDepartmentsAsync(ApplicationDbContext context)
    {
        var propertyId = await context.Properties
            .OrderBy(property => property.Id)
            .Select(property => (int?)property.Id)
            .FirstOrDefaultAsync();
        if (propertyId is null)
        {
            return;
        }

        var defaults = new[]
        {
            ("FO", "Front Office"),
            ("HK", "Housekeeping"),
            ("FIN", "Finance"),
            ("FB", "Food and Beverage"),
            ("SLS", "Sales"),
            ("BNQ", "Banquet"),
            ("INV", "Inventory")
        };

        foreach (var (code, name) in defaults)
        {
            if (!await context.Departments.AnyAsync(department => department.PropertyId == propertyId && department.Code == code))
            {
                context.Departments.Add(new Department
                {
                    PropertyId = propertyId.Value,
                    Code = code,
                    Name = name,
                    IsActive = true
                });
            }
        }
    }

    private static async Task SeedSystemSettingsAsync(ApplicationDbContext context)
    {
        var defaults = new[]
        {
            ("BusinessDate.UseHotelBusinessDate", "true", "Use configured hotel business date instead of relying only on server date.", "Core"),
            ("Finance.DefaultServiceChargePercentage", "10", "Default service charge percentage used for future calculations.", "Finance"),
            ("Finance.DefaultTaxPercentage", "12", "Default tax percentage used for future calculations.", "Finance"),
            ("AccountsPayable.ControlAccountCode", "2000", "GL account code used for Accounts Payable control postings.", "Accounts Payable"),
            ("AccountsPayable.InputVatAccountCode", "2020", "GL account code used for AP input VAT postings.", "Accounts Payable"),
            ("AccountsPayable.WithholdingTaxAccountCode", "2030", "GL account code used for withholding tax payable postings.", "Accounts Payable"),
            ("AccountsPayable.DefaultExpenseAccountCode", "6200", "Fallback expense GL account code for AP invoice lines without explicit mapping.", "Accounts Payable"),
            ("AccountsPayable.InventoryAccountCode", "1200", "Fallback inventory GL account code for AP invoice inventory lines.", "Accounts Payable"),
            ("AccountsPayable.PurchaseDiscountAccountCode", "6200", "Fallback GL account code for AP purchase discount postings.", "Accounts Payable"),
            ("Treasury.CashOnHandAccountCode", "1000", "Default GL account code for cash-on-hand payments.", "Treasury"),
            ("Treasury.CashInBankAccountCode", "1010", "Default GL account code for bank and card payments.", "Treasury"),
            ("Treasury.EWalletAccountCode", "1020", "Default GL account code for e-wallet payments.", "Treasury"),
            ("FrontOffice.DefaultCheckoutTime", "12:00", "Default hotel checkout time.", "Front Office"),
            ("FrontOffice.DefaultCheckInTime", "14:00", "Default hotel check-in time.", "Front Office"),
            ("Finance.HighBalanceFolioWarningThreshold", "50000", "Warning threshold for high guest folio balances.", "Finance"),
            ("AccountsReceivable.OverdueWarningThreshold", "100000", "Warning threshold for overdue AR balances.", "Accounts Receivable"),
            ("Inventory.LowStockAlertEnabled", "true", "Enable low stock notification alerts.", "Inventory"),
            ("ManagementAI.RuleBasedModeEnabled", "true", "Use rule-based management AI recommendations.", "Management AI"),
            ("DemoModeEnabled", "false", "Enables demo labels and sample data indicators.", "System")
        };

        foreach (var (key, value, description, module) in defaults)
        {
            if (!await context.SystemSettings.AnyAsync(setting => setting.SettingKey == key))
            {
                context.SystemSettings.Add(new SystemSetting
                {
                    SettingKey = key,
                    SettingValue = value,
                    Description = description,
                    Module = module,
                    IsEditable = true,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = "Seed"
                });
            }
        }
    }

    private static async Task SeedAIRecommendationRulesAsync(ApplicationDbContext context)
    {
        var defaults = new[]
        {
            ("High Occupancy Review", "Front Office", "Occupancy above 90%.", "Review remaining inventory, upgrade opportunities, and rate fences.", ManagementInsightSeverity.High),
            ("Dirty Room Backlog", "Housekeeping", "Dirty rooms above 20% of total inventory.", "Assign additional attendants to due-out and dirty rooms before 2:00 PM.", ManagementInsightSeverity.High),
            ("High Guest Balances", "Finance", "Outstanding guest balances above threshold.", "Review high-balance guest folios before allowing checkout.", ManagementInsightSeverity.High),
            ("Low Stock Alert", "Inventory", "Active inventory items are at or below reorder level.", "Reorder low-stock items before operations are affected.", ManagementInsightSeverity.Medium),
            ("Overdue AR Review", "Accounts Receivable", "AR overdue balance above threshold.", "Follow up AR accounts with overdue invoices.", ManagementInsightSeverity.High)
        };

        foreach (var (name, module, condition, recommendation, severity) in defaults)
        {
            if (!await context.AIRecommendationRules.AnyAsync(rule => rule.RuleName == name))
            {
                context.AIRecommendationRules.Add(new AIRecommendationRule
                {
                    RuleName = name,
                    Module = module,
                    ConditionDescription = condition,
                    RecommendationText = recommendation,
                    Severity = severity,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }
        }
    }

    private static async Task SeedQAChecklistAsync(ApplicationDbContext context)
    {
        var defaults = new[]
        {
            ("Front Office", "Reservation can be created.", "Create a basic reservation with guest, room type, dates, and rate."),
            ("Front Office", "Reservation can be checked in.", "Check in a reserved booking and confirm the room becomes occupied."),
            ("Front Office", "Reservation can be checked out.", "Settle the folio, check out the guest, and confirm the room becomes dirty."),
            ("Finance", "Folio charge can be posted.", "Post a manual folio charge with a valid charge code."),
            ("Finance", "Payment can be posted.", "Post a folio payment and confirm cashier controls."),
            ("Housekeeping", "Housekeeping can update room status.", "Move dirty to clean, inspected, and available where allowed."),
            ("Finance", "Night audit can run.", "Resolve blocking issues and advance the business date."),
            ("F&B Service", "POS order can be charged to room.", "Close a POS order to an in-house guest folio."),
            ("F&B Kitchen", "Kitchen can update item status.", "Move a kitchen item from new to preparing, ready, and served."),
            ("Banquet", "Banquet BEO can be approved.", "Create a BEO and mark it approved."),
            ("Booking Engine", "Booking request can convert to reservation.", "Convert a valid booking request and confirm the reservation link."),
            ("Guest Portal", "Guest can submit service request.", "Submit a guest service request through the portal lookup flow."),
            ("Inventory", "Purchase order receiving updates stock.", "Post receiving and verify stock movement and current stock."),
            ("Accounts Receivable", "AR payment allocation updates invoice balance.", "Allocate payment to AR invoice and verify balance."),
            ("Accounting", "Core accounting reports load safely.", "Open General Ledger, Trial Balance, Balance Sheet, P&L, USALI, and Statement of Cash Flows with empty and demo data."),
            ("Accounts Payable", "AP invoice to payment voucher flow works.", "Approve an AP invoice, release a payment voucher, and verify AP balance."),
            ("Banking", "Bank reconciliation can be prepared.", "Create a reconciliation, clear transactions, and confirm zero difference before approval."),
            ("Labor Costing", "Payroll period can be posted.", "Approve and post a payroll period with balanced labor journal entries."),
            ("Reports", "Report Center routes are safe.", "Open Report Center, Printable Documents, and key print views without broken links."),
            ("Executive", "Owner and GM reports load safely.", "Open Executive Dashboard, KPI Scorecard, Monthly Owner Report, and Owner Packages."),
            ("Navigation", "Sidebar is role-aware and searchable.", "Confirm active groups open, module search works, and unauthorized roles do not see protected modules."),
            ("Management AI", "AI summary can be generated.", "Generate today's management summary and review insights.")
        };

        foreach (var (module, testName, description) in defaults)
        {
            if (!await context.QATestChecklistItems.AnyAsync(item => item.Module == module && item.TestName == testName))
            {
                context.QATestChecklistItems.Add(new QATestChecklistItem
                {
                    Module = module,
                    TestName = testName,
                    Description = description,
                    Status = QATestChecklistStatus.NotTested
                });
            }
        }
    }

    private static string BuildIdentityErrorMessage(string message, IEnumerable<IdentityError> errors)
    {
        return $"{message} {string.Join(" ", errors.Select(error => error.Description))}";
    }
}
