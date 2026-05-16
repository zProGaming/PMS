using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupBookingPseudoRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    SalesAccountId = table.Column<int>(type: "int", nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArrivalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepartureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BookingStatus = table.Column<int>(type: "int", nullable: false),
                    MarketSegment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillingInstruction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositRequired = table.Column<bool>(type: "bit", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupBookings_SalesAccounts_SalesAccountId",
                        column: x => x.SalesAccountId,
                        principalTable: "SalesAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupDeposits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupBookingId = table.Column<int>(type: "int", nullable: false),
                    DepositDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FolioId = table.Column<int>(type: "int", nullable: true),
                    FinanceDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsRefundable = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupDeposits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupDeposits_FinanceDocuments_FinanceDocumentId",
                        column: x => x.FinanceDocumentId,
                        principalTable: "FinanceDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupDeposits_Folios_FolioId",
                        column: x => x.FolioId,
                        principalTable: "Folios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupDeposits_GroupBookings_GroupBookingId",
                        column: x => x.GroupBookingId,
                        principalTable: "GroupBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupMemberReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupBookingId = table.Column<int>(type: "int", nullable: false),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    IsPrimaryGuest = table.Column<bool>(type: "bit", nullable: false),
                    BillingRoutingType = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMemberReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMemberReservations_GroupBookings_GroupBookingId",
                        column: x => x.GroupBookingId,
                        principalTable: "GroupBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMemberReservations_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupRoomBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupBookingId = table.Column<int>(type: "int", nullable: false),
                    RoomTypeId = table.Column<int>(type: "int", nullable: false),
                    BlockDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RoomsBlocked = table.Column<int>(type: "int", nullable: false),
                    RoomsPickedUp = table.Column<int>(type: "int", nullable: false),
                    RoomsReleased = table.Column<int>(type: "int", nullable: false),
                    RatePlanId = table.Column<int>(type: "int", nullable: true),
                    RateAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CutOffDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupRoomBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupRoomBlocks_GroupBookings_GroupBookingId",
                        column: x => x.GroupBookingId,
                        principalTable: "GroupBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupRoomBlocks_RatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupRoomBlocks_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PseudoRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PseudoRoomCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PseudoRoomName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    PseudoRoomType = table.Column<int>(type: "int", nullable: false),
                    LinkedSalesAccountId = table.Column<int>(type: "int", nullable: true),
                    LinkedBanquetEventId = table.Column<int>(type: "int", nullable: true),
                    LinkedGroupBookingId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PseudoRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PseudoRooms_BanquetEvents_LinkedBanquetEventId",
                        column: x => x.LinkedBanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PseudoRooms_GroupBookings_LinkedGroupBookingId",
                        column: x => x.LinkedGroupBookingId,
                        principalTable: "GroupBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PseudoRooms_SalesAccounts_LinkedSalesAccountId",
                        column: x => x.LinkedSalesAccountId,
                        principalTable: "SalesAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupPaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupBookingId = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    GroupDepositId = table.Column<int>(type: "int", nullable: true),
                    TargetFolioId = table.Column<int>(type: "int", nullable: true),
                    TargetReservationId = table.Column<int>(type: "int", nullable: true),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllocatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupPaymentAllocations_Folios_TargetFolioId",
                        column: x => x.TargetFolioId,
                        principalTable: "Folios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupPaymentAllocations_GroupBookings_GroupBookingId",
                        column: x => x.GroupBookingId,
                        principalTable: "GroupBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupPaymentAllocations_GroupDeposits_GroupDepositId",
                        column: x => x.GroupDepositId,
                        principalTable: "GroupDeposits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupPaymentAllocations_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupPaymentAllocations_Reservations_TargetReservationId",
                        column: x => x.TargetReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupFolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupBookingId = table.Column<int>(type: "int", nullable: false),
                    PseudoRoomId = table.Column<int>(type: "int", nullable: true),
                    FolioId = table.Column<int>(type: "int", nullable: true),
                    FolioName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    BillingName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillingAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillingTIN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupFolios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupFolios_Folios_FolioId",
                        column: x => x.FolioId,
                        principalTable: "Folios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupFolios_GroupBookings_GroupBookingId",
                        column: x => x.GroupBookingId,
                        principalTable: "GroupBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupFolios_PseudoRooms_PseudoRoomId",
                        column: x => x.PseudoRoomId,
                        principalTable: "PseudoRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChargeRoutingRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupBookingId = table.Column<int>(type: "int", nullable: true),
                    ReservationId = table.Column<int>(type: "int", nullable: true),
                    FolioId = table.Column<int>(type: "int", nullable: true),
                    SourceChargeCategory = table.Column<int>(type: "int", nullable: false),
                    RouteToType = table.Column<int>(type: "int", nullable: false),
                    TargetFolioId = table.Column<int>(type: "int", nullable: true),
                    TargetGroupFolioId = table.Column<int>(type: "int", nullable: true),
                    TargetPseudoRoomId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeRoutingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChargeRoutingRules_Folios_FolioId",
                        column: x => x.FolioId,
                        principalTable: "Folios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChargeRoutingRules_Folios_TargetFolioId",
                        column: x => x.TargetFolioId,
                        principalTable: "Folios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChargeRoutingRules_GroupBookings_GroupBookingId",
                        column: x => x.GroupBookingId,
                        principalTable: "GroupBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChargeRoutingRules_GroupFolios_TargetGroupFolioId",
                        column: x => x.TargetGroupFolioId,
                        principalTable: "GroupFolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChargeRoutingRules_PseudoRooms_TargetPseudoRoomId",
                        column: x => x.TargetPseudoRoomId,
                        principalTable: "PseudoRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChargeRoutingRules_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeRoutingRules_FolioId",
                table: "ChargeRoutingRules",
                column: "FolioId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeRoutingRules_GroupBookingId_ReservationId_FolioId_SourceChargeCategory_IsActive",
                table: "ChargeRoutingRules",
                columns: new[] { "GroupBookingId", "ReservationId", "FolioId", "SourceChargeCategory", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeRoutingRules_ReservationId",
                table: "ChargeRoutingRules",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeRoutingRules_TargetFolioId",
                table: "ChargeRoutingRules",
                column: "TargetFolioId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeRoutingRules_TargetGroupFolioId",
                table: "ChargeRoutingRules",
                column: "TargetGroupFolioId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeRoutingRules_TargetPseudoRoomId",
                table: "ChargeRoutingRules",
                column: "TargetPseudoRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupBookings_GroupCode",
                table: "GroupBookings",
                column: "GroupCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupBookings_SalesAccountId",
                table: "GroupBookings",
                column: "SalesAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupDeposits_FinanceDocumentId",
                table: "GroupDeposits",
                column: "FinanceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupDeposits_FolioId",
                table: "GroupDeposits",
                column: "FolioId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupDeposits_GroupBookingId",
                table: "GroupDeposits",
                column: "GroupBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupFolios_FolioId",
                table: "GroupFolios",
                column: "FolioId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupFolios_GroupBookingId",
                table: "GroupFolios",
                column: "GroupBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupFolios_PseudoRoomId",
                table: "GroupFolios",
                column: "PseudoRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberReservations_GroupBookingId_ReservationId",
                table: "GroupMemberReservations",
                columns: new[] { "GroupBookingId", "ReservationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberReservations_ReservationId",
                table: "GroupMemberReservations",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupPaymentAllocations_GroupBookingId",
                table: "GroupPaymentAllocations",
                column: "GroupBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupPaymentAllocations_GroupDepositId",
                table: "GroupPaymentAllocations",
                column: "GroupDepositId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupPaymentAllocations_PaymentId",
                table: "GroupPaymentAllocations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupPaymentAllocations_TargetFolioId",
                table: "GroupPaymentAllocations",
                column: "TargetFolioId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupPaymentAllocations_TargetReservationId",
                table: "GroupPaymentAllocations",
                column: "TargetReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRoomBlocks_GroupBookingId_RoomTypeId_BlockDate",
                table: "GroupRoomBlocks",
                columns: new[] { "GroupBookingId", "RoomTypeId", "BlockDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupRoomBlocks_RatePlanId",
                table: "GroupRoomBlocks",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupRoomBlocks_RoomTypeId",
                table: "GroupRoomBlocks",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PseudoRooms_LinkedBanquetEventId",
                table: "PseudoRooms",
                column: "LinkedBanquetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_PseudoRooms_LinkedGroupBookingId",
                table: "PseudoRooms",
                column: "LinkedGroupBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_PseudoRooms_LinkedSalesAccountId",
                table: "PseudoRooms",
                column: "LinkedSalesAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PseudoRooms_PseudoRoomCode",
                table: "PseudoRooms",
                column: "PseudoRoomCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChargeRoutingRules");

            migrationBuilder.DropTable(
                name: "GroupMemberReservations");

            migrationBuilder.DropTable(
                name: "GroupPaymentAllocations");

            migrationBuilder.DropTable(
                name: "GroupRoomBlocks");

            migrationBuilder.DropTable(
                name: "GroupFolios");

            migrationBuilder.DropTable(
                name: "GroupDeposits");

            migrationBuilder.DropTable(
                name: "PseudoRooms");

            migrationBuilder.DropTable(
                name: "GroupBookings");
        }
    }
}
