using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodBeverageKitchenDisplayMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PreparingAt",
                table: "POSOrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyAt",
                table: "POSOrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToKitchenAt",
                table: "POSOrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServedAt",
                table: "POSOrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KitchenStationId",
                table: "MenuItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KitchenStations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenStations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_KitchenStationId",
                table: "MenuItems",
                column: "KitchenStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_KitchenStations_KitchenStationId",
                table: "MenuItems",
                column: "KitchenStationId",
                principalTable: "KitchenStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_KitchenStations_KitchenStationId",
                table: "MenuItems");

            migrationBuilder.DropTable(
                name: "KitchenStations");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_KitchenStationId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "PreparingAt",
                table: "POSOrderItems");

            migrationBuilder.DropColumn(
                name: "ReadyAt",
                table: "POSOrderItems");

            migrationBuilder.DropColumn(
                name: "SentToKitchenAt",
                table: "POSOrderItems");

            migrationBuilder.DropColumn(
                name: "ServedAt",
                table: "POSOrderItems");

            migrationBuilder.DropColumn(
                name: "KitchenStationId",
                table: "MenuItems");
        }
    }
}
