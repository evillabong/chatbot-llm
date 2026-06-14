using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mimo.Infrastructure.Data.Migrations.Global
{
    /// <inheritdoc />
    public partial class AddAiPlanPoliciesAndUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_plan_policies",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    plan_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    allowed_providers = table.Column<string>(type: "jsonb", nullable: false),
                    monthly_request_quota = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    monthly_token_quota = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_plan_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_usage_records",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    completion_tokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_tokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_plan_policies_plan_code",
                schema: "public",
                table: "ai_plan_policies",
                column: "plan_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_usage_records_tenant_created",
                schema: "public",
                table: "ai_usage_records",
                columns: new[] { "tenant_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_plan_policies",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ai_usage_records",
                schema: "public");
        }
    }
}
