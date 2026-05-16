using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportPrintExportCenterMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ReportName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ReportCategory = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoutePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredRoles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportsPrint = table.Column<bool>(type: "bit", nullable: false),
                    SupportsCsvExport = table.Column<bool>(type: "bit", nullable: false),
                    SupportsHtmlExport = table.Column<bool>(type: "bit", nullable: false),
                    SupportsPdfViaBrowserPrint = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportCatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportExportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ReportName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ReportCategory = table.Column<int>(type: "int", nullable: false),
                    ExportType = table.Column<int>(type: "int", nullable: false),
                    ExportedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateRangeStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateRangeEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportExportLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportTemplateSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ReportName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ReportCategory = table.Column<int>(type: "int", nullable: false),
                    HeaderTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FooterText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowLogo = table.Column<bool>(type: "bit", nullable: false),
                    ShowHotelName = table.Column<bool>(type: "bit", nullable: false),
                    ShowPreparedBy = table.Column<bool>(type: "bit", nullable: false),
                    ShowReviewedBy = table.Column<bool>(type: "bit", nullable: false),
                    ShowGeneratedDate = table.Column<bool>(type: "bit", nullable: false),
                    ShowDateRange = table.Column<bool>(type: "bit", nullable: false),
                    ShowDisclaimer = table.Column<bool>(type: "bit", nullable: false),
                    DisclaimerText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsLandscape = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTemplateSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedReportRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ReportName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ReportCategory = table.Column<int>(type: "int", nullable: false),
                    DateRangeStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateRangeEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RunBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RunAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedReportRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportCatalogItems_ReportKey",
                table: "ReportCatalogItems",
                column: "ReportKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportExportLogs_ReportKey_ExportedAt",
                table: "ReportExportLogs",
                columns: new[] { "ReportKey", "ExportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportTemplateSettings_ReportKey",
                table: "ReportTemplateSettings",
                column: "ReportKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedReportRuns_ReportKey_RunAt",
                table: "SavedReportRuns",
                columns: new[] { "ReportKey", "RunAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportCatalogItems");

            migrationBuilder.DropTable(
                name: "ReportExportLogs");

            migrationBuilder.DropTable(
                name: "ReportTemplateSettings");

            migrationBuilder.DropTable(
                name: "SavedReportRuns");
        }
    }
}
