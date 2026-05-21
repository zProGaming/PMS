using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vantage.PMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApTreasuryControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_APInvoices_PurchaseOrderId",
                table: "APInvoices");

            migrationBuilder.DropIndex(
                name: "IX_APInvoices_ReceivingRecordId",
                table: "APInvoices");

            migrationBuilder.AddColumn<int>(
                name: "JournalEntryId",
                table: "BankTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierInvoiceNumber",
                table: "APInvoices",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_JournalEntryId",
                table: "BankTransactions",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_APInvoices_PurchaseOrderId",
                table: "APInvoices",
                column: "PurchaseOrderId",
                unique: true,
                filter: "[PurchaseOrderId] IS NOT NULL AND [Status] <> 5 AND [Status] <> 6");

            migrationBuilder.CreateIndex(
                name: "IX_APInvoices_ReceivingRecordId",
                table: "APInvoices",
                column: "ReceivingRecordId",
                unique: true,
                filter: "[ReceivingRecordId] IS NOT NULL AND [Status] <> 5 AND [Status] <> 6");

            migrationBuilder.CreateIndex(
                name: "IX_APInvoices_SupplierId_SupplierInvoiceNumber",
                table: "APInvoices",
                columns: new[] { "SupplierId", "SupplierInvoiceNumber" },
                unique: true,
                filter: "[SupplierInvoiceNumber] IS NOT NULL AND [SupplierInvoiceNumber] <> ''");

            migrationBuilder.AddForeignKey(
                name: "FK_BankTransactions_JournalEntries_JournalEntryId",
                table: "BankTransactions",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankTransactions_JournalEntries_JournalEntryId",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_JournalEntryId",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_APInvoices_PurchaseOrderId",
                table: "APInvoices");

            migrationBuilder.DropIndex(
                name: "IX_APInvoices_ReceivingRecordId",
                table: "APInvoices");

            migrationBuilder.DropIndex(
                name: "IX_APInvoices_SupplierId_SupplierInvoiceNumber",
                table: "APInvoices");

            migrationBuilder.DropColumn(
                name: "JournalEntryId",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "SupplierInvoiceNumber",
                table: "APInvoices");

            migrationBuilder.CreateIndex(
                name: "IX_APInvoices_PurchaseOrderId",
                table: "APInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_APInvoices_ReceivingRecordId",
                table: "APInvoices",
                column: "ReceivingRecordId");
        }
    }
}
