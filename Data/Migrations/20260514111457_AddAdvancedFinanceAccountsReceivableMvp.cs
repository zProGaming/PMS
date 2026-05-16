using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedFinanceAccountsReceivableMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChargeCodeId",
                table: "FolioItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ARAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesAccountId = table.Column<int>(type: "int", nullable: true),
                    AccountName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillingAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ARAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ARAccounts_SalesAccounts_SalesAccountId",
                        column: x => x.SalesAccountId,
                        principalTable: "SalesAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashierShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningCashFloat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosingCashCount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpectedCashAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CashOverShort = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashierShifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChargeCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChargeCategory = table.Column<int>(type: "int", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    IsServiceChargeable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscountApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FolioId = table.Column<int>(type: "int", nullable: true),
                    FolioItemId = table.Column<int>(type: "int", nullable: true),
                    POSOrderId = table.Column<int>(type: "int", nullable: true),
                    BanquetEventId = table.Column<int>(type: "int", nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountApprovals_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscountApprovals_FolioItems_FolioItemId",
                        column: x => x.FolioItemId,
                        principalTable: "FolioItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscountApprovals_Folios_FolioId",
                        column: x => x.FolioId,
                        principalTable: "Folios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscountApprovals_POSOrders_POSOrderId",
                        column: x => x.POSOrderId,
                        principalTable: "POSOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentNumberSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextNumber = table.Column<int>(type: "int", nullable: false),
                    PaddingLength = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    FolioId = table.Column<int>(type: "int", nullable: true),
                    ReservationId = table.Column<int>(type: "int", nullable: true),
                    GuestId = table.Column<int>(type: "int", nullable: true),
                    SalesAccountId = table.Column<int>(type: "int", nullable: true),
                    BanquetEventId = table.Column<int>(type: "int", nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ServiceCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BillingName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillingAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillingTIN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoidedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceDocuments_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceDocuments_Folios_FolioId",
                        column: x => x.FolioId,
                        principalTable: "Folios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceDocuments_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceDocuments_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceDocuments_SalesAccounts_SalesAccountId",
                        column: x => x.SalesAccountId,
                        principalTable: "SalesAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefundTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FolioId = table.Column<int>(type: "int", nullable: true),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    RefundNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefundDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundMethod = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundTransactions_Folios_FolioId",
                        column: x => x.FolioId,
                        principalTable: "Folios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundTransactions_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoidRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoidRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ARPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ARAccountId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ARPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ARPayments_ARAccounts_ARAccountId",
                        column: x => x.ARAccountId,
                        principalTable: "ARAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashDrops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashierShiftId = table.Column<int>(type: "int", nullable: false),
                    DropDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DroppedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashDrops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashDrops_CashierShifts_CashierShiftId",
                        column: x => x.CashierShiftId,
                        principalTable: "CashierShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashierTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashierShiftId = table.Column<int>(type: "int", nullable: true),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    FolioId = table.Column<int>(type: "int", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVoided = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashierTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashierTransactions_CashierShifts_CashierShiftId",
                        column: x => x.CashierShiftId,
                        principalTable: "CashierShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashierTransactions_Folios_FolioId",
                        column: x => x.FolioId,
                        principalTable: "Folios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashierTransactions_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ARInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ARAccountId = table.Column<int>(type: "int", nullable: false),
                    FinanceDocumentId = table.Column<int>(type: "int", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ARInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ARInvoices_ARAccounts_ARAccountId",
                        column: x => x.ARAccountId,
                        principalTable: "ARAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ARInvoices_FinanceDocuments_FinanceDocumentId",
                        column: x => x.FinanceDocumentId,
                        principalTable: "FinanceDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceDocumentLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinanceDocumentId = table.Column<int>(type: "int", nullable: false),
                    ChargeCodeId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ServiceCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceDocumentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceDocumentLines_ChargeCodes_ChargeCodeId",
                        column: x => x.ChargeCodeId,
                        principalTable: "ChargeCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceDocumentLines_FinanceDocuments_FinanceDocumentId",
                        column: x => x.FinanceDocumentId,
                        principalTable: "FinanceDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ARPaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ARPaymentId = table.Column<int>(type: "int", nullable: false),
                    ARInvoiceId = table.Column<int>(type: "int", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllocatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ARPaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ARPaymentAllocations_ARInvoices_ARInvoiceId",
                        column: x => x.ARInvoiceId,
                        principalTable: "ARInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ARPaymentAllocations_ARPayments_ARPaymentId",
                        column: x => x.ARPaymentId,
                        principalTable: "ARPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditMemos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ARAccountId = table.Column<int>(type: "int", nullable: true),
                    ARInvoiceId = table.Column<int>(type: "int", nullable: true),
                    FinanceDocumentId = table.Column<int>(type: "int", nullable: true),
                    CreditMemoNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreditMemoDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditMemos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditMemos_ARAccounts_ARAccountId",
                        column: x => x.ARAccountId,
                        principalTable: "ARAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditMemos_ARInvoices_ARInvoiceId",
                        column: x => x.ARInvoiceId,
                        principalTable: "ARInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditMemos_FinanceDocuments_FinanceDocumentId",
                        column: x => x.FinanceDocumentId,
                        principalTable: "FinanceDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DebitMemos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ARAccountId = table.Column<int>(type: "int", nullable: true),
                    ARInvoiceId = table.Column<int>(type: "int", nullable: true),
                    FinanceDocumentId = table.Column<int>(type: "int", nullable: true),
                    DebitMemoNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DebitMemoDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebitMemos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebitMemos_ARAccounts_ARAccountId",
                        column: x => x.ARAccountId,
                        principalTable: "ARAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DebitMemos_ARInvoices_ARInvoiceId",
                        column: x => x.ARInvoiceId,
                        principalTable: "ARInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DebitMemos_FinanceDocuments_FinanceDocumentId",
                        column: x => x.FinanceDocumentId,
                        principalTable: "FinanceDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FolioItems_ChargeCodeId",
                table: "FolioItems",
                column: "ChargeCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ARAccounts_SalesAccountId",
                table: "ARAccounts",
                column: "SalesAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ARInvoices_ARAccountId",
                table: "ARInvoices",
                column: "ARAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ARInvoices_FinanceDocumentId",
                table: "ARInvoices",
                column: "FinanceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ARPaymentAllocations_ARInvoiceId",
                table: "ARPaymentAllocations",
                column: "ARInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ARPaymentAllocations_ARPaymentId",
                table: "ARPaymentAllocations",
                column: "ARPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ARPayments_ARAccountId",
                table: "ARPayments",
                column: "ARAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDrops_CashierShiftId",
                table: "CashDrops",
                column: "CashierShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_CashierTransactions_CashierShiftId",
                table: "CashierTransactions",
                column: "CashierShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_CashierTransactions_FolioId",
                table: "CashierTransactions",
                column: "FolioId");

            migrationBuilder.CreateIndex(
                name: "IX_CashierTransactions_PaymentId",
                table: "CashierTransactions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_ARAccountId",
                table: "CreditMemos",
                column: "ARAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_ARInvoiceId",
                table: "CreditMemos",
                column: "ARInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_FinanceDocumentId",
                table: "CreditMemos",
                column: "FinanceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DebitMemos_ARAccountId",
                table: "DebitMemos",
                column: "ARAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DebitMemos_ARInvoiceId",
                table: "DebitMemos",
                column: "ARInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_DebitMemos_FinanceDocumentId",
                table: "DebitMemos",
                column: "FinanceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountApprovals_BanquetEventId",
                table: "DiscountApprovals",
                column: "BanquetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountApprovals_FolioId",
                table: "DiscountApprovals",
                column: "FolioId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountApprovals_FolioItemId",
                table: "DiscountApprovals",
                column: "FolioItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountApprovals_POSOrderId",
                table: "DiscountApprovals",
                column: "POSOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceDocumentLines_ChargeCodeId",
                table: "FinanceDocumentLines",
                column: "ChargeCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceDocumentLines_FinanceDocumentId",
                table: "FinanceDocumentLines",
                column: "FinanceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceDocuments_BanquetEventId",
                table: "FinanceDocuments",
                column: "BanquetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceDocuments_FolioId",
                table: "FinanceDocuments",
                column: "FolioId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceDocuments_GuestId",
                table: "FinanceDocuments",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceDocuments_ReservationId",
                table: "FinanceDocuments",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceDocuments_SalesAccountId",
                table: "FinanceDocuments",
                column: "SalesAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransactions_FolioId",
                table: "RefundTransactions",
                column: "FolioId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransactions_PaymentId",
                table: "RefundTransactions",
                column: "PaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_FolioItems_ChargeCodes_ChargeCodeId",
                table: "FolioItems",
                column: "ChargeCodeId",
                principalTable: "ChargeCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FolioItems_ChargeCodes_ChargeCodeId",
                table: "FolioItems");

            migrationBuilder.DropTable(
                name: "ARPaymentAllocations");

            migrationBuilder.DropTable(
                name: "CashDrops");

            migrationBuilder.DropTable(
                name: "CashierTransactions");

            migrationBuilder.DropTable(
                name: "CreditMemos");

            migrationBuilder.DropTable(
                name: "DebitMemos");

            migrationBuilder.DropTable(
                name: "DiscountApprovals");

            migrationBuilder.DropTable(
                name: "DocumentNumberSequences");

            migrationBuilder.DropTable(
                name: "FinanceDocumentLines");

            migrationBuilder.DropTable(
                name: "RefundTransactions");

            migrationBuilder.DropTable(
                name: "VoidRequests");

            migrationBuilder.DropTable(
                name: "ARPayments");

            migrationBuilder.DropTable(
                name: "CashierShifts");

            migrationBuilder.DropTable(
                name: "ARInvoices");

            migrationBuilder.DropTable(
                name: "ChargeCodes");

            migrationBuilder.DropTable(
                name: "ARAccounts");

            migrationBuilder.DropTable(
                name: "FinanceDocuments");

            migrationBuilder.DropIndex(
                name: "IX_FolioItems_ChargeCodeId",
                table: "FolioItems");

            migrationBuilder.DropColumn(
                name: "ChargeCodeId",
                table: "FolioItems");
        }
    }
}
