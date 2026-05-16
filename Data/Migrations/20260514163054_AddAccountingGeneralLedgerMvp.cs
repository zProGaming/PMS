using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingGeneralLedgerMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingExportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportType = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExportedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingExportLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClosedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountingReportSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingReportSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostingBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BatchDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceModule = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "USALIDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentType = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USALIDepartments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "USALIReportLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportSection = table.Column<int>(type: "int", nullable: false),
                    ParentUSALIReportLineId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsSubtotal = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USALIReportLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USALIReportLines_USALIReportLines_ParentUSALIReportLineId",
                        column: x => x.ParentUSALIReportLineId,
                        principalTable: "USALIReportLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JournalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountingPeriodId = table.Column<int>(type: "int", nullable: true),
                    SourceModule = table.Column<int>(type: "int", nullable: false),
                    SourceTransactionType = table.Column<int>(type: "int", nullable: false),
                    SourceReferenceId = table.Column<int>(type: "int", nullable: true),
                    SourceReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PostedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReversedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_AccountingPeriods_AccountingPeriodId",
                        column: x => x.AccountingPeriodId,
                        principalTable: "AccountingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingReportSnapshotLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountingReportSnapshotId = table.Column<int>(type: "int", nullable: false),
                    LineCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingReportSnapshotLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingReportSnapshotLines_AccountingReportSnapshots_AccountingReportSnapshotId",
                        column: x => x.AccountingReportSnapshotId,
                        principalTable: "AccountingReportSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GLAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    NormalBalance = table.Column<int>(type: "int", nullable: false),
                    ParentGLAccountId = table.Column<int>(type: "int", nullable: true),
                    UsaliDepartmentId = table.Column<int>(type: "int", nullable: true),
                    UsaliReportLineId = table.Column<int>(type: "int", nullable: true),
                    PhilippineReportCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsControlAccount = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GLAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GLAccounts_GLAccounts_ParentGLAccountId",
                        column: x => x.ParentGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GLAccounts_USALIDepartments_UsaliDepartmentId",
                        column: x => x.UsaliDepartmentId,
                        principalTable: "USALIDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GLAccounts_USALIReportLines_UsaliReportLineId",
                        column: x => x.UsaliReportLineId,
                        principalTable: "USALIReportLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostingBatchItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostingBatchId = table.Column<int>(type: "int", nullable: false),
                    SourceTransactionType = table.Column<int>(type: "int", nullable: false),
                    SourceReferenceId = table.Column<int>(type: "int", nullable: false),
                    SourceReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingBatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostingBatchItems_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostingBatchItems_PostingBatches_PostingBatchId",
                        column: x => x.PostingBatchId,
                        principalTable: "PostingBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<int>(type: "int", nullable: false),
                    GLAccountId = table.Column<int>(type: "int", nullable: false),
                    USALIDepartmentId = table.Column<int>(type: "int", nullable: true),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LineReferenceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LineReferenceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_GLAccounts_GLAccountId",
                        column: x => x.GLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_USALIDepartments_USALIDepartmentId",
                        column: x => x.USALIDepartmentId,
                        principalTable: "USALIDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostingRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceModule = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    ChargeCodeId = table.Column<int>(type: "int", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    USALIDepartmentId = table.Column<int>(type: "int", nullable: true),
                    DebitGLAccountId = table.Column<int>(type: "int", nullable: false),
                    CreditGLAccountId = table.Column<int>(type: "int", nullable: false),
                    TaxGLAccountId = table.Column<int>(type: "int", nullable: true),
                    ServiceChargeGLAccountId = table.Column<int>(type: "int", nullable: true),
                    DiscountGLAccountId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostingRules_ChargeCodes_ChargeCodeId",
                        column: x => x.ChargeCodeId,
                        principalTable: "ChargeCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostingRules_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostingRules_GLAccounts_CreditGLAccountId",
                        column: x => x.CreditGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostingRules_GLAccounts_DebitGLAccountId",
                        column: x => x.DebitGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostingRules_GLAccounts_DiscountGLAccountId",
                        column: x => x.DiscountGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostingRules_GLAccounts_ServiceChargeGLAccountId",
                        column: x => x.ServiceChargeGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostingRules_GLAccounts_TaxGLAccountId",
                        column: x => x.TaxGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostingRules_USALIDepartments_USALIDepartmentId",
                        column: x => x.USALIDepartmentId,
                        principalTable: "USALIDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceChargeSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LiabilityGLAccountId = table.Column<int>(type: "int", nullable: true),
                    RevenueGLAccountId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceChargeSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceChargeSettings_GLAccounts_LiabilityGLAccountId",
                        column: x => x.LiabilityGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceChargeSettings_GLAccounts_RevenueGLAccountId",
                        column: x => x.RevenueGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaxType = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GLAccountId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxCodes_GLAccounts_GLAccountId",
                        column: x => x.GLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhilippineTaxReportLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportType = table.Column<int>(type: "int", nullable: false),
                    LineCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GLAccountId = table.Column<int>(type: "int", nullable: true),
                    TaxCodeId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhilippineTaxReportLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhilippineTaxReportLines_GLAccounts_GLAccountId",
                        column: x => x.GLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhilippineTaxReportLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_PeriodName",
                table: "AccountingPeriods",
                column: "PeriodName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingReportSnapshotLines_AccountingReportSnapshotId",
                table: "AccountingReportSnapshotLines",
                column: "AccountingReportSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_GLAccounts_AccountCode",
                table: "GLAccounts",
                column: "AccountCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GLAccounts_ParentGLAccountId",
                table: "GLAccounts",
                column: "ParentGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GLAccounts_UsaliDepartmentId",
                table: "GLAccounts",
                column: "UsaliDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GLAccounts_UsaliReportLineId",
                table: "GLAccounts",
                column: "UsaliReportLineId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_AccountingPeriodId",
                table: "JournalEntries",
                column: "AccountingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_JournalNumber",
                table: "JournalEntries",
                column: "JournalNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_GLAccountId",
                table: "JournalEntryLines",
                column: "GLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_JournalEntryId",
                table: "JournalEntryLines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_USALIDepartmentId",
                table: "JournalEntryLines",
                column: "USALIDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PhilippineTaxReportLines_GLAccountId",
                table: "PhilippineTaxReportLines",
                column: "GLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PhilippineTaxReportLines_TaxCodeId",
                table: "PhilippineTaxReportLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingBatches_BatchNumber",
                table: "PostingBatches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostingBatchItems_JournalEntryId",
                table: "PostingBatchItems",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingBatchItems_PostingBatchId",
                table: "PostingBatchItems",
                column: "PostingBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_ChargeCodeId",
                table: "PostingRules",
                column: "ChargeCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_CreditGLAccountId",
                table: "PostingRules",
                column: "CreditGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_DebitGLAccountId",
                table: "PostingRules",
                column: "DebitGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_DepartmentId",
                table: "PostingRules",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_DiscountGLAccountId",
                table: "PostingRules",
                column: "DiscountGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_ServiceChargeGLAccountId",
                table: "PostingRules",
                column: "ServiceChargeGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_TaxGLAccountId",
                table: "PostingRules",
                column: "TaxGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_USALIDepartmentId",
                table: "PostingRules",
                column: "USALIDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceChargeSettings_LiabilityGLAccountId",
                table: "ServiceChargeSettings",
                column: "LiabilityGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceChargeSettings_RevenueGLAccountId",
                table: "ServiceChargeSettings",
                column: "RevenueGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_Code",
                table: "TaxCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_GLAccountId",
                table: "TaxCodes",
                column: "GLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_USALIDepartments_Code",
                table: "USALIDepartments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USALIReportLines_Code",
                table: "USALIReportLines",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USALIReportLines_ParentUSALIReportLineId",
                table: "USALIReportLines",
                column: "ParentUSALIReportLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingExportLogs");

            migrationBuilder.DropTable(
                name: "AccountingReportSnapshotLines");

            migrationBuilder.DropTable(
                name: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "PhilippineTaxReportLines");

            migrationBuilder.DropTable(
                name: "PostingBatchItems");

            migrationBuilder.DropTable(
                name: "PostingRules");

            migrationBuilder.DropTable(
                name: "ServiceChargeSettings");

            migrationBuilder.DropTable(
                name: "AccountingReportSnapshots");

            migrationBuilder.DropTable(
                name: "TaxCodes");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "PostingBatches");

            migrationBuilder.DropTable(
                name: "GLAccounts");

            migrationBuilder.DropTable(
                name: "AccountingPeriods");

            migrationBuilder.DropTable(
                name: "USALIDepartments");

            migrationBuilder.DropTable(
                name: "USALIReportLines");
        }
    }
}
