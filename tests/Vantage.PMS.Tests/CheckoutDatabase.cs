using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using Xunit;

namespace Vantage.PMS.Tests;

[CollectionDefinition("Checkout SQL", DisableParallelization = true)]
public class CheckoutSqlCollection : ICollectionFixture<CheckoutDatabase> { }

public sealed class CheckoutDatabase : IAsyncLifetime
{
    private readonly string databaseName = "VantageCheckoutTests_" + Guid.NewGuid().ToString("N");
    public string ConnectionString => new SqlConnectionStringBuilder
    {
        DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = databaseName,
        IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 60
    }.ConnectionString;

    public ApplicationDbContext Open(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.EnableRetryOnFailure()).AddInterceptors(interceptors).Options;
        return new ApplicationDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = Open();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        // This fixture can delete ONLY the generated, local, isolated test database.
        var parsed = new SqlConnectionStringBuilder(ConnectionString);
        if (parsed.DataSource != @"(localdb)\MSSQLLocalDB" || parsed.InitialCatalog != databaseName || !databaseName.StartsWith("VantageCheckoutTests_"))
            throw new InvalidOperationException("Refusing to delete a non-test database.");
        await using var context = Open();
        await context.Database.EnsureDeletedAsync();
    }

    public async Task<int> SeedStayAsync(params decimal[] balances)
    {
        await using var context = Open();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var hotel = new Hotel { Code = suffix, Name = "QA Hotel" };
        var property = new Property { Hotel = hotel, Code = suffix, Name = "Training property" };
        var roomType = new RoomType { Property = property, Code = suffix, Name = "Standard", MaxOccupancy = 2 };
        var guest = new Guest { FirstName = "Alexandria", LastName = "De la Cruz-Santos (training guest)" };
        var stay = new Reservation
        {
            Property = property, Guest = guest, RoomType = roomType,
            Room = new Room { Property = property, RoomType = roomType, RoomNumber = suffix, Status = RoomStatus.Occupied },
            ConfirmationNumber = "QA-" + suffix, ArrivalDate = DateTime.Today.AddDays(-2), DepartureDate = DateTime.Today,
            Status = ReservationStatus.CheckedIn, Adults = 1, ActualCheckInDate = DateTime.Today.AddDays(-2)
        };
        foreach (var balance in balances)
            stay.Folios.Add(new Folio
            {
                Property = property, Guest = guest, FolioNumber = $"F-{suffix}-{stay.Folios.Count + 1}",
                Items = [new FolioItem { Amount = 1000, Quantity = 1, UnitPrice = 1000, ChargeCode = "ROOM", Description = "Training charge" }],
                Payments = [new Payment { Amount = 1000 - balance, PaymentMethod = "Cash", Status = PaymentStatus.Completed, PaymentDate = DateTime.Today.AddDays(-1) }]
            });
        context.Reservations.Add(stay);
        await context.SaveChangesAsync();
        return stay.Id;
    }
}
