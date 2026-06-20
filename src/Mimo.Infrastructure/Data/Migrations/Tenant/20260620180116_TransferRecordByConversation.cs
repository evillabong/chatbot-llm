using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mimo.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class TransferRecordByConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transfer_records_tickets_ticket_id",
                table: "transfer_records");

            migrationBuilder.DropIndex(
                name: "ix_transfer_records_ticket",
                table: "transfer_records");

            migrationBuilder.AddColumn<Guid>(
                name: "conversation_id",
                table: "transfer_records",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_transfer_records_conversation",
                table: "transfer_records",
                column: "conversation_id");

            migrationBuilder.AddForeignKey(
                name: "FK_transfer_records_conversations_conversation_id",
                table: "transfer_records",
                column: "conversation_id",
                principalTable: "conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transfer_records_conversations_conversation_id",
                table: "transfer_records");

            migrationBuilder.DropIndex(
                name: "ix_transfer_records_conversation",
                table: "transfer_records");

            migrationBuilder.DropColumn(
                name: "conversation_id",
                table: "transfer_records");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_records_ticket",
                table: "transfer_records",
                column: "ticket_id");

            migrationBuilder.AddForeignKey(
                name: "FK_transfer_records_tickets_ticket_id",
                table: "transfer_records",
                column: "ticket_id",
                principalTable: "tickets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
