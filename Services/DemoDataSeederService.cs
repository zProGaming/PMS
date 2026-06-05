using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Banquet;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Models.Core;
using Vantage.PMS.Models.Executive;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.Housekeeping;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Models.ManagementAI;
using Vantage.PMS.Models.Revenue;
using Vantage.PMS.Models.Sales;
using Vantage.PMS.Models.SystemAdministration;
using InventorySupplier = Vantage.PMS.Models.Inventory.Supplier;
using RevenueDiscountType = Vantage.PMS.Models.Revenue.DiscountType;

namespace Vantage.PMS.Services;

public class DemoDataSeederService(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    AuditLogService auditLogService,
    CashFlowReportService cashFlowReportService)
{
    private const string DemoMarker = "DEMO DATA";
    private const string DemoUserPassword = "VantageDemo@123";

    public async Task<DemoSeedResult> SeedDemoHotelAsync(string userName)
    {
        var result = new DemoSeedResult("Demo Hotel Setup");
        await EnsureDemoModeSettingAsync(result, userName);
        var propertyId = await EnsureHotelPropertyDepartmentsAsync(result);
        await EnsureRoomTypesAndRoomsAsync(propertyId, result);
        await EnsureDemoUsersAsync(result);
        await LogAsync(result, userName);
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<DemoSeedResult> SeedDemoOperationsAsync(string userName)
    {
        var result = new DemoSeedResult("Demo Operations");
        var propertyId = await EnsureHotelPropertyDepartmentsAsync(result);
        await EnsureRoomTypesAndRoomsAsync(propertyId, result);
        await EnsureGuestsReservationsFoliosAsync(propertyId, result);
        await EnsureHousekeepingAsync(result);
        await EnsureGuestPortalDemoAsync(result);
        await EnsureGroupManagementAsync(result);
        await LogAsync(result, userName);
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<DemoSeedResult> SeedDemoFinanceAsync(string userName)
    {
        var result = new DemoSeedResult("Demo Finance");
        var propertyId = await EnsureHotelPropertyDepartmentsAsync(result);
        await EnsureGuestsReservationsFoliosAsync(propertyId, result);
        await EnsureAdvancedFinanceAsync(result);
        await EnsureAccountingDemoAsync(result);
        await EnsureLaborCostingAsync(result);
        await EnsureExecutiveReportingAsync(result);
        await EnsureGroupManagementAsync(result);
        await EnsureDemoAccountsPayableCoverageAsync(result);
        await EnsureDemoGroupCollectionCoverageAsync(result);
        await LogAsync(result, userName);
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<DemoSeedResult> SeedDemoFoodBeverageAsync(string userName)
    {
        var result = new DemoSeedResult("Demo F&B");
        var propertyId = await EnsureHotelPropertyDepartmentsAsync(result);
        await EnsureGuestsReservationsFoliosAsync(propertyId, result);
        await EnsureFoodBeverageAsync(result);
        await LogAsync(result, userName);
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<DemoSeedResult> SeedDemoBanquetAsync(string userName)
    {
        var result = new DemoSeedResult("Demo Banquet");
        await EnsureSalesCrmAsync(result);
        await EnsureBanquetAsync(result);
        await LogAsync(result, userName);
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<DemoSeedResult> SeedDemoInventoryAsync(string userName)
    {
        var result = new DemoSeedResult("Demo Inventory");
        await EnsureInventoryPurchasingAsync(result);
        await EnsureDemoStockAdjustmentCoverageAsync(result);
        await LogAsync(result, userName);
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<DemoSeedResult> SeedFullDemoDatasetAsync(string userName)
    {
        var result = new DemoSeedResult("Full Demo Dataset");
        await EnsureDemoModeSettingAsync(result, userName);
        var propertyId = await EnsureHotelPropertyDepartmentsAsync(result);
        await EnsureRoomTypesAndRoomsAsync(propertyId, result);
        await EnsureDemoUsersAsync(result);
        await EnsureGuestsReservationsFoliosAsync(propertyId, result);
        await EnsureHousekeepingAsync(result);
        await EnsureFoodBeverageAsync(result);
        await EnsureSalesCrmAsync(result);
        await EnsureBanquetAsync(result);
        await EnsureRevenueAsync(result);
        await EnsureBookingEngineAsync(result);
        await EnsureGuestPortalDemoAsync(result);
        await EnsureInventoryPurchasingAsync(result);
        await EnsureAdvancedFinanceAsync(result);
        await EnsureAccountingDemoAsync(result);
        await EnsureLaborCostingAsync(result);
        await EnsureExecutiveReportingAsync(result);
        await EnsureManagementAiAsync(result);
        await EnsureGroupManagementAsync(result);
        await EnsureDemoAccountsPayableCoverageAsync(result);
        await EnsureDemoStockAdjustmentCoverageAsync(result);
        await EnsureDemoGroupCollectionCoverageAsync(result);
        await LogAsync(result, userName);
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<DemoSeedResult> SeedDemoFinanceClosePackAsync(string userName)
    {
        var result = new DemoSeedResult("Demo Finance Close Pack");
        await EnsureDemoModeSettingAsync(result, userName);
        await AccountingSeedData.SeedAsync(context);
        await CashFlowSeedData.SeedAsync(context);

        var propertyId = await EnsureHotelPropertyDepartmentsAsync(result);
        await EnsureRoomTypesAndRoomsAsync(propertyId, result);
        await EnsureAdvancedFinanceAsync(result);
        await EnsureDemoFinanceClosePackAsync(propertyId, userName, result);
        await LogAsync(result, userName);
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<DemoDataStatus> GetStatusAsync()
    {
        return new DemoDataStatus
        {
            DemoModeEnabled = await context.SystemSettings.AsNoTracking().AnyAsync(setting => setting.SettingKey == "DemoModeEnabled" && setting.SettingValue == "true"),
            Hotels = await context.Hotels.AsNoTracking().CountAsync(hotel => hotel.Name.Contains("Vantage Grand")),
            Rooms = await context.Rooms.AsNoTracking().CountAsync(room => room.StatusNotes != null && room.StatusNotes.Contains(DemoMarker)),
            Guests = await context.Guests.AsNoTracking().CountAsync(guest => guest.Email != null && guest.Email.EndsWith("@demo.example")),
            Reservations = await context.Reservations.AsNoTracking().CountAsync(reservation => reservation.ConfirmationNumber.StartsWith("DEMO-")),
            Folios = await context.Folios.AsNoTracking().CountAsync(folio => folio.FolioNumber.StartsWith("DEMO-FOL-")),
            PosOrders = await context.POSOrders.AsNoTracking().CountAsync(order => order.OrderNumber.StartsWith("DEMO-POS-")),
            BanquetEvents = await context.BanquetEvents.AsNoTracking().CountAsync(item => item.Notes != null && item.Notes.Contains(DemoMarker)),
            InventoryItems = await context.InventoryItems.AsNoTracking().CountAsync(item => item.CreatedBy == "DemoDataSeeder"),
            ARInvoices = await context.ARInvoices.AsNoTracking().CountAsync(invoice => invoice.CreatedBy == "DemoDataSeeder"),
            APInvoices = await context.APInvoices.AsNoTracking().CountAsync(invoice => invoice.CreatedBy == "DemoDataSeeder" || invoice.InvoiceNumber.StartsWith("DEMO-")),
            PaymentVouchers = await context.PaymentVouchers.AsNoTracking().CountAsync(voucher => voucher.PreparedBy == "DemoDataSeeder" || voucher.VoucherNumber.StartsWith("DEMO-")),
            StockAdjustments = await context.StockAdjustments.AsNoTracking().CountAsync(adjustment => adjustment.PreparedBy == "DemoDataSeeder" || adjustment.AdjustmentNumber.StartsWith("DEMO-")),
            GroupBookings = await context.GroupBookings.AsNoTracking().CountAsync(group => group.CreatedBy == "DemoDataSeeder" || group.GroupCode.StartsWith("DEMO-")),
            FinanceClosePackRecords = await context.JournalEntries.AsNoTracking().CountAsync(entry => entry.JournalNumber.StartsWith("DEMO-CLOSE-")),
            LaborProfiles = await context.EmployeeCostProfiles.AsNoTracking().CountAsync(employee => employee.CreatedBy == "DemoDataSeeder"),
            ManagementInsights = await context.ManagementInsights.AsNoTracking().CountAsync(insight => insight.Summary.Contains(DemoMarker))
        };
    }

    public async Task<IList<DemoReadinessItem>> GetReadinessAsync()
    {
        var items = new List<DemoReadinessItem>
        {
            await ReadyAsync("Admin Setup", () => context.Hotels.AnyAsync(), () => context.RoomTypes.AnyAsync()),
            await ReadyAsync("Front Office", () => context.Reservations.AnyAsync(), () => context.Guests.AnyAsync()),
            await ReadyAsync("Housekeeping", () => context.HousekeepingTasks.AnyAsync(), () => context.Rooms.AnyAsync(room => room.Status == RoomStatus.Dirty || room.Status == RoomStatus.Clean)),
            await ReadyAsync("Finance", () => context.Folios.AnyAsync(), () => context.Payments.AnyAsync()),
            await ReadyAsync("F&B Service", () => context.POSOrders.AnyAsync(), () => context.MenuItems.AnyAsync()),
            await ReadyAsync("F&B Kitchen", () => context.KitchenStations.AnyAsync(), () => context.POSOrderItems.AnyAsync()),
            await ReadyAsync("Sales CRM", () => context.SalesAccounts.AnyAsync(), () => context.SalesLeads.AnyAsync()),
            await ReadyAsync("Banquet", () => context.BanquetEvents.AnyAsync(), () => context.BanquetEventOrders.AnyAsync()),
            await ReadyAsync("Revenue", () => context.RatePlans.AnyAsync(), () => context.RoomTypeRates.AnyAsync()),
            await ReadyAsync("Booking Engine", () => context.BookingEngineSettings.AnyAsync(), () => context.BookingRequests.AnyAsync()),
            await ReadyAsync("Guest Portal", () => context.GuestPortalSettings.AnyAsync(), () => context.GuestServiceRequests.AnyAsync()),
            await ReadyAsync("Inventory", () => context.InventoryItems.AnyAsync(), () => context.StockMovements.AnyAsync()),
            await ReadyAsync("Purchasing", () => context.PurchaseOrders.AnyAsync(), () => context.ReceivingRecords.AnyAsync()),
            await ReadyAsync("Accounts Receivable", () => context.ARAccounts.AnyAsync(), () => context.ARInvoices.AnyAsync()),
            await ReadyAsync("Accounts Payable", () => context.APInvoices.AnyAsync(), () => context.PaymentVouchers.AnyAsync()),
            await ReadyAsync("Inventory Adjustments", () => context.StockAdjustments.AnyAsync(), () => context.StockAdjustmentItems.AnyAsync()),
            await ReadyAsync("Group Bookings", () => context.GroupBookings.AnyAsync(), () => context.GroupFolios.AnyAsync()),
            await ReadyAsync("Finance Close Pack", () => context.JournalEntries.AnyAsync(entry => entry.JournalNumber.StartsWith("DEMO-CLOSE-")), () => context.BankReconciliations.AnyAsync(item => item.Notes != null && item.Notes.Contains("DEMO-CLOSE"))),
            await ReadyAsync("Labor Costing", () => context.EmployeeCostProfiles.AnyAsync(), () => context.PayrollPeriods.AnyAsync()),
            await ReadyAsync("Management AI", () => context.ManagementDailySummaries.AnyAsync(), () => context.ManagementInsights.AnyAsync()),
            await ReadyAsync("Audit Trail", () => context.AuditLogs.AnyAsync(), () => Task.FromResult(true)),
            await ReadyAsync("System Health", () => context.QATestChecklistItems.AnyAsync(), () => context.DataValidationIssues.AnyAsync())
        };
        return items;
    }

    private async Task EnsureDemoModeSettingAsync(DemoSeedResult result, string userName)
    {
        if (!await context.SystemSettings.AnyAsync(setting => setting.SettingKey == "DemoModeEnabled"))
        {
            context.SystemSettings.Add(new SystemSetting
            {
                SettingKey = "DemoModeEnabled",
                SettingValue = "false",
                Description = "Enables demo labels and sample data indicators.",
                Module = "System",
                IsEditable = true,
                UpdatedAt = DateTime.Now,
                UpdatedBy = userName
            });
            result.Inserted++;
            result.Messages.Add("Added DemoModeEnabled system setting.");
        }
    }

    private async Task<int> EnsureHotelPropertyDepartmentsAsync(DemoSeedResult result)
    {
        var hotel = await context.Hotels.FirstOrDefaultAsync(item => item.Code == "VGH");
        if (hotel is null)
        {
            hotel = new Hotel { Code = "VGH", Name = "Vantage Grand Hotel", LegalName = "Vantage Hospitality - Quezon City Demo" };
            context.Hotels.Add(hotel);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var properties = new[]
        {
            ("VGH-MAIN", "Vantage Grand Hotel - Main Tower", "Quezon City", "Philippines"),
            ("VGH-ANNEX", "Vantage Grand Hotel - Annex", "Quezon City", "Philippines")
        };
        foreach (var (code, name, city, country) in properties)
        {
            if (!await context.Properties.AnyAsync(item => item.Code == code))
            {
                context.Properties.Add(new Property
                {
                    HotelId = hotel.Id,
                    Code = code,
                    Name = name,
                    AddressLine1 = "Vantage Avenue",
                    City = city,
                    Country = country
                });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var propertyId = await context.Properties.Where(item => item.Code == "VGH-MAIN").Select(item => item.Id).FirstAsync();
        var departments = new[]
        {
            ("FO", "Front Office"), ("HK", "Housekeeping"), ("FIN", "Finance"), ("FB", "Food and Beverage"),
            ("KIT", "Kitchen"), ("SLS", "Sales"), ("BNQ", "Banquet"), ("REV", "Revenue"),
            ("PUR", "Purchasing"), ("INV", "Inventory"), ("MGT", "Management"), ("ENG", "Engineering")
        };
        foreach (var (code, name) in departments)
        {
            if (!await context.Departments.AnyAsync(item => item.PropertyId == propertyId && item.Code == code))
            {
                context.Departments.Add(new Department { PropertyId = propertyId, Code = code, Name = name, IsActive = true });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();
        return propertyId;
    }

    private async Task EnsureRoomTypesAndRoomsAsync(int propertyId, DemoSeedResult result)
    {
        var roomTypes = new[]
        {
            ("STD", "Standard Room", 2500m, 2),
            ("DLX", "Deluxe Room", 3800m, 3),
            ("EXE", "Executive Room", 5200m, 3),
            ("FAM", "Family Room", 6500m, 5),
            ("JS", "Junior Suite", 7800m, 3),
            ("PS", "Presidential Suite", 18000m, 4)
        };
        foreach (var (code, name, rate, occupancy) in roomTypes)
        {
            if (!await context.RoomTypes.AnyAsync(item => item.PropertyId == propertyId && item.Code == code))
            {
                context.RoomTypes.Add(new RoomType
                {
                    PropertyId = propertyId,
                    Code = code,
                    Name = name,
                    BaseRate = rate,
                    MaxOccupancy = occupancy,
                    Description = $"{name} demo setup. {DemoMarker}",
                    IsActive = true
                });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var roomTypeIds = await context.RoomTypes.Where(item => item.PropertyId == propertyId).ToDictionaryAsync(item => item.Code, item => item.Id);
        var statuses = new[] { RoomStatus.Available, RoomStatus.Occupied, RoomStatus.Dirty, RoomStatus.Clean, RoomStatus.Inspected, RoomStatus.Maintenance, RoomStatus.OutOfOrder };
        var rooms = new List<(string Number, string Floor, string TypeCode)>
        {
            ("201", "2", "STD"), ("202", "2", "STD"), ("203", "2", "STD"), ("204", "2", "STD"), ("205", "2", "STD"),
            ("301", "3", "DLX"), ("302", "3", "DLX"), ("303", "3", "DLX"), ("304", "3", "DLX"), ("305", "3", "DLX"),
            ("401", "4", "EXE"), ("402", "4", "EXE"), ("403", "4", "EXE"), ("404", "4", "FAM"), ("405", "4", "FAM"),
            ("501", "5", "JS"), ("502", "5", "JS"), ("503", "5", "PS")
        };

        var index = 0;
        foreach (var room in rooms)
        {
            if (!await context.Rooms.AnyAsync(item => item.PropertyId == propertyId && item.RoomNumber == room.Number))
            {
                context.Rooms.Add(new Room
                {
                    PropertyId = propertyId,
                    RoomTypeId = roomTypeIds[room.TypeCode],
                    RoomNumber = room.Number,
                    Floor = room.Floor,
                    Status = statuses[index % statuses.Length],
                    StatusNotes = DemoMarker,
                    IsActive = true
                });
                result.Inserted++;
            }
            index++;
        }
        await context.SaveChangesAsync();
    }

    private async Task EnsureDemoUsersAsync(DemoSeedResult result)
    {
        foreach (var role in PmsRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                result.Inserted++;
            }
        }

        var users = new[]
        {
            ("gm@vantagepms.demo", PmsRoles.GeneralManager),
            ("frontoffice@vantagepms.demo", PmsRoles.FrontOfficeManager),
            ("frontdesk@vantagepms.demo", PmsRoles.FrontDesk),
            ("housekeeping@vantagepms.demo", PmsRoles.HousekeepingSupervisor),
            ("finance@vantagepms.demo", PmsRoles.FinanceManager),
            ("cashier@vantagepms.demo", PmsRoles.Cashier),
            ("fb@vantagepms.demo", PmsRoles.FBManager),
            ("kitchen@vantagepms.demo", PmsRoles.Chef),
            ("sales@vantagepms.demo", PmsRoles.SalesManager),
            ("banquet@vantagepms.demo", PmsRoles.BanquetManager),
            ("revenue@vantagepms.demo", PmsRoles.RevenueManager),
            ("inventory@vantagepms.demo", PmsRoles.InventoryManager)
        };

        foreach (var (email, role) in users)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var created = await userManager.CreateAsync(user, DemoUserPassword);
                if (created.Succeeded)
                {
                    result.Inserted++;
                }
                else
                {
                    result.Messages.Add($"Could not create {email}: {string.Join(" ", created.Errors.Select(error => error.Description))}");
                    continue;
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
                result.Inserted++;
            }

            await EnsureDemoHotelAccessAsync(user, role, result);
        }
    }

    private async Task EnsureDemoHotelAccessAsync(IdentityUser user, string role, DemoSeedResult result)
    {
        var hotelId = await context.Hotels
            .Where(hotel => hotel.Code == "VGH")
            .Select(hotel => (int?)hotel.Id)
            .FirstOrDefaultAsync();

        if (hotelId is null)
        {
            return;
        }

        var existing = await context.HotelUserAccesses
            .FirstOrDefaultAsync(access => access.UserId == user.Id && access.HotelId == hotelId.Value);

        if (existing is null)
        {
            context.HotelUserAccesses.Add(new HotelUserAccess
            {
                UserId = user.Id,
                HotelId = hotelId.Value,
                RoleName = role,
                IsDefaultCompany = true,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = "DemoDataSeeder"
            });
            result.Inserted++;
            return;
        }

        if (!existing.IsActive)
        {
            existing.IsActive = true;
            result.Inserted++;
        }
    }

    private async Task EnsureGuestsReservationsFoliosAsync(int propertyId, DemoSeedResult result)
    {
        var guests = new[]
        {
            ("Juan", "Dela Cruz", "juan.delacruz@demo.example", "+63 917 100 0001"),
            ("Maria", "Santos", "maria.santos@demo.example", "+63 917 100 0002"),
            ("Carlo", "Reyes", "carlo.reyes@demo.example", "+63 917 100 0003"),
            ("Angela", "Cruz", "angela.cruz@demo.example", "+63 917 100 0004"),
            ("Miguel", "Tan", "miguel.tan@demo.example", "+63 917 100 0005"),
            ("Sofia", "Lim", "sofia.lim@demo.example", "+63 917 100 0006"),
            ("Robert", "Chua", "robert.chua@demo.example", "+63 917 100 0007"),
            ("Patricia", "Garcia", "patricia.garcia@demo.example", "+63 917 100 0008"),
            ("Daniel", "Mendoza", "daniel.mendoza@demo.example", "+63 917 100 0009"),
            ("Isabella", "Navarro", "isabella.navarro@demo.example", "+63 917 100 0010")
        };
        foreach (var guest in guests)
        {
            if (!await context.Guests.AnyAsync(item => item.Email == guest.Item3))
            {
                context.Guests.Add(new Guest
                {
                    FirstName = guest.Item1,
                    LastName = guest.Item2,
                    Email = guest.Item3,
                    PhoneNumber = guest.Item4,
                    AddressLine1 = "Demo address, Quezon City",
                    City = "Quezon City",
                    Country = "Philippines"
                });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var guestList = await context.Guests.Where(item => item.Email != null && item.Email.EndsWith("@demo.example")).OrderBy(item => item.Id).ToListAsync();
        var rooms = await context.Rooms.Include(item => item.RoomType).Where(item => item.PropertyId == propertyId).OrderBy(item => item.RoomNumber).ToListAsync();
        if (guestList.Count == 0 || rooms.Count == 0)
        {
            return;
        }

        var today = DateTime.Today;
        var reservations = new List<(string Code, int GuestIndex, int RoomIndex, DateTime Arrival, DateTime Departure, ReservationStatus Status)>
        {
            ("DEMO-ARR-001", 0, 0, today, today.AddDays(2), ReservationStatus.Reserved),
            ("DEMO-ARR-002", 1, 1, today, today.AddDays(1), ReservationStatus.Reserved),
            ("DEMO-ARR-003", 2, 2, today, today.AddDays(3), ReservationStatus.Reserved),
            ("DEMO-ARR-004", 3, 3, today, today.AddDays(2), ReservationStatus.Reserved),
            ("DEMO-ARR-005", 4, 4, today, today.AddDays(1), ReservationStatus.Reserved),
            ("DEMO-INH-001", 5, 5, today.AddDays(-2), today.AddDays(2), ReservationStatus.CheckedIn),
            ("DEMO-INH-002", 6, 6, today.AddDays(-1), today.AddDays(3), ReservationStatus.CheckedIn),
            ("DEMO-INH-003", 7, 7, today.AddDays(-3), today.AddDays(1), ReservationStatus.CheckedIn),
            ("DEMO-INH-004", 8, 8, today.AddDays(-1), today.AddDays(4), ReservationStatus.CheckedIn),
            ("DEMO-INH-005", 9, 9, today.AddDays(-2), today.AddDays(1), ReservationStatus.CheckedIn),
            ("DEMO-INH-006", 0, 10, today.AddDays(-1), today.AddDays(2), ReservationStatus.CheckedIn),
            ("DEMO-DEP-001", 1, 11, today.AddDays(-2), today, ReservationStatus.CheckedOut),
            ("DEMO-DEP-002", 2, 12, today.AddDays(-3), today, ReservationStatus.CheckedOut),
            ("DEMO-DEP-003", 3, 13, today.AddDays(-1), today, ReservationStatus.CheckedOut),
            ("DEMO-DEP-004", 4, 14, today.AddDays(-2), today, ReservationStatus.CheckedOut),
            ("DEMO-FUT-001", 5, 15, today.AddDays(3), today.AddDays(5), ReservationStatus.Reserved),
            ("DEMO-FUT-002", 6, 16, today.AddDays(5), today.AddDays(8), ReservationStatus.Reserved),
            ("DEMO-FUT-003", 7, 17, today.AddDays(7), today.AddDays(10), ReservationStatus.Reserved),
            ("DEMO-CAN-001", 8, 0, today.AddDays(2), today.AddDays(4), ReservationStatus.Cancelled),
            ("DEMO-CAN-002", 9, 1, today.AddDays(4), today.AddDays(6), ReservationStatus.Cancelled),
            ("DEMO-NOS-001", 0, 2, today.AddDays(-1), today.AddDays(1), ReservationStatus.NoShow)
        };

        foreach (var reservationData in reservations)
        {
            if (!await context.Reservations.AnyAsync(item => item.ConfirmationNumber == reservationData.Code))
            {
                var room = rooms[reservationData.RoomIndex % rooms.Count];
                context.Reservations.Add(new Reservation
                {
                    PropertyId = propertyId,
                    GuestId = guestList[reservationData.GuestIndex % guestList.Count].Id,
                    RoomId = reservationData.Status == ReservationStatus.Cancelled || reservationData.Status == ReservationStatus.NoShow ? null : room.Id,
                    RoomTypeId = room.RoomTypeId,
                    ConfirmationNumber = reservationData.Code,
                    ArrivalDate = reservationData.Arrival,
                    DepartureDate = reservationData.Departure,
                    ActualCheckInDate = reservationData.Status is ReservationStatus.CheckedIn or ReservationStatus.CheckedOut ? reservationData.Arrival.AddHours(14) : null,
                    ActualCheckOutDate = reservationData.Status == ReservationStatus.CheckedOut ? reservationData.Departure.AddHours(10) : null,
                    RateAmount = room.RoomType?.BaseRate ?? 3500,
                    Adults = 2,
                    Children = reservationData.Code.Contains("FUT") ? 1 : 0,
                    Status = reservationData.Status
                });
                if (reservationData.Status == ReservationStatus.CheckedIn)
                {
                    room.Status = RoomStatus.Occupied;
                }
                else if (reservationData.Status == ReservationStatus.CheckedOut)
                {
                    room.Status = RoomStatus.Dirty;
                }
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var activeReservations = await context.Reservations
            .Include(item => item.Guest)
            .Where(item => item.ConfirmationNumber.StartsWith("DEMO-") && (item.Status == ReservationStatus.CheckedIn || item.Status == ReservationStatus.CheckedOut))
            .ToListAsync();
        foreach (var reservation in activeReservations)
        {
            var folio = await context.Folios.Include(item => item.Items).Include(item => item.Payments).FirstOrDefaultAsync(item => item.ReservationId == reservation.Id);
            if (folio is null)
            {
                folio = new Folio
                {
                    PropertyId = propertyId,
                    ReservationId = reservation.Id,
                    GuestId = reservation.GuestId,
                    FolioNumber = $"DEMO-FOL-{reservation.ConfirmationNumber}",
                    Status = reservation.Status == ReservationStatus.CheckedOut ? FolioStatus.Closed : FolioStatus.Open,
                    ClosedAtUtc = reservation.Status == ReservationStatus.CheckedOut ? DateTime.UtcNow : null
                };
                context.Folios.Add(folio);
                result.Inserted++;
                await context.SaveChangesAsync();
            }

            if (!folio.Items.Any())
            {
                var nights = Math.Max(1, (reservation.DepartureDate.Date - reservation.ArrivalDate.Date).Days);
                var roomCharge = reservation.RateAmount * nights;
                var charges = new[]
                {
                    ("Room Charge", "ROOM", roomCharge),
                    ("Food and Beverage", "FB", 1450m),
                    ("Laundry", "MISC", 350m),
                    ("Transportation", "MISC", 900m),
                    ("Service Charge", "SC", 450m),
                    ("Tax", "TAX", 650m)
                };
                foreach (var charge in charges)
                {
                    context.FolioItems.Add(new FolioItem
                    {
                        FolioId = folio.Id,
                        Description = $"{charge.Item1} - {DemoMarker}",
                        ChargeCode = charge.Item2,
                        Quantity = 1,
                        UnitPrice = charge.Item3,
                        Amount = charge.Item3,
                        PostedBy = "DemoDataSeeder",
                        PostingDate = DateTime.Now.AddHours(-charges.ToList().IndexOf(charge))
                    });
                    result.Inserted++;
                }
            }

            if (!folio.Payments.Any())
            {
                var totalCharges = await context.FolioItems.Where(item => item.FolioId == folio.Id && !item.IsVoided).SumAsync(item => item.Amount);
                var paymentAmount = reservation.ConfirmationNumber.EndsWith("001") || reservation.Status == ReservationStatus.CheckedOut ? totalCharges : totalCharges / 2;
                context.Payments.Add(new Payment
                {
                    FolioId = folio.Id,
                    Amount = paymentAmount,
                    PaymentMethod = reservation.Status == ReservationStatus.CheckedOut ? "Credit Card" : "Cash",
                    PaymentDate = DateTime.Now,
                    ReferenceNumber = $"DEMO-PAY-{reservation.Id:0000}",
                    Notes = DemoMarker,
                    Status = PaymentStatus.Completed
                });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();
    }

    private async Task EnsureHousekeepingAsync(DemoSeedResult result)
    {
        var taskTemplates = new[]
        {
            ("203", "Clean Room 203", HousekeepingTaskStatus.Open, HousekeepingTaskPriority.High),
            ("304", "Inspect Room 304", HousekeepingTaskStatus.InProgress, HousekeepingTaskPriority.Normal),
            ("401", "Make Up Room 401", HousekeepingTaskStatus.Open, HousekeepingTaskPriority.Normal),
            ("501", "Turndown Service 501", HousekeepingTaskStatus.Completed, HousekeepingTaskPriority.Low),
            ("302", "Extra Towels 302", HousekeepingTaskStatus.Open, HousekeepingTaskPriority.High),
            ("503", "VIP Arrival Setup 503", HousekeepingTaskStatus.InProgress, HousekeepingTaskPriority.Urgent)
        };
        foreach (var template in taskTemplates)
        {
            var room = await context.Rooms.FirstOrDefaultAsync(item => item.RoomNumber == template.Item1);
            if (room is not null && !await context.HousekeepingTasks.AnyAsync(item => item.RoomId == room.Id && item.Notes != null && item.Notes.Contains(template.Item2)))
            {
                context.HousekeepingTasks.Add(new HousekeepingTask
                {
                    RoomId = room.Id,
                    AssignedTo = "housekeeping@vantagepms.demo",
                    TaskStatus = template.Item3,
                    Priority = template.Item4,
                    Notes = $"{template.Item2}. {DemoMarker}",
                    StartedAt = template.Item3 is HousekeepingTaskStatus.InProgress or HousekeepingTaskStatus.Completed ? DateTime.Now.AddHours(-1) : null,
                    CompletedAt = template.Item3 == HousekeepingTaskStatus.Completed ? DateTime.Now.AddMinutes(-20) : null
                });
                result.Inserted++;
            }
        }
    }

    private async Task EnsureFoodBeverageAsync(DemoSeedResult result)
    {
        var stations = new[] { "Hot Kitchen", "Cold Kitchen", "Pastry", "Bar", "Room Service" };
        foreach (var station in stations)
        {
            if (!await context.KitchenStations.AnyAsync(item => item.Name == station))
            {
                context.KitchenStations.Add(new KitchenStation { Name = station, Description = $"{station} demo station. {DemoMarker}", IsActive = true });
                result.Inserted++;
            }
        }
        var outlets = new[] { ("Vantage Cafe", OutletType.Cafe), ("Sky Lounge Bar", OutletType.Bar), ("Main Restaurant", OutletType.Restaurant), ("Room Service", OutletType.RoomService) };
        foreach (var outlet in outlets)
        {
            if (!await context.Outlets.AnyAsync(item => item.Name == outlet.Item1))
            {
                context.Outlets.Add(new Outlet { Name = outlet.Item1, OutletType = outlet.Item2, IsActive = true });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var restaurantId = await context.Outlets.Where(item => item.Name == "Main Restaurant").Select(item => item.Id).FirstAsync();
        var cafeId = await context.Outlets.Where(item => item.Name == "Vantage Cafe").Select(item => item.Id).FirstAsync();
        var barId = await context.Outlets.Where(item => item.Name == "Sky Lounge Bar").Select(item => item.Id).FirstAsync();
        foreach (var table in Enumerable.Range(1, 10).Select(i => ("T" + i, restaurantId)).Concat(Enumerable.Range(1, 5).Select(i => ("C" + i, cafeId))).Concat(Enumerable.Range(1, 5).Select(i => ("B" + i, barId))))
        {
            if (!await context.DiningTables.AnyAsync(item => item.OutletId == table.Item2 && item.TableName == table.Item1))
            {
                context.DiningTables.Add(new DiningTable { OutletId = table.Item2, TableName = table.Item1, SeatingCapacity = table.Item1.StartsWith("T") ? 4 : 2, Status = DiningTableStatus.Available });
                result.Inserted++;
            }
        }

        var categories = new[] { "Breakfast", "Appetizers", "Main Course", "Desserts", "Beverages", "Bar", "Room Service" };
        var sort = 10;
        foreach (var category in categories)
        {
            if (!await context.MenuCategories.AnyAsync(item => item.Name == category))
            {
                context.MenuCategories.Add(new MenuCategory { Name = category, SortOrder = sort, IsActive = true });
                result.Inserted++;
            }
            sort += 10;
        }
        await context.SaveChangesAsync();

        var categoryIds = await context.MenuCategories.ToDictionaryAsync(item => item.Name, item => item.Id);
        var stationIds = await context.KitchenStations.ToDictionaryAsync(item => item.Name, item => item.Id);
        var menuItems = new[]
        {
            ("Filipino Breakfast", "Breakfast", "Hot Kitchen", 420m), ("Continental Breakfast", "Breakfast", "Cold Kitchen", 380m),
            ("Clubhouse Sandwich", "Room Service", "Room Service", 320m), ("Chicken Alfredo", "Main Course", "Hot Kitchen", 420m),
            ("Beef Salpicao", "Main Course", "Hot Kitchen", 580m), ("Caesar Salad", "Appetizers", "Cold Kitchen", 280m),
            ("Mango Cheesecake", "Desserts", "Pastry", 260m), ("Brewed Coffee", "Beverages", "Bar", 150m),
            ("Iced Tea", "Beverages", "Bar", 140m), ("Fresh Mango Shake", "Beverages", "Bar", 180m),
            ("House Wine", "Bar", "Bar", 350m), ("Local Beer", "Bar", "Bar", 180m)
        };
        foreach (var item in menuItems)
        {
            if (!await context.MenuItems.AnyAsync(menuItem => menuItem.Name == item.Item1))
            {
                context.MenuItems.Add(new MenuItem
                {
                    MenuCategoryId = categoryIds[item.Item2],
                    KitchenStationId = stationIds[item.Item3],
                    Name = item.Item1,
                    Description = $"{item.Item1} demo menu item. {DemoMarker}",
                    Price = item.Item4,
                    IsAvailable = true,
                    IsTaxable = true,
                    IsServiceChargeable = true
                });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        if (!await context.POSOrders.AnyAsync(item => item.OrderNumber.StartsWith("DEMO-POS-")))
        {
            var menu = await context.MenuItems.OrderBy(item => item.Id).Take(6).ToListAsync();
            var tables = await context.DiningTables.OrderBy(item => item.Id).Take(4).ToListAsync();
            var checkedIn = await context.Reservations.Include(item => item.Guest).Where(item => item.Status == ReservationStatus.CheckedIn).Take(3).ToListAsync();
            for (var i = 0; i < 8; i++)
            {
                var outletId = i == 3 || i == 4 ? await context.Outlets.Where(item => item.Name == "Room Service").Select(item => item.Id).FirstAsync() : restaurantId;
                var order = new POSOrder
                {
                    OutletId = outletId,
                    DiningTableId = i < 3 && i < tables.Count ? tables[i].Id : null,
                    ReservationId = i >= 3 && checkedIn.Count > 0 ? checkedIn[i % checkedIn.Count].Id : null,
                    GuestId = i >= 3 && checkedIn.Count > 0 ? checkedIn[i % checkedIn.Count].GuestId : null,
                    OrderNumber = $"DEMO-POS-{i + 1:000}",
                    OrderType = i >= 3 ? POSOrderType.RoomService : POSOrderType.DineIn,
                    OrderStatus = i < 3 ? POSOrderStatus.Open : i < 6 ? POSOrderStatus.SentToKitchen : POSOrderStatus.Closed,
                    PaymentStatus = i == 7 ? POSPaymentStatus.ChargedToRoom : i >= 6 ? POSPaymentStatus.Paid : POSPaymentStatus.Unpaid,
                    CreatedBy = "DemoDataSeeder",
                    Notes = DemoMarker,
                    ClosedAt = i >= 6 ? DateTime.Now.AddMinutes(-30) : null
                };
                foreach (var menuItem in menu.Skip(i % 3).Take(2))
                {
                    var lineTotal = menuItem.Price * 1;
                    order.Items.Add(new POSOrderItem
                    {
                        MenuItemId = menuItem.Id,
                        Quantity = 1,
                        UnitPrice = menuItem.Price,
                        LineTotal = lineTotal,
                        Notes = DemoMarker,
                        ItemStatus = i == 0 ? POSOrderItemStatus.New : i == 1 ? POSOrderItemStatus.Preparing : i == 2 ? POSOrderItemStatus.Ready : POSOrderItemStatus.Served,
                        SentToKitchenAt = i <= 2 ? DateTime.Now.AddMinutes(i == 0 ? -25 : -10) : DateTime.Now.AddHours(-1),
                        PreparingAt = i >= 1 ? DateTime.Now.AddMinutes(-8) : null,
                        ReadyAt = i >= 2 ? DateTime.Now.AddMinutes(-3) : null,
                        ServedAt = i >= 3 ? DateTime.Now.AddMinutes(-1) : null
                    });
                    order.SubTotal += lineTotal;
                }
                order.ServiceCharge = Math.Round(order.SubTotal * 0.10m, 2);
                order.TaxAmount = Math.Round(order.SubTotal * 0.12m, 2);
                order.TotalAmount = order.SubTotal + order.ServiceCharge + order.TaxAmount - order.DiscountAmount;
                context.POSOrders.Add(order);
                result.Inserted++;
            }
            await context.SaveChangesAsync();
            var chargeToRoomOrder = await context.POSOrders.Include(item => item.Outlet).FirstOrDefaultAsync(item => item.OrderNumber == "DEMO-POS-008");
            if (chargeToRoomOrder?.ReservationId is not null)
            {
                var folio = await context.Folios.FirstOrDefaultAsync(item => item.ReservationId == chargeToRoomOrder.ReservationId.Value);
                if (folio is not null && !await context.FolioItems.AnyAsync(item => item.FolioId == folio.Id && item.Description.Contains(chargeToRoomOrder.OrderNumber)))
                {
                    context.FolioItems.Add(new FolioItem
                    {
                        FolioId = folio.Id,
                        Description = $"F&B Charge - {chargeToRoomOrder.Outlet?.Name} - Order #{chargeToRoomOrder.OrderNumber}",
                        ChargeCode = "FB",
                        Quantity = 1,
                        UnitPrice = chargeToRoomOrder.TotalAmount,
                        Amount = chargeToRoomOrder.TotalAmount,
                        PostedBy = "DemoDataSeeder",
                        PostingDate = DateTime.Now
                    });
                    result.Inserted++;
                }
            }
        }
    }

    private async Task EnsureSalesCrmAsync(DemoSeedResult result)
    {
        var accounts = new[]
        {
            ("ABC Corporation", SalesAccountType.Corporate), ("XYZ Travel and Tours", SalesAccountType.TravelAgency),
            ("Quezon City LGU", SalesAccountType.Government), ("Santos Wedding Events", SalesAccountType.EventClient),
            ("GlobalTech Solutions", SalesAccountType.Corporate), ("Manila Medical Group", SalesAccountType.Corporate)
        };
        foreach (var account in accounts)
        {
            if (!await context.SalesAccounts.AnyAsync(item => item.AccountName == account.Item1))
            {
                context.SalesAccounts.Add(new SalesAccount
                {
                    AccountName = account.Item1,
                    AccountType = account.Item2,
                    Address = "Metro Manila",
                    Phone = "+63 2 8888 1000",
                    Email = account.Item1.Replace(" ", ".").ToLowerInvariant() + "@demo.example",
                    CreditLimit = 250000,
                    Notes = DemoMarker,
                    CreatedBy = "DemoDataSeeder"
                });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var accountIds = await context.SalesAccounts.ToDictionaryAsync(item => item.AccountName, item => item.Id);
        var contacts = new[] { ("ABC Corporation", "Maria Santos", "HR Manager"), ("XYZ Travel and Tours", "Carlo Reyes", "Reservations Officer"), ("Santos Wedding Events", "Angela Cruz", "Events Coordinator"), ("GlobalTech Solutions", "Patrick Lim", "Admin Manager"), ("Manila Medical Group", "Denise Chua", "Procurement Lead") };
        foreach (var contact in contacts)
        {
            if (!await context.ContactPersons.AnyAsync(item => item.FullName == contact.Item2))
            {
                context.ContactPersons.Add(new ContactPerson { SalesAccountId = accountIds[contact.Item1], FullName = contact.Item2, Position = contact.Item3, Mobile = "+63 917 200 0000", Email = contact.Item2.Replace(" ", ".").ToLowerInvariant() + "@demo.example", IsPrimary = true, Notes = DemoMarker });
                result.Inserted++;
            }
        }

        var leads = new[] { "ABC Corporate Room Nights 2026", "Santos-Reyes Wedding Reception", "Quezon City Leadership Seminar", "GlobalTech Annual Conference", "Manila Medical Board Meeting" };
        foreach (var leadName in leads)
        {
            if (!await context.SalesLeads.AnyAsync(item => item.LeadName == leadName))
            {
                var accountId = accountIds.Values.First();
                context.SalesLeads.Add(new SalesLead
                {
                    SalesAccountId = accountId,
                    LeadName = leadName,
                    LeadSource = "Demo Pipeline",
                    EstimatedValue = leadName.Contains("Wedding") ? 350000 : 180000,
                    Status = leadName.Contains("ABC") ? SalesLeadStatus.Negotiation : SalesLeadStatus.ProposalSent,
                    ExpectedCloseDate = DateTime.Today.AddDays(30),
                    AssignedTo = "sales@vantagepms.demo",
                    Notes = DemoMarker,
                    CreatedBy = "DemoDataSeeder"
                });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var firstLead = await context.SalesLeads.OrderBy(item => item.Id).FirstOrDefaultAsync();
        if (firstLead is not null && !await context.SalesActivities.AnyAsync(item => item.Notes != null && item.Notes.Contains(DemoMarker)))
        {
            foreach (var activity in new[] { SalesActivityType.Call, SalesActivityType.SiteInspection, SalesActivityType.ProposalFollowUp, SalesActivityType.ContractFollowUp, SalesActivityType.Meeting })
            {
                context.SalesActivities.Add(new SalesActivity { SalesLeadId = firstLead.Id, SalesAccountId = firstLead.SalesAccountId, ActivityType = activity, ActivityDate = DateTime.Now.AddDays(-(int)activity), Notes = $"{activity} demo activity. {DemoMarker}", NextFollowUpDate = DateTime.Today.AddDays(7), CreatedBy = "DemoDataSeeder" });
                result.Inserted++;
            }
        }
    }

    private async Task EnsureBanquetAsync(DemoSeedResult result)
    {
        var rooms = new[] { ("Grand Ballroom", "Level 2", 350, 90000m), ("Emerald Hall", "Level 3", 120, 45000m), ("Boardroom A", "Level 4", 20, 12000m), ("Garden Deck", "Podium", 180, 65000m), ("Sky Function Room", "Level 5", 80, 35000m) };
        foreach (var room in rooms)
        {
            if (!await context.FunctionRooms.AnyAsync(item => item.Name == room.Item1))
            {
                context.FunctionRooms.Add(new FunctionRoom { Name = room.Item1, Location = room.Item2, Capacity = room.Item3, Rate = room.Item4, IsActive = true, Notes = DemoMarker });
                result.Inserted++;
            }
        }
        var packages = new[] { ("Wedding Package", 2200m, 100), ("Corporate Seminar Package", 1600m, 40), ("Birthday Package", 1300m, 50), ("Government Meeting Package", 1200m, 30), ("Executive Meeting Package", 1800m, 15) };
        foreach (var package in packages)
        {
            if (!await context.BanquetPackages.AnyAsync(item => item.PackageName == package.Item1))
            {
                context.BanquetPackages.Add(new BanquetPackage { PackageName = package.Item1, PricePerPax = package.Item2, MinimumPax = package.Item3, Description = $"{package.Item1}. {DemoMarker}", IsActive = true });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var functionRooms = await context.FunctionRooms.ToListAsync();
        var banquetPackages = await context.BanquetPackages.ToListAsync();
        var events = new[] { ("Santos-Reyes Wedding Reception", BanquetEventStatus.Confirmed, BanquetEventType.Wedding), ("ABC Corporation Seminar", BanquetEventStatus.Tentative, BanquetEventType.Seminar), ("Quezon City LGU Planning Session", BanquetEventStatus.Confirmed, BanquetEventType.GovernmentEvent), ("GlobalTech Product Launch", BanquetEventStatus.Inquiry, BanquetEventType.CorporateEvent), ("Navarro Birthday Celebration", BanquetEventStatus.Completed, BanquetEventType.Birthday) };
        for (var i = 0; i < events.Length; i++)
        {
            var eventData = events[i];
            if (!await context.BanquetEvents.AnyAsync(item => item.EventName == eventData.Item1))
            {
                var banquetEvent = new BanquetEvent
                {
                    EventName = eventData.Item1,
                    ClientName = eventData.Item1.Split(' ')[0],
                    ContactNumber = "+63 917 300 0000",
                    Email = "events@demo.example",
                    FunctionRoomId = functionRooms[i % functionRooms.Count].Id,
                    BanquetPackageId = banquetPackages[i % banquetPackages.Count].Id,
                    EventDate = DateTime.Today.AddDays(i - 1),
                    StartTime = new TimeSpan(9 + i, 0, 0),
                    EndTime = new TimeSpan(13 + i, 0, 0),
                    ExpectedPax = 80 + (i * 20),
                    GuaranteedPax = 70 + (i * 20),
                    EventStatus = eventData.Item2,
                    EventType = eventData.Item3,
                    Notes = DemoMarker,
                    CreatedBy = "DemoDataSeeder"
                };
                banquetEvent.Charges.Add(new BanquetCharge { Description = "Package charge", Quantity = banquetEvent.GuaranteedPax, UnitPrice = banquetPackages[i % banquetPackages.Count].PricePerPax, Amount = banquetEvent.GuaranteedPax * banquetPackages[i % banquetPackages.Count].PricePerPax, ChargeDate = banquetEvent.EventDate });
                banquetEvent.Charges.Add(new BanquetCharge { Description = "LED wall rental", Quantity = 1, UnitPrice = 18000, Amount = 18000, ChargeDate = banquetEvent.EventDate });
                if (eventData.Item2 == BanquetEventStatus.Confirmed)
                {
                    banquetEvent.BanquetEventOrder = new BanquetEventOrder
                    {
                        BEODate = DateTime.Today,
                        MenuDetails = "Chef's plated menu with coffee and tea service.",
                        SetupInstructions = "Round tables, stage front, registration table.",
                        EquipmentRequirements = "LED wall, two wireless microphones, projector.",
                        ServiceInstructions = "Dedicated banquet captain and plated service.",
                        KitchenInstructions = "Serve hot courses at 12:00 PM.",
                        BillingInstructions = "Bill to client account after event.",
                        SpecialInstructions = DemoMarker,
                        PreparedBy = "banquet@vantagepms.demo",
                        ApprovedBy = "gm@vantagepms.demo",
                        Status = BanquetEventOrderStatus.Approved
                    };
                }
                context.BanquetEvents.Add(banquetEvent);
                result.Inserted++;
            }
        }
    }

    private async Task EnsureRevenueAsync(DemoSeedResult result)
    {
        var plans = new[] { ("BAR", "Best Available Rate", false, false), ("CORP", "Corporate Rate", false, true), ("WALKIN", "Walk-in Rate", false, false), ("PKG-BF", "Room with Breakfast", true, false), ("LONGSTAY", "Long Stay Rate", false, false), ("PROMO", "Promotional Rate", false, false) };
        foreach (var plan in plans)
        {
            if (!await context.RatePlans.AnyAsync(item => item.Code == plan.Item1))
            {
                context.RatePlans.Add(new RatePlan { Code = plan.Item1, Name = plan.Item2, Description = $"{plan.Item2}. {DemoMarker}", IsActive = true, IncludesBreakfast = plan.Item3, IsCorporateRate = plan.Item4, CancellationPolicy = "Flexible demo policy", DepositPolicy = "50% deposit for public bookings", CreatedBy = "DemoDataSeeder" });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var activePlans = await context.RatePlans.Where(item => item.IsActive).ToListAsync();
        var roomTypes = await context.RoomTypes.Where(item => item.IsActive).ToListAsync();
        foreach (var roomType in roomTypes)
        {
            foreach (var plan in activePlans.Take(4))
            {
                if (!await context.RoomTypeRates.AnyAsync(item => item.RoomTypeId == roomType.Id && item.RatePlanId == plan.Id))
                {
                    var factor = plan.Code == "CORP" ? 0.90m : plan.Code == "PKG-BF" ? 1.12m : 1m;
                    context.RoomTypeRates.Add(new RoomTypeRate { RoomTypeId = roomType.Id, RatePlanId = plan.Id, BaseRate = Math.Round(roomType.BaseRate * factor, 2), ExtraAdultRate = 850, ExtraChildRate = 450, EffectiveFrom = DateTime.Today.AddMonths(-1), EffectiveTo = DateTime.Today.AddYears(1), IsActive = true });
                    result.Inserted++;
                }
            }
        }
        var bar = await context.RatePlans.FirstOrDefaultAsync(item => item.Code == "BAR");
        if (bar is not null)
        {
            foreach (var roomType in roomTypes.Take(3))
            {
                if (!await context.SeasonalRates.AnyAsync(item => item.RoomTypeId == roomType.Id && item.RatePlanId == bar.Id && item.SeasonName == "Weekend Premium"))
                {
                    context.SeasonalRates.Add(new SeasonalRate { RoomTypeId = roomType.Id, RatePlanId = bar.Id, SeasonName = "Weekend Premium", StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(2), Rate = roomType.BaseRate * 1.15m, ExtraAdultRate = 900, ExtraChildRate = 500, IsActive = true });
                    result.Inserted++;
                }
            }
            var firstRoomType = roomTypes.FirstOrDefault();
            if (firstRoomType is not null && !await context.RateRestrictions.AnyAsync(item => item.RoomTypeId == firstRoomType.Id && item.RestrictionDate == DateTime.Today.AddDays(14)))
            {
                context.RateRestrictions.Add(new RateRestriction { RatePlanId = bar.Id, RoomTypeId = firstRoomType.Id, RestrictionDate = DateTime.Today.AddDays(14), MinimumLengthOfStay = 2, ClosedToArrival = true, StopSell = false, Notes = $"{DemoMarker} closed-to-arrival sample." });
                result.Inserted++;
            }
        }
        foreach (var roomType in roomTypes)
        {
            if (!await context.RoomInventoryControls.AnyAsync(item => item.RoomTypeId == roomType.Id && item.InventoryDate == DateTime.Today.AddDays(7)))
            {
                var total = await context.Rooms.CountAsync(room => room.RoomTypeId == roomType.Id && room.IsActive);
                context.RoomInventoryControls.Add(new RoomInventoryControl { RoomTypeId = roomType.Id, InventoryDate = DateTime.Today.AddDays(7), TotalRooms = total, RoomsToSell = Math.Max(0, total - 1), OverbookingLimit = 1, StopSell = false, Notes = DemoMarker });
                result.Inserted++;
            }
        }
        foreach (var promo in new[] { ("DIRECT10", 10m), ("WEEKEND15", 15m), ("STAY3PAY2", 33m), ("CORP2026", 12m) })
        {
            if (!await context.PromotionCodes.AnyAsync(item => item.Code == promo.Item1))
            {
                context.PromotionCodes.Add(new PromotionCode { Code = promo.Item1, Description = $"{promo.Item1} demo promo. {DemoMarker}", DiscountType = RevenueDiscountType.Percentage, DiscountValue = promo.Item2, ValidFrom = DateTime.Today.AddDays(-15), ValidTo = DateTime.Today.AddMonths(6), IsActive = true, UsageLimit = 100 });
                result.Inserted++;
            }
        }
    }

    private async Task EnsureBookingEngineAsync(DemoSeedResult result)
    {
        var defaultRatePlanId = await context.RatePlans.Where(item => item.Code == "BAR").Select(item => (int?)item.Id).FirstOrDefaultAsync();
        if (!await context.BookingEngineSettings.AnyAsync())
        {
            context.BookingEngineSettings.Add(new BookingEngineSetting { HotelName = "Vantage Grand Hotel", BookingEngineTitle = "Book Your Stay at Vantage Grand Hotel", WelcomeMessage = "Experience modern comfort powered by smarter hotel operations.", ContactEmail = "info@vantagegrandhotel.example", ContactNumber = "+63 2 8888 0000", DefaultRatePlanId = defaultRatePlanId, RequireDeposit = true, DepositPercentage = 50, AllowPromoCodes = true, AllowSpecialRequests = true, IsBookingEngineEnabled = true, TermsAndConditions = DemoMarker, PrivacyPolicy = DemoMarker });
            result.Inserted++;
        }
        var roomTypes = await context.RoomTypes.Where(item => item.IsActive).OrderBy(item => item.BaseRate).ToListAsync();
        var sort = 10;
        foreach (var roomType in roomTypes)
        {
            if (!await context.BookingEngineRoomContents.AnyAsync(item => item.RoomTypeId == roomType.Id))
            {
                context.BookingEngineRoomContents.Add(new BookingEngineRoomContent { RoomTypeId = roomType.Id, DisplayName = roomType.Name, ShortDescription = $"Comfortable {roomType.Name.ToLowerInvariant()} for Vantage Grand Hotel guests.", LongDescription = $"{roomType.Description} Direct booking demo content. {DemoMarker}", ImageUrl = $"/images/demo/rooms/{roomType.Code.ToLowerInvariant()}.jpg", SortOrder = sort, IsVisible = true });
                result.Inserted++;
            }
            sort += 10;
        }
        foreach (var addon in new[] { ("Breakfast Add-on", 550m), ("Airport Transfer", 1800m), ("Extra Bed", 1200m), ("Romantic Room Setup", 2500m), ("Late Checkout Request", 900m) })
        {
            if (!await context.BookingAddOns.AnyAsync(item => item.Name == addon.Item1))
            {
                context.BookingAddOns.Add(new BookingAddOn { Name = addon.Item1, Description = $"{addon.Item1}. {DemoMarker}", Price = addon.Item2, IsPerNight = addon.Item1.Contains("Breakfast"), IsPerPerson = addon.Item1.Contains("Breakfast"), IsActive = true });
                result.Inserted++;
            }
        }
        if (!await context.BookingRequests.AnyAsync(item => item.BookingReference.StartsWith("DEMO-BR-")) && roomTypes.Any())
        {
            var statuses = new[] { BookingRequestStatus.Pending, BookingRequestStatus.Confirmed, BookingRequestStatus.ConvertedToReservation, BookingRequestStatus.Cancelled };
            for (var i = 0; i < statuses.Length; i++)
            {
                context.BookingRequests.Add(new BookingRequest { BookingReference = $"DEMO-BR-{i + 1:000}", GuestFirstName = "Demo", GuestLastName = $"Booker {i + 1}", GuestEmail = $"booking{i + 1}@demo.example", GuestPhone = "+63 917 400 0000", GuestAddress = "Quezon City", CheckInDate = DateTime.Today.AddDays(10 + i), CheckOutDate = DateTime.Today.AddDays(12 + i), AdultCount = 2, ChildCount = i % 2, RoomTypeId = roomTypes[i % roomTypes.Count].Id, RatePlanId = defaultRatePlanId, RoomRate = roomTypes[i % roomTypes.Count].BaseRate, TotalRoomAmount = roomTypes[i % roomTypes.Count].BaseRate * 2, DepositRequired = true, DepositAmount = roomTypes[i % roomTypes.Count].BaseRate, SpecialRequests = DemoMarker, BookingStatus = statuses[i], ConfirmedAt = statuses[i] == BookingRequestStatus.Confirmed ? DateTime.Now : null, CancelledAt = statuses[i] == BookingRequestStatus.Cancelled ? DateTime.Now : null });
                result.Inserted++;
            }
        }
    }

    private async Task EnsureGuestPortalDemoAsync(DemoSeedResult result)
    {
        if (!await context.GuestPortalSettings.AnyAsync())
        {
            context.GuestPortalSettings.Add(new GuestPortalSetting { PortalTitle = "Vantage Grand Guest Portal", WelcomeMessage = "Welcome to Vantage Grand Hotel.", AllowPreCheckIn = true, AllowServiceRequests = true, AllowFolioView = true, AllowExpressCheckoutRequest = true, AllowFeedback = true, RequireReservationLookupVerification = true, IsGuestPortalEnabled = true, TermsAndConditions = DemoMarker, PrivacyPolicy = DemoMarker });
            result.Inserted++;
        }
        var reservation = await context.Reservations.Include(item => item.Guest).FirstOrDefaultAsync(item => item.Status == ReservationStatus.CheckedIn);
        if (reservation is null) return;
        if (!await context.GuestPreCheckIns.AnyAsync(item => item.ReservationId == reservation.Id))
        {
            context.GuestPreCheckIns.Add(new GuestPreCheckIn { ReservationId = reservation.Id, GuestId = reservation.GuestId, ArrivalTime = DateTime.Today.AddHours(15), IdType = "Passport", IdNumber = "DEMO12345", Address = "Quezon City", Nationality = "Filipino", Birthday = DateTime.Today.AddYears(-35), SpecialRequests = DemoMarker, TermsAccepted = true, Status = GuestPreCheckInStatus.Submitted });
            result.Inserted++;
        }
        if (!await context.GuestServiceRequests.AnyAsync(item => item.Description.Contains(DemoMarker)))
        {
            context.GuestServiceRequests.Add(new GuestServiceRequest { ReservationId = reservation.Id, GuestId = reservation.GuestId, RoomId = reservation.RoomId, RequestType = GuestServiceRequestType.ExtraTowels, Priority = GuestServiceRequestPriority.High, Description = $"Extra towels requested. {DemoMarker}", Status = GuestServiceRequestStatus.New, AssignedTo = "housekeeping@vantagepms.demo" });
            result.Inserted++;
        }
        if (!await context.GuestFeedbacks.AnyAsync(item => item.Comments != null && item.Comments.Contains(DemoMarker)))
        {
            context.GuestFeedbacks.Add(new GuestFeedback { ReservationId = reservation.Id, GuestId = reservation.GuestId, Rating = 5, Comments = $"Great service and smooth check-in. {DemoMarker}", SubmittedAt = DateTime.Now });
            result.Inserted++;
        }
        if (!await context.ExpressCheckoutRequests.AnyAsync(item => item.ReservationId == reservation.Id))
        {
            context.ExpressCheckoutRequests.Add(new ExpressCheckoutRequest { ReservationId = reservation.Id, GuestId = reservation.GuestId, Status = ExpressCheckoutRequestStatus.Requested, GuestNotes = DemoMarker });
            result.Inserted++;
        }
    }

    private async Task EnsureInventoryPurchasingAsync(DemoSeedResult result)
    {
        var categories = new[] { "F&B Ingredients", "Beverages", "Housekeeping Supplies", "Guest Amenities", "Linen", "Maintenance Supplies", "Office Supplies" };
        foreach (var category in categories)
        {
            if (!await context.InventoryCategories.AnyAsync(item => item.Name == category))
            {
                context.InventoryCategories.Add(new InventoryCategory { Name = category, Description = DemoMarker, IsActive = true });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var categoryIds = await context.InventoryCategories.ToDictionaryAsync(item => item.Name, item => item.Id);
        var items = new[] { ("RICE", "Rice", "F&B Ingredients", "kg", 50m, 120m, 62m, 95m, false), ("COFFEE", "Coffee Beans", "Beverages", "kg", 10m, 25m, 780m, 8m, true), ("TOWEL", "Bath Towel", "Linen", "pc", 80m, 200m, 220m, 140m, false), ("SHAMPOO", "Shampoo Sachet", "Guest Amenities", "pc", 300m, 800m, 12m, 240m, false), ("BULB", "LED Bulb", "Maintenance Supplies", "pc", 20m, 60m, 180m, 18m, false), ("PAPER", "Bond Paper", "Office Supplies", "ream", 10m, 40m, 210m, 35m, false), ("WATER", "Mineral Water", "Beverages", "case", 30m, 100m, 180m, 22m, true), ("DETERGENT", "Laundry Detergent", "Housekeeping Supplies", "kg", 20m, 80m, 95m, 45m, false), ("CHICKEN", "Chicken Breast", "F&B Ingredients", "kg", 25m, 70m, 210m, 18m, true), ("PASTA", "Pasta Noodles", "F&B Ingredients", "kg", 15m, 45m, 150m, 25m, true) };
        foreach (var item in items)
        {
            if (!await context.InventoryItems.AnyAsync(existing => existing.ItemCode == item.Item1))
            {
                context.InventoryItems.Add(new InventoryItem { InventoryCategoryId = categoryIds[item.Item3], ItemCode = item.Item1, ItemName = item.Item2, Description = DemoMarker, UnitOfMeasure = item.Item4, ReorderLevel = item.Item5, ParStockLevel = item.Item6, StandardCost = item.Item7, CurrentStock = item.Item8, IsActive = true, IsPerishable = item.Item9, ExpiryDate = item.Item9 ? DateTime.Today.AddDays(6) : null, CreatedBy = "DemoDataSeeder" });
                result.Inserted++;
            }
        }
        foreach (var supplier in new[] { "ABC Food Supplies", "CleanPro Hotel Supplies", "Palawan Linen Trading", "Metro Electrical Supply", "OfficeMart PH" })
        {
            if (!await context.Suppliers.AnyAsync(item => item.SupplierName == supplier))
            {
                context.Suppliers.Add(new InventorySupplier { SupplierName = supplier, ContactPerson = "Demo Contact", Phone = "+63 2 8888 2000", Email = supplier.Replace(" ", ".").ToLowerInvariant() + "@demo.example", Address = "Metro Manila", Terms = "30 days", IsActive = true, Notes = DemoMarker });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        if (!await context.PurchaseRequests.AnyAsync(item => item.RequestNumber == "DEMO-PR-001"))
        {
            var departmentId = await context.Departments.Where(item => item.Code == "FB").Select(item => (int?)item.Id).FirstOrDefaultAsync();
            var rice = await context.InventoryItems.FirstAsync(item => item.ItemCode == "RICE");
            var pr = new PurchaseRequest { RequestNumber = "DEMO-PR-001", DepartmentId = departmentId, RequestedBy = "inventory@vantagepms.demo", RequestDate = DateTime.Today, NeededDate = DateTime.Today.AddDays(3), Status = PurchaseRequestStatus.Approved, Purpose = "Demo F&B replenishment", Notes = DemoMarker, ApprovedBy = "finance@vantagepms.demo", ApprovedAt = DateTime.Now };
            pr.Items.Add(new PurchaseRequestItem { InventoryItemId = rice.Id, Quantity = 50, EstimatedUnitCost = rice.StandardCost, EstimatedAmount = 50 * rice.StandardCost, Notes = DemoMarker });
            context.PurchaseRequests.Add(pr);
            result.Inserted++;
        }
        await context.SaveChangesAsync();

        if (!await context.PurchaseOrders.AnyAsync(item => item.PONumber == "DEMO-PO-001"))
        {
            var supplier = await context.Suppliers.FirstAsync(item => item.SupplierName == "ABC Food Supplies");
            var request = await context.PurchaseRequests.FirstAsync(item => item.RequestNumber == "DEMO-PR-001");
            var rice = await context.InventoryItems.FirstAsync(item => item.ItemCode == "RICE");
            var po = new PurchaseOrder { PONumber = "DEMO-PO-001", SupplierId = supplier.Id, PurchaseRequestId = request.Id, OrderDate = DateTime.Today, ExpectedDeliveryDate = DateTime.Today.AddDays(2), Status = PurchaseOrderStatus.Approved, PreparedBy = "inventory@vantagepms.demo", ApprovedBy = "finance@vantagepms.demo", ApprovedAt = DateTime.Now, Notes = DemoMarker };
            po.Items.Add(new PurchaseOrderItem { InventoryItemId = rice.Id, Quantity = 50, UnitCost = rice.StandardCost, Amount = 50 * rice.StandardCost, Notes = DemoMarker });
            po.SubTotal = 50 * rice.StandardCost;
            po.TaxAmount = Math.Round(po.SubTotal * 0.12m, 2);
            po.TotalAmount = po.SubTotal + po.TaxAmount;
            context.PurchaseOrders.Add(po);
            result.Inserted++;
        }
        await context.SaveChangesAsync();

        if (!await context.ReceivingRecords.AnyAsync(item => item.ReceivingNumber == "DEMO-RR-001"))
        {
            var po = await context.PurchaseOrders.Include(item => item.Items).FirstAsync(item => item.PONumber == "DEMO-PO-001");
            var receiving = new ReceivingRecord { ReceivingNumber = "DEMO-RR-001", PurchaseOrderId = po.Id, SupplierId = po.SupplierId, ReceivedDate = DateTime.Today, ReceivedBy = "inventory@vantagepms.demo", Status = ReceivingStatus.Posted, Notes = DemoMarker };
            foreach (var line in po.Items)
            {
                receiving.Items.Add(new ReceivingRecordItem { InventoryItemId = line.InventoryItemId, QuantityReceived = line.Quantity, UnitCost = line.UnitCost, Amount = line.Amount, Notes = DemoMarker });
                context.StockMovements.Add(new StockMovement { InventoryItemId = line.InventoryItemId, MovementDate = DateTime.Now, MovementType = StockMovementType.PurchaseReceiving, Quantity = line.Quantity, UnitCost = line.UnitCost, ReferenceType = nameof(ReceivingRecord), Remarks = DemoMarker, CreatedBy = "DemoDataSeeder" });
            }
            context.ReceivingRecords.Add(receiving);
            result.Inserted++;
        }
    }

    private async Task EnsureAdvancedFinanceAsync(DemoSeedResult result)
    {
        var chargeCodes = new[] { ("ROOM", "Room Charge", ChargeCategory.Room), ("FB", "Food and Beverage", ChargeCategory.FoodBeverage), ("BNQ", "Banquet", ChargeCategory.Banquet), ("MISC", "Miscellaneous", ChargeCategory.Miscellaneous), ("DISC", "Discount", ChargeCategory.Discount), ("TAX", "Tax", ChargeCategory.Tax), ("SC", "Service Charge", ChargeCategory.ServiceCharge), ("REF", "Refund", ChargeCategory.Refund), ("ADJ", "Adjustment", ChargeCategory.Adjustment) };
        foreach (var code in chargeCodes)
        {
            if (!await context.ChargeCodes.AnyAsync(item => item.Code == code.Item1))
            {
                context.ChargeCodes.Add(new ChargeCode { Code = code.Item1, Name = code.Item2, ChargeCategory = code.Item3, IsActive = true, IsTaxable = code.Item3 is ChargeCategory.Room or ChargeCategory.FoodBeverage or ChargeCategory.Banquet or ChargeCategory.Miscellaneous, IsServiceChargeable = code.Item3 is ChargeCategory.Room or ChargeCategory.FoodBeverage or ChargeCategory.Banquet, CreatedBy = "DemoDataSeeder" });
                result.Inserted++;
            }
        }
        var accounts = new[] { ("ABC Corporation", ARAccountType.Corporate), ("XYZ Travel and Tours", ARAccountType.TravelAgency), ("Quezon City LGU", ARAccountType.Government), ("Santos Wedding Events", ARAccountType.EventClient) };
        foreach (var account in accounts)
        {
            if (!await context.ARAccounts.AnyAsync(item => item.AccountName == account.Item1))
            {
                context.ARAccounts.Add(new ARAccount { AccountName = account.Item1, AccountType = account.Item2, ContactPerson = "Demo Contact", Phone = "+63 2 8888 3000", Email = account.Item1.Replace(" ", ".").ToLowerInvariant() + "@demo.example", BillingAddress = "Metro Manila", CreditLimit = 500000, CurrentBalance = 0, IsActive = true, Notes = DemoMarker });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();
        var firstAccount = await context.ARAccounts.FirstOrDefaultAsync();
        if (firstAccount is not null && !await context.ARInvoices.AnyAsync(item => item.InvoiceNumber.StartsWith("DEMO-AR-")))
        {
            var buckets = new[] { 0, 15, 45, 75, 110 };
            for (var i = 0; i < buckets.Length; i++)
            {
                var amount = 30000 + (i * 10000);
                context.ARInvoices.Add(new ARInvoice { ARAccountId = firstAccount.Id, InvoiceNumber = $"DEMO-AR-{i + 1:000}", InvoiceDate = DateTime.Today.AddDays(-buckets[i] - 30), DueDate = DateTime.Today.AddDays(-buckets[i]), OriginalAmount = amount, AmountPaid = i == 0 ? amount / 2 : 0, Balance = i == 0 ? amount / 2 : amount, Status = i == 0 ? ARInvoiceStatus.PartiallyPaid : buckets[i] > 0 ? ARInvoiceStatus.Overdue : ARInvoiceStatus.Open, Notes = DemoMarker, CreatedBy = "DemoDataSeeder" });
                firstAccount.CurrentBalance += i == 0 ? amount / 2 : amount;
                result.Inserted++;
            }
        }
        if (firstAccount is not null && !await context.ARPayments.AnyAsync(item => item.ReferenceNumber == "DEMO-AR-PAY-001"))
        {
            context.ARPayments.Add(new ARPayment { ARAccountId = firstAccount.Id, PaymentDate = DateTime.Today, Amount = 15000, PaymentMethod = FinancePaymentMethod.BankTransfer, ReferenceNumber = "DEMO-AR-PAY-001", ReceivedBy = "finance@vantagepms.demo", Notes = DemoMarker });
            context.CreditMemos.Add(new CreditMemo { ARAccountId = firstAccount.Id, CreditMemoNumber = "DEMO-CM-001", CreditMemoDate = DateTime.Today, Amount = 2500, Reason = "Demo goodwill credit", Status = MemoStatus.Approved, CreatedBy = "DemoDataSeeder", ApprovedBy = "finance@vantagepms.demo", ApprovedAt = DateTime.Now, Notes = DemoMarker });
            context.DebitMemos.Add(new DebitMemo { ARAccountId = firstAccount.Id, DebitMemoNumber = "DEMO-DM-001", DebitMemoDate = DateTime.Today, Amount = 1800, Reason = "Demo adjustment debit", Status = MemoStatus.Approved, CreatedBy = "DemoDataSeeder", ApprovedBy = "finance@vantagepms.demo", ApprovedAt = DateTime.Now, Notes = DemoMarker });
            result.Inserted += 3;
        }
    }

    private async Task EnsureAccountingDemoAsync(DemoSeedResult result)
    {
        var today = DateTime.Today;
        if (!await context.AccountingPeriods.AnyAsync(item => item.PeriodName == "Demo Current Month"))
        {
            context.AccountingPeriods.Add(new Models.Accounting.AccountingPeriod { PeriodName = "Demo Current Month", StartDate = new DateTime(today.Year, today.Month, 1), EndDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)), Status = Models.Accounting.AccountingPeriodStatus.Open, Notes = DemoMarker });
            result.Inserted++;
        }

        await CashFlowSeedData.SeedAsync(context);

        var monthStart = new DateTime(today.Year, today.Month, 1);
        await EnsureDemoJournalEntriesAsync(monthStart, result);

        if (!await context.CashFlowReportSnapshots.AnyAsync(snapshot => snapshot.Notes != null && snapshot.Notes.Contains(DemoMarker)))
        {
            var snapshot = await cashFlowReportService.SaveSnapshotAsync(monthStart, today, CashFlowMethod.Direct, "DemoDataSeeder");
            snapshot.Notes = $"{DemoMarker} cash flow snapshot generated from configured mappings and posted demo journal entries where available.";
            await context.SaveChangesAsync();
            result.Inserted++;
        }
    }

    private async Task EnsureDemoFinanceClosePackAsync(int propertyId, string userName, DemoSeedResult result)
    {
        var closeDate = await GetCleanFinanceCloseDateAsync();
        await EnsureBusinessDateAsync(closeDate, result);
        var periodId = await EnsureAccountingPeriodForDateAsync(closeDate, result);
        var accounts = await context.GLAccounts.ToDictionaryAsync(account => account.AccountCode, account => account.Id);

        int Account(string preferredCode, params string[] fallbackCodes)
        {
            if (accounts.TryGetValue(preferredCode, out var id))
            {
                return id;
            }

            foreach (var fallbackCode in fallbackCodes)
            {
                if (accounts.TryGetValue(fallbackCode, out id))
                {
                    return id;
                }
            }

            throw new InvalidOperationException($"Demo finance close pack requires GL account {preferredCode}.");
        }

        var roomRevenueAccountId = Account("4000");
        var fbRevenueAccountId = Account("4100", "4000");
        var guestLedgerAccountId = Account("1100", "1110");
        var cityLedgerAccountId = Account("1110", "1100");
        var cashAccountId = Account("1000", "1010");
        var bankAccountId = Account("1010", "1000");
        var apAccountId = Account("2000");
        var expenseAccountId = Account("6200", "6400", "6500");

        var chargeCodes = await context.ChargeCodes.ToDictionaryAsync(chargeCode => chargeCode.Code, chargeCode => chargeCode.Id);
        int? ChargeCode(string code) => chargeCodes.TryGetValue(code, out var id) ? id : null;

        var roomTypeId = await context.RoomTypes
            .Where(roomType => roomType.PropertyId == propertyId && roomType.IsActive)
            .OrderBy(roomType => roomType.Id)
            .Select(roomType => roomType.Id)
            .FirstAsync();

        var inHouseRoom = await EnsureCleanCloseRoomAsync(propertyId, roomTypeId, "901", RoomStatus.Occupied, result);
        var departureRoom = await EnsureCleanCloseRoomAsync(propertyId, roomTypeId, "902", RoomStatus.Dirty, result);

        var inHouseGuest = await EnsureCleanCloseGuestAsync("finance.close.inhouse@demo.example", "Leandro", "Villanueva", result);
        var settledGuest = await EnsureCleanCloseGuestAsync("finance.close.settled@demo.example", "Camille", "Ramos", result);

        var inHouseReservation = await EnsureCleanCloseReservationAsync(
            propertyId,
            inHouseGuest.Id,
            inHouseRoom.RoomTypeId,
            inHouseRoom.Id,
            "DEMO-CLOSE-INH-001",
            closeDate.AddDays(-1),
            closeDate.AddDays(1),
            ReservationStatus.CheckedIn,
            5200m,
            result);

        var settledReservation = await EnsureCleanCloseReservationAsync(
            propertyId,
            settledGuest.Id,
            departureRoom.RoomTypeId,
            departureRoom.Id,
            "DEMO-CLOSE-DEP-001",
            closeDate.AddDays(-2),
            closeDate,
            ReservationStatus.CheckedOut,
            3800m,
            result);

        inHouseReservation.ActualCheckInDate ??= closeDate.AddDays(-1).AddHours(15);
        settledReservation.ActualCheckInDate ??= closeDate.AddDays(-2).AddHours(15);
        settledReservation.ActualCheckOutDate ??= closeDate.AddHours(10);

        var inHouseFolio = await EnsureCleanCloseFolioAsync(propertyId, inHouseReservation.Id, inHouseGuest.Id, "DEMO-CLOSE-FOL-INH-001", FolioStatus.Open, result);
        var settledFolio = await EnsureCleanCloseFolioAsync(propertyId, settledReservation.Id, settledGuest.Id, "DEMO-CLOSE-FOL-DEP-001", FolioStatus.Closed, result);

        var inHouseRoomCharge = await EnsureCleanCloseFolioItemAsync(inHouseFolio.Id, ChargeCode("ROOM"), "ROOM", "Demo finance close room night", 5200m, closeDate, result);
        await EnsurePostedJournalAsync("DEMO-CLOSE-JE-ROOM-INH", closeDate, periodId, SourceModule.FrontOffice, SourceTransactionType.RoomCharge, inHouseRoomCharge.Id, inHouseFolio.FolioNumber, "Posted demo in-house room night", new[]
        {
            JournalLine(guestLedgerAccountId, 5200m, 0m, "Guest ledger room charge"),
            JournalLine(roomRevenueAccountId, 0m, 5200m, "Rooms revenue")
        }, result);

        var settledRoomCharge = await EnsureCleanCloseFolioItemAsync(settledFolio.Id, ChargeCode("ROOM"), "ROOM", "Demo checked-out room night", 3800m, closeDate.AddDays(-1), result);
        await EnsurePostedJournalAsync("DEMO-CLOSE-JE-ROOM-DEP", closeDate.AddDays(-1), periodId, SourceModule.FrontOffice, SourceTransactionType.RoomCharge, settledRoomCharge.Id, settledFolio.FolioNumber, "Posted demo checked-out room night", new[]
        {
            JournalLine(guestLedgerAccountId, 3800m, 0m, "Guest ledger room charge"),
            JournalLine(roomRevenueAccountId, 0m, 3800m, "Rooms revenue")
        }, result);

        var settledPayment = await EnsureCleanClosePaymentAsync(settledFolio.Id, 3800m, "Cash", closeDate, "DEMO-CLOSE-PAY-FOL-001", result);
        await EnsurePostedJournalAsync("DEMO-CLOSE-JE-PAY-FOL", closeDate, periodId, SourceModule.Finance, SourceTransactionType.FolioPayment, settledPayment.Id, settledPayment.ReferenceNumber, "Posted demo folio settlement", new[]
        {
            JournalLine(cashAccountId, 3800m, 0m, "Cash received from checked-out guest"),
            JournalLine(guestLedgerAccountId, 0m, 3800m, "Guest ledger collection")
        }, result);

        var (paidOrder, roomChargeOrder, _) = await EnsureCleanClosePosAsync(propertyId, inHouseReservation, inHouseFolio, ChargeCode("FB"), closeDate, result);
        await EnsurePostedJournalAsync("DEMO-CLOSE-JE-POS-CASH", closeDate, periodId, SourceModule.FoodBeverage, SourceTransactionType.POSPayment, paidOrder.Id, paidOrder.OrderNumber, "Posted demo POS cash settlement", new[]
        {
            JournalLine(cashAccountId, paidOrder.TotalAmount, 0m, "Cash received from outlet"),
            JournalLine(fbRevenueAccountId, 0m, paidOrder.TotalAmount, "F&B revenue")
        }, result);
        await EnsurePostedJournalAsync("DEMO-CLOSE-JE-POS-ROOM", closeDate, periodId, SourceModule.FoodBeverage, SourceTransactionType.POSChargeToRoom, roomChargeOrder.Id, roomChargeOrder.OrderNumber, "Posted demo POS charge to room", new[]
        {
            JournalLine(guestLedgerAccountId, roomChargeOrder.TotalAmount, 0m, "Guest ledger POS charge"),
            JournalLine(fbRevenueAccountId, 0m, roomChargeOrder.TotalAmount, "F&B revenue")
        }, result);
        await EnsureCleanCloseArAsync(closeDate, periodId, roomRevenueAccountId, cityLedgerAccountId, bankAccountId, result);
        await EnsureCleanCloseApTreasuryAsync(closeDate, periodId, expenseAccountId, apAccountId, bankAccountId, result);

        result.Messages.Add($"Installed clean finance close pack for business date {closeDate:MMM dd, yyyy}. Seeded records use DEMO-CLOSE-* references and are already posted/reconciled for trial walkthroughs.");
    }

    private async Task<DateTime> GetCleanFinanceCloseDateAsync()
    {
        var earliestArrival = await context.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.ConfirmationNumber.StartsWith("DEMO-") && !reservation.ConfirmationNumber.StartsWith("DEMO-CLOSE-"))
            .Select(reservation => (DateTime?)reservation.ArrivalDate)
            .OrderBy(date => date)
            .FirstOrDefaultAsync();

        return (earliestArrival?.Date.AddDays(-2) ?? DateTime.Today.Date).Date;
    }

    private async Task EnsureBusinessDateAsync(DateTime businessDate, DemoSeedResult result)
    {
        var setting = await context.BusinessDateSettings.FirstOrDefaultAsync();
        if (setting is null)
        {
            context.BusinessDateSettings.Add(new BusinessDateSetting { CurrentBusinessDate = businessDate, UpdatedAtUtc = DateTime.UtcNow });
            result.Inserted++;
            return;
        }

        if (setting.CurrentBusinessDate.Date != businessDate.Date)
        {
            setting.CurrentBusinessDate = businessDate.Date;
            setting.UpdatedAtUtc = DateTime.UtcNow;
            result.Messages.Add($"Demo business date set to {businessDate:MMM dd, yyyy} for clean finance close rehearsal.");
        }
    }

    private async Task<int?> EnsureAccountingPeriodForDateAsync(DateTime date, DemoSeedResult result)
    {
        var periodId = await context.AccountingPeriods
            .Where(period => period.StartDate <= date && period.EndDate >= date)
            .OrderBy(period => period.Id)
            .Select(period => (int?)period.Id)
            .FirstOrDefaultAsync();

        if (periodId is not null)
        {
            return periodId;
        }

        var period = new AccountingPeriod
        {
            PeriodName = $"Demo Close {date:yyyy-MM}",
            StartDate = new DateTime(date.Year, date.Month, 1),
            EndDate = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)),
            Status = AccountingPeriodStatus.Open,
            Notes = $"{DemoMarker} clean finance close period."
        };
        context.AccountingPeriods.Add(period);
        await context.SaveChangesAsync();
        result.Inserted++;
        return period.Id;
    }

    private async Task<Room> EnsureCleanCloseRoomAsync(int propertyId, int roomTypeId, string roomNumber, RoomStatus status, DemoSeedResult result)
    {
        var room = await context.Rooms.FirstOrDefaultAsync(item => item.PropertyId == propertyId && item.RoomNumber == roomNumber);
        if (room is null)
        {
            room = new Room
            {
                PropertyId = propertyId,
                RoomTypeId = roomTypeId,
                RoomNumber = roomNumber,
                Floor = "9",
                Status = status,
                StatusNotes = $"{DemoMarker} clean finance close room.",
                IsActive = true
            };
            context.Rooms.Add(room);
            await context.SaveChangesAsync();
            result.Inserted++;
            return room;
        }

        room.RoomTypeId = roomTypeId;
        room.Status = status;
        room.StatusNotes = $"{DemoMarker} clean finance close room.";
        room.IsActive = true;
        await context.SaveChangesAsync();
        return room;
    }

    private async Task<Guest> EnsureCleanCloseGuestAsync(string email, string firstName, string lastName, DemoSeedResult result)
    {
        var guest = await context.Guests.FirstOrDefaultAsync(item => item.Email == email);
        if (guest is not null)
        {
            return guest;
        }

        guest = new Guest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = "+63 917 555 9000",
            AddressLine1 = "Demo close account",
            City = "Quezon City",
            Country = "Philippines"
        };
        context.Guests.Add(guest);
        await context.SaveChangesAsync();
        result.Inserted++;
        return guest;
    }

    private async Task<Reservation> EnsureCleanCloseReservationAsync(
        int propertyId,
        int guestId,
        int roomTypeId,
        int roomId,
        string confirmationNumber,
        DateTime arrivalDate,
        DateTime departureDate,
        ReservationStatus status,
        decimal rateAmount,
        DemoSeedResult result)
    {
        var reservation = await context.Reservations.FirstOrDefaultAsync(item => item.ConfirmationNumber == confirmationNumber);
        if (reservation is null)
        {
            reservation = new Reservation
            {
                PropertyId = propertyId,
                GuestId = guestId,
                RoomTypeId = roomTypeId,
                RoomId = roomId,
                ConfirmationNumber = confirmationNumber,
                ArrivalDate = arrivalDate,
                DepartureDate = departureDate,
                Status = status,
                RateAmount = rateAmount,
                Adults = 2,
                Children = 0,
                CreatedAtUtc = DateTime.UtcNow
            };
            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();
            result.Inserted++;
            return reservation;
        }

        reservation.PropertyId = propertyId;
        reservation.GuestId = guestId;
        reservation.RoomTypeId = roomTypeId;
        reservation.RoomId = roomId;
        reservation.ArrivalDate = arrivalDate;
        reservation.DepartureDate = departureDate;
        reservation.Status = status;
        reservation.RateAmount = rateAmount;
        await context.SaveChangesAsync();
        return reservation;
    }

    private async Task<Folio> EnsureCleanCloseFolioAsync(int propertyId, int reservationId, int guestId, string folioNumber, FolioStatus status, DemoSeedResult result)
    {
        var folio = await context.Folios.FirstOrDefaultAsync(item => item.FolioNumber == folioNumber);
        if (folio is null)
        {
            folio = new Folio
            {
                PropertyId = propertyId,
                ReservationId = reservationId,
                GuestId = guestId,
                FolioNumber = folioNumber,
                Status = status,
                OpenedAtUtc = DateTime.UtcNow.AddDays(-2),
                ClosedAtUtc = status == FolioStatus.Closed ? DateTime.UtcNow : null
            };
            context.Folios.Add(folio);
            await context.SaveChangesAsync();
            result.Inserted++;
            return folio;
        }

        folio.PropertyId = propertyId;
        folio.ReservationId = reservationId;
        folio.GuestId = guestId;
        folio.Status = status;
        folio.ClosedAtUtc = status == FolioStatus.Closed ? folio.ClosedAtUtc ?? DateTime.UtcNow : null;
        await context.SaveChangesAsync();
        return folio;
    }

    private async Task<FolioItem> EnsureCleanCloseFolioItemAsync(int folioId, int? chargeCodeId, string chargeCode, string description, decimal amount, DateTime postingDate, DemoSeedResult result)
    {
        var item = await context.FolioItems.FirstOrDefaultAsync(existing => existing.FolioId == folioId && existing.ChargeCode == chargeCode && existing.Description == description && existing.PostingDate.Date == postingDate.Date);
        if (item is not null)
        {
            item.ChargeCodeId = chargeCodeId;
            item.Amount = amount;
            item.UnitPrice = amount;
            item.IsVoided = false;
            return item;
        }

        item = new FolioItem
        {
            FolioId = folioId,
            ChargeCodeId = chargeCodeId,
            ChargeCode = chargeCode,
            Description = description,
            Quantity = 1,
            UnitPrice = amount,
            Amount = amount,
            PostingDate = postingDate,
            PostedBy = "DemoDataSeeder",
            IsLocked = true
        };
        context.FolioItems.Add(item);
        await context.SaveChangesAsync();
        result.Inserted++;
        return item;
    }

    private async Task<Payment> EnsureCleanClosePaymentAsync(int folioId, decimal amount, string paymentMethod, DateTime paymentDate, string referenceNumber, DemoSeedResult result)
    {
        var payment = await context.Payments.FirstOrDefaultAsync(item => item.ReferenceNumber == referenceNumber);
        if (payment is not null)
        {
            payment.Amount = amount;
            payment.PaymentMethod = paymentMethod;
            payment.PaymentDate = paymentDate;
            payment.Status = PaymentStatus.Completed;
            payment.IsLocked = true;
            return payment;
        }

        payment = new Payment
        {
            FolioId = folioId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            PaymentDate = paymentDate,
            ReferenceNumber = referenceNumber,
            Notes = $"{DemoMarker} clean finance close payment.",
            Status = PaymentStatus.Completed,
            IsLocked = true
        };
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        result.Inserted++;
        return payment;
    }

    private async Task<(POSOrder PaidOrder, POSOrder RoomChargeOrder, FolioItem RoomChargeFolioItem)> EnsureCleanClosePosAsync(
        int propertyId,
        Reservation inHouseReservation,
        Folio inHouseFolio,
        int? fbChargeCodeId,
        DateTime closeDate,
        DemoSeedResult result)
    {
        var outlet = await context.Outlets.FirstOrDefaultAsync(item => item.Name == "Demo Close Lobby Cafe");
        if (outlet is null)
        {
            outlet = new Outlet { Name = "Demo Close Lobby Cafe", OutletType = OutletType.Cafe, IsActive = true };
            context.Outlets.Add(outlet);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var table = await context.DiningTables.FirstOrDefaultAsync(item => item.OutletId == outlet.Id && item.TableName == "DC-01");
        if (table is null)
        {
            table = new DiningTable { OutletId = outlet.Id, TableName = "DC-01", SeatingCapacity = 4, Status = DiningTableStatus.Available };
            context.DiningTables.Add(table);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var category = await context.MenuCategories.FirstOrDefaultAsync(item => item.Name == "Demo Close Cafe");
        if (category is null)
        {
            category = new MenuCategory { Name = "Demo Close Cafe", SortOrder = 90, IsActive = true };
            context.MenuCategories.Add(category);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var menuItem = await context.MenuItems.FirstOrDefaultAsync(item => item.Name == "Demo Close Breakfast Set");
        if (menuItem is null)
        {
            menuItem = new MenuItem
            {
                MenuCategoryId = category.Id,
                Name = "Demo Close Breakfast Set",
                Description = $"{DemoMarker} finance close POS item.",
                Price = 1250m,
                IsAvailable = true,
                IsTaxable = true,
                IsServiceChargeable = true
            };
            context.MenuItems.Add(menuItem);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var paidOrder = await EnsureCleanClosePosOrderAsync(outlet.Id, table.Id, null, null, "DEMO-CLOSE-POS-CASH-001", POSOrderType.DineIn, POSPaymentStatus.Paid, 1250m, closeDate, result);
        await EnsureCleanClosePosOrderItemAsync(paidOrder.Id, menuItem.Id, 1, 1250m, result);

        var roomChargeOrder = await EnsureCleanClosePosOrderAsync(outlet.Id, table.Id, inHouseReservation.Id, inHouseReservation.GuestId, "DEMO-CLOSE-POS-ROOM-001", POSOrderType.RoomService, POSPaymentStatus.ChargedToRoom, 1850m, closeDate, result);
        await EnsureCleanClosePosOrderItemAsync(roomChargeOrder.Id, menuItem.Id, 1, 1850m, result);
        var folioItem = await EnsureCleanCloseFolioItemAsync(inHouseFolio.Id, fbChargeCodeId, "FB", $"F&B charge to room - Order #{roomChargeOrder.OrderNumber}", roomChargeOrder.TotalAmount, closeDate, result);
        return (paidOrder, roomChargeOrder, folioItem);
    }

    private async Task<POSOrder> EnsureCleanClosePosOrderAsync(
        int outletId,
        int? diningTableId,
        int? reservationId,
        int? guestId,
        string orderNumber,
        POSOrderType orderType,
        POSPaymentStatus paymentStatus,
        decimal totalAmount,
        DateTime orderDate,
        DemoSeedResult result)
    {
        var order = await context.POSOrders.FirstOrDefaultAsync(item => item.OrderNumber == orderNumber);
        if (order is null)
        {
            order = new POSOrder
            {
                OutletId = outletId,
                DiningTableId = diningTableId,
                ReservationId = reservationId,
                GuestId = guestId,
                OrderNumber = orderNumber,
                OrderType = orderType,
                OrderStatus = POSOrderStatus.Closed,
                OrderDate = orderDate,
                SubTotal = totalAmount,
                TotalAmount = totalAmount,
                PaymentStatus = paymentStatus,
                Notes = $"{DemoMarker} clean finance close POS order.",
                CreatedBy = "DemoDataSeeder",
                ClosedAt = orderDate.AddHours(12)
            };
            context.POSOrders.Add(order);
            await context.SaveChangesAsync();
            result.Inserted++;
            return order;
        }

        order.OutletId = outletId;
        order.DiningTableId = diningTableId;
        order.ReservationId = reservationId;
        order.GuestId = guestId;
        order.OrderStatus = POSOrderStatus.Closed;
        order.PaymentStatus = paymentStatus;
        order.SubTotal = totalAmount;
        order.TotalAmount = totalAmount;
        order.ClosedAt = order.ClosedAt ?? orderDate.AddHours(12);
        await context.SaveChangesAsync();
        return order;
    }

    private async Task EnsureCleanClosePosOrderItemAsync(int orderId, int menuItemId, decimal quantity, decimal unitPrice, DemoSeedResult result)
    {
        if (await context.POSOrderItems.AnyAsync(item => item.POSOrderId == orderId && item.MenuItemId == menuItemId))
        {
            return;
        }

        context.POSOrderItems.Add(new POSOrderItem
        {
            POSOrderId = orderId,
            MenuItemId = menuItemId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = quantity * unitPrice,
            ItemStatus = POSOrderItemStatus.Served,
            SentToKitchenAt = DateTime.Now.AddHours(-2),
            PreparingAt = DateTime.Now.AddHours(-2).AddMinutes(5),
            ReadyAt = DateTime.Now.AddHours(-2).AddMinutes(18),
            ServedAt = DateTime.Now.AddHours(-2).AddMinutes(25),
            Notes = $"{DemoMarker} clean finance close POS item."
        });
        await context.SaveChangesAsync();
        result.Inserted++;
    }

    private async Task EnsureCleanCloseArAsync(DateTime closeDate, int? periodId, int revenueAccountId, int cityLedgerAccountId, int bankAccountId, DemoSeedResult result)
    {
        var account = await context.ARAccounts.FirstOrDefaultAsync(item => item.AccountName == "Demo Close City Ledger");
        if (account is null)
        {
            account = new ARAccount
            {
                AccountName = "Demo Close City Ledger",
                AccountType = ARAccountType.Corporate,
                ContactPerson = "Finance Trial Controller",
                Phone = "+63 2 8888 9000",
                Email = "demo.close.cityledger@demo.example",
                BillingAddress = "Quezon City",
                CreditLimit = 250000,
                CurrentBalance = 0,
                IsActive = true,
                Notes = $"{DemoMarker} clean finance close AR account."
            };
            context.ARAccounts.Add(account);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var invoice = await context.ARInvoices.FirstOrDefaultAsync(item => item.InvoiceNumber == "DEMO-CLOSE-AR-001");
        if (invoice is null)
        {
            invoice = new ARInvoice
            {
                ARAccountId = account.Id,
                InvoiceNumber = "DEMO-CLOSE-AR-001",
                InvoiceDate = closeDate,
                DueDate = closeDate.AddDays(15),
                OriginalAmount = 25000m,
                AmountPaid = 25000m,
                Balance = 0m,
                Status = ARInvoiceStatus.Paid,
                Notes = $"{DemoMarker} clean finance close AR invoice.",
                CreatedBy = "DemoDataSeeder"
            };
            context.ARInvoices.Add(invoice);
            await context.SaveChangesAsync();
            result.Inserted++;
        }
        else
        {
            invoice.AmountPaid = invoice.OriginalAmount;
            invoice.Balance = 0m;
            invoice.Status = ARInvoiceStatus.Paid;
        }

        var payment = await context.ARPayments.FirstOrDefaultAsync(item => item.ReferenceNumber == "DEMO-CLOSE-AR-PAY-001");
        if (payment is null)
        {
            payment = new ARPayment
            {
                ARAccountId = account.Id,
                PaymentDate = closeDate,
                Amount = invoice.OriginalAmount,
                PaymentMethod = FinancePaymentMethod.BankTransfer,
                ReferenceNumber = "DEMO-CLOSE-AR-PAY-001",
                ReceivedBy = "finance@vantagepms.demo",
                Notes = $"{DemoMarker} clean finance close AR payment."
            };
            context.ARPayments.Add(payment);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        if (!await context.ARPaymentAllocations.AnyAsync(item => item.ARPaymentId == payment.Id && item.ARInvoiceId == invoice.Id))
        {
            context.ARPaymentAllocations.Add(new ARPaymentAllocation
            {
                ARPaymentId = payment.Id,
                ARInvoiceId = invoice.Id,
                AllocatedAmount = invoice.OriginalAmount,
                AllocationDate = closeDate,
                AllocatedBy = "DemoDataSeeder"
            });
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        account.CurrentBalance = 0m;
        await EnsurePostedJournalAsync("DEMO-CLOSE-JE-AR-INV", closeDate, periodId, SourceModule.AccountsReceivable, SourceTransactionType.ARInvoice, invoice.Id, invoice.InvoiceNumber, "Posted demo AR invoice", new[]
        {
            JournalLine(cityLedgerAccountId, invoice.OriginalAmount, 0m, "City ledger invoice"),
            JournalLine(revenueAccountId, 0m, invoice.OriginalAmount, "Rooms revenue billed to city ledger")
        }, result);
        await EnsurePostedJournalAsync("DEMO-CLOSE-JE-AR-PAY", closeDate, periodId, SourceModule.AccountsReceivable, SourceTransactionType.ARPayment, payment.Id, payment.ReferenceNumber, "Posted demo AR collection", new[]
        {
            JournalLine(bankAccountId, payment.Amount, 0m, "Bank collection from city ledger"),
            JournalLine(cityLedgerAccountId, 0m, payment.Amount, "City ledger collection")
        }, result);
    }

    private async Task EnsureCleanCloseApTreasuryAsync(DateTime closeDate, int? periodId, int expenseAccountId, int apAccountId, int bankAccountId, DemoSeedResult result)
    {
        var supplier = await context.Suppliers.FirstOrDefaultAsync(item => item.SupplierName == "Demo Close Supplier");
        if (supplier is null)
        {
            supplier = new InventorySupplier
            {
                SupplierName = "Demo Close Supplier",
                ContactPerson = "Finance Trial Supplier",
                Phone = "+63 2 8888 9010",
                Email = "demo.close.supplier@demo.example",
                Address = "Metro Manila",
                Terms = "Due on receipt",
                IsActive = true,
                Notes = $"{DemoMarker} clean finance close supplier."
            };
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var invoice = await context.APInvoices.FirstOrDefaultAsync(item => item.InvoiceNumber == "DEMO-CLOSE-AP-001");
        if (invoice is null)
        {
            invoice = new APInvoice
            {
                SupplierId = supplier.Id,
                InvoiceNumber = "DEMO-CLOSE-AP-001",
                SupplierInvoiceNumber = "SUP-DEMO-CLOSE-001",
                InvoiceDate = closeDate,
                DueDate = closeDate,
                SubTotal = 15000m,
                TotalAmount = 15000m,
                AmountPaid = 15000m,
                Balance = 0m,
                Status = APInvoiceStatus.Paid,
                Notes = $"{DemoMarker} clean finance close AP invoice.",
                CreatedBy = "DemoDataSeeder",
                ApprovedBy = "finance@vantagepms.demo",
                ApprovedAt = closeDate
            };
            context.APInvoices.Add(invoice);
            await context.SaveChangesAsync();
            result.Inserted++;
        }
        else
        {
            invoice.AmountPaid = invoice.TotalAmount;
            invoice.Balance = 0m;
            invoice.Status = APInvoiceStatus.Paid;
            invoice.ApprovedBy ??= "finance@vantagepms.demo";
            invoice.ApprovedAt ??= closeDate;
        }

        if (!await context.APInvoiceLines.AnyAsync(item => item.APInvoiceId == invoice.Id))
        {
            context.APInvoiceLines.Add(new APInvoiceLine
            {
                APInvoiceId = invoice.Id,
                GLAccountId = expenseAccountId,
                Description = "Demo close operating expense",
                Quantity = 1,
                UnitCost = 15000m,
                LineTotal = 15000m
            });
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var voucher = await context.PaymentVouchers.FirstOrDefaultAsync(item => item.VoucherNumber == "DEMO-CLOSE-PV-001");
        if (voucher is null)
        {
            voucher = new PaymentVoucher
            {
                VoucherNumber = "DEMO-CLOSE-PV-001",
                SupplierId = supplier.Id,
                APInvoiceId = invoice.Id,
                VoucherDate = closeDate,
                PaymentMethod = FinancePaymentMethod.BankTransfer,
                BankAccountName = "Demo Close Operating Account",
                BankReferenceNumber = "BANK-DEMO-CLOSE-001",
                Amount = 15000m,
                NetPaymentAmount = 15000m,
                Status = PaymentVoucherStatus.Released,
                PreparedBy = "DemoDataSeeder",
                ApprovedBy = "finance@vantagepms.demo",
                ApprovedAt = closeDate,
                ReleasedBy = "finance@vantagepms.demo",
                ReleasedAt = closeDate,
                Notes = $"{DemoMarker} clean finance close voucher."
            };
            context.PaymentVouchers.Add(voucher);
            await context.SaveChangesAsync();
            result.Inserted++;
        }
        else
        {
            voucher.Status = PaymentVoucherStatus.Released;
            voucher.Amount = 15000m;
            voucher.NetPaymentAmount = 15000m;
            voucher.ApprovedBy ??= "finance@vantagepms.demo";
            voucher.ApprovedAt ??= closeDate;
            voucher.ReleasedBy ??= "finance@vantagepms.demo";
            voucher.ReleasedAt ??= closeDate;
        }

        var disbursement = await context.Disbursements.FirstOrDefaultAsync(item => item.DisbursementNumber == "DEMO-CLOSE-DISB-001");
        if (disbursement is null)
        {
            disbursement = new Disbursement
            {
                DisbursementNumber = "DEMO-CLOSE-DISB-001",
                PaymentVoucherId = voucher.Id,
                SupplierId = supplier.Id,
                DisbursementDate = closeDate,
                PaymentMethod = FinancePaymentMethod.BankTransfer,
                Amount = voucher.NetPaymentAmount,
                ReferenceNumber = voucher.BankReferenceNumber,
                PaidBy = "finance@vantagepms.demo",
                Status = DisbursementStatus.Cleared,
                Notes = $"{DemoMarker} clean finance close disbursement."
            };
            context.Disbursements.Add(disbursement);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var apJournal = await EnsurePostedJournalAsync("DEMO-CLOSE-JE-AP-INV", closeDate, periodId, SourceModule.Purchasing, SourceTransactionType.APInvoice, invoice.Id, invoice.InvoiceNumber, "Posted demo AP invoice", new[]
        {
            JournalLine(expenseAccountId, invoice.TotalAmount, 0m, "Operating expense"),
            JournalLine(apAccountId, 0m, invoice.TotalAmount, "Accounts payable")
        }, result);
        invoice.JournalEntryId ??= apJournal.Id;

        var voucherJournal = await EnsurePostedJournalAsync("DEMO-CLOSE-JE-PV-REL", closeDate, periodId, SourceModule.Finance, SourceTransactionType.PaymentVoucher, voucher.Id, voucher.VoucherNumber, "Posted demo payment voucher release", new[]
        {
            JournalLine(apAccountId, voucher.Amount, 0m, "Accounts payable cleared"),
            JournalLine(bankAccountId, 0m, voucher.NetPaymentAmount, "Bank disbursement")
        }, result);
        voucher.JournalEntryId ??= voucherJournal.Id;
        disbursement.JournalEntryId ??= voucherJournal.Id;

        var bankAccount = await EnsureCleanCloseBankAccountAsync(bankAccountId, result);
        var bankTransaction = await context.BankTransactions.FirstOrDefaultAsync(item => item.ReferenceNumber == "BANK-DEMO-CLOSE-001");
        if (bankTransaction is null)
        {
            bankTransaction = new BankTransaction
            {
                BankAccountId = bankAccount.Id,
                TransactionDate = closeDate,
                Description = "Demo close supplier disbursement",
                ReferenceNumber = "BANK-DEMO-CLOSE-001",
                CreditAmount = voucher.NetPaymentAmount,
                SourceModule = SourceModule.Finance,
                SourceReferenceId = voucher.Id,
                JournalEntryId = voucherJournal.Id,
                IsReconciled = true,
                ReconciledAt = closeDate,
                ReconciledBy = "finance@vantagepms.demo",
                Notes = $"{DemoMarker} clean finance close bank transaction."
            };
            context.BankTransactions.Add(bankTransaction);
            await context.SaveChangesAsync();
            result.Inserted++;
        }
        else
        {
            bankTransaction.BankAccountId = bankAccount.Id;
            bankTransaction.CreditAmount = voucher.NetPaymentAmount;
            bankTransaction.JournalEntryId = voucherJournal.Id;
            bankTransaction.IsReconciled = true;
            bankTransaction.ReconciledAt ??= closeDate;
            bankTransaction.ReconciledBy ??= "finance@vantagepms.demo";
        }

        var bookBalance = await context.JournalEntryLines
            .Where(line => line.GLAccountId == bankAccountId && line.JournalEntry != null && line.JournalEntry.Status == JournalEntryStatus.Posted && line.JournalEntry.JournalDate <= closeDate)
            .SumAsync(line => line.DebitAmount - line.CreditAmount);

        var reconciliation = await context.BankReconciliations.FirstOrDefaultAsync(item => item.BankAccountId == bankAccount.Id && item.Notes != null && item.Notes.Contains("DEMO-CLOSE-BANKREC-001"));
        if (reconciliation is null)
        {
            reconciliation = new BankReconciliation
            {
                BankAccountId = bankAccount.Id,
                ReconciliationDate = closeDate,
                StatementEndingBalance = bookBalance,
                BookEndingBalance = bookBalance,
                Difference = 0m,
                Status = BankReconciliationStatus.Approved,
                PreparedBy = "DemoDataSeeder",
                ApprovedBy = "finance@vantagepms.demo",
                ApprovedAt = closeDate,
                Notes = $"DEMO-CLOSE-BANKREC-001 {DemoMarker} clean finance close reconciliation."
            };
            context.BankReconciliations.Add(reconciliation);
            await context.SaveChangesAsync();
            result.Inserted++;
        }
        else
        {
            reconciliation.ReconciliationDate = closeDate;
            reconciliation.StatementEndingBalance = bookBalance;
            reconciliation.BookEndingBalance = bookBalance;
            reconciliation.Difference = 0m;
            reconciliation.Status = BankReconciliationStatus.Approved;
        }

        if (!await context.BankReconciliationItems.AnyAsync(item => item.BankReconciliationId == reconciliation.Id && item.BankTransactionId == bankTransaction.Id))
        {
            context.BankReconciliationItems.Add(new BankReconciliationItem
            {
                BankReconciliationId = reconciliation.Id,
                BankTransactionId = bankTransaction.Id,
                Description = "Cleared demo close supplier payment",
                Amount = -bankTransaction.CreditAmount,
                ItemType = BankReconciliationItemType.Withdrawal,
                IsCleared = true,
                Notes = $"{DemoMarker} clean finance close cleared item."
            });
            await context.SaveChangesAsync();
            result.Inserted++;
        }
    }

    private async Task<BankAccount> EnsureCleanCloseBankAccountAsync(int glAccountId, DemoSeedResult result)
    {
        var bankAccount = await context.BankAccounts.FirstOrDefaultAsync(item => item.AccountName == "Demo Close Operating Account");
        if (bankAccount is not null)
        {
            bankAccount.GLAccountId = glAccountId;
            bankAccount.IsActive = true;
            return bankAccount;
        }

        bankAccount = new BankAccount
        {
            AccountName = "Demo Close Operating Account",
            BankName = "Demo Bank PH",
            AccountNumber = "DEMO-CLOSE-001",
            GLAccountId = glAccountId,
            Currency = "PHP",
            OpeningBalance = 0m,
            IsActive = true,
            Notes = $"{DemoMarker} clean finance close bank account."
        };
        context.BankAccounts.Add(bankAccount);
        await context.SaveChangesAsync();
        result.Inserted++;
        return bankAccount;
    }

    private async Task<JournalEntry> EnsurePostedJournalAsync(
        string journalNumber,
        DateTime journalDate,
        int? periodId,
        SourceModule sourceModule,
        SourceTransactionType sourceTransactionType,
        int? sourceReferenceId,
        string? sourceReferenceNumber,
        string description,
        IReadOnlyCollection<JournalEntryLine> lines,
        DemoSeedResult result)
    {
        var entry = await context.JournalEntries.Include(item => item.Lines).FirstOrDefaultAsync(item => item.JournalNumber == journalNumber);
        if (entry is not null)
        {
            entry.JournalDate = journalDate;
            entry.AccountingPeriodId = periodId;
            entry.SourceModule = sourceModule;
            entry.SourceTransactionType = sourceTransactionType;
            entry.SourceReferenceId = sourceReferenceId;
            entry.SourceReferenceNumber = sourceReferenceNumber;
            entry.Description = $"{description}. {DemoMarker}";
            entry.Status = JournalEntryStatus.Posted;
            entry.PostedBy ??= "DemoDataSeeder";
            entry.PostedAt ??= DateTime.Now;
            return entry;
        }

        entry = new JournalEntry
        {
            JournalNumber = journalNumber,
            JournalDate = journalDate,
            AccountingPeriodId = periodId,
            SourceModule = sourceModule,
            SourceTransactionType = sourceTransactionType,
            SourceReferenceId = sourceReferenceId,
            SourceReferenceNumber = sourceReferenceNumber,
            Description = $"{description}. {DemoMarker}",
            Status = JournalEntryStatus.Posted,
            PostedBy = "DemoDataSeeder",
            PostedAt = DateTime.Now,
            CreatedAt = DateTime.Now,
            CreatedBy = "DemoDataSeeder"
        };

        foreach (var line in lines)
        {
            entry.Lines.Add(line);
        }

        context.JournalEntries.Add(entry);
        await context.SaveChangesAsync();
        result.Inserted++;
        return entry;
    }

    private static JournalEntryLine JournalLine(int glAccountId, decimal debitAmount, decimal creditAmount, string description)
        => new()
        {
            GLAccountId = glAccountId,
            DebitAmount = debitAmount,
            CreditAmount = creditAmount,
            Description = description
        };

    private async Task EnsureDemoJournalEntriesAsync(DateTime monthStart, DemoSeedResult result)
    {
        if (await context.JournalEntries.AnyAsync(entry => entry.JournalNumber.StartsWith("DEMO-GL-")))
        {
            return;
        }

        var accounts = await context.GLAccounts.ToDictionaryAsync(account => account.AccountCode, account => account.Id);
        var usaliDepartments = await context.USALIDepartments.ToDictionaryAsync(department => department.Code, department => department.Id);
        var periodId = await context.AccountingPeriods
            .Where(period => period.StartDate <= monthStart && period.EndDate >= monthStart)
            .OrderBy(period => period.Id)
            .Select(period => (int?)period.Id)
            .FirstOrDefaultAsync();

        int Account(string code)
        {
            if (!accounts.TryGetValue(code, out var id))
            {
                throw new InvalidOperationException($"Demo accounting seed requires GL account {code}.");
            }

            return id;
        }

        int? Usali(string code) => usaliDepartments.TryGetValue(code, out var id) ? id : null;

        var entries = new[]
        {
            new DemoJournalEntrySeed(
                "DEMO-GL-001",
                monthStart,
                "Demo opening cash and owner equity",
                new[]
                {
                    Line("1010", 500000m, 0, "Opening bank balance"),
                    Line("3000", 0, 500000m, "Owner equity opening balance")
                }),
            new DemoJournalEntrySeed(
                "DEMO-GL-002",
                monthStart.AddDays(3),
                "Demo hotel operating revenue",
                new[]
                {
                    Line("1000", 300000m, 0, "Cash receipts from guests"),
                    Line("1100", 80000m, 0, "Guest ledger receivables"),
                    Line("4000", 0, 220000m, "Rooms revenue", "ROOMS"),
                    Line("4100", 0, 90000m, "F&B revenue", "FB"),
                    Line("4200", 0, 70000m, "Banquet revenue", "BNQ")
                }),
            new DemoJournalEntrySeed(
                "DEMO-GL-003",
                monthStart.AddDays(8),
                "Demo operating expenses paid",
                new[]
                {
                    Line("5000", 45000m, 0, "Food cost", "FB"),
                    Line("6000", 60000m, 0, "Rooms payroll", "ROOMS"),
                    Line("6100", 35000m, 0, "F&B payroll", "FB"),
                    Line("6200", 40000m, 0, "Administrative expenses", "A&G"),
                    Line("6500", 22000m, 0, "Utilities", "UTIL"),
                    Line("1010", 0, 202000m, "Operating cash disbursements")
                }),
            new DemoJournalEntrySeed(
                "DEMO-GL-004",
                monthStart.AddDays(12),
                "Demo supplier invoice accrual",
                new[]
                {
                    Line("6400", 25000m, 0, "Maintenance expense", "POM"),
                    Line("2020", 3000m, 0, "Input VAT"),
                    Line("2000", 0, 28000m, "Accounts payable")
                }),
            new DemoJournalEntrySeed(
                "DEMO-GL-005",
                monthStart.AddDays(15),
                "Demo city ledger collection",
                new[]
                {
                    Line("1010", 50000m, 0, "AR bank collection"),
                    Line("1110", 0, 50000m, "City ledger collection")
                })
        };

        foreach (var seed in entries)
        {
            var entry = new JournalEntry
            {
                JournalNumber = seed.JournalNumber,
                JournalDate = seed.JournalDate,
                AccountingPeriodId = periodId,
                SourceModule = SourceModule.Manual,
                SourceTransactionType = SourceTransactionType.ManualJournal,
                SourceReferenceNumber = seed.JournalNumber,
                Description = $"{seed.Description}. {DemoMarker}",
                Status = JournalEntryStatus.Posted,
                PostedBy = "DemoDataSeeder",
                PostedAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                CreatedBy = "DemoDataSeeder"
            };

            foreach (var seedLine in seed.Lines)
            {
                entry.Lines.Add(new JournalEntryLine
                {
                    GLAccountId = Account(seedLine.AccountCode),
                    USALIDepartmentId = seedLine.UsaliDepartmentCode is null ? null : Usali(seedLine.UsaliDepartmentCode),
                    DebitAmount = seedLine.DebitAmount,
                    CreditAmount = seedLine.CreditAmount,
                    Description = seedLine.Description
                });
            }

            context.JournalEntries.Add(entry);
            result.Inserted++;
        }

        await context.SaveChangesAsync();

        static DemoJournalLineSeed Line(string accountCode, decimal debitAmount, decimal creditAmount, string description, string? usaliDepartmentCode = null)
            => new(accountCode, debitAmount, creditAmount, description, usaliDepartmentCode);
    }

    private async Task EnsureLaborCostingAsync(DemoSeedResult result)
    {
        var departmentIds = (await context.Departments
                .AsNoTracking()
                .Where(item => item.Code != null)
                .OrderByDescending(item => item.IsActive)
                .ThenBy(item => item.Id)
                .Select(item => new { item.Code, item.Id })
                .ToListAsync())
            .GroupBy(item => item.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
        var usaliIds = await context.USALIDepartments.ToDictionaryAsync(item => item.Code, item => item.Id);
        var accountIds = await context.GLAccounts.ToDictionaryAsync(item => item.AccountCode, item => item.Id);
        int? Dept(string code) => departmentIds.TryGetValue(code, out var id) ? id : null;
        int? Usali(string code) => usaliIds.TryGetValue(code, out var id) ? id : null;
        int? Account(string code) => accountIds.TryGetValue(code, out var id) ? id : null;

        var employees = new[]
        {
            ("DEMO-FO-001", "Andrea Villanueva", "FO", "ROOMS", "Front Office Supervisor", EmploymentType.Regular, "6000"),
            ("DEMO-HK-001", "Rafael Santos", "HK", "ROOMS", "Housekeeping Supervisor", EmploymentType.Regular, "6000"),
            ("DEMO-FB-001", "Bianca Cruz", "FB", "FB", "F&B Service Captain", EmploymentType.Regular, "6100"),
            ("DEMO-KIT-001", "Marco Reyes", "KIT", "FB", "Chef de Partie", EmploymentType.Regular, "6100"),
            ("DEMO-SLS-001", "Patricia Lim", "SLS", "S&M", "Sales Manager", EmploymentType.Regular, "6300"),
            ("DEMO-BNQ-001", "Carlo Mendoza", "BNQ", "BNQ", "Banquet Coordinator", EmploymentType.Regular, "6150"),
            ("DEMO-FIN-001", "Elena Garcia", "FIN", "A&G", "Finance Associate", EmploymentType.Regular, "6200"),
            ("DEMO-ENG-001", "Nico Tan", "ENG", "POM", "Engineering Technician", EmploymentType.Regular, "6400")
        };

        foreach (var employee in employees)
        {
            if (!await context.EmployeeCostProfiles.AnyAsync(item => item.EmployeeCode == employee.Item1))
            {
                context.EmployeeCostProfiles.Add(new EmployeeCostProfile
                {
                    EmployeeCode = employee.Item1,
                    FullName = employee.Item2,
                    DepartmentId = Dept(employee.Item3),
                    USALIDepartmentId = Usali(employee.Item4),
                    Position = employee.Item5,
                    EmploymentType = employee.Item6,
                    DefaultLaborGLAccountId = Account(employee.Item7) ?? Account("6200"),
                    DefaultPayrollLiabilityGLAccountId = Account("2070") ?? Account("2000"),
                    IsActive = true,
                    Notes = DemoMarker,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "DemoDataSeeder"
                });
                result.Inserted++;
            }
        }
        await context.SaveChangesAsync();

        var today = DateTime.Today;
        var periodName = $"DEMO-PAY-{today:yyyyMM}";
        var period = await context.PayrollPeriods.Include(item => item.Entries).FirstOrDefaultAsync(item => item.PeriodName == periodName);
        if (period is null)
        {
            period = new PayrollPeriod
            {
                PeriodName = periodName,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)),
                PayDate = new DateTime(today.Year, today.Month, Math.Min(15, DateTime.DaysInMonth(today.Year, today.Month))),
                Status = PayrollPeriodStatus.ForApproval,
                PreparedBy = "DemoDataSeeder",
                Notes = DemoMarker
            };
            context.PayrollPeriods.Add(period);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        if (!period.Entries.Any())
        {
            var profiles = await context.EmployeeCostProfiles.Where(item => item.EmployeeCode.StartsWith("DEMO-")).OrderBy(item => item.EmployeeCode).ToListAsync();
            var index = 0;
            foreach (var profile in profiles)
            {
                var basePay = 24000m + (index * 1800m);
                var entry = new PayrollCostEntry
                {
                    PayrollPeriodId = period.Id,
                    EmployeeCostProfileId = profile.Id,
                    DepartmentId = profile.DepartmentId,
                    USALIDepartmentId = profile.USALIDepartmentId,
                    Position = profile.Position,
                    RegularHours = 176,
                    OvertimeHours = index % 3 == 0 ? 8 : 2,
                    NightDifferentialHours = index % 4 == 0 ? 6 : 0,
                    RegularPay = basePay,
                    OvertimePay = index % 3 == 0 ? 2200 : 550,
                    NightDifferentialPay = index % 4 == 0 ? 700 : 0,
                    Allowances = 1500,
                    ServiceChargeShare = 2500,
                    OtherEarnings = 0,
                    Deductions = 2500,
                    LaborGLAccountId = profile.DefaultLaborGLAccountId,
                    PayrollLiabilityGLAccountId = profile.DefaultPayrollLiabilityGLAccountId,
                    Notes = DemoMarker
                };
                entry.GrossPay = entry.RegularPay + entry.OvertimePay + entry.NightDifferentialPay + entry.Allowances + entry.ServiceChargeShare + entry.OtherEarnings;
                entry.EmployerCost = entry.GrossPay;
                entry.NetPay = entry.GrossPay - entry.Deductions;
                context.PayrollCostEntries.Add(entry);
                result.Inserted++;
                index++;
            }
        }

        foreach (var department in await context.Departments.Where(item => item.Code == "FO" || item.Code == "HK" || item.Code == "FB" || item.Code == "KIT" || item.Code == "BNQ" || item.Code == "FIN").ToListAsync())
        {
            if (!await context.DepartmentLaborBudgets.AnyAsync(item => item.DepartmentId == department.Id && item.Month == today.Month && item.Year == today.Year))
            {
                context.DepartmentLaborBudgets.Add(new DepartmentLaborBudget
                {
                    DepartmentId = department.Id,
                    Month = today.Month,
                    Year = today.Year,
                    BudgetedLaborCost = 85000,
                    BudgetedLaborHours = 520,
                    BudgetedHeadcount = 3,
                    Notes = $"{DemoMarker} monthly labor budget."
                });
                result.Inserted++;
            }
        }

        var poolName = $"DEMO-SC-{today:yyyyMM}";
        var pool = await context.ServiceChargePools.Include(item => item.DistributionLines).FirstOrDefaultAsync(item => item.PoolName == poolName);
        if (pool is null)
        {
            pool = new ServiceChargePool
            {
                PoolName = poolName,
                PeriodStart = new DateTime(today.Year, today.Month, 1),
                PeriodEnd = today,
                TotalServiceChargeCollected = 40000,
                DistributionMethod = ServiceChargeDistributionMethod.EqualShare,
                Status = ServiceChargePoolStatus.ForApproval,
                PreparedBy = "DemoDataSeeder",
                Notes = DemoMarker
            };
            context.ServiceChargePools.Add(pool);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        if (!pool.DistributionLines.Any())
        {
            var profiles = await context.EmployeeCostProfiles.Where(item => item.EmployeeCode.StartsWith("DEMO-")).OrderBy(item => item.EmployeeCode).Take(8).ToListAsync();
            var amount = profiles.Count == 0 ? 0 : Math.Round(pool.TotalServiceChargeCollected / profiles.Count, 2);
            foreach (var profile in profiles)
            {
                context.ServiceChargeDistributionLines.Add(new ServiceChargeDistributionLine
                {
                    ServiceChargePoolId = pool.Id,
                    EmployeeCostProfileId = profile.Id,
                    DepartmentId = profile.DepartmentId,
                    EligibleDays = 15,
                    EligibleHours = 88,
                    DistributionPercentage = profiles.Count == 0 ? 0 : 100m / profiles.Count,
                    Amount = amount,
                    Notes = DemoMarker
                });
                result.Inserted++;
            }
        }

        if (!await context.LaborProductivitySnapshots.AnyAsync(item => item.Notes != null && item.Notes.Contains(DemoMarker)))
        {
            context.LaborProductivitySnapshots.Add(new LaborProductivitySnapshot
            {
                SnapshotDate = DateTime.Today,
                DepartmentId = Dept("HK"),
                USALIDepartmentId = Usali("ROOMS"),
                LaborHours = 64,
                LaborCost = 18500,
                DepartmentRevenue = 180000,
                LaborCostPercentage = 10.28m,
                RevenuePerLaborHour = 2812.50m,
                RoomsCleaned = 42,
                Notes = DemoMarker
            });
            context.LaborProductivitySnapshots.Add(new LaborProductivitySnapshot
            {
                SnapshotDate = DateTime.Today,
                DepartmentId = Dept("FB"),
                USALIDepartmentId = Usali("FB"),
                LaborHours = 72,
                LaborCost = 22500,
                DepartmentRevenue = 85000,
                LaborCostPercentage = 26.47m,
                RevenuePerLaborHour = 1180.56m,
                CoversServed = 160,
                Notes = DemoMarker
            });
            result.Inserted += 2;
        }
    }

    private async Task EnsureGroupManagementAsync(DemoSeedResult result)
    {
        var today = DateTime.Today;
        var salesAccounts = await context.SalesAccounts
            .Where(item => item.AccountName == "ABC Corporation" || item.AccountName == "Santos Wedding Events")
            .ToDictionaryAsync(item => item.AccountName, item => item.Id);
        var banquetEventId = await context.BanquetEvents
            .Where(item => item.EventName.Contains("Santos-Reyes"))
            .Select(item => (int?)item.Id)
            .FirstOrDefaultAsync();

        var groupSeeds = new[]
        {
            new
            {
                Code = "DEMO-GRP-ABC",
                Name = "ABC Corporate Training",
                AccountName = "ABC Corporation",
                Contact = "Maria Santos",
                Email = "maria.santos@demo.example",
                Arrival = today.AddDays(7),
                Departure = today.AddDays(10),
                Segment = "Corporate",
                Source = "Sales CRM",
                Billing = "Room and tax to master. Incidentals paid by guest.",
                CreditLimit = 150000m,
                Deposit = 50000m
            },
            new
            {
                Code = "DEMO-GRP-WED",
                Name = "Santos-Reyes Wedding Group",
                AccountName = "Santos Wedding Events",
                Contact = "Angela Cruz",
                Email = "angela.cruz@demo.example",
                Arrival = today.AddDays(14),
                Departure = today.AddDays(16),
                Segment = "Wedding",
                Source = "Banquet",
                Billing = "Banquet and selected room charges to master folio.",
                CreditLimit = 250000m,
                Deposit = 80000m
            }
        };

        foreach (var seed in groupSeeds)
        {
            if (!await context.GroupBookings.AnyAsync(item => item.GroupCode == seed.Code))
            {
                context.GroupBookings.Add(new GroupBooking
                {
                    GroupCode = seed.Code,
                    GroupName = seed.Name,
                    SalesAccountId = salesAccounts.TryGetValue(seed.AccountName, out var salesAccountId) ? salesAccountId : null,
                    ContactPerson = seed.Contact,
                    ContactNumber = "+63 2 8888 0000",
                    Email = seed.Email,
                    ArrivalDate = seed.Arrival,
                    DepartureDate = seed.Departure,
                    BookingStatus = GroupBookingStatus.Confirmed,
                    MarketSegment = seed.Segment,
                    Source = seed.Source,
                    BillingInstruction = $"{seed.Billing} {DemoMarker}",
                    CreditLimit = seed.CreditLimit,
                    DepositRequired = true,
                    DepositAmount = seed.Deposit,
                    Notes = DemoMarker,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "DemoDataSeeder"
                });
                result.Inserted++;
            }
        }

        await context.SaveChangesAsync();

        var abcGroup = await context.GroupBookings.FirstOrDefaultAsync(item => item.GroupCode == "DEMO-GRP-ABC");
        var weddingGroup = await context.GroupBookings.FirstOrDefaultAsync(item => item.GroupCode == "DEMO-GRP-WED");
        if (abcGroup is null || weddingGroup is null)
        {
            return;
        }

        if (!await context.PseudoRooms.AnyAsync(item => item.PseudoRoomCode == "PM-001"))
        {
            context.PseudoRooms.Add(new PseudoRoom
            {
                PseudoRoomCode = "PM-001",
                PseudoRoomName = "ABC Corporation Paymaster",
                PseudoRoomType = PseudoRoomType.Paymaster,
                LinkedSalesAccountId = salesAccounts.TryGetValue("ABC Corporation", out var salesAccountId) ? salesAccountId : null,
                LinkedGroupBookingId = abcGroup.Id,
                IsActive = true,
                Notes = DemoMarker,
                CreatedAt = DateTime.Now,
                CreatedBy = "DemoDataSeeder"
            });
            result.Inserted++;
        }

        if (!await context.PseudoRooms.AnyAsync(item => item.PseudoRoomCode == "PM-002"))
        {
            context.PseudoRooms.Add(new PseudoRoom
            {
                PseudoRoomCode = "PM-002",
                PseudoRoomName = "Banquet Paymaster",
                PseudoRoomType = PseudoRoomType.BanquetAccount,
                LinkedSalesAccountId = salesAccounts.TryGetValue("Santos Wedding Events", out var weddingAccountId) ? weddingAccountId : null,
                LinkedBanquetEventId = banquetEventId,
                LinkedGroupBookingId = weddingGroup.Id,
                IsActive = true,
                Notes = DemoMarker,
                CreatedAt = DateTime.Now,
                CreatedBy = "DemoDataSeeder"
            });
            result.Inserted++;
        }

        await context.SaveChangesAsync();

        var pseudoRooms = await context.PseudoRooms
            .Where(item => item.PseudoRoomCode == "PM-001" || item.PseudoRoomCode == "PM-002")
            .ToDictionaryAsync(item => item.PseudoRoomCode, item => item.Id);
        int? GetPseudoRoomId(string code) => pseudoRooms.TryGetValue(code, out var id) ? id : null;
        var roomTypes = await context.RoomTypes
            .Where(item => item.Code == "STD" || item.Code == "DLX" || item.Code == "FAM")
            .ToDictionaryAsync(item => item.Code, item => item.Id);
        var ratePlanId = await context.RatePlans.Select(item => (int?)item.Id).FirstOrDefaultAsync();

        async Task EnsureBlocksAsync(GroupBooking group, string roomTypeCode, int roomsBlocked, decimal rateAmount)
        {
            if (!roomTypes.TryGetValue(roomTypeCode, out var roomTypeId))
            {
                return;
            }

            for (var date = group.ArrivalDate.Date; date < group.DepartureDate.Date; date = date.AddDays(1))
            {
                if (!await context.GroupRoomBlocks.AnyAsync(item => item.GroupBookingId == group.Id && item.RoomTypeId == roomTypeId && item.BlockDate == date))
                {
                    context.GroupRoomBlocks.Add(new GroupRoomBlock
                    {
                        GroupBookingId = group.Id,
                        RoomTypeId = roomTypeId,
                        BlockDate = date,
                        RoomsBlocked = roomsBlocked,
                        RoomsPickedUp = Math.Min(roomsBlocked, Math.Max(1, roomsBlocked - 2)),
                        RoomsReleased = 0,
                        RatePlanId = ratePlanId,
                        RateAmount = rateAmount,
                        CutOffDate = group.ArrivalDate.Date.AddDays(-3),
                        Notes = DemoMarker
                    });
                    result.Inserted++;
                }
            }
        }

        await EnsureBlocksAsync(abcGroup, "STD", 8, 2600m);
        await EnsureBlocksAsync(abcGroup, "DLX", 4, 3900m);
        await EnsureBlocksAsync(weddingGroup, "DLX", 6, 4200m);
        await EnsureBlocksAsync(weddingGroup, "FAM", 3, 6800m);
        await context.SaveChangesAsync();

        var demoReservations = await context.Reservations
            .Where(item => item.ConfirmationNumber.StartsWith("DEMO-"))
            .OrderBy(item => item.ArrivalDate)
            .Take(4)
            .ToListAsync();
        var reservationOffset = 0;
        foreach (var group in new[] { abcGroup, weddingGroup })
        {
            foreach (var reservation in demoReservations.Skip(reservationOffset).Take(2))
            {
                if (!await context.GroupMemberReservations.AnyAsync(item => item.GroupBookingId == group.Id && item.ReservationId == reservation.Id))
                {
                    context.GroupMemberReservations.Add(new GroupMemberReservation
                    {
                        GroupBookingId = group.Id,
                        ReservationId = reservation.Id,
                        IsPrimaryGuest = reservationOffset == 0,
                        BillingRoutingType = group.Id == abcGroup.Id ? BillingRoutingType.RoomAndTaxToMaster : BillingRoutingType.BanquetToMaster,
                        Notes = DemoMarker
                    });
                    result.Inserted++;
                }
            }

            reservationOffset += 2;
        }

        var demoFolios = await context.Folios
            .Where(item => item.FolioNumber.StartsWith("DEMO-FOL-"))
            .OrderBy(item => item.Id)
            .Take(2)
            .ToListAsync();

        async Task<GroupFolio?> EnsureGroupFolioAsync(GroupBooking group, string folioName, int? pseudoRoomId, int folioOffset)
        {
            var existing = await context.GroupFolios.FirstOrDefaultAsync(item => item.GroupBookingId == group.Id && item.FolioName == folioName);
            if (existing is not null)
            {
                return existing;
            }

            var folio = new GroupFolio
            {
                GroupBookingId = group.Id,
                PseudoRoomId = pseudoRoomId,
                FolioId = demoFolios.Count > folioOffset ? demoFolios[folioOffset].Id : null,
                FolioName = folioName,
                BillingName = group.GroupName,
                BillingAddress = "Vantage demo billing address",
                BillingTIN = "DEMO-TIN",
                Status = GroupFolioStatus.Open,
                CreatedAt = DateTime.Now,
                CreatedBy = "DemoDataSeeder",
                Notes = DemoMarker
            };
            context.GroupFolios.Add(folio);
            result.Inserted++;
            await context.SaveChangesAsync();
            return folio;
        }

        var abcFolio = await EnsureGroupFolioAsync(abcGroup, "ABC Corporate Training Master", GetPseudoRoomId("PM-001"), 0);
        var weddingFolio = await EnsureGroupFolioAsync(weddingGroup, "Santos-Reyes Wedding Master", GetPseudoRoomId("PM-002"), 1);

        async Task EnsureDepositAsync(GroupBooking group, decimal amount, string reference)
        {
            if (!await context.GroupDeposits.AnyAsync(item => item.GroupBookingId == group.Id && item.ReferenceNumber == reference))
            {
                context.GroupDeposits.Add(new GroupDeposit
                {
                    GroupBookingId = group.Id,
                    DepositDate = DateTime.Today,
                    Amount = amount,
                    PaymentMethod = "Bank Transfer",
                    ReferenceNumber = reference,
                    ReceivedBy = "DemoDataSeeder",
                    IsRefundable = true,
                    Status = GroupDepositStatus.Received,
                    Notes = DemoMarker
                });
                result.Inserted++;
            }
        }

        await EnsureDepositAsync(abcGroup, 50000m, "DEMO-GDEP-ABC");
        await EnsureDepositAsync(weddingGroup, 80000m, "DEMO-GDEP-WED");

        async Task EnsureRoutingRuleAsync(GroupBooking group, ChargeCategory category, GroupFolio? targetFolio)
        {
            if (targetFolio is null)
            {
                return;
            }

            if (!await context.ChargeRoutingRules.AnyAsync(item => item.GroupBookingId == group.Id && item.SourceChargeCategory == category && item.TargetGroupFolioId == targetFolio.Id))
            {
                context.ChargeRoutingRules.Add(new ChargeRoutingRule
                {
                    GroupBookingId = group.Id,
                    SourceChargeCategory = category,
                    RouteToType = RouteToType.GroupMasterFolio,
                    TargetGroupFolioId = targetFolio.Id,
                    IsActive = true,
                    Notes = DemoMarker
                });
                result.Inserted++;
            }
        }

        await EnsureRoutingRuleAsync(abcGroup, ChargeCategory.Room, abcFolio);
        await EnsureRoutingRuleAsync(abcGroup, ChargeCategory.Tax, abcFolio);
        await EnsureRoutingRuleAsync(weddingGroup, ChargeCategory.Banquet, weddingFolio);
        await EnsureRoutingRuleAsync(weddingGroup, ChargeCategory.FoodBeverage, weddingFolio);
        await context.SaveChangesAsync();
    }

    private async Task EnsureDemoAccountsPayableCoverageAsync(DemoSeedResult result)
    {
        var supplier = await context.Suppliers.FirstOrDefaultAsync(item => item.SupplierName == "ABC Food Supplies");
        if (supplier is null)
        {
            supplier = new InventorySupplier
            {
                SupplierName = "ABC Food Supplies",
                ContactPerson = "Demo AP Contact",
                Phone = "+63 2 8888 2000",
                Email = "abc.food.supplies@demo.example",
                Address = "Metro Manila",
                Terms = "30 days",
                IsActive = true,
                Notes = DemoMarker
            };
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var expenseAccountId = await context.GLAccounts
            .Where(account => account.AccountCode == "6200")
            .Select(account => (int?)account.Id)
            .FirstOrDefaultAsync();

        var invoice = await context.APInvoices.FirstOrDefaultAsync(item => item.InvoiceNumber == "DEMO-AP-CTRL-001");
        if (invoice is null)
        {
            invoice = new APInvoice
            {
                SupplierId = supplier.Id,
                InvoiceNumber = "DEMO-AP-CTRL-001",
                SupplierInvoiceNumber = "ABC-SI-2026-CTRL",
                InvoiceDate = DateTime.Today.AddDays(-10),
                DueDate = DateTime.Today.AddDays(10),
                SubTotal = 24000m,
                TaxAmount = 2880m,
                WithholdingTaxAmount = 480m,
                DiscountAmount = 0m,
                TotalAmount = 26400m,
                AmountPaid = 12000m,
                Balance = 14400m,
                Status = APInvoiceStatus.PartiallyPaid,
                Notes = $"{DemoMarker} AP control dialog coverage invoice.",
                CreatedAt = DateTime.Now,
                CreatedBy = "DemoDataSeeder",
                ApprovedBy = "finance@vantagepms.demo",
                ApprovedAt = DateTime.Today.AddDays(-8)
            };
            context.APInvoices.Add(invoice);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        if (!await context.APInvoiceLines.AnyAsync(item => item.APInvoiceId == invoice.Id && item.Description == "Demo supplier services for AP control"))
        {
            context.APInvoiceLines.Add(new APInvoiceLine
            {
                APInvoiceId = invoice.Id,
                GLAccountId = expenseAccountId,
                Description = "Demo supplier services for AP control",
                Quantity = 1,
                UnitCost = 24000m,
                TaxAmount = 2880m,
                WithholdingTaxAmount = 480m,
                LineTotal = 26400m
            });
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var voucher = await context.PaymentVouchers.FirstOrDefaultAsync(item => item.VoucherNumber == "DEMO-PV-CTRL-001");
        if (voucher is null)
        {
            voucher = new PaymentVoucher
            {
                VoucherNumber = "DEMO-PV-CTRL-001",
                SupplierId = supplier.Id,
                APInvoiceId = invoice.Id,
                VoucherDate = DateTime.Today.AddDays(-3),
                PaymentMethod = FinancePaymentMethod.BankTransfer,
                BankAccountName = "Vantage Demo Operating Account",
                BankReferenceNumber = "BANK-DEMO-PV-CTRL",
                Amount = 12000m,
                WithholdingTaxAmount = 480m,
                NetPaymentAmount = 11520m,
                Status = PaymentVoucherStatus.Released,
                PreparedBy = "DemoDataSeeder",
                ApprovedBy = "finance@vantagepms.demo",
                ApprovedAt = DateTime.Today.AddDays(-2),
                ReleasedBy = "finance@vantagepms.demo",
                ReleasedAt = DateTime.Today.AddDays(-1),
                Notes = $"{DemoMarker} payment voucher control dialog coverage."
            };
            context.PaymentVouchers.Add(voucher);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        if (!await context.Disbursements.AnyAsync(item => item.DisbursementNumber == "DEMO-DISB-CTRL-001"))
        {
            context.Disbursements.Add(new Disbursement
            {
                DisbursementNumber = "DEMO-DISB-CTRL-001",
                PaymentVoucherId = voucher.Id,
                SupplierId = supplier.Id,
                DisbursementDate = DateTime.Today.AddDays(-1),
                PaymentMethod = FinancePaymentMethod.BankTransfer,
                Amount = voucher.NetPaymentAmount,
                ReferenceNumber = voucher.BankReferenceNumber,
                PaidBy = "finance@vantagepms.demo",
                Status = DisbursementStatus.Cleared,
                Notes = DemoMarker
            });
            await context.SaveChangesAsync();
            result.Inserted++;
        }
    }

    private async Task EnsureDemoStockAdjustmentCoverageAsync(DemoSeedResult result)
    {
        var category = await context.InventoryCategories.FirstOrDefaultAsync(item => item.Name == "Linen");
        if (category is null)
        {
            category = new InventoryCategory { Name = "Linen", Description = DemoMarker, IsActive = true };
            context.InventoryCategories.Add(category);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var item = await context.InventoryItems.FirstOrDefaultAsync(existing => existing.ItemCode == "TOWEL");
        if (item is null)
        {
            item = new InventoryItem
            {
                InventoryCategoryId = category.Id,
                ItemCode = "TOWEL",
                ItemName = "Bath Towel",
                Description = DemoMarker,
                UnitOfMeasure = "pc",
                ReorderLevel = 80m,
                ParStockLevel = 200m,
                StandardCost = 220m,
                CurrentStock = 140m,
                IsActive = true,
                CreatedBy = "DemoDataSeeder"
            };
            context.InventoryItems.Add(item);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        var adjustment = await context.StockAdjustments.FirstOrDefaultAsync(existing => existing.AdjustmentNumber == "DEMO-SA-001");
        if (adjustment is null)
        {
            adjustment = new StockAdjustment
            {
                AdjustmentNumber = "DEMO-SA-001",
                AdjustmentDate = DateTime.Today,
                Status = StockAdjustmentStatus.ForApproval,
                Reason = "Demo physical count variance",
                PreparedBy = "DemoDataSeeder",
                Notes = $"{DemoMarker} stock adjustment control dialog coverage."
            };
            context.StockAdjustments.Add(adjustment);
            await context.SaveChangesAsync();
            result.Inserted++;
        }

        if (!await context.StockAdjustmentItems.AnyAsync(line => line.StockAdjustmentId == adjustment.Id && line.InventoryItemId == item.Id))
        {
            var actualQuantity = Math.Max(0m, item.CurrentStock - 5m);
            context.StockAdjustmentItems.Add(new StockAdjustmentItem
            {
                StockAdjustmentId = adjustment.Id,
                InventoryItemId = item.Id,
                SystemQuantity = item.CurrentStock,
                ActualQuantity = actualQuantity,
                VarianceQuantity = actualQuantity - item.CurrentStock,
                UnitCost = item.StandardCost,
                VarianceAmount = (actualQuantity - item.CurrentStock) * item.StandardCost,
                Notes = DemoMarker
            });
            await context.SaveChangesAsync();
            result.Inserted++;
        }
    }

    private async Task EnsureDemoGroupCollectionCoverageAsync(DemoSeedResult result)
    {
        var groups = await context.GroupBookings
            .Include(group => group.GroupFolios)
            .Include(group => group.Deposits)
            .Where(group => group.GroupCode == "DEMO-GRP-ABC" || group.GroupCode == "DEMO-GRP-WED")
            .ToListAsync();

        foreach (var group in groups)
        {
            var deposit = group.Deposits
                .Where(item => item.Status != GroupDepositStatus.Cancelled)
                .OrderByDescending(item => item.DepositDate)
                .FirstOrDefault();
            var groupFolio = group.GroupFolios.FirstOrDefault();
            if (deposit is null || groupFolio is null)
            {
                continue;
            }

            var marker = group.GroupCode == "DEMO-GRP-ABC" ? "DEMO-GALLOC-ABC" : "DEMO-GALLOC-WED";
            if (!await context.GroupPaymentAllocations.AnyAsync(item => item.GroupBookingId == group.Id && item.Notes != null && item.Notes.Contains(marker)))
            {
                context.GroupPaymentAllocations.Add(new GroupPaymentAllocation
                {
                    GroupBookingId = group.Id,
                    GroupDepositId = deposit.Id,
                    TargetFolioId = groupFolio.FolioId,
                    AllocatedAmount = Math.Min(deposit.Amount, group.GroupCode == "DEMO-GRP-ABC" ? 25000m : 40000m),
                    AllocationDate = DateTime.Today,
                    AllocatedBy = "DemoDataSeeder",
                    Notes = $"{marker} | {DemoMarker}"
                });
                result.Inserted++;
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task EnsureManagementAiAsync(DemoSeedResult result)
    {
        var rules = new[] { "High occupancy action", "Dirty room risk", "High guest balance risk", "Low stock warning", "Banquet BEO missing warning", "AR aging critical warning", "Low occupancy revenue opportunity" };
        foreach (var rule in rules)
        {
            if (!await context.AIRecommendationRules.AnyAsync(item => item.RuleName == rule))
            {
                context.AIRecommendationRules.Add(new AIRecommendationRule { RuleName = rule, Module = "Demo", ConditionDescription = $"{rule} demo condition", RecommendationText = $"Review {rule.ToLowerInvariant()} before the demo walkthrough.", Severity = ManagementInsightSeverity.Medium, IsActive = true });
                result.Inserted++;
            }
        }
        if (!await context.ManagementDailySummaries.AnyAsync(item => item.GeneratedBy == "DemoDataSeeder"))
        {
            var totalRooms = await context.Rooms.CountAsync(item => item.IsActive);
            var occupied = await context.Rooms.CountAsync(item => item.Status == RoomStatus.Occupied);
            context.ManagementDailySummaries.Add(new ManagementDailySummary { BusinessDate = DateTime.Today, TotalRooms = totalRooms, OccupiedRooms = occupied, AvailableRooms = await context.Rooms.CountAsync(item => item.Status == RoomStatus.Available), DirtyRooms = await context.Rooms.CountAsync(item => item.Status == RoomStatus.Dirty), OutOfOrderRooms = await context.Rooms.CountAsync(item => item.Status == RoomStatus.OutOfOrder), OccupancyPercentage = totalRooms == 0 ? 0 : Math.Round(occupied * 100m / totalRooms, 2), ArrivalsToday = await context.Reservations.CountAsync(item => item.ArrivalDate == DateTime.Today), DeparturesToday = await context.Reservations.CountAsync(item => item.DepartureDate == DateTime.Today), InHouseGuests = await context.Reservations.CountAsync(item => item.Status == ReservationStatus.CheckedIn), RoomRevenue = 185000, FBRevenue = 28500, BanquetRevenue = 120000, TotalRevenue = 333500, TotalPayments = 95000, OutstandingGuestBalances = 65000, ARBalance = await context.ARAccounts.SumAsync(item => item.CurrentBalance), OpenServiceRequests = await context.GuestServiceRequests.CountAsync(item => item.Status != GuestServiceRequestStatus.Completed), PendingHousekeepingTasks = await context.HousekeepingTasks.CountAsync(item => item.TaskStatus != HousekeepingTaskStatus.Completed), LowStockItems = await context.InventoryItems.CountAsync(item => item.CurrentStock <= item.ReorderLevel), PendingPurchaseRequests = await context.PurchaseRequests.CountAsync(item => item.Status == PurchaseRequestStatus.Submitted), PendingApprovals = await context.VoidRequests.CountAsync(item => item.Status == ApprovalStatus.Pending), SummaryText = $"Demo management summary for Vantage Grand Hotel. {DemoMarker}", GeneratedBy = "DemoDataSeeder" });
            result.Inserted++;
        }
        var insights = new[] { ("Critical AR overdue insight", ManagementInsightSeverity.Critical, ManagementInsightType.Financial), ("High dirty room insight", ManagementInsightSeverity.High, ManagementInsightType.Housekeeping), ("Medium revenue opportunity", ManagementInsightSeverity.Medium, ManagementInsightType.Revenue), ("Info banquet event summary", ManagementInsightSeverity.Info, ManagementInsightType.Banquet), ("Inventory low stock warning", ManagementInsightSeverity.Medium, ManagementInsightType.Inventory) };
        foreach (var insight in insights)
        {
            if (!await context.ManagementInsights.AnyAsync(item => item.Title == insight.Item1))
            {
                context.ManagementInsights.Add(new ManagementInsight { InsightDate = DateTime.Today, Severity = insight.Item2, InsightType = insight.Item3, Title = insight.Item1, Summary = $"{insight.Item1}. {DemoMarker}", Recommendation = "Use this record during the guided demo to show management actions.", RelatedModule = insight.Item3.ToString(), RelatedReferenceType = "Demo" });
                result.Inserted++;
            }
        }
    }

    private async Task EnsureExecutiveReportingAsync(DemoSeedResult result)
    {
        await ExecutiveSeedData.SeedAsync(context);
        var today = DateTime.Today;
        var periodStart = new DateTime(today.Year, today.Month, 1);
        var periodEnd = today;

        var kpis = await context.ExecutiveKPIs.Where(kpi => kpi.IsActive).ToListAsync();
        foreach (var kpi in kpis.Where(kpi => kpi.TargetValue is null))
        {
            kpi.TargetValue = kpi.KPICode switch
            {
                "OCC" => 75,
                "ADR" => 3800,
                "REVPAR" => 2600,
                "LABOR_PCT" => 28,
                _ => kpi.TargetValue
            };
        }

        if (!await context.ExecutiveReportSnapshots.AnyAsync(snapshot => snapshot.Notes != null && snapshot.Notes.Contains(DemoMarker)))
        {
            context.ExecutiveReportSnapshots.Add(new ExecutiveReportSnapshot
            {
                ReportDate = today,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ReportType = ExecutiveReportType.MonthlyOwnerReport,
                HotelName = "Vantage Grand Hotel",
                PreparedBy = "DemoDataSeeder",
                PreparedAt = DateTime.Now,
                OccupancyPercentage = 78.4m,
                ADR = 3850,
                RevPAR = 3018,
                TotalRoomRevenue = 1245000,
                TotalFBRevenue = 385000,
                TotalBanquetRevenue = 520000,
                TotalOtherRevenue = 85000,
                TotalRevenue = 2235000,
                TotalPayments = 1680000,
                GrossOperatingProfit = 740000,
                NetIncome = 510000,
                ARBalance = 420000,
                APBalance = 315000,
                LaborCost = 628000,
                LaborCostPercentage = 28.1m,
                GuestSatisfactionScore = 4.4m,
                OpenCriticalIssues = 2,
                SummaryText = "For the selected period, the hotel achieved 78.4% occupancy with ADR of ₱3,850 and RevPAR of ₱3,018. Total revenue reached ₱2.24M, led by Rooms and F&B. Labor cost was 28.1% of revenue, within the configured target. AR overdue remains a risk at ₱420,000. Management should prioritize collection follow-up and monitor dirty room turnaround during high-arrival days.",
                Notes = DemoMarker
            });
            result.Inserted++;
        }

        if (!await context.DepartmentPerformanceSnapshots.AnyAsync(snapshot => snapshot.Notes != null && snapshot.Notes.Contains(DemoMarker)))
        {
            var departmentRows = new[]
            {
                ("Rooms", 1245000m, 0m, 245000m, 180000m),
                ("Food and Beverage", 385000m, 128000m, 118000m, 45000m),
                ("Banquet", 520000m, 145000m, 98000m, 60000m),
                ("Administrative and General", 0m, 0m, 82000m, 135000m)
            };
            foreach (var row in departmentRows)
            {
                var profit = row.Item2 - row.Item3 - row.Item4 - row.Item5;
                context.DepartmentPerformanceSnapshots.Add(new DepartmentPerformanceSnapshot
                {
                    SnapshotDate = today,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    DepartmentName = row.Item1,
                    Revenue = row.Item2,
                    CostOfSales = row.Item3,
                    PayrollCost = row.Item4,
                    OtherExpenses = row.Item5,
                    DepartmentProfit = profit,
                    DepartmentProfitMargin = row.Item2 == 0 ? 0 : profit / row.Item2 * 100,
                    LaborCostPercentage = row.Item2 == 0 ? 0 : row.Item4 / row.Item2 * 100,
                    BudgetAmount = row.Item4 * 1.10m,
                    VarianceAmount = row.Item4 - row.Item4 * 1.10m,
                    VariancePercentage = -10,
                    Notes = DemoMarker
                });
                result.Inserted++;
            }
        }

        var demoAlerts = new[]
        {
            ("Critical AR overdue risk", ExecutiveAlertType.FinancialRisk, KPIStatus.Critical, "Accounts Receivable", "Overdue city ledger balances require owner review.", "Prioritize collections for corporate and event accounts before month-end."),
            ("Dirty room turnaround pressure", ExecutiveAlertType.OperationalRisk, KPIStatus.Warning, "Housekeeping", "Dirty room levels are elevated during high-arrival days.", "Assign additional attendants to due-out rooms and inspect VIP arrivals first."),
            ("Low occupancy revenue opportunity", ExecutiveAlertType.RevenueOpportunity, KPIStatus.Watch, "Revenue", "Several forward dates are below optimal occupancy.", "Launch direct booking and corporate account pickup actions.")
        };
        foreach (var alert in demoAlerts)
        {
            if (!await context.ExecutiveAlerts.AnyAsync(item => item.Title == alert.Item1 && !item.IsResolved))
            {
                context.ExecutiveAlerts.Add(new ExecutiveAlert
                {
                    AlertDate = today,
                    AlertType = alert.Item2,
                    Severity = alert.Item3,
                    Module = alert.Item4,
                    Title = alert.Item1,
                    Message = alert.Item5,
                    RecommendedAction = alert.Item6,
                    RelatedReferenceType = "Demo",
                    CreatedAt = DateTime.Now
                });
                result.Inserted++;
            }
        }

        if (!await context.OwnerReportPackages.AnyAsync(package => package.PackageName == "Demo Owner Report Package"))
        {
            var package = new OwnerReportPackage
            {
                PackageName = "Demo Owner Report Package",
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                PreparedFor = "Sample Hotel Owner",
                PreparedBy = "DemoDataSeeder",
                PreparedAt = DateTime.Now,
                Status = OwnerReportPackageStatus.Ready,
                Notes = DemoMarker
            };
            var sort = 10;
            foreach (var item in new[] { "Cover Page", "Executive Summary", "KPI Scorecard", "Department Performance", "Financial Summary", "Guest Experience", "Labor Cost", "Revenue Intelligence", "Cost Control", "Management Action Items" })
            {
                package.Items.Add(new OwnerReportPackageItem
                {
                    ReportName = item,
                    ReportType = item,
                    SortOrder = sort,
                    IsIncluded = true,
                    Notes = DemoMarker
                });
                sort += 10;
            }
            context.OwnerReportPackages.Add(package);
            result.Inserted += 1 + package.Items.Count;
        }
    }

    private static async Task<DemoReadinessItem> ReadyAsync(string module, Func<Task<bool>> primary, Func<Task<bool>> secondary)
    {
        var first = await primary();
        var second = await secondary();
        var status = first && second ? "Ready" : first || second ? "Needs Review" : "Not Configured";
        return new DemoReadinessItem(module, status);
    }

    private async Task LogAsync(DemoSeedResult result, string userName)
    {
        await auditLogService.LogAsync(AuditActionType.Generate, "Demo", "DemoDataSeeder", result.ActionName, null, new { result.ActionName, result.Inserted, result.Messages }, userName);
    }
}

internal sealed record DemoJournalEntrySeed(string JournalNumber, DateTime JournalDate, string Description, IReadOnlyCollection<DemoJournalLineSeed> Lines);

internal sealed record DemoJournalLineSeed(string AccountCode, decimal DebitAmount, decimal CreditAmount, string Description, string? UsaliDepartmentCode);

public class DemoSeedResult(string actionName)
{
    public string ActionName { get; } = actionName;
    public int Inserted { get; set; }
    public IList<string> Messages { get; } = new List<string>();
}

public class DemoDataStatus
{
    public bool DemoModeEnabled { get; set; }
    public int Hotels { get; set; }
    public int Rooms { get; set; }
    public int Guests { get; set; }
    public int Reservations { get; set; }
    public int Folios { get; set; }
    public int PosOrders { get; set; }
    public int BanquetEvents { get; set; }
    public int InventoryItems { get; set; }
    public int ARInvoices { get; set; }
    public int APInvoices { get; set; }
    public int PaymentVouchers { get; set; }
    public int StockAdjustments { get; set; }
    public int GroupBookings { get; set; }
    public int FinanceClosePackRecords { get; set; }
    public int LaborProfiles { get; set; }
    public int ManagementInsights { get; set; }
}

public record DemoReadinessItem(string Module, string Status);
