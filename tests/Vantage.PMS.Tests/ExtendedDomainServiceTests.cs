using Vantage.PMS.Models.Labor;
using Vantage.PMS.Services;
using Xunit;

namespace Vantage.PMS.Tests;

public class ExtendedDomainServiceTests
{
    [Theory]
    [InlineData("Waiter", true)]
    [InlineData("Room Attendant", true)]
    [InlineData("Front Desk Agent", true)]
    [InlineData("General Manager", false)]
    [InlineData("Restaurant Manager", false)]
    [InlineData("Finance Director", false)]
    [InlineData("Chief Accountant", false)]
    public void ServiceChargeEligibility_IsEligible_CorrectlyEvaluatesPosition(string position, bool expectedEligible)
    {
        var employee = new EmployeeCostProfile
        {
            IsActive = true,
            EmploymentType = EmploymentType.Regular,
            Position = position
        };

        var isEligible = ServiceChargeEligibility.IsEligible(employee);

        Assert.Equal(expectedEligible, isEligible);
    }

    [Fact]
    public void ServiceChargeEligibility_IsEligible_ExcludesAgencyAndInactiveEmployees()
    {
        var agencyEmployee = new EmployeeCostProfile
        {
            IsActive = true,
            EmploymentType = EmploymentType.Agency,
            Position = "Housekeeper"
        };

        var inactiveEmployee = new EmployeeCostProfile
        {
            IsActive = false,
            EmploymentType = EmploymentType.Regular,
            Position = "Housekeeper"
        };

        Assert.False(ServiceChargeEligibility.IsEligible(agencyEmployee));
        Assert.False(ServiceChargeEligibility.IsEligible(inactiveEmployee));
    }

    [Fact]
    public void ServiceChargeEligibility_GetReadinessLabel_ReturnsExpectedDescriptiveLabels()
    {
        Assert.Equal("Department-level line", ServiceChargeEligibility.GetReadinessLabel(null));

        var inactive = new EmployeeCostProfile { IsActive = false, Position = "Cook" };
        Assert.Equal("Excluded: inactive profile", ServiceChargeEligibility.GetReadinessLabel(inactive));

        var agency = new EmployeeCostProfile { IsActive = true, EmploymentType = EmploymentType.Agency, Position = "Server" };
        Assert.Equal("Review: agency profile", ServiceChargeEligibility.GetReadinessLabel(agency));

        var manager = new EmployeeCostProfile { IsActive = true, EmploymentType = EmploymentType.Regular, Position = "Night Manager" };
        Assert.Equal("Excluded: managerial/executive role", ServiceChargeEligibility.GetReadinessLabel(manager));

        var regular = new EmployeeCostProfile { IsActive = true, EmploymentType = EmploymentType.Regular, Position = "Bellman" };
        Assert.Equal("Service-charge eligible", ServiceChargeEligibility.GetReadinessLabel(regular));
    }
}
