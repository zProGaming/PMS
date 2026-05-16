using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLaborCostingMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartmentLaborBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    USALIDepartmentId = table.Column<int>(type: "int", nullable: true),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    BudgetedLaborCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BudgetedLaborHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BudgetedHeadcount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentLaborBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentLaborBudgets_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartmentLaborBudgets_USALIDepartments_USALIDepartmentId",
                        column: x => x.USALIDepartmentId,
                        principalTable: "USALIDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCostProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    USALIDepartmentId = table.Column<int>(type: "int", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    DefaultLaborGLAccountId = table.Column<int>(type: "int", nullable: true),
                    DefaultPayrollLiabilityGLAccountId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCostProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCostProfiles_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeCostProfiles_GLAccounts_DefaultLaborGLAccountId",
                        column: x => x.DefaultLaborGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeCostProfiles_GLAccounts_DefaultPayrollLiabilityGLAccountId",
                        column: x => x.DefaultPayrollLiabilityGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeCostProfiles_USALIDepartments_USALIDepartmentId",
                        column: x => x.USALIDepartmentId,
                        principalTable: "USALIDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LaborProductivitySnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    USALIDepartmentId = table.Column<int>(type: "int", nullable: true),
                    LaborHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepartmentRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LaborCostPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevenuePerLaborHour = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RoomsCleaned = table.Column<int>(type: "int", nullable: true),
                    CoversServed = table.Column<int>(type: "int", nullable: true),
                    EventsServed = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborProductivitySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaborProductivitySnapshots_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LaborProductivitySnapshots_USALIDepartments_USALIDepartmentId",
                        column: x => x.USALIDepartmentId,
                        principalTable: "USALIDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreparedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollPeriods_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceChargePools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoolName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalServiceChargeCollected = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DistributionMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreparedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceChargePools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceChargePools_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAllocationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeCostProfileId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    USALIDepartmentId = table.Column<int>(type: "int", nullable: true),
                    AllocationPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LaborGLAccountId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAllocationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAllocationRules_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAllocationRules_EmployeeCostProfiles_EmployeeCostProfileId",
                        column: x => x.EmployeeCostProfileId,
                        principalTable: "EmployeeCostProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAllocationRules_GLAccounts_LaborGLAccountId",
                        column: x => x.LaborGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAllocationRules_USALIDepartments_USALIDepartmentId",
                        column: x => x.USALIDepartmentId,
                        principalTable: "USALIDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollCostEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollPeriodId = table.Column<int>(type: "int", nullable: false),
                    EmployeeCostProfileId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    USALIDepartmentId = table.Column<int>(type: "int", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegularHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NightDifferentialHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RegularPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OvertimePay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NightDifferentialPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Allowances = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ServiceChargeShare = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherEarnings = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployerCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Deductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LaborGLAccountId = table.Column<int>(type: "int", nullable: true),
                    PayrollLiabilityGLAccountId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCostEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollCostEntries_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollCostEntries_EmployeeCostProfiles_EmployeeCostProfileId",
                        column: x => x.EmployeeCostProfileId,
                        principalTable: "EmployeeCostProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollCostEntries_GLAccounts_LaborGLAccountId",
                        column: x => x.LaborGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollCostEntries_GLAccounts_PayrollLiabilityGLAccountId",
                        column: x => x.PayrollLiabilityGLAccountId,
                        principalTable: "GLAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollCostEntries_PayrollPeriods_PayrollPeriodId",
                        column: x => x.PayrollPeriodId,
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollCostEntries_USALIDepartments_USALIDepartmentId",
                        column: x => x.USALIDepartmentId,
                        principalTable: "USALIDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceChargeDistributionLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceChargePoolId = table.Column<int>(type: "int", nullable: false),
                    EmployeeCostProfileId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    EligibleDays = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EligibleHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DistributionPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceChargeDistributionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceChargeDistributionLines_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceChargeDistributionLines_EmployeeCostProfiles_EmployeeCostProfileId",
                        column: x => x.EmployeeCostProfileId,
                        principalTable: "EmployeeCostProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceChargeDistributionLines_ServiceChargePools_ServiceChargePoolId",
                        column: x => x.ServiceChargePoolId,
                        principalTable: "ServiceChargePools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentLaborBudgets_DepartmentId_USALIDepartmentId_Month_Year",
                table: "DepartmentLaborBudgets",
                columns: new[] { "DepartmentId", "USALIDepartmentId", "Month", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentLaborBudgets_USALIDepartmentId",
                table: "DepartmentLaborBudgets",
                column: "USALIDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCostProfiles_DefaultLaborGLAccountId",
                table: "EmployeeCostProfiles",
                column: "DefaultLaborGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCostProfiles_DefaultPayrollLiabilityGLAccountId",
                table: "EmployeeCostProfiles",
                column: "DefaultPayrollLiabilityGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCostProfiles_DepartmentId",
                table: "EmployeeCostProfiles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCostProfiles_EmployeeCode",
                table: "EmployeeCostProfiles",
                column: "EmployeeCode",
                unique: true,
                filter: "[EmployeeCode] IS NOT NULL AND [EmployeeCode] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCostProfiles_USALIDepartmentId",
                table: "EmployeeCostProfiles",
                column: "USALIDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborProductivitySnapshots_DepartmentId",
                table: "LaborProductivitySnapshots",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborProductivitySnapshots_USALIDepartmentId",
                table: "LaborProductivitySnapshots",
                column: "USALIDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAllocationRules_DepartmentId",
                table: "PayrollAllocationRules",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAllocationRules_EmployeeCostProfileId",
                table: "PayrollAllocationRules",
                column: "EmployeeCostProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAllocationRules_LaborGLAccountId",
                table: "PayrollAllocationRules",
                column: "LaborGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAllocationRules_USALIDepartmentId",
                table: "PayrollAllocationRules",
                column: "USALIDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCostEntries_DepartmentId",
                table: "PayrollCostEntries",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCostEntries_EmployeeCostProfileId",
                table: "PayrollCostEntries",
                column: "EmployeeCostProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCostEntries_LaborGLAccountId",
                table: "PayrollCostEntries",
                column: "LaborGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCostEntries_PayrollLiabilityGLAccountId",
                table: "PayrollCostEntries",
                column: "PayrollLiabilityGLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCostEntries_PayrollPeriodId",
                table: "PayrollCostEntries",
                column: "PayrollPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCostEntries_USALIDepartmentId",
                table: "PayrollCostEntries",
                column: "USALIDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_JournalEntryId",
                table: "PayrollPeriods",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_PeriodName",
                table: "PayrollPeriods",
                column: "PeriodName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceChargeDistributionLines_DepartmentId",
                table: "ServiceChargeDistributionLines",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceChargeDistributionLines_EmployeeCostProfileId",
                table: "ServiceChargeDistributionLines",
                column: "EmployeeCostProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceChargeDistributionLines_ServiceChargePoolId",
                table: "ServiceChargeDistributionLines",
                column: "ServiceChargePoolId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceChargePools_JournalEntryId",
                table: "ServiceChargePools",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceChargePools_PoolName",
                table: "ServiceChargePools",
                column: "PoolName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentLaborBudgets");

            migrationBuilder.DropTable(
                name: "LaborProductivitySnapshots");

            migrationBuilder.DropTable(
                name: "PayrollAllocationRules");

            migrationBuilder.DropTable(
                name: "PayrollCostEntries");

            migrationBuilder.DropTable(
                name: "ServiceChargeDistributionLines");

            migrationBuilder.DropTable(
                name: "PayrollPeriods");

            migrationBuilder.DropTable(
                name: "EmployeeCostProfiles");

            migrationBuilder.DropTable(
                name: "ServiceChargePools");
        }
    }
}
