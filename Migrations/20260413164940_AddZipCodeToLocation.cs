using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ACC_Demo.Migrations
{
    /// <inheritdoc />
    public partial class AddZipCodeToLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "UserLocationPreferences",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "UserLocationPreferences");
        }
    }
}
