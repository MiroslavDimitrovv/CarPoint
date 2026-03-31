using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarPoint.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPaidToRentals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Rentals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Rentals",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Rentals");
        }
    }
}
