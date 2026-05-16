using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCashFlowReportingMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashAccountSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GLAccountId = table.Column<int>(type: "int", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCashOnHand = table.Column<bool>(type: "bit", nullable: false),
                    IsCashInBank = table.Column<bool>(type: "bit", nullable: false),
                    IsEWallet = table.Column<bool>(type: "bit", nullable: false),
                    IsCashEquivalent = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashAccountSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashAccountSettings_GLAccounts_GLAccountId",
                        column: x => x.GLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashFlowCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    CashFlowSection = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsSubtotal = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashFlowCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashFlowReportSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BeginningCashBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetCashFromOperatingActivities = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetCashFromInvestingActivities = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetCashFromFinancingActivities = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetIncreaseDecreaseInCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EndingCashBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashFlowReportSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashFlowMappingRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GLAccountId = table.Column<int>(type: "int", nullable: true),
                    SourceModule = table.Column<int>(type: "int", nullable: true),
                    SourceTransactionType = table.Column<int>(type: "int", nullable: true),
                    CashFlowCategoryId = table.Column<int>(type: "int", nullable: false),
                    CashFlowSection = table.Column<int>(type: "int", nullable: false),
                    IsCashAccountRule = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashFlowMappingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashFlowMappingRules_CashFlowCategories_CashFlowCategoryId",
                        column: x => x.CashFlowCategoryId,
                        principalTable: "CashFlowCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashFlowMappingRules_GLAccounts_GLAccountId",
                        column: x => x.GLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashFlowReportSnapshotLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashFlowReportSnapshotId = table.Column<int>(type: "int", nullable: false),
                    CashFlowSection = table.Column<int>(type: "int", nullable: false),
                    CashFlowCategoryId = table.Column<int>(type: "int", nullable: true),
                    LineCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsSubtotal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashFlowReportSnapshotLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashFlowReportSnapshotLines_CashFlowCategories_CashFlowCategoryId",
                        column: x => x.CashFlowCategoryId,
                        principalTable: "CashFlowCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashFlowReportSnapshotLines_CashFlowReportSnapshots_CashFlowReportSnapshotId",
                        column: x => x.CashFlowReportSnapshotId,
                        principalTable: "CashFlowReportSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountSettings_GLAccountId",
                table: "CashAccountSettings",
                column: "GLAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowCategories_Code",
                table: "CashFlowCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowMappingRules_CashFlowCategoryId",
                table: "CashFlowMappingRules",
                column: "CashFlowCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowMappingRules_GLAccountId_SourceModule_SourceTransactionType_IsActive",
                table: "CashFlowMappingRules",
                columns: new[] { "GLAccountId", "SourceModule", "SourceTransactionType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowReportSnapshotLines_CashFlowCategoryId",
                table: "CashFlowReportSnapshotLines",
                column: "CashFlowCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowReportSnapshotLines_CashFlowReportSnapshotId",
                table: "CashFlowReportSnapshotLines",
                column: "CashFlowReportSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowReportSnapshots_PeriodStart_PeriodEnd_GeneratedAt",
                table: "CashFlowReportSnapshots",
                columns: new[] { "PeriodStart", "PeriodEnd", "GeneratedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashAccountSettings");

            migrationBuilder.DropTable(
                name: "CashFlowMappingRules");

            migrationBuilder.DropTable(
                name: "CashFlowReportSnapshotLines");

            migrationBuilder.DropTable(
                name: "CashFlowCategories");

            migrationBuilder.DropTable(
                name: "CashFlowReportSnapshots");
        }
    }
}
