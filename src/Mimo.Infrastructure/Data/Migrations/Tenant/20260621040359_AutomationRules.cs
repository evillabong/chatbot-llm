using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mimo.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AutomationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "automation_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    trigger_event = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    conditions_json = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'[]'"),
                    action_type = table.Column<int>(type: "integer", nullable: false),
                    action_task_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action_assigned_agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_automation_rules_trigger_enabled",
                table: "automation_rules",
                columns: new[] { "trigger_event", "is_enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "automation_rules");
        }
    }
}
