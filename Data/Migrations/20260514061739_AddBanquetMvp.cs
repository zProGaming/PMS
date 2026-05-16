using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBanquetMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BanquetPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PricePerPax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FunctionRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BanquetEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalesAccountId = table.Column<int>(type: "int", nullable: true),
                    FunctionRoomId = table.Column<int>(type: "int", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ExpectedPax = table.Column<int>(type: "int", nullable: false),
                    GuaranteedPax = table.Column<int>(type: "int", nullable: false),
                    EventStatus = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetEvents_FunctionRooms_FunctionRoomId",
                        column: x => x.FunctionRoomId,
                        principalTable: "FunctionRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BanquetEvents_SalesAccounts_SalesAccountId",
                        column: x => x.SalesAccountId,
                        principalTable: "SalesAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BanquetCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BanquetEventId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetCharges_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BanquetEventOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BanquetEventId = table.Column<int>(type: "int", nullable: false),
                    BEODate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MenuDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SetupInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EquipmentRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KitchenInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillingInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreparedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetEventOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetEventOrders_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanquetCharges_BanquetEventId",
                table: "BanquetCharges",
                column: "BanquetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetEventOrders_BanquetEventId",
                table: "BanquetEventOrders",
                column: "BanquetEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetEvents_FunctionRoomId",
                table: "BanquetEvents",
                column: "FunctionRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetEvents_SalesAccountId",
                table: "BanquetEvents",
                column: "SalesAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanquetCharges");

            migrationBuilder.DropTable(
                name: "BanquetEventOrders");

            migrationBuilder.DropTable(
                name: "BanquetPackages");

            migrationBuilder.DropTable(
                name: "BanquetEvents");

            migrationBuilder.DropTable(
                name: "FunctionRooms");
        }
    }
}
