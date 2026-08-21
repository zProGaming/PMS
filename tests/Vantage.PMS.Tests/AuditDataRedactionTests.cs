using Vantage.PMS.Data;
using Xunit;

namespace Vantage.PMS.Tests;

public class AuditDataRedactionTests
{
    [Theory]
    [InlineData("Email")]
    [InlineData("PhoneNumber")]
    [InlineData("AddressLine1")]
    [InlineData("CardNumber")]
    [InlineData("PasswordHash")]
    [InlineData("ApiToken")]
    public void RedactAuditValue_hides_sensitive_values(string propertyName)
    {
        Assert.Equal("[REDACTED]", AuditDataRedaction.RedactAuditValue(propertyName, "sensitive-value"));
    }

    [Fact]
    public void RedactAuditValue_keeps_non_sensitive_operational_values()
    {
        Assert.Equal("CheckedIn", AuditDataRedaction.RedactAuditValue("Status", "CheckedIn"));
    }

    [Fact]
    public void RedactErrorText_removes_connection_and_query_secrets()
    {
        var value = "Password=secret-value; Server=database; request?token=abc123&mode=test";

        var redacted = AuditDataRedaction.RedactErrorText(value);

        Assert.Equal("Password=[REDACTED]; Server=database; request?token=[REDACTED]&mode=test", redacted);
    }
}
