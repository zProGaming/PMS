using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Xunit;

namespace Vantage.PMS.Tests;

public class EnterpriseReadinessTests
{
    [Fact]
    public void PmsRoles_And_PmsPolicies_HaveMatchingMappings()
    {
        Assert.NotEmpty(PmsRoles.All);
        Assert.NotEmpty(PmsRoles.AdminSetup);
        Assert.NotEmpty(PmsRoles.FrontOffice);
        Assert.NotEmpty(PmsRoles.Housekeeping);
        Assert.NotEmpty(PmsRoles.Finance);
        Assert.NotEmpty(PmsRoles.FinanceApprovals);
        Assert.NotEmpty(PmsRoles.AccountsReceivable);
        Assert.NotEmpty(PmsRoles.LaborCosting);
        Assert.NotEmpty(PmsRoles.ExecutiveReporting);
        Assert.NotEmpty(PmsRoles.GroupManagement);

        Assert.Contains(PmsRoles.SystemAdmin, PmsRoles.AdminSetup);
        Assert.Contains(PmsRoles.SystemAdmin, PmsRoles.FinanceApprovals);
        Assert.Contains(PmsRoles.FinanceManager, PmsRoles.FinanceApprovals);
    }

    [Fact]
    public void RedactAuditValue_SafeguardsSensitiveFields()
    {
        Assert.Equal("[REDACTED]", AuditDataRedaction.RedactAuditValue("CreditCardNumber", "1234-5678-9012-3456"));
        Assert.Equal("[REDACTED]", AuditDataRedaction.RedactAuditValue("CVV", "123"));
        Assert.Equal("[REDACTED]", AuditDataRedaction.RedactAuditValue("TaxIdentificationNumber", "TIN-987-654"));
        Assert.Equal("[REDACTED]", AuditDataRedaction.RedactAuditValue("PhilHealthNumber", "12-345678901-2"));
    }

    [Fact]
    public void RedactErrorText_RemovesSensitiveConnectionKeys()
    {
        var errorWithSecret = "User ID=admin; Password=SuperSecretKey123!; Data Source=db.vantage.internal; token=xyz987";
        var redacted = AuditDataRedaction.RedactErrorText(errorWithSecret);

        Assert.DoesNotContain("SuperSecretKey123!", redacted);
        Assert.Contains("Password=[REDACTED]", redacted);
    }
}
