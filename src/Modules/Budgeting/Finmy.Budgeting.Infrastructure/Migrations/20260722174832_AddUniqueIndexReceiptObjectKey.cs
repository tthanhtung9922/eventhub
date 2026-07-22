using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finmy.Budgeting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexReceiptObjectKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receipts_ObjectKey",
                schema: "budgeting",
                table: "Receipts");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_ObjectKey",
                schema: "budgeting",
                table: "Receipts",
                column: "ObjectKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receipts_ObjectKey",
                schema: "budgeting",
                table: "Receipts");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_ObjectKey",
                schema: "budgeting",
                table: "Receipts",
                column: "ObjectKey");
        }
    }
}
