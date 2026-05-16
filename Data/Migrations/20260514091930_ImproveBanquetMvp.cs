using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImproveBanquetMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "FunctionRooms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumPax",
                table: "BanquetPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActualPax",
                table: "BanquetEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BanquetPackageId",
                table: "BanquetEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BanquetEvents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "BanquetEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "BanquetEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventType",
                table: "BanquetEvents",
                type: "int",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.AddColumn<int>(
                name: "SalesLeadId",
                table: "BanquetEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BanquetEventOrders",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "SpecialInstructions",
                table: "BanquetEventOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChargeDate",
                table: "BanquetCharges",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "BanquetCharges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetEvents_BanquetPackageId",
                table: "BanquetEvents",
                column: "BanquetPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetEvents_SalesLeadId",
                table: "BanquetEvents",
                column: "SalesLeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_BanquetEvents_BanquetPackages_BanquetPackageId",
                table: "BanquetEvents",
                column: "BanquetPackageId",
                principalTable: "BanquetPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BanquetEvents_SalesLeads_SalesLeadId",
                table: "BanquetEvents",
                column: "SalesLeadId",
                principalTable: "SalesLeads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE BanquetEvents
                SET EventStatus = EventStatus + 1
                WHERE EventStatus BETWEEN 0 AND 4;
                """);

            migrationBuilder.Sql("""
                UPDATE BanquetEventOrders
                SET Status = 5
                WHERE Status = 4;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BanquetEvents_BanquetPackages_BanquetPackageId",
                table: "BanquetEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_BanquetEvents_SalesLeads_SalesLeadId",
                table: "BanquetEvents");

            migrationBuilder.DropIndex(
                name: "IX_BanquetEvents_BanquetPackageId",
                table: "BanquetEvents");

            migrationBuilder.DropIndex(
                name: "IX_BanquetEvents_SalesLeadId",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "FunctionRooms");

            migrationBuilder.DropColumn(
                name: "MinimumPax",
                table: "BanquetPackages");

            migrationBuilder.DropColumn(
                name: "ActualPax",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "BanquetPackageId",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "SalesLeadId",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BanquetEventOrders");

            migrationBuilder.DropColumn(
                name: "SpecialInstructions",
                table: "BanquetEventOrders");

            migrationBuilder.DropColumn(
                name: "ChargeDate",
                table: "BanquetCharges");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "BanquetCharges");

            migrationBuilder.Sql("""
                UPDATE BanquetEvents
                SET EventStatus = CASE
                    WHEN EventStatus BETWEEN 1 AND 5 THEN EventStatus - 1
                    WHEN EventStatus = 6 THEN 4
                    ELSE EventStatus
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE BanquetEventOrders
                SET Status = CASE
                    WHEN Status = 5 THEN 4
                    WHEN Status = 4 THEN 1
                    ELSE Status
                END;
                """);
        }
    }
}
