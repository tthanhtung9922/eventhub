using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finmy.Budgeting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategorySeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "budgeting",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.UpdateData(
                schema: "budgeting",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "Name",
                value: "Essentials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "budgeting",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "Name",
                value: "Ăn uống");

            migrationBuilder.InsertData(
                schema: "budgeting",
                table: "Categories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), null, "Nhà cửa" });
        }
    }
}
