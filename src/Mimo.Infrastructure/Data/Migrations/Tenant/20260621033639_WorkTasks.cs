using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mimo.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class WorkTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    assigned_agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_assigned_agent",
                table: "tasks",
                column: "assigned_agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_status_created",
                table: "tasks",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tasks");
        }
    }
}
