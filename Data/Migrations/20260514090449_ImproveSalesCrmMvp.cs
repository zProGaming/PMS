using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImproveSalesCrmMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SalesLeads",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SalesLeads",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SalesActivities",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SalesAccounts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SalesAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "SalesAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "SalesAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ContactPersons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE SalesAccounts
                SET AccountType = 'Other'
                WHERE AccountType IS NULL
                   OR LTRIM(RTRIM(AccountType)) = ''
                   OR AccountType NOT IN ('Corporate', 'TravelAgency', 'Government', 'EventClient', 'OnlineTravelAgency', 'Other');
                """);

            migrationBuilder.Sql("""
                UPDATE SalesActivities
                SET ActivityType = 'Other'
                WHERE ActivityType IS NULL
                   OR LTRIM(RTRIM(ActivityType)) = ''
                   OR ActivityType NOT IN ('Call', 'Email', 'Meeting', 'SiteInspection', 'ProposalFollowUp', 'ContractFollowUp', 'Other');
                """);

            migrationBuilder.Sql("""
                UPDATE SalesLeads
                SET Status = CASE
                    WHEN Status = 2 THEN 3
                    WHEN Status = 3 THEN 2
                    ELSE Status
                END
                WHERE Status IN (2, 3);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SalesLeads");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SalesLeads");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SalesActivities");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SalesAccounts");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SalesAccounts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "SalesAccounts");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "SalesAccounts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ContactPersons");

            migrationBuilder.Sql("""
                UPDATE SalesLeads
                SET Status = CASE
                    WHEN Status = 2 THEN 3
                    WHEN Status = 3 THEN 2
                    ELSE Status
                END
                WHERE Status IN (2, 3);
                """);
        }
    }
}
