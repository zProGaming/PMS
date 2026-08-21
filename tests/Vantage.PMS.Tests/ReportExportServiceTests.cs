using System.Text;
using Vantage.PMS.Services;
using Xunit;

namespace Vantage.PMS.Tests;

public class ReportExportServiceTests
{
    [Theory]
    [InlineData("=1+1")]
    [InlineData("+SUM(A1:A2)")]
    [InlineData("-10+5")]
    [InlineData("@SUM(A1:A2)")]
    [InlineData("  =1+1")]
    public void ExportToCsv_neutralizes_spreadsheet_formula_values(string input)
    {
        var service = new ReportExportService(null!, null!, null!, null!, null!, null!, null!);

        var csv = Encoding.UTF8.GetString(service.ExportToCsv(
            "Security export",
            null,
            null,
            [[input]]));

        Assert.Contains($"'{input}", csv, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportToCsv_preserves_regular_values_and_csv_quoting()
    {
        var service = new ReportExportService(null!, null!, null!, null!, null!, null!, null!);

        var csv = Encoding.UTF8.GetString(service.ExportToCsv(
            "Security export",
            null,
            null,
            [["Guest, Inc.", "Normal text"]]));

        Assert.Contains("\"Guest, Inc.\",Normal text", csv, StringComparison.Ordinal);
    }
}
