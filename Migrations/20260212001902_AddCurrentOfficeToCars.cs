using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarPoint.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentOfficeToCars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentOffice",
                table: "Cars",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentOffice",
                table: "Cars");
        }
    }
}
