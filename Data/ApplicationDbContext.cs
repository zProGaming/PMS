using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Banquet;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Models.Core;
using Vantage.PMS.Models.Executive;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Models.Housekeeping;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Models.ManagementAI;
using Vantage.PMS.Models.Revenue;
using Vantage.PMS.Models.Reports;
using Vantage.PMS.Models.Sales;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : IdentityDbContext(options)
    {
        private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);
        private bool _isSavingAudit;

        public DbSet<Hotel> Hotels => Set<Hotel>();

        public DbSet<HotelUserAccess> HotelUserAccesses => Set<HotelUserAccess>();

        public DbSet<Property> Properties => Set<Property>();

        public DbSet<Department> Departments => Set<Department>();

        public DbSet<RoomType> RoomTypes => Set<RoomType>();

        public DbSet<Room> Rooms => Set<Room>();

        public DbSet<Guest> Guests => Set<Guest>();

        public DbSet<Reservation> Reservations => Set<Reservation>();

        public DbSet<Folio> Folios => Set<Folio>();

        public DbSet<FolioItem> FolioItems => Set<FolioItem>();

        public DbSet<Payment> Payments => Set<Payment>();

        public DbSet<ChargeCode> ChargeCodes => Set<ChargeCode>();

        public DbSet<CashierShift> CashierShifts => Set<CashierShift>();

        public DbSet<CashierTransaction> CashierTransactions => Set<CashierTransaction>();

        public DbSet<CashDrop> CashDrops => Set<CashDrop>();

        public DbSet<RefundTransaction> RefundTransactions => Set<RefundTransaction>();

        public DbSet<VoidRequest> VoidRequests => Set<VoidRequest>();

        public DbSet<DiscountApproval> DiscountApprovals => Set<DiscountApproval>();

        public DbSet<FinanceDocument> FinanceDocuments => Set<FinanceDocument>();

        public DbSet<FinanceDocumentLine> FinanceDocumentLines => Set<FinanceDocumentLine>();

        public DbSet<ARAccount> ARAccounts => Set<ARAccount>();

        public DbSet<ARInvoice> ARInvoices => Set<ARInvoice>();

        public DbSet<ARPayment> ARPayments => Set<ARPayment>();

        public DbSet<ARPaymentAllocation> ARPaymentAllocations => Set<ARPaymentAllocation>();

        public DbSet<CreditMemo> CreditMemos => Set<CreditMemo>();

        public DbSet<DebitMemo> DebitMemos => Set<DebitMemo>();

        public DbSet<DocumentNumberSequence> DocumentNumberSequences => Set<DocumentNumberSequence>();

        public DbSet<GLAccount> GLAccounts => Set<GLAccount>();

        public DbSet<USALIDepartment> USALIDepartments => Set<USALIDepartment>();

        public DbSet<USALIReportLine> USALIReportLines => Set<USALIReportLine>();

        public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();

        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

        public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

        public DbSet<PostingRule> PostingRules => Set<PostingRule>();

        public DbSet<PostingBatch> PostingBatches => Set<PostingBatch>();

        public DbSet<PostingBatchItem> PostingBatchItems => Set<PostingBatchItem>();

        public DbSet<TaxCode> TaxCodes => Set<TaxCode>();

        public DbSet<ServiceChargeSetting> ServiceChargeSettings => Set<ServiceChargeSetting>();

        public DbSet<PhilippineTaxReportLine> PhilippineTaxReportLines => Set<PhilippineTaxReportLine>();

        public DbSet<AccountingReportSnapshot> AccountingReportSnapshots => Set<AccountingReportSnapshot>();

        public DbSet<AccountingReportSnapshotLine> AccountingReportSnapshotLines => Set<AccountingReportSnapshotLine>();

        public DbSet<AccountingExportLog> AccountingExportLogs => Set<AccountingExportLog>();

        public DbSet<APInvoice> APInvoices => Set<APInvoice>();

        public DbSet<APInvoiceLine> APInvoiceLines => Set<APInvoiceLine>();

        public DbSet<PaymentVoucher> PaymentVouchers => Set<PaymentVoucher>();

        public DbSet<Disbursement> Disbursements => Set<Disbursement>();

        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

        public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();

        public DbSet<BankReconciliation> BankReconciliations => Set<BankReconciliation>();

        public DbSet<BankReconciliationItem> BankReconciliationItems => Set<BankReconciliationItem>();

        public DbSet<AccrualEntry> AccrualEntries => Set<AccrualEntry>();

        public DbSet<MonthEndCloseChecklist> MonthEndCloseChecklists => Set<MonthEndCloseChecklist>();

        public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();

        public DbSet<BusinessDateSetting> BusinessDateSettings => Set<BusinessDateSetting>();

        public DbSet<NightAudit> NightAudits => Set<NightAudit>();

        public DbSet<Outlet> Outlets => Set<Outlet>();

        public DbSet<DiningTable> DiningTables => Set<DiningTable>();

        public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();

        public DbSet<MenuItem> MenuItems => Set<MenuItem>();

        public DbSet<KitchenStation> KitchenStations => Set<KitchenStation>();

        public DbSet<POSOrder> POSOrders => Set<POSOrder>();

        public DbSet<POSOrderItem> POSOrderItems => Set<POSOrderItem>();

        public DbSet<SalesAccount> SalesAccounts => Set<SalesAccount>();

        public DbSet<ContactPerson> ContactPersons => Set<ContactPerson>();

        public DbSet<SalesLead> SalesLeads => Set<SalesLead>();

        public DbSet<SalesActivity> SalesActivities => Set<SalesActivity>();

        public DbSet<FunctionRoom> FunctionRooms => Set<FunctionRoom>();

        public DbSet<BanquetEvent> BanquetEvents => Set<BanquetEvent>();

        public DbSet<BanquetEventOrder> BanquetEventOrders => Set<BanquetEventOrder>();

        public DbSet<BanquetPackage> BanquetPackages => Set<BanquetPackage>();

        public DbSet<BanquetCharge> BanquetCharges => Set<BanquetCharge>();

        public DbSet<RatePlan> RatePlans => Set<RatePlan>();

        public DbSet<RoomTypeRate> RoomTypeRates => Set<RoomTypeRate>();

        public DbSet<SeasonalRate> SeasonalRates => Set<SeasonalRate>();

        public DbSet<RateRestriction> RateRestrictions => Set<RateRestriction>();

        public DbSet<RoomInventoryControl> RoomInventoryControls => Set<RoomInventoryControl>();

        public DbSet<PromotionCode> PromotionCodes => Set<PromotionCode>();

        public DbSet<BookingEngineSetting> BookingEngineSettings => Set<BookingEngineSetting>();

        public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();

        public DbSet<BookingEngineRoomContent> BookingEngineRoomContents => Set<BookingEngineRoomContent>();

        public DbSet<BookingAddOn> BookingAddOns => Set<BookingAddOn>();

        public DbSet<BookingRequestAddOn> BookingRequestAddOns => Set<BookingRequestAddOn>();

        public DbSet<GuestPortalSetting> GuestPortalSettings => Set<GuestPortalSetting>();

        public DbSet<GuestPortalAccess> GuestPortalAccesses => Set<GuestPortalAccess>();

        public DbSet<GuestPreCheckIn> GuestPreCheckIns => Set<GuestPreCheckIn>();

        public DbSet<GuestServiceRequest> GuestServiceRequests => Set<GuestServiceRequest>();

        public DbSet<GuestFeedback> GuestFeedbacks => Set<GuestFeedback>();

        public DbSet<ExpressCheckoutRequest> ExpressCheckoutRequests => Set<ExpressCheckoutRequest>();

        public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();

        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

        public DbSet<Supplier> Suppliers => Set<Supplier>();

        public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();

        public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();

        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

        public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

        public DbSet<ReceivingRecord> ReceivingRecords => Set<ReceivingRecord>();

        public DbSet<ReceivingRecordItem> ReceivingRecordItems => Set<ReceivingRecordItem>();

        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

        public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();

        public DbSet<StockAdjustmentItem> StockAdjustmentItems => Set<StockAdjustmentItem>();

        public DbSet<ManagementInsight> ManagementInsights => Set<ManagementInsight>();

        public DbSet<ManagementDailySummary> ManagementDailySummaries => Set<ManagementDailySummary>();

        public DbSet<AIRecommendationRule> AIRecommendationRules => Set<AIRecommendationRule>();

        public DbSet<AIActionLog> AIActionLogs => Set<AIActionLog>();

        public DbSet<AIIntegrationSetting> AIIntegrationSettings => Set<AIIntegrationSetting>();

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        public DbSet<SystemErrorLog> SystemErrorLogs => Set<SystemErrorLog>();

        public DbSet<SystemNotification> SystemNotifications => Set<SystemNotification>();

        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

        public DbSet<DataValidationIssue> DataValidationIssues => Set<DataValidationIssue>();

        public DbSet<QATestChecklistItem> QATestChecklistItems => Set<QATestChecklistItem>();

        public DbSet<DocumentTemplateSetting> DocumentTemplateSettings => Set<DocumentTemplateSetting>();

        public DbSet<ClientDemoPackage> ClientDemoPackages => Set<ClientDemoPackage>();

        public DbSet<ClientDemoPackageItem> ClientDemoPackageItems => Set<ClientDemoPackageItem>();

        public DbSet<EmployeeCostProfile> EmployeeCostProfiles => Set<EmployeeCostProfile>();

        public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();

        public DbSet<PayrollCostEntry> PayrollCostEntries => Set<PayrollCostEntry>();

        public DbSet<PayrollAllocationRule> PayrollAllocationRules => Set<PayrollAllocationRule>();

        public DbSet<DepartmentLaborBudget> DepartmentLaborBudgets => Set<DepartmentLaborBudget>();

        public DbSet<ServiceChargePool> ServiceChargePools => Set<ServiceChargePool>();

        public DbSet<ServiceChargeDistributionLine> ServiceChargeDistributionLines => Set<ServiceChargeDistributionLine>();

        public DbSet<LaborProductivitySnapshot> LaborProductivitySnapshots => Set<LaborProductivitySnapshot>();

        public DbSet<ExecutiveReportSnapshot> ExecutiveReportSnapshots => Set<ExecutiveReportSnapshot>();

        public DbSet<ExecutiveKPI> ExecutiveKPIs => Set<ExecutiveKPI>();

        public DbSet<ExecutiveKPIResult> ExecutiveKPIResults => Set<ExecutiveKPIResult>();

        public DbSet<DepartmentPerformanceSnapshot> DepartmentPerformanceSnapshots => Set<DepartmentPerformanceSnapshot>();

        public DbSet<ExecutiveAlert> ExecutiveAlerts => Set<ExecutiveAlert>();

        public DbSet<OwnerReportPackage> OwnerReportPackages => Set<OwnerReportPackage>();

        public DbSet<OwnerReportPackageItem> OwnerReportPackageItems => Set<OwnerReportPackageItem>();

        public DbSet<KPIBenchmarkSetting> KPIBenchmarkSettings => Set<KPIBenchmarkSetting>();

        public DbSet<CashFlowCategory> CashFlowCategories => Set<CashFlowCategory>();

        public DbSet<CashFlowMappingRule> CashFlowMappingRules => Set<CashFlowMappingRule>();

        public DbSet<CashFlowReportSnapshot> CashFlowReportSnapshots => Set<CashFlowReportSnapshot>();

        public DbSet<CashFlowReportSnapshotLine> CashFlowReportSnapshotLines => Set<CashFlowReportSnapshotLine>();

        public DbSet<CashAccountSetting> CashAccountSettings => Set<CashAccountSetting>();

        public DbSet<ReportTemplateSetting> ReportTemplateSettings => Set<ReportTemplateSetting>();

        public DbSet<ReportExportLog> ReportExportLogs => Set<ReportExportLog>();

        public DbSet<SavedReportRun> SavedReportRuns => Set<SavedReportRun>();

        public DbSet<ReportCatalogItem> ReportCatalogItems => Set<ReportCatalogItem>();

        public DbSet<PseudoRoom> PseudoRooms => Set<PseudoRoom>();

        public DbSet<GroupBooking> GroupBookings => Set<GroupBooking>();

        public DbSet<GroupRoomBlock> GroupRoomBlocks => Set<GroupRoomBlock>();

        public DbSet<GroupMemberReservation> GroupMemberReservations => Set<GroupMemberReservation>();

        public DbSet<GroupFolio> GroupFolios => Set<GroupFolio>();

        public DbSet<ChargeRoutingRule> ChargeRoutingRules => Set<ChargeRoutingRule>();

        public DbSet<GroupDeposit> GroupDeposits => Set<GroupDeposit>();

        public DbSet<GroupPaymentAllocation> GroupPaymentAllocations => Set<GroupPaymentAllocation>();

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_isSavingAudit)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            var auditEntries = BuildAuditEntries();
            var result = await base.SaveChangesAsync(cancellationToken);

            if (auditEntries.Count == 0)
            {
                return result;
            }

            foreach (var auditEntry in auditEntries)
            {
                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            _isSavingAudit = true;
            try
            {
                await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _isSavingAudit = false;
            }

            return result;
        }

        private List<PendingAuditEntry> BuildAuditEntries()
        {
            ChangeTracker.DetectChanges();
            var httpContext = httpContextAccessor?.HttpContext;
            var userId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = httpContext?.User.Identity?.Name ?? "System";
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
            var entries = new List<PendingAuditEntry>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog or SystemErrorLog)
                {
                    continue;
                }

                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                {
                    continue;
                }

                var entityName = entry.Metadata.ClrType.Name;
                var auditEntry = new PendingAuditEntry(entry)
                {
                    UserId = userId,
                    UserName = userName,
                    EntityName = entityName,
                    Action = ResolveAuditAction(entry),
                    Module = InferModule(entry.Metadata.ClrType),
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.Now
                };

                CapturePropertyValues(entry, auditEntry);
                if (auditEntry.OldValues.Count > 0 || auditEntry.NewValues.Count > 0)
                {
                    entries.Add(auditEntry);
                }
            }

            return entries;
        }

        private static void CapturePropertyValues(EntityEntry entry, PendingAuditEntry auditEntry)
        {
            foreach (var property in entry.Properties)
            {
                var propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = SafeAuditValue(propertyName, property.CurrentValue);
                        break;
                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = SafeAuditValue(propertyName, property.OriginalValue);
                        break;
                    case EntityState.Modified when property.IsModified:
                        auditEntry.OldValues[propertyName] = SafeAuditValue(propertyName, property.OriginalValue);
                        auditEntry.NewValues[propertyName] = SafeAuditValue(propertyName, property.CurrentValue);
                        break;
                }
            }
        }

        private static AuditActionType ResolveAuditAction(EntityEntry entry)
        {
            if (entry.State == EntityState.Added)
            {
                return AuditActionType.Create;
            }

            if (entry.State == EntityState.Deleted)
            {
                return AuditActionType.Delete;
            }

            if (entry.Properties.Any(property =>
                    property.Metadata.Name == "IsActive" &&
                    property.IsModified &&
                    property.CurrentValue is false))
            {
                return AuditActionType.SoftDelete;
            }

            if (entry.Properties.Any(property =>
                    property.Metadata.Name == "IsVoided" &&
                    property.IsModified &&
                    property.CurrentValue is true))
            {
                return AuditActionType.Void;
            }

            var status = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "Status" && property.IsModified)?.CurrentValue?.ToString();
            return status switch
            {
                "Approved" => AuditActionType.Approve,
                "Rejected" => AuditActionType.Reject,
                "Cancelled" or "Canceled" or "NoShow" => AuditActionType.Cancel,
                "Voided" => AuditActionType.Void,
                "Posted" or "Completed" or "Processed" or "Closed" or "Issued" or "CheckedIn" or "CheckedOut" => AuditActionType.Post,
                _ => AuditActionType.Update
            };
        }

        private static object? SafeAuditValue(string propertyName, object? value)
        {
            if (value is null)
            {
                return null;
            }

            var normalized = propertyName.ToUpperInvariant();
            if (normalized.Contains("PASSWORD") || normalized.Contains("APIKEY") || normalized.Contains("SECRET") || normalized.Contains("TOKEN"))
            {
                return "***";
            }

            return value;
        }

        private static string InferModule(Type entityType)
        {
            var namespaceName = entityType.Namespace ?? string.Empty;
            if (!namespaceName.Contains(".Models."))
            {
                return "System";
            }

            var module = namespaceName.Split(".Models.", StringSplitOptions.RemoveEmptyEntries).Last().Split('.').FirstOrDefault() ?? "System";
            return module switch
            {
                "FrontOffice" => "Front Office",
                "FoodBeverage" => "F&B",
                "GuestPortal" => "Guest Portal",
                "ManagementAI" => "Management AI",
                "Accounting" => "Accounting",
                "SystemAdministration" => "System",
                _ => module
            };
        }

        private sealed class PendingAuditEntry(EntityEntry entry)
        {
            public EntityEntry Entry { get; } = entry;
            public string? UserId { get; init; }
            public string? UserName { get; init; }
            public AuditActionType Action { get; init; }
            public string EntityName { get; init; } = string.Empty;
            public string Module { get; init; } = string.Empty;
            public string? IpAddress { get; init; }
            public string? UserAgent { get; init; }
            public DateTime CreatedAt { get; init; }
            public Dictionary<string, object?> OldValues { get; } = [];
            public Dictionary<string, object?> NewValues { get; } = [];

            public AuditLog ToAuditLog()
            {
                var primaryKey = Entry.Metadata.FindPrimaryKey();
                var entityId = primaryKey is null
                    ? null
                    : string.Join(",", primaryKey.Properties.Select(property => Entry.Property(property.Name).CurrentValue?.ToString()));

                return new AuditLog
                {
                    UserId = UserId,
                    UserName = UserName,
                    Action = Action,
                    EntityName = EntityName,
                    EntityId = entityId,
                    OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues, AuditJsonOptions),
                    NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues, AuditJsonOptions),
                    IpAddress = IpAddress,
                    UserAgent = UserAgent,
                    CreatedAt = CreatedAt,
                    Module = Module
                };
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RoomType>()
                .Property(roomType => roomType.BaseRate)
                .HasPrecision(18, 2);

            builder.Entity<Reservation>()
                .Property(reservation => reservation.RateAmount)
                .HasPrecision(18, 2);

            builder.Entity<RoomTypeRate>()
                .Property(rate => rate.BaseRate)
                .HasPrecision(18, 2);

            builder.Entity<RoomTypeRate>()
                .Property(rate => rate.ExtraAdultRate)
                .HasPrecision(18, 2);

            builder.Entity<RoomTypeRate>()
                .Property(rate => rate.ExtraChildRate)
                .HasPrecision(18, 2);

            builder.Entity<SeasonalRate>()
                .Property(rate => rate.Rate)
                .HasPrecision(18, 2);

            builder.Entity<SeasonalRate>()
                .Property(rate => rate.ExtraAdultRate)
                .HasPrecision(18, 2);

            builder.Entity<SeasonalRate>()
                .Property(rate => rate.ExtraChildRate)
                .HasPrecision(18, 2);

            builder.Entity<PromotionCode>()
                .Property(promotion => promotion.DiscountValue)
                .HasPrecision(18, 2);

            builder.Entity<BookingEngineSetting>()
                .Property(setting => setting.DepositPercentage)
                .HasPrecision(18, 2);

            builder.Entity<BookingRequest>()
                .Property(request => request.RoomRate)
                .HasPrecision(18, 2);

            builder.Entity<BookingRequest>()
                .Property(request => request.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<BookingRequest>()
                .Property(request => request.TotalRoomAmount)
                .HasPrecision(18, 2);

            builder.Entity<BookingRequest>()
                .Property(request => request.DepositAmount)
                .HasPrecision(18, 2);

            builder.Entity<BookingAddOn>()
                .Property(addOn => addOn.Price)
                .HasPrecision(18, 2);

            builder.Entity<BookingRequestAddOn>()
                .Property(addOn => addOn.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<BookingRequestAddOn>()
                .Property(addOn => addOn.Amount)
                .HasPrecision(18, 2);

            builder.Entity<InventoryItem>()
                .Property(item => item.ReorderLevel)
                .HasPrecision(18, 2);

            builder.Entity<InventoryItem>()
                .Property(item => item.ParStockLevel)
                .HasPrecision(18, 2);

            builder.Entity<InventoryItem>()
                .Property(item => item.StandardCost)
                .HasPrecision(18, 2);

            builder.Entity<InventoryItem>()
                .Property(item => item.CurrentStock)
                .HasPrecision(18, 2);

            builder.Entity<PurchaseRequestItem>()
                .Property(item => item.Quantity)
                .HasPrecision(18, 2);

            builder.Entity<PurchaseRequestItem>()
                .Property(item => item.EstimatedUnitCost)
                .HasPrecision(18, 2);

            builder.Entity<PurchaseRequestItem>()
                .Property(item => item.EstimatedAmount)
                .HasPrecision(18, 2);

            builder.Entity<PurchaseOrder>()
                .Property(order => order.SubTotal)
                .HasPrecision(18, 2);

            builder.Entity<PurchaseOrder>()
                .Property(order => order.TaxAmount)
                .HasPrecision(18, 2);

            builder.Entity<PurchaseOrder>()
                .Property(order => order.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<PurchaseOrder>()
                .Property(order => order.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<PurchaseOrderItem>()
                .Property(item => item.Quantity)
                .HasPrecision(18, 2);

            builder.Entity<PurchaseOrderItem>()
                .Property(item => item.UnitCost)
                .HasPrecision(18, 2);

            builder.Entity<PurchaseOrderItem>()
                .Property(item => item.Amount)
                .HasPrecision(18, 2);

            builder.Entity<ReceivingRecordItem>()
                .Property(item => item.QuantityReceived)
                .HasPrecision(18, 2);

            builder.Entity<ReceivingRecordItem>()
                .Property(item => item.UnitCost)
                .HasPrecision(18, 2);

            builder.Entity<ReceivingRecordItem>()
                .Property(item => item.Amount)
                .HasPrecision(18, 2);

            builder.Entity<StockMovement>()
                .Property(movement => movement.Quantity)
                .HasPrecision(18, 2);

            builder.Entity<StockMovement>()
                .Property(movement => movement.UnitCost)
                .HasPrecision(18, 2);

            builder.Entity<StockAdjustmentItem>()
                .Property(item => item.SystemQuantity)
                .HasPrecision(18, 2);

            builder.Entity<StockAdjustmentItem>()
                .Property(item => item.ActualQuantity)
                .HasPrecision(18, 2);

            builder.Entity<StockAdjustmentItem>()
                .Property(item => item.VarianceQuantity)
                .HasPrecision(18, 2);

            builder.Entity<StockAdjustmentItem>()
                .Property(item => item.UnitCost)
                .HasPrecision(18, 2);

            builder.Entity<StockAdjustmentItem>()
                .Property(item => item.VarianceAmount)
                .HasPrecision(18, 2);

            builder.Entity<ManagementDailySummary>()
                .Property(summary => summary.OccupancyPercentage)
                .HasPrecision(18, 2);

            builder.Entity<ManagementDailySummary>()
                .Property(summary => summary.RoomRevenue)
                .HasPrecision(18, 2);

            builder.Entity<ManagementDailySummary>()
                .Property(summary => summary.FBRevenue)
                .HasPrecision(18, 2);

            builder.Entity<ManagementDailySummary>()
                .Property(summary => summary.BanquetRevenue)
                .HasPrecision(18, 2);

            builder.Entity<ManagementDailySummary>()
                .Property(summary => summary.TotalRevenue)
                .HasPrecision(18, 2);

            builder.Entity<ManagementDailySummary>()
                .Property(summary => summary.TotalPayments)
                .HasPrecision(18, 2);

            builder.Entity<ManagementDailySummary>()
                .Property(summary => summary.OutstandingGuestBalances)
                .HasPrecision(18, 2);

            builder.Entity<ManagementDailySummary>()
                .Property(summary => summary.ARBalance)
                .HasPrecision(18, 2);

            builder.Entity<FolioItem>()
                .Property(folioItem => folioItem.Quantity)
                .HasPrecision(18, 2);

            builder.Entity<FolioItem>()
                .Property(folioItem => folioItem.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<FolioItem>()
                .Property(folioItem => folioItem.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Payment>()
                .Property(payment => payment.Amount)
                .HasPrecision(18, 2);

            builder.Entity<ChargeCode>()
                .Property(chargeCode => chargeCode.DefaultAmount)
                .HasPrecision(18, 2);

            builder.Entity<CashierShift>()
                .Property(shift => shift.OpeningCashFloat)
                .HasPrecision(18, 2);

            builder.Entity<CashierShift>()
                .Property(shift => shift.ClosingCashCount)
                .HasPrecision(18, 2);

            builder.Entity<CashierShift>()
                .Property(shift => shift.ExpectedCashAmount)
                .HasPrecision(18, 2);

            builder.Entity<CashierShift>()
                .Property(shift => shift.CashOverShort)
                .HasPrecision(18, 2);

            builder.Entity<CashierTransaction>()
                .Property(transaction => transaction.Amount)
                .HasPrecision(18, 2);

            builder.Entity<CashDrop>()
                .Property(drop => drop.Amount)
                .HasPrecision(18, 2);

            builder.Entity<RefundTransaction>()
                .Property(refund => refund.Amount)
                .HasPrecision(18, 2);

            builder.Entity<DiscountApproval>()
                .Property(discount => discount.DiscountValue)
                .HasPrecision(18, 2);

            builder.Entity<DiscountApproval>()
                .Property(discount => discount.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocument>()
                .Property(document => document.SubTotal)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocument>()
                .Property(document => document.TaxAmount)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocument>()
                .Property(document => document.ServiceCharge)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocument>()
                .Property(document => document.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocument>()
                .Property(document => document.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocument>()
                .Property(document => document.AmountPaid)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocument>()
                .Property(document => document.Balance)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocumentLine>()
                .Property(line => line.Quantity)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocumentLine>()
                .Property(line => line.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocumentLine>()
                .Property(line => line.TaxAmount)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocumentLine>()
                .Property(line => line.ServiceCharge)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocumentLine>()
                .Property(line => line.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<FinanceDocumentLine>()
                .Property(line => line.LineTotal)
                .HasPrecision(18, 2);

            builder.Entity<ARAccount>()
                .Property(account => account.CreditLimit)
                .HasPrecision(18, 2);

            builder.Entity<ARAccount>()
                .Property(account => account.CurrentBalance)
                .HasPrecision(18, 2);

            builder.Entity<ARInvoice>()
                .Property(invoice => invoice.OriginalAmount)
                .HasPrecision(18, 2);

            builder.Entity<ARInvoice>()
                .Property(invoice => invoice.AmountPaid)
                .HasPrecision(18, 2);

            builder.Entity<ARInvoice>()
                .Property(invoice => invoice.Balance)
                .HasPrecision(18, 2);

            builder.Entity<ARPayment>()
                .Property(payment => payment.Amount)
                .HasPrecision(18, 2);

            builder.Entity<ARPaymentAllocation>()
                .Property(allocation => allocation.AllocatedAmount)
                .HasPrecision(18, 2);

            builder.Entity<CreditMemo>()
                .Property(memo => memo.Amount)
                .HasPrecision(18, 2);

            builder.Entity<DebitMemo>()
                .Property(memo => memo.Amount)
                .HasPrecision(18, 2);

            builder.Entity<MenuItem>()
                .Property(menuItem => menuItem.Price)
                .HasPrecision(18, 2);

            builder.Entity<POSOrder>()
                .Property(order => order.SubTotal)
                .HasPrecision(18, 2);

            builder.Entity<POSOrder>()
                .Property(order => order.ServiceCharge)
                .HasPrecision(18, 2);

            builder.Entity<POSOrder>()
                .Property(order => order.TaxAmount)
                .HasPrecision(18, 2);

            builder.Entity<POSOrder>()
                .Property(order => order.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<POSOrder>()
                .Property(order => order.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<POSOrderItem>()
                .Property(item => item.Quantity)
                .HasPrecision(18, 2);

            builder.Entity<POSOrderItem>()
                .Property(item => item.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<POSOrderItem>()
                .Property(item => item.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<POSOrderItem>()
                .Property(item => item.LineTotal)
                .HasPrecision(18, 2);

            builder.Entity<SalesAccount>()
                .Property(account => account.CreditLimit)
                .HasPrecision(18, 2);

            builder.Entity<SalesAccount>()
                .Property(account => account.AccountType)
                .HasConversion<string>();

            builder.Entity<SalesLead>()
                .Property(lead => lead.EstimatedValue)
                .HasPrecision(18, 2);

            builder.Entity<SalesActivity>()
                .Property(activity => activity.ActivityType)
                .HasConversion<string>();

            builder.Entity<FunctionRoom>()
                .Property(room => room.Rate)
                .HasPrecision(18, 2);

            builder.Entity<BanquetPackage>()
                .Property(package => package.PricePerPax)
                .HasPrecision(18, 2);

            builder.Entity<BanquetCharge>()
                .Property(charge => charge.Quantity)
                .HasPrecision(18, 2);

            builder.Entity<BanquetCharge>()
                .Property(charge => charge.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<BanquetCharge>()
                .Property(charge => charge.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Hotel>(entity =>
            {
                entity.HasIndex(hotel => hotel.Code).IsUnique();
                entity.Property(hotel => hotel.Code).HasMaxLength(80);
            });

            builder.Entity<HotelUserAccess>(entity =>
            {
                entity.HasIndex(access => new { access.UserId, access.HotelId }).IsUnique();
                entity.Property(access => access.UserId).HasMaxLength(450);
                entity.Property(access => access.RoleName).HasMaxLength(120);
                entity.Property(access => access.CreatedBy).HasMaxLength(180);

                entity.HasOne(access => access.User)
                    .WithMany()
                    .HasForeignKey(access => access.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(access => access.Hotel)
                    .WithMany()
                    .HasForeignKey(access => access.HotelId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Property>()
                .HasOne(property => property.Hotel)
                .WithMany(hotel => hotel.Properties)
                .HasForeignKey(property => property.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Department>()
                .HasOne(department => department.Property)
                .WithMany(property => property.Departments)
                .HasForeignKey(department => department.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RoomType>()
                .HasOne(roomType => roomType.Property)
                .WithMany(property => property.RoomTypes)
                .HasForeignKey(roomType => roomType.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Room>()
                .HasOne(room => room.Property)
                .WithMany(property => property.Rooms)
                .HasForeignKey(room => room.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Room>()
                .HasOne(room => room.RoomType)
                .WithMany(roomType => roomType.Rooms)
                .HasForeignKey(room => room.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(reservation => reservation.Property)
                .WithMany()
                .HasForeignKey(reservation => reservation.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(reservation => reservation.Guest)
                .WithMany(guest => guest.Reservations)
                .HasForeignKey(reservation => reservation.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(reservation => reservation.RoomType)
                .WithMany()
                .HasForeignKey(reservation => reservation.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(reservation => reservation.Room)
                .WithMany()
                .HasForeignKey(reservation => reservation.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(reservation => reservation.RatePlan)
                .WithMany(ratePlan => ratePlan.Reservations)
                .HasForeignKey(reservation => reservation.RatePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Folio>()
                .HasOne(folio => folio.Property)
                .WithMany()
                .HasForeignKey(folio => folio.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Folio>()
                .HasOne(folio => folio.Reservation)
                .WithMany(reservation => reservation.Folios)
                .HasForeignKey(folio => folio.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Folio>()
                .HasOne(folio => folio.Guest)
                .WithMany()
                .HasForeignKey(folio => folio.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FolioItem>()
                .HasOne(folioItem => folioItem.Folio)
                .WithMany(folio => folio.Items)
                .HasForeignKey(folioItem => folioItem.FolioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FolioItem>()
                .HasOne(folioItem => folioItem.ChargeCodeDefinition)
                .WithMany()
                .HasForeignKey(folioItem => folioItem.ChargeCodeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(payment => payment.Folio)
                .WithMany(folio => folio.Payments)
                .HasForeignKey(payment => payment.FolioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CashierTransaction>()
                .HasOne(transaction => transaction.CashierShift)
                .WithMany(shift => shift.Transactions)
                .HasForeignKey(transaction => transaction.CashierShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CashierTransaction>()
                .HasOne(transaction => transaction.Payment)
                .WithMany(payment => payment.CashierTransactions)
                .HasForeignKey(transaction => transaction.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CashierTransaction>()
                .HasOne(transaction => transaction.Folio)
                .WithMany()
                .HasForeignKey(transaction => transaction.FolioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CashDrop>()
                .HasOne(drop => drop.CashierShift)
                .WithMany(shift => shift.CashDrops)
                .HasForeignKey(drop => drop.CashierShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RefundTransaction>()
                .HasOne(refund => refund.Folio)
                .WithMany()
                .HasForeignKey(refund => refund.FolioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RefundTransaction>()
                .HasOne(refund => refund.Payment)
                .WithMany()
                .HasForeignKey(refund => refund.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DiscountApproval>()
                .HasOne(discount => discount.Folio)
                .WithMany()
                .HasForeignKey(discount => discount.FolioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DiscountApproval>()
                .HasOne(discount => discount.FolioItem)
                .WithMany()
                .HasForeignKey(discount => discount.FolioItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DiscountApproval>()
                .HasOne(discount => discount.POSOrder)
                .WithMany()
                .HasForeignKey(discount => discount.POSOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DiscountApproval>()
                .HasOne(discount => discount.BanquetEvent)
                .WithMany()
                .HasForeignKey(discount => discount.BanquetEventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FinanceDocument>()
                .HasOne(document => document.Folio)
                .WithMany()
                .HasForeignKey(document => document.FolioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FinanceDocument>()
                .HasOne(document => document.Reservation)
                .WithMany()
                .HasForeignKey(document => document.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FinanceDocument>()
                .HasOne(document => document.Guest)
                .WithMany()
                .HasForeignKey(document => document.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FinanceDocument>()
                .HasOne(document => document.SalesAccount)
                .WithMany()
                .HasForeignKey(document => document.SalesAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FinanceDocument>()
                .HasOne(document => document.BanquetEvent)
                .WithMany()
                .HasForeignKey(document => document.BanquetEventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FinanceDocumentLine>()
                .HasOne(line => line.FinanceDocument)
                .WithMany(document => document.Lines)
                .HasForeignKey(line => line.FinanceDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FinanceDocumentLine>()
                .HasOne(line => line.ChargeCode)
                .WithMany()
                .HasForeignKey(line => line.ChargeCodeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ARAccount>()
                .HasOne(account => account.SalesAccount)
                .WithMany()
                .HasForeignKey(account => account.SalesAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ARInvoice>()
                .HasOne(invoice => invoice.ARAccount)
                .WithMany(account => account.Invoices)
                .HasForeignKey(invoice => invoice.ARAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ARInvoice>()
                .HasOne(invoice => invoice.FinanceDocument)
                .WithMany()
                .HasForeignKey(invoice => invoice.FinanceDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ARPayment>()
                .HasOne(payment => payment.ARAccount)
                .WithMany(account => account.Payments)
                .HasForeignKey(payment => payment.ARAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ARPaymentAllocation>()
                .HasOne(allocation => allocation.ARPayment)
                .WithMany(payment => payment.Allocations)
                .HasForeignKey(allocation => allocation.ARPaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ARPaymentAllocation>()
                .HasOne(allocation => allocation.ARInvoice)
                .WithMany(invoice => invoice.Allocations)
                .HasForeignKey(allocation => allocation.ARInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CreditMemo>()
                .HasOne(memo => memo.ARAccount)
                .WithMany()
                .HasForeignKey(memo => memo.ARAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CreditMemo>()
                .HasOne(memo => memo.ARInvoice)
                .WithMany()
                .HasForeignKey(memo => memo.ARInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CreditMemo>()
                .HasOne(memo => memo.FinanceDocument)
                .WithMany()
                .HasForeignKey(memo => memo.FinanceDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DebitMemo>()
                .HasOne(memo => memo.ARAccount)
                .WithMany()
                .HasForeignKey(memo => memo.ARAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DebitMemo>()
                .HasOne(memo => memo.ARInvoice)
                .WithMany()
                .HasForeignKey(memo => memo.ARInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DebitMemo>()
                .HasOne(memo => memo.FinanceDocument)
                .WithMany()
                .HasForeignKey(memo => memo.FinanceDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<HousekeepingTask>()
                .HasOne(task => task.Room)
                .WithMany()
                .HasForeignKey(task => task.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DiningTable>()
                .HasOne(table => table.Outlet)
                .WithMany(outlet => outlet.DiningTables)
                .HasForeignKey(table => table.OutletId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MenuItem>()
                .HasOne(menuItem => menuItem.MenuCategory)
                .WithMany(category => category.MenuItems)
                .HasForeignKey(menuItem => menuItem.MenuCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MenuItem>()
                .HasOne(menuItem => menuItem.KitchenStation)
                .WithMany(station => station.MenuItems)
                .HasForeignKey(menuItem => menuItem.KitchenStationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MenuItem>()
                .HasOne(menuItem => menuItem.InventoryItem)
                .WithMany()
                .HasForeignKey(menuItem => menuItem.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<POSOrder>()
                .HasOne(order => order.Outlet)
                .WithMany(outlet => outlet.Orders)
                .HasForeignKey(order => order.OutletId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<POSOrder>()
                .HasOne(order => order.DiningTable)
                .WithMany()
                .HasForeignKey(order => order.DiningTableId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<POSOrder>()
                .HasOne(order => order.Reservation)
                .WithMany()
                .HasForeignKey(order => order.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<POSOrder>()
                .HasOne(order => order.Guest)
                .WithMany()
                .HasForeignKey(order => order.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<POSOrderItem>()
                .HasOne(item => item.POSOrder)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.POSOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<POSOrderItem>()
                .HasOne(item => item.MenuItem)
                .WithMany(menuItem => menuItem.OrderItems)
                .HasForeignKey(item => item.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ContactPerson>()
                .HasOne(contact => contact.SalesAccount)
                .WithMany(account => account.ContactPersons)
                .HasForeignKey(contact => contact.SalesAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SalesLead>()
                .HasOne(lead => lead.SalesAccount)
                .WithMany(account => account.SalesLeads)
                .HasForeignKey(lead => lead.SalesAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SalesActivity>()
                .HasOne(activity => activity.SalesLead)
                .WithMany(lead => lead.SalesActivities)
                .HasForeignKey(activity => activity.SalesLeadId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SalesActivity>()
                .HasOne(activity => activity.SalesAccount)
                .WithMany(account => account.SalesActivities)
                .HasForeignKey(activity => activity.SalesAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BanquetEvent>()
                .HasOne(banquetEvent => banquetEvent.SalesAccount)
                .WithMany()
                .HasForeignKey(banquetEvent => banquetEvent.SalesAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BanquetEvent>()
                .HasOne(banquetEvent => banquetEvent.SalesLead)
                .WithMany()
                .HasForeignKey(banquetEvent => banquetEvent.SalesLeadId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BanquetEvent>()
                .HasOne(banquetEvent => banquetEvent.FunctionRoom)
                .WithMany(room => room.BanquetEvents)
                .HasForeignKey(banquetEvent => banquetEvent.FunctionRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BanquetEvent>()
                .HasOne(banquetEvent => banquetEvent.BanquetPackage)
                .WithMany(package => package.BanquetEvents)
                .HasForeignKey(banquetEvent => banquetEvent.BanquetPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BanquetEventOrder>()
                .HasOne(order => order.BanquetEvent)
                .WithOne(banquetEvent => banquetEvent.BanquetEventOrder)
                .HasForeignKey<BanquetEventOrder>(order => order.BanquetEventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BanquetCharge>()
                .HasOne(charge => charge.BanquetEvent)
                .WithMany(banquetEvent => banquetEvent.Charges)
                .HasForeignKey(charge => charge.BanquetEventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RoomTypeRate>()
                .HasOne(rate => rate.RatePlan)
                .WithMany(ratePlan => ratePlan.RoomTypeRates)
                .HasForeignKey(rate => rate.RatePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RoomTypeRate>()
                .HasOne(rate => rate.RoomType)
                .WithMany()
                .HasForeignKey(rate => rate.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SeasonalRate>()
                .HasOne(rate => rate.RatePlan)
                .WithMany(ratePlan => ratePlan.SeasonalRates)
                .HasForeignKey(rate => rate.RatePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SeasonalRate>()
                .HasOne(rate => rate.RoomType)
                .WithMany()
                .HasForeignKey(rate => rate.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RateRestriction>()
                .HasOne(restriction => restriction.RatePlan)
                .WithMany()
                .HasForeignKey(restriction => restriction.RatePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RateRestriction>()
                .HasOne(restriction => restriction.RoomType)
                .WithMany()
                .HasForeignKey(restriction => restriction.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RoomInventoryControl>()
                .HasOne(control => control.RoomType)
                .WithMany()
                .HasForeignKey(control => control.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PromotionCode>()
                .HasOne(promotion => promotion.AppliesToRatePlan)
                .WithMany()
                .HasForeignKey(promotion => promotion.AppliesToRatePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PromotionCode>()
                .HasOne(promotion => promotion.AppliesToRoomType)
                .WithMany()
                .HasForeignKey(promotion => promotion.AppliesToRoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookingEngineSetting>()
                .HasOne(setting => setting.DefaultRatePlan)
                .WithMany()
                .HasForeignKey(setting => setting.DefaultRatePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookingRequest>()
                .HasOne(request => request.RoomType)
                .WithMany()
                .HasForeignKey(request => request.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookingRequest>()
                .HasOne(request => request.RatePlan)
                .WithMany()
                .HasForeignKey(request => request.RatePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookingRequest>()
                .HasOne(request => request.PromotionCode)
                .WithMany()
                .HasForeignKey(request => request.PromotionCodeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookingRequest>()
                .HasOne(request => request.Reservation)
                .WithMany()
                .HasForeignKey(request => request.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookingEngineRoomContent>()
                .HasOne(content => content.RoomType)
                .WithMany()
                .HasForeignKey(content => content.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookingRequestAddOn>()
                .HasOne(addOn => addOn.BookingRequest)
                .WithMany(request => request.AddOns)
                .HasForeignKey(addOn => addOn.BookingRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookingRequestAddOn>()
                .HasOne(addOn => addOn.BookingAddOn)
                .WithMany(bookingAddOn => bookingAddOn.BookingRequestAddOns)
                .HasForeignKey(addOn => addOn.BookingAddOnId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GuestPortalAccess>()
                .HasOne(access => access.Reservation)
                .WithMany()
                .HasForeignKey(access => access.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GuestPortalAccess>()
                .HasOne(access => access.BookingRequest)
                .WithMany()
                .HasForeignKey(access => access.BookingRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GuestPortalAccess>()
                .HasOne(access => access.Guest)
                .WithMany()
                .HasForeignKey(access => access.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GuestPreCheckIn>()
                .HasOne(preCheckIn => preCheckIn.Reservation)
                .WithMany()
                .HasForeignKey(preCheckIn => preCheckIn.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GuestPreCheckIn>()
                .HasOne(preCheckIn => preCheckIn.Guest)
                .WithMany()
                .HasForeignKey(preCheckIn => preCheckIn.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GuestServiceRequest>()
                .HasOne(request => request.Reservation)
                .WithMany()
                .HasForeignKey(request => request.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GuestServiceRequest>()
                .HasOne(request => request.Guest)
                .WithMany()
                .HasForeignKey(request => request.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GuestServiceRequest>()
                .HasOne(request => request.Room)
                .WithMany()
                .HasForeignKey(request => request.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GuestFeedback>()
                .HasOne(feedback => feedback.Reservation)
                .WithMany()
                .HasForeignKey(feedback => feedback.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GuestFeedback>()
                .HasOne(feedback => feedback.Guest)
                .WithMany()
                .HasForeignKey(feedback => feedback.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExpressCheckoutRequest>()
                .HasOne(request => request.Reservation)
                .WithMany()
                .HasForeignKey(request => request.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExpressCheckoutRequest>()
                .HasOne(request => request.Guest)
                .WithMany()
                .HasForeignKey(request => request.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InventoryItem>()
                .HasOne(item => item.InventoryCategory)
                .WithMany(category => category.Items)
                .HasForeignKey(item => item.InventoryCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseRequest>()
                .HasOne(request => request.Department)
                .WithMany()
                .HasForeignKey(request => request.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseRequestItem>()
                .HasOne(item => item.PurchaseRequest)
                .WithMany(request => request.Items)
                .HasForeignKey(item => item.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseRequestItem>()
                .HasOne(item => item.InventoryItem)
                .WithMany()
                .HasForeignKey(item => item.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseOrder>()
                .HasOne(order => order.Supplier)
                .WithMany()
                .HasForeignKey(order => order.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseOrder>()
                .HasOne(order => order.PurchaseRequest)
                .WithMany()
                .HasForeignKey(order => order.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseOrderItem>()
                .HasOne(item => item.PurchaseOrder)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseOrderItem>()
                .HasOne(item => item.InventoryItem)
                .WithMany()
                .HasForeignKey(item => item.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReceivingRecord>()
                .HasOne(record => record.PurchaseOrder)
                .WithMany()
                .HasForeignKey(record => record.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReceivingRecord>()
                .HasOne(record => record.Supplier)
                .WithMany()
                .HasForeignKey(record => record.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReceivingRecordItem>()
                .HasOne(item => item.ReceivingRecord)
                .WithMany(record => record.Items)
                .HasForeignKey(item => item.ReceivingRecordId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReceivingRecordItem>()
                .HasOne(item => item.InventoryItem)
                .WithMany()
                .HasForeignKey(item => item.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StockMovement>()
                .HasOne(movement => movement.InventoryItem)
                .WithMany(item => item.StockMovements)
                .HasForeignKey(movement => movement.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StockMovement>()
                .HasOne(movement => movement.Department)
                .WithMany()
                .HasForeignKey(movement => movement.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StockAdjustmentItem>()
                .HasOne(item => item.StockAdjustment)
                .WithMany(adjustment => adjustment.Items)
                .HasForeignKey(item => item.StockAdjustmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StockAdjustmentItem>()
                .HasOne(item => item.InventoryItem)
                .WithMany()
                .HasForeignKey(item => item.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GLAccount>()
                .HasIndex(account => account.AccountCode)
                .IsUnique();

            builder.Entity<GLAccount>()
                .HasOne(account => account.ParentGLAccount)
                .WithMany(account => account.ChildAccounts)
                .HasForeignKey(account => account.ParentGLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GLAccount>()
                .HasOne(account => account.UsaliDepartment)
                .WithMany()
                .HasForeignKey(account => account.UsaliDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GLAccount>()
                .HasOne(account => account.UsaliReportLine)
                .WithMany()
                .HasForeignKey(account => account.UsaliReportLineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<USALIDepartment>()
                .HasIndex(department => department.Code)
                .IsUnique();

            builder.Entity<USALIReportLine>()
                .HasIndex(line => line.Code)
                .IsUnique();

            builder.Entity<USALIReportLine>()
                .HasOne(line => line.ParentUSALIReportLine)
                .WithMany(line => line.ChildLines)
                .HasForeignKey(line => line.ParentUSALIReportLineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AccountingPeriod>()
                .HasIndex(period => period.PeriodName)
                .IsUnique();

            builder.Entity<JournalEntry>()
                .HasIndex(entry => entry.JournalNumber)
                .IsUnique();

            builder.Entity<JournalEntry>()
                .HasOne(entry => entry.AccountingPeriod)
                .WithMany()
                .HasForeignKey(entry => entry.AccountingPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JournalEntryLine>()
                .Property(line => line.DebitAmount)
                .HasPrecision(18, 2);

            builder.Entity<JournalEntryLine>()
                .Property(line => line.CreditAmount)
                .HasPrecision(18, 2);

            builder.Entity<JournalEntryLine>()
                .HasOne(line => line.JournalEntry)
                .WithMany(entry => entry.Lines)
                .HasForeignKey(line => line.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JournalEntryLine>()
                .HasOne(line => line.GLAccount)
                .WithMany()
                .HasForeignKey(line => line.GLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JournalEntryLine>()
                .HasOne(line => line.USALIDepartment)
                .WithMany()
                .HasForeignKey(line => line.USALIDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostingRule>()
                .HasOne(rule => rule.ChargeCode)
                .WithMany()
                .HasForeignKey(rule => rule.ChargeCodeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostingRule>()
                .HasOne(rule => rule.Department)
                .WithMany()
                .HasForeignKey(rule => rule.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostingRule>()
                .HasOne(rule => rule.USALIDepartment)
                .WithMany()
                .HasForeignKey(rule => rule.USALIDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostingRule>()
                .HasOne(rule => rule.DebitGLAccount)
                .WithMany()
                .HasForeignKey(rule => rule.DebitGLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostingRule>()
                .HasOne(rule => rule.CreditGLAccount)
                .WithMany()
                .HasForeignKey(rule => rule.CreditGLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostingRule>()
                .HasOne(rule => rule.TaxGLAccount)
                .WithMany()
                .HasForeignKey(rule => rule.TaxGLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostingRule>()
                .HasOne(rule => rule.ServiceChargeGLAccount)
                .WithMany()
                .HasForeignKey(rule => rule.ServiceChargeGLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostingRule>()
                .HasOne(rule => rule.DiscountGLAccount)
                .WithMany()
                .HasForeignKey(rule => rule.DiscountGLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostingBatch>()
                .HasIndex(batch => batch.BatchNumber)
                .IsUnique();

            builder.Entity<PostingBatchItem>()
                .HasOne(item => item.PostingBatch)
                .WithMany(batch => batch.Items)
                .HasForeignKey(item => item.PostingBatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostingBatchItem>()
                .HasOne(item => item.JournalEntry)
                .WithMany()
                .HasForeignKey(item => item.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TaxCode>()
                .HasIndex(taxCode => taxCode.Code)
                .IsUnique();

            builder.Entity<TaxCode>()
                .Property(taxCode => taxCode.Rate)
                .HasPrecision(18, 2);

            builder.Entity<TaxCode>()
                .HasOne(taxCode => taxCode.GLAccount)
                .WithMany()
                .HasForeignKey(taxCode => taxCode.GLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ServiceChargeSetting>()
                .Property(setting => setting.Rate)
                .HasPrecision(18, 2);

            builder.Entity<ServiceChargeSetting>()
                .HasOne(setting => setting.LiabilityGLAccount)
                .WithMany()
                .HasForeignKey(setting => setting.LiabilityGLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ServiceChargeSetting>()
                .HasOne(setting => setting.RevenueGLAccount)
                .WithMany()
                .HasForeignKey(setting => setting.RevenueGLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PhilippineTaxReportLine>()
                .HasOne(line => line.GLAccount)
                .WithMany()
                .HasForeignKey(line => line.GLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PhilippineTaxReportLine>()
                .HasOne(line => line.TaxCode)
                .WithMany()
                .HasForeignKey(line => line.TaxCodeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AccountingReportSnapshotLine>()
                .Property(line => line.Amount)
                .HasPrecision(18, 2);

            builder.Entity<AccountingReportSnapshotLine>()
                .HasOne(line => line.AccountingReportSnapshot)
                .WithMany(snapshot => snapshot.Lines)
                .HasForeignKey(line => line.AccountingReportSnapshotId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<APInvoice>()
                .HasIndex(invoice => new { invoice.SupplierId, invoice.InvoiceNumber })
                .IsUnique();

            builder.Entity<APInvoice>()
                .Property(invoice => invoice.SubTotal)
                .HasPrecision(18, 2);

            builder.Entity<APInvoice>()
                .Property(invoice => invoice.TaxAmount)
                .HasPrecision(18, 2);

            builder.Entity<APInvoice>()
                .Property(invoice => invoice.WithholdingTaxAmount)
                .HasPrecision(18, 2);

            builder.Entity<APInvoice>()
                .Property(invoice => invoice.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<APInvoice>()
                .Property(invoice => invoice.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<APInvoice>()
                .Property(invoice => invoice.AmountPaid)
                .HasPrecision(18, 2);

            builder.Entity<APInvoice>()
                .Property(invoice => invoice.Balance)
                .HasPrecision(18, 2);

            builder.Entity<APInvoice>()
                .HasOne(invoice => invoice.Supplier)
                .WithMany()
                .HasForeignKey(invoice => invoice.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<APInvoice>()
                .HasOne(invoice => invoice.PurchaseOrder)
                .WithMany()
                .HasForeignKey(invoice => invoice.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<APInvoice>()
                .HasOne(invoice => invoice.ReceivingRecord)
                .WithMany()
                .HasForeignKey(invoice => invoice.ReceivingRecordId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<APInvoice>()
                .HasOne(invoice => invoice.JournalEntry)
                .WithMany()
                .HasForeignKey(invoice => invoice.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<APInvoiceLine>()
                .Property(line => line.Quantity)
                .HasPrecision(18, 2);

            builder.Entity<APInvoiceLine>()
                .Property(line => line.UnitCost)
                .HasPrecision(18, 2);

            builder.Entity<APInvoiceLine>()
                .Property(line => line.TaxAmount)
                .HasPrecision(18, 2);

            builder.Entity<APInvoiceLine>()
                .Property(line => line.WithholdingTaxAmount)
                .HasPrecision(18, 2);

            builder.Entity<APInvoiceLine>()
                .Property(line => line.LineTotal)
                .HasPrecision(18, 2);

            builder.Entity<APInvoiceLine>()
                .HasOne(line => line.APInvoice)
                .WithMany(invoice => invoice.Lines)
                .HasForeignKey(line => line.APInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<APInvoiceLine>()
                .HasOne(line => line.GLAccount)
                .WithMany()
                .HasForeignKey(line => line.GLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<APInvoiceLine>()
                .HasOne(line => line.InventoryItem)
                .WithMany()
                .HasForeignKey(line => line.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PaymentVoucher>()
                .HasIndex(voucher => voucher.VoucherNumber)
                .IsUnique();

            builder.Entity<PaymentVoucher>()
                .Property(voucher => voucher.Amount)
                .HasPrecision(18, 2);

            builder.Entity<PaymentVoucher>()
                .Property(voucher => voucher.WithholdingTaxAmount)
                .HasPrecision(18, 2);

            builder.Entity<PaymentVoucher>()
                .Property(voucher => voucher.NetPaymentAmount)
                .HasPrecision(18, 2);

            builder.Entity<PaymentVoucher>()
                .HasOne(voucher => voucher.Supplier)
                .WithMany()
                .HasForeignKey(voucher => voucher.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PaymentVoucher>()
                .HasOne(voucher => voucher.APInvoice)
                .WithMany(invoice => invoice.PaymentVouchers)
                .HasForeignKey(voucher => voucher.APInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PaymentVoucher>()
                .HasOne(voucher => voucher.JournalEntry)
                .WithMany()
                .HasForeignKey(voucher => voucher.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Disbursement>()
                .HasIndex(disbursement => disbursement.DisbursementNumber)
                .IsUnique();

            builder.Entity<Disbursement>()
                .Property(disbursement => disbursement.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Disbursement>()
                .HasOne(disbursement => disbursement.PaymentVoucher)
                .WithMany(voucher => voucher.Disbursements)
                .HasForeignKey(disbursement => disbursement.PaymentVoucherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Disbursement>()
                .HasOne(disbursement => disbursement.Supplier)
                .WithMany()
                .HasForeignKey(disbursement => disbursement.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Disbursement>()
                .HasOne(disbursement => disbursement.JournalEntry)
                .WithMany()
                .HasForeignKey(disbursement => disbursement.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BankAccount>()
                .Property(account => account.OpeningBalance)
                .HasPrecision(18, 2);

            builder.Entity<BankAccount>()
                .HasOne(account => account.GLAccount)
                .WithMany()
                .HasForeignKey(account => account.GLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BankTransaction>()
                .Property(transaction => transaction.DebitAmount)
                .HasPrecision(18, 2);

            builder.Entity<BankTransaction>()
                .Property(transaction => transaction.CreditAmount)
                .HasPrecision(18, 2);

            builder.Entity<BankTransaction>()
                .HasOne(transaction => transaction.BankAccount)
                .WithMany(account => account.BankTransactions)
                .HasForeignKey(transaction => transaction.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BankReconciliation>()
                .Property(reconciliation => reconciliation.StatementEndingBalance)
                .HasPrecision(18, 2);

            builder.Entity<BankReconciliation>()
                .Property(reconciliation => reconciliation.BookEndingBalance)
                .HasPrecision(18, 2);

            builder.Entity<BankReconciliation>()
                .Property(reconciliation => reconciliation.Difference)
                .HasPrecision(18, 2);

            builder.Entity<BankReconciliation>()
                .HasOne(reconciliation => reconciliation.BankAccount)
                .WithMany()
                .HasForeignKey(reconciliation => reconciliation.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BankReconciliationItem>()
                .Property(item => item.Amount)
                .HasPrecision(18, 2);

            builder.Entity<BankReconciliationItem>()
                .HasOne(item => item.BankReconciliation)
                .WithMany(reconciliation => reconciliation.Items)
                .HasForeignKey(item => item.BankReconciliationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BankReconciliationItem>()
                .HasOne(item => item.BankTransaction)
                .WithMany()
                .HasForeignKey(item => item.BankTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AccrualEntry>()
                .HasIndex(accrual => accrual.AccrualNumber)
                .IsUnique();

            builder.Entity<AccrualEntry>()
                .Property(accrual => accrual.Amount)
                .HasPrecision(18, 2);

            builder.Entity<AccrualEntry>()
                .HasOne(accrual => accrual.AccountingPeriod)
                .WithMany()
                .HasForeignKey(accrual => accrual.AccountingPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AccrualEntry>()
                .HasOne(accrual => accrual.DebitGLAccount)
                .WithMany()
                .HasForeignKey(accrual => accrual.DebitGLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AccrualEntry>()
                .HasOne(accrual => accrual.CreditGLAccount)
                .WithMany()
                .HasForeignKey(accrual => accrual.CreditGLAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AccrualEntry>()
                .HasOne(accrual => accrual.JournalEntry)
                .WithMany()
                .HasForeignKey(accrual => accrual.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AccrualEntry>()
                .HasOne(accrual => accrual.ReversalJournalEntry)
                .WithMany()
                .HasForeignKey(accrual => accrual.ReversalJournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MonthEndCloseChecklist>()
                .HasOne(item => item.AccountingPeriod)
                .WithMany()
                .HasForeignKey(item => item.AccountingPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EmployeeCostProfile>(entity =>
            {
                entity.HasIndex(employee => employee.EmployeeCode)
                    .IsUnique()
                    .HasFilter("[EmployeeCode] IS NOT NULL AND [EmployeeCode] <> ''");

                entity.HasOne(employee => employee.Department)
                    .WithMany()
                    .HasForeignKey(employee => employee.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(employee => employee.USALIDepartment)
                    .WithMany()
                    .HasForeignKey(employee => employee.USALIDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(employee => employee.DefaultLaborGLAccount)
                    .WithMany()
                    .HasForeignKey(employee => employee.DefaultLaborGLAccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(employee => employee.DefaultPayrollLiabilityGLAccount)
                    .WithMany()
                    .HasForeignKey(employee => employee.DefaultPayrollLiabilityGLAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PayrollPeriod>(entity =>
            {
                entity.HasIndex(period => period.PeriodName)
                    .IsUnique();

                entity.HasOne(period => period.JournalEntry)
                    .WithMany()
                    .HasForeignKey(period => period.JournalEntryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PayrollCostEntry>(entity =>
            {
                entity.Property(entry => entry.RegularHours).HasPrecision(18, 2);
                entity.Property(entry => entry.OvertimeHours).HasPrecision(18, 2);
                entity.Property(entry => entry.NightDifferentialHours).HasPrecision(18, 2);
                entity.Property(entry => entry.RegularPay).HasPrecision(18, 2);
                entity.Property(entry => entry.OvertimePay).HasPrecision(18, 2);
                entity.Property(entry => entry.NightDifferentialPay).HasPrecision(18, 2);
                entity.Property(entry => entry.Allowances).HasPrecision(18, 2);
                entity.Property(entry => entry.ServiceChargeShare).HasPrecision(18, 2);
                entity.Property(entry => entry.OtherEarnings).HasPrecision(18, 2);
                entity.Property(entry => entry.GrossPay).HasPrecision(18, 2);
                entity.Property(entry => entry.EmployerCost).HasPrecision(18, 2);
                entity.Property(entry => entry.Deductions).HasPrecision(18, 2);
                entity.Property(entry => entry.NetPay).HasPrecision(18, 2);

                entity.HasOne(entry => entry.PayrollPeriod)
                    .WithMany(period => period.Entries)
                    .HasForeignKey(entry => entry.PayrollPeriodId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(entry => entry.EmployeeCostProfile)
                    .WithMany()
                    .HasForeignKey(entry => entry.EmployeeCostProfileId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(entry => entry.Department)
                    .WithMany()
                    .HasForeignKey(entry => entry.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(entry => entry.USALIDepartment)
                    .WithMany()
                    .HasForeignKey(entry => entry.USALIDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(entry => entry.LaborGLAccount)
                    .WithMany()
                    .HasForeignKey(entry => entry.LaborGLAccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(entry => entry.PayrollLiabilityGLAccount)
                    .WithMany()
                    .HasForeignKey(entry => entry.PayrollLiabilityGLAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PayrollAllocationRule>(entity =>
            {
                entity.Property(rule => rule.AllocationPercentage).HasPrecision(18, 2);

                entity.HasOne(rule => rule.EmployeeCostProfile)
                    .WithMany()
                    .HasForeignKey(rule => rule.EmployeeCostProfileId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(rule => rule.Department)
                    .WithMany()
                    .HasForeignKey(rule => rule.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(rule => rule.USALIDepartment)
                    .WithMany()
                    .HasForeignKey(rule => rule.USALIDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(rule => rule.LaborGLAccount)
                    .WithMany()
                    .HasForeignKey(rule => rule.LaborGLAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<DepartmentLaborBudget>(entity =>
            {
                entity.HasIndex(budget => new { budget.DepartmentId, budget.USALIDepartmentId, budget.Month, budget.Year });
                entity.Property(budget => budget.BudgetedLaborCost).HasPrecision(18, 2);
                entity.Property(budget => budget.BudgetedLaborHours).HasPrecision(18, 2);

                entity.HasOne(budget => budget.Department)
                    .WithMany()
                    .HasForeignKey(budget => budget.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(budget => budget.USALIDepartment)
                    .WithMany()
                    .HasForeignKey(budget => budget.USALIDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ServiceChargePool>(entity =>
            {
                entity.HasIndex(pool => pool.PoolName).IsUnique();
                entity.Property(pool => pool.TotalServiceChargeCollected).HasPrecision(18, 2);

                entity.HasOne(pool => pool.JournalEntry)
                    .WithMany()
                    .HasForeignKey(pool => pool.JournalEntryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ServiceChargeDistributionLine>(entity =>
            {
                entity.Property(line => line.EligibleDays).HasPrecision(18, 2);
                entity.Property(line => line.EligibleHours).HasPrecision(18, 2);
                entity.Property(line => line.DistributionPercentage).HasPrecision(18, 2);
                entity.Property(line => line.Amount).HasPrecision(18, 2);

                entity.HasOne(line => line.ServiceChargePool)
                    .WithMany(pool => pool.DistributionLines)
                    .HasForeignKey(line => line.ServiceChargePoolId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(line => line.EmployeeCostProfile)
                    .WithMany()
                    .HasForeignKey(line => line.EmployeeCostProfileId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(line => line.Department)
                    .WithMany()
                    .HasForeignKey(line => line.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<LaborProductivitySnapshot>(entity =>
            {
                entity.Property(snapshot => snapshot.LaborHours).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.LaborCost).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.DepartmentRevenue).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.LaborCostPercentage).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.RevenuePerLaborHour).HasPrecision(18, 2);

                entity.HasOne(snapshot => snapshot.Department)
                    .WithMany()
                    .HasForeignKey(snapshot => snapshot.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(snapshot => snapshot.USALIDepartment)
                    .WithMany()
                    .HasForeignKey(snapshot => snapshot.USALIDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ExecutiveReportSnapshot>(entity =>
            {
                entity.Property(snapshot => snapshot.OccupancyPercentage).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.ADR).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.RevPAR).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.TotalRoomRevenue).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.TotalFBRevenue).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.TotalBanquetRevenue).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.TotalOtherRevenue).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.TotalRevenue).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.TotalPayments).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.GrossOperatingProfit).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.NetIncome).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.ARBalance).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.APBalance).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.LaborCost).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.LaborCostPercentage).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.GuestSatisfactionScore).HasPrecision(18, 2);
                entity.HasIndex(snapshot => new { snapshot.ReportType, snapshot.PeriodStart, snapshot.PeriodEnd });
            });

            builder.Entity<ExecutiveKPI>(entity =>
            {
                entity.HasIndex(kpi => kpi.KPICode).IsUnique();
                entity.Property(kpi => kpi.TargetValue).HasPrecision(18, 2);
                entity.Property(kpi => kpi.WarningThreshold).HasPrecision(18, 2);
                entity.Property(kpi => kpi.CriticalThreshold).HasPrecision(18, 2);
            });

            builder.Entity<ExecutiveKPIResult>(entity =>
            {
                entity.Property(result => result.ActualValue).HasPrecision(18, 2);
                entity.Property(result => result.TargetValue).HasPrecision(18, 2);
                entity.Property(result => result.Variance).HasPrecision(18, 2);
                entity.Property(result => result.VariancePercentage).HasPrecision(18, 2);
                entity.HasIndex(result => new { result.ExecutiveKPIId, result.ResultDate, result.PeriodStart, result.PeriodEnd });
                entity.HasOne(result => result.ExecutiveKPI)
                    .WithMany(kpi => kpi.Results)
                    .HasForeignKey(result => result.ExecutiveKPIId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<DepartmentPerformanceSnapshot>(entity =>
            {
                entity.Property(snapshot => snapshot.Revenue).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.CostOfSales).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.PayrollCost).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.OtherExpenses).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.DepartmentProfit).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.DepartmentProfitMargin).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.LaborCostPercentage).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.BudgetAmount).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.VarianceAmount).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.VariancePercentage).HasPrecision(18, 2);
                entity.HasIndex(snapshot => new { snapshot.PeriodStart, snapshot.PeriodEnd, snapshot.DepartmentId, snapshot.USALIDepartmentId });
                entity.HasOne(snapshot => snapshot.Department)
                    .WithMany()
                    .HasForeignKey(snapshot => snapshot.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(snapshot => snapshot.USALIDepartment)
                    .WithMany()
                    .HasForeignKey(snapshot => snapshot.USALIDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ExecutiveAlert>(entity =>
            {
                entity.HasIndex(alert => new { alert.AlertDate, alert.Module, alert.Title, alert.RelatedReferenceType, alert.RelatedReferenceId });
            });

            builder.Entity<OwnerReportPackageItem>()
                .HasOne(item => item.OwnerReportPackage)
                .WithMany(package => package.Items)
                .HasForeignKey(item => item.OwnerReportPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<KPIBenchmarkSetting>(entity =>
            {
                entity.Property(setting => setting.TargetValue).HasPrecision(18, 2);
                entity.Property(setting => setting.WarningThreshold).HasPrecision(18, 2);
                entity.Property(setting => setting.CriticalThreshold).HasPrecision(18, 2);
                entity.HasIndex(setting => new { setting.KPIName, setting.DepartmentId, setting.USALIDepartmentId, setting.EffectiveFrom });
                entity.HasOne(setting => setting.Department)
                    .WithMany()
                    .HasForeignKey(setting => setting.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(setting => setting.USALIDepartment)
                    .WithMany()
                    .HasForeignKey(setting => setting.USALIDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CashFlowCategory>(entity =>
            {
                entity.HasIndex(category => category.Code).IsUnique();
                entity.Property(category => category.Code).HasMaxLength(60);
                entity.Property(category => category.Name).HasMaxLength(180);
            });

            builder.Entity<CashFlowMappingRule>(entity =>
            {
                entity.HasIndex(rule => new { rule.GLAccountId, rule.SourceModule, rule.SourceTransactionType, rule.IsActive });
                entity.HasOne(rule => rule.GLAccount)
                    .WithMany()
                    .HasForeignKey(rule => rule.GLAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(rule => rule.CashFlowCategory)
                    .WithMany()
                    .HasForeignKey(rule => rule.CashFlowCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CashFlowReportSnapshot>(entity =>
            {
                entity.Property(snapshot => snapshot.BeginningCashBalance).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.NetCashFromOperatingActivities).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.NetCashFromInvestingActivities).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.NetCashFromFinancingActivities).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.NetIncreaseDecreaseInCash).HasPrecision(18, 2);
                entity.Property(snapshot => snapshot.EndingCashBalance).HasPrecision(18, 2);
                entity.HasIndex(snapshot => new { snapshot.PeriodStart, snapshot.PeriodEnd, snapshot.GeneratedAt });
            });

            builder.Entity<CashFlowReportSnapshotLine>(entity =>
            {
                entity.Property(line => line.Amount).HasPrecision(18, 2);
                entity.HasOne(line => line.CashFlowReportSnapshot)
                    .WithMany(snapshot => snapshot.Lines)
                    .HasForeignKey(line => line.CashFlowReportSnapshotId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(line => line.CashFlowCategory)
                    .WithMany()
                    .HasForeignKey(line => line.CashFlowCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CashAccountSetting>(entity =>
            {
                entity.HasIndex(setting => setting.GLAccountId).IsUnique();
                entity.HasOne(setting => setting.GLAccount)
                    .WithMany()
                    .HasForeignKey(setting => setting.GLAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ReportTemplateSetting>(entity =>
            {
                entity.HasIndex(setting => setting.ReportKey).IsUnique();
                entity.Property(setting => setting.ReportKey).HasMaxLength(120);
                entity.Property(setting => setting.ReportName).HasMaxLength(180);
            });

            builder.Entity<ReportExportLog>(entity =>
            {
                entity.HasIndex(log => new { log.ReportKey, log.ExportedAt });
                entity.Property(log => log.ReportKey).HasMaxLength(120);
                entity.Property(log => log.ReportName).HasMaxLength(180);
            });

            builder.Entity<SavedReportRun>(entity =>
            {
                entity.HasIndex(run => new { run.ReportKey, run.RunAt });
                entity.Property(run => run.ReportKey).HasMaxLength(120);
                entity.Property(run => run.ReportName).HasMaxLength(180);
            });

            builder.Entity<ReportCatalogItem>(entity =>
            {
                entity.HasIndex(item => item.ReportKey).IsUnique();
                entity.Property(item => item.ReportKey).HasMaxLength(120);
                entity.Property(item => item.ReportName).HasMaxLength(180);
            });

            builder.Entity<PseudoRoom>(entity =>
            {
                entity.HasIndex(item => item.PseudoRoomCode).IsUnique();
                entity.Property(item => item.PseudoRoomCode).HasMaxLength(40);
                entity.Property(item => item.PseudoRoomName).HasMaxLength(180);
                entity.HasOne(item => item.LinkedSalesAccount)
                    .WithMany()
                    .HasForeignKey(item => item.LinkedSalesAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.LinkedBanquetEvent)
                    .WithMany()
                    .HasForeignKey(item => item.LinkedBanquetEventId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.LinkedGroupBooking)
                    .WithMany()
                    .HasForeignKey(item => item.LinkedGroupBookingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<GroupBooking>(entity =>
            {
                entity.HasIndex(item => item.GroupCode).IsUnique();
                entity.Property(item => item.GroupCode).HasMaxLength(40);
                entity.Property(item => item.GroupName).HasMaxLength(180);
                entity.Property(item => item.CreditLimit).HasPrecision(18, 2);
                entity.Property(item => item.DepositAmount).HasPrecision(18, 2);
                entity.HasOne(item => item.SalesAccount)
                    .WithMany()
                    .HasForeignKey(item => item.SalesAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<GroupRoomBlock>(entity =>
            {
                entity.HasIndex(item => new { item.GroupBookingId, item.RoomTypeId, item.BlockDate });
                entity.Property(item => item.RateAmount).HasPrecision(18, 2);
                entity.HasOne(item => item.GroupBooking)
                    .WithMany(item => item.RoomBlocks)
                    .HasForeignKey(item => item.GroupBookingId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.RoomType)
                    .WithMany()
                    .HasForeignKey(item => item.RoomTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.RatePlan)
                    .WithMany()
                    .HasForeignKey(item => item.RatePlanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<GroupMemberReservation>(entity =>
            {
                entity.HasIndex(item => new { item.GroupBookingId, item.ReservationId }).IsUnique();
                entity.HasOne(item => item.GroupBooking)
                    .WithMany(item => item.Members)
                    .HasForeignKey(item => item.GroupBookingId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Reservation)
                    .WithMany()
                    .HasForeignKey(item => item.ReservationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<GroupFolio>(entity =>
            {
                entity.Property(item => item.FolioName).HasMaxLength(180);
                entity.HasOne(item => item.GroupBooking)
                    .WithMany(item => item.GroupFolios)
                    .HasForeignKey(item => item.GroupBookingId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.PseudoRoom)
                    .WithMany(item => item.GroupFolios)
                    .HasForeignKey(item => item.PseudoRoomId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Folio)
                    .WithMany()
                    .HasForeignKey(item => item.FolioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ChargeRoutingRule>(entity =>
            {
                entity.HasIndex(item => new { item.GroupBookingId, item.ReservationId, item.FolioId, item.SourceChargeCategory, item.IsActive });
                entity.HasOne(item => item.GroupBooking)
                    .WithMany(item => item.ChargeRoutingRules)
                    .HasForeignKey(item => item.GroupBookingId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Reservation)
                    .WithMany()
                    .HasForeignKey(item => item.ReservationId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Folio)
                    .WithMany()
                    .HasForeignKey(item => item.FolioId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.TargetFolio)
                    .WithMany()
                    .HasForeignKey(item => item.TargetFolioId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.TargetGroupFolio)
                    .WithMany()
                    .HasForeignKey(item => item.TargetGroupFolioId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.TargetPseudoRoom)
                    .WithMany()
                    .HasForeignKey(item => item.TargetPseudoRoomId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<GroupDeposit>(entity =>
            {
                entity.Property(item => item.Amount).HasPrecision(18, 2);
                entity.HasOne(item => item.GroupBooking)
                    .WithMany(item => item.Deposits)
                    .HasForeignKey(item => item.GroupBookingId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Folio)
                    .WithMany()
                    .HasForeignKey(item => item.FolioId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.FinanceDocument)
                    .WithMany()
                    .HasForeignKey(item => item.FinanceDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<GroupPaymentAllocation>(entity =>
            {
                entity.Property(item => item.AllocatedAmount).HasPrecision(18, 2);
                entity.HasOne(item => item.GroupBooking)
                    .WithMany(item => item.PaymentAllocations)
                    .HasForeignKey(item => item.GroupBookingId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Payment)
                    .WithMany()
                    .HasForeignKey(item => item.PaymentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.GroupDeposit)
                    .WithMany()
                    .HasForeignKey(item => item.GroupDepositId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.TargetFolio)
                    .WithMany()
                    .HasForeignKey(item => item.TargetFolioId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.TargetReservation)
                    .WithMany()
                    .HasForeignKey(item => item.TargetReservationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AIActionLog>()
                .HasOne(log => log.RelatedInsight)
                .WithMany(insight => insight.ActionLogs)
                .HasForeignKey(log => log.RelatedInsightId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DocumentTemplateSetting>()
                .HasIndex(setting => setting.DocumentType)
                .IsUnique();

            builder.Entity<ClientDemoPackageItem>()
                .HasOne(item => item.ClientDemoPackage)
                .WithMany(package => package.Items)
                .HasForeignKey(item => item.ClientDemoPackageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
