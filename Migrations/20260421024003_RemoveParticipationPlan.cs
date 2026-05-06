using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ACC_Demo.Migrations
{
    /// <inheritdoc />
    public partial class RemoveParticipationPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParticipationPlan",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParticipationPlan",
                table: "Users",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
