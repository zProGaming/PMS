using Vantage.PMS.Presentation;
using Xunit;

namespace Vantage.PMS.Tests;

public class UiTextTests
{
    [Theory]
    [InlineData("Housekeeping task", "Housekeeping Task")]
    [InlineData("RequestedBy", "Requested By")]
    [InlineData("ARInvoiceId", "AR Invoice ID")]
    [InlineData("CreditCard", "Credit Card")]
    [InlineData("OutOfOrder", "Out Of Order")]
    [InlineData("KPIs and VAT", "KPIs And VAT")]
    [InlineData("USALI report", "USALI Report")]
    [InlineData("Check-in review", "Check-In Review")]
    public void LabelsUseTitleCaseWithoutLosingAcronyms(string source, string expected) => Assert.Equal(expected, UiText.Label(source));

    [Fact] public void DisplayPreservesEnteredStringsAndFormatsOnlyEnums()
    {
        Assert.Equal("De la Cruz-Santos", UiText.Display("De la Cruz-Santos"));
        Assert.Equal("Out Of Order", UiText.Display(Models.FrontOffice.RoomStatus.OutOfOrder));
    }
}
