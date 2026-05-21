using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCodeLoginAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Hotels WHERE LEN(Code) > 80)
                    THROW 51001, 'Company Code migration stopped because one or more hotel codes are longer than 80 characters.', 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Hotels",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "HotelUserAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    HotelId = table.Column<int>(type: "int", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsDefaultCompany = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelUserAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotelUserAccesses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HotelUserAccesses_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_Code",
                table: "Hotels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelUserAccesses_HotelId",
                table: "HotelUserAccesses",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelUserAccesses_UserId_HotelId",
                table: "HotelUserAccesses",
                columns: new[] { "UserId", "HotelId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HotelUserAccesses");

            migrationBuilder.DropIndex(
                name: "IX_Hotels_Code",
                table: "Hotels");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Hotels",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);
        }
    }
}
