using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ACC_Demo.Migrations
{
    /// <inheritdoc />
    public partial class MakeTimeLedgerTransactionOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeLedgers_Transactions_TransactionId",
                table: "TimeLedgers");

            migrationBuilder.AlterColumn<int>(
                name: "TransactionId",
                table: "TimeLedgers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TimeLedgers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeLedgers_Transactions_TransactionId",
                table: "TimeLedgers",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeLedgers_Transactions_TransactionId",
                table: "TimeLedgers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TimeLedgers");

            migrationBuilder.AlterColumn<int>(
                name: "TransactionId",
                table: "TimeLedgers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeLedgers_Transactions_TransactionId",
                table: "TimeLedgers",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "TransactionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
