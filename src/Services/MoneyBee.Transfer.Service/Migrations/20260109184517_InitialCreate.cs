using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyBee.Transfer.Service.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<int>(type: "integer", nullable: false),
                    amount_in_try = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    transaction_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    transaction_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    risk_level = table.Column<int>(type: "integer", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    approval_required_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sender_national_id = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    receiver_national_id = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transfers_created_at",
                table: "transfers",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_idempotency_key",
                table: "transfers",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_receiver_id",
                table: "transfers",
                column: "receiver_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_sender_id",
                table: "transfers",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_status",
                table: "transfers",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_transaction_code",
                table: "transfers",
                column: "transaction_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transfers");
        }
    }
}
