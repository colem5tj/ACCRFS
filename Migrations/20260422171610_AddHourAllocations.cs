using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ACC_Demo.Migrations
{
    /// <inheritdoc />
    public partial class AddHourAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HourAllocations",
                columns: table => new
                {
                    HourAllocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    OrganizationId = table.Column<int>(type: "int", nullable: true),
                    HoursPerPeriod = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PeriodType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByAdminId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourAllocations", x => x.HourAllocationId);
                    table.ForeignKey(
                        name: "FK_HourAllocations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId");
                    table.ForeignKey(
                        name: "FK_HourAllocations_Users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_HourAllocations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HourAllocations_CreatedByAdminId",
                table: "HourAllocations",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_HourAllocations_OrganizationId",
                table: "HourAllocations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_HourAllocations_UserId",
                table: "HourAllocations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HourAllocations");
        }
    }
}
