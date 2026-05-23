using Vantage.PMS.Models.Labor;

namespace Vantage.PMS.Services;

public static class ServiceChargeEligibility
{
    private static readonly string[] ManagerialPositionTerms =
    [
        "owner",
        "partner",
        "president",
        "chief",
        "director",
        "executive",
        "general manager",
        "resident manager",
        "manager",
        "controller"
    ];

    public static bool IsEligible(EmployeeCostProfile? employee)
    {
        return employee is not null &&
            employee.IsActive &&
            employee.EmploymentType != EmploymentType.Agency &&
            !IsManagerialPosition(employee.Position);
    }

    public static string GetReadinessLabel(EmployeeCostProfile? employee)
    {
        if (employee is null)
        {
            return "Department-level line";
        }

        if (!employee.IsActive)
        {
            return "Excluded: inactive profile";
        }

        if (employee.EmploymentType == EmploymentType.Agency)
        {
            return "Review: agency profile";
        }

        if (IsManagerialPosition(employee.Position))
        {
            return "Excluded: managerial/executive role";
        }

        return "Service-charge eligible";
    }

    public static string BuildGeneratedLineNote(EmployeeCostProfile employee)
    {
        return $"Generated line. {GetReadinessLabel(employee)} based on active non-managerial employee profile.";
    }

    private static bool IsManagerialPosition(string? position)
    {
        if (string.IsNullOrWhiteSpace(position))
        {
            return false;
        }

        var normalized = position.Trim().ToLowerInvariant();
        return ManagerialPositionTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal));
    }
}
