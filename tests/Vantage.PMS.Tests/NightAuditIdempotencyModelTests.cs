using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Xunit;

namespace Vantage.PMS.Tests;

public class NightAuditIdempotencyModelTests
{
    [Fact]
    public void Model_requires_one_completed_night_audit_per_business_date()
    {
        using var context = CreateContext();

        var index = context.Model
            .FindEntityType(typeof(NightAudit))!
            .GetIndexes()
            .Single(candidate => candidate.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(NightAudit.BusinessDate)]));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Model_uses_a_unique_key_for_automated_night_audit_charges()
    {
        using var context = CreateContext();

        var index = context.Model
            .FindEntityType(typeof(FolioItem))!
            .GetIndexes()
            .Single(candidate => candidate.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(FolioItem.FolioId),
                    nameof(FolioItem.NightAuditBusinessDate),
                    nameof(FolioItem.NightAuditChargeCode)
                ]));

        Assert.True(index.IsUnique);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=VantagePmsModelOnly;Integrated Security=True;TrustServerCertificate=True")
            .Options;

        return new ApplicationDbContext(options);
    }
}
