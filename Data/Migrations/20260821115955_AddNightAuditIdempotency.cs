using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNightAuditIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FolioItems_FolioId",
                table: "FolioItems");

            migrationBuilder.AddColumn<DateTime>(
                name: "NightAuditBusinessDate",
                table: "FolioItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NightAuditChargeCode",
                table: "FolioItems",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NightAudits_BusinessDate",
                table: "NightAudits",
                column: "BusinessDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolioItems_FolioId_NightAuditBusinessDate_NightAuditChargeCode",
                table: "FolioItems",
                columns: new[] { "FolioId", "NightAuditBusinessDate", "NightAuditChargeCode" },
                unique: true,
                filter: "[NightAuditBusinessDate] IS NOT NULL AND [NightAuditChargeCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NightAudits_BusinessDate",
                table: "NightAudits");

            migrationBuilder.DropIndex(
                name: "IX_FolioItems_FolioId_NightAuditBusinessDate_NightAuditChargeCode",
                table: "FolioItems");

            migrationBuilder.DropColumn(
                name: "NightAuditBusinessDate",
                table: "FolioItems");

            migrationBuilder.DropColumn(
                name: "NightAuditChargeCode",
                table: "FolioItems");

            migrationBuilder.CreateIndex(
                name: "IX_FolioItems_FolioId",
                table: "FolioItems",
                column: "FolioId");
        }
    }
}
