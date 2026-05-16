using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRevenueRateManagementMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RatePlanId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RatePlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IncludesBreakfast = table.Column<bool>(type: "bit", nullable: false),
                    IsCorporateRate = table.Column<bool>(type: "bit", nullable: false),
                    CancellationPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepositPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomInventoryControls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomTypeId = table.Column<int>(type: "int", nullable: false),
                    InventoryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalRooms = table.Column<int>(type: "int", nullable: false),
                    RoomsToSell = table.Column<int>(type: "int", nullable: false),
                    OverbookingLimit = table.Column<int>(type: "int", nullable: false),
                    StopSell = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInventoryControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomInventoryControls_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    AppliesToRatePlanId = table.Column<int>(type: "int", nullable: true),
                    AppliesToRoomTypeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionCodes_RatePlans_AppliesToRatePlanId",
                        column: x => x.AppliesToRatePlanId,
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionCodes_RoomTypes_AppliesToRoomTypeId",
                        column: x => x.AppliesToRoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RateRestrictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RatePlanId = table.Column<int>(type: "int", nullable: true),
                    RoomTypeId = table.Column<int>(type: "int", nullable: true),
                    RestrictionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MinimumLengthOfStay = table.Column<int>(type: "int", nullable: false),
                    MaximumLengthOfStay = table.Column<int>(type: "int", nullable: true),
                    ClosedToArrival = table.Column<bool>(type: "bit", nullable: false),
                    ClosedToDeparture = table.Column<bool>(type: "bit", nullable: false),
                    StopSell = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateRestrictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateRestrictions_RatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RateRestrictions_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RatePlanId = table.Column<int>(type: "int", nullable: false),
                    RoomTypeId = table.Column<int>(type: "int", nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExtraAdultRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExtraChildRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomTypeRates_RatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomTypeRates_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeasonalRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RatePlanId = table.Column<int>(type: "int", nullable: false),
                    RoomTypeId = table.Column<int>(type: "int", nullable: false),
                    SeasonName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExtraAdultRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExtraChildRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonalRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonalRates_RatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeasonalRates_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RatePlanId",
                table: "Reservations",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCodes_AppliesToRatePlanId",
                table: "PromotionCodes",
                column: "AppliesToRatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCodes_AppliesToRoomTypeId",
                table: "PromotionCodes",
                column: "AppliesToRoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RateRestrictions_RatePlanId",
                table: "RateRestrictions",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RateRestrictions_RoomTypeId",
                table: "RateRestrictions",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomInventoryControls_RoomTypeId",
                table: "RoomInventoryControls",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeRates_RatePlanId",
                table: "RoomTypeRates",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeRates_RoomTypeId",
                table: "RoomTypeRates",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalRates_RatePlanId",
                table: "SeasonalRates",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalRates_RoomTypeId",
                table: "SeasonalRates",
                column: "RoomTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_RatePlans_RatePlanId",
                table: "Reservations",
                column: "RatePlanId",
                principalTable: "RatePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_RatePlans_RatePlanId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "PromotionCodes");

            migrationBuilder.DropTable(
                name: "RateRestrictions");

            migrationBuilder.DropTable(
                name: "RoomInventoryControls");

            migrationBuilder.DropTable(
                name: "RoomTypeRates");

            migrationBuilder.DropTable(
                name: "SeasonalRates");

            migrationBuilder.DropTable(
                name: "RatePlans");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_RatePlanId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RatePlanId",
                table: "Reservations");
        }
    }
}
