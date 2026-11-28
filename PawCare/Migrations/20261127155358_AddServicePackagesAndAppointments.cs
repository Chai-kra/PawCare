using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PawCare.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePackagesAndAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Pets",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "ServicePackages",
                columns: new[] { "Id", "Price", "ServiceType", "TimeAmount" },
                values: new object[,]
                {
                    { "SP00001", 45.00m, "Basic Bath & Blowout", 60 },
                    { "SP00002", 85.00m, "Full Grooming & Trim", 90 },
                    { "SP00003", 65.00m, "Spa & Nail Care", 60 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: "SP00001");

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: "SP00002");

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: "SP00003");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Pets");
        }
    }
}
