using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mimo.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddChatbotFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "flow_node_id",
                table: "conversations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "chatbot_flows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    definition = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chatbot_flows", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_chatbot_flows_active",
                table: "chatbot_flows",
                column: "is_active",
                unique: true,
                filter: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chatbot_flows");

            migrationBuilder.DropColumn(
                name: "flow_node_id",
                table: "conversations");
        }
    }
}
