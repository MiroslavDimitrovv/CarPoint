using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarPoint.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupReturnOfficeToRentals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PickupOffice",
                table: "Rentals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReturnOffice",
                table: "Rentals",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupOffice",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "ReturnOffice",
                table: "Rentals");
        }
    }
}
