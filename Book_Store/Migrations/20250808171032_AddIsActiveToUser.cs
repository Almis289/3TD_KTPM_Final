using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Book_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Authors",
                keyColumn: "AuthorId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 8, 17, 10, 31, 949, DateTimeKind.Utc).AddTicks(2268));

            migrationBuilder.UpdateData(
                table: "Authors",
                keyColumn: "AuthorId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 8, 17, 10, 31, 949, DateTimeKind.Utc).AddTicks(2270));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Authors",
                keyColumn: "AuthorId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 8, 10, 52, 23, 981, DateTimeKind.Utc).AddTicks(693));

            migrationBuilder.UpdateData(
                table: "Authors",
                keyColumn: "AuthorId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 8, 10, 52, 23, 981, DateTimeKind.Utc).AddTicks(697));
        }
    }
}
