using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mimo.Infrastructure.Data.Migrations.Global
{
    /// <inheritdoc />
    public partial class AddPlanCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plans",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.id);
                    table.UniqueConstraint("AK_plans_code", x => x.code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenants_plan",
                schema: "public",
                table: "tenants",
                column: "plan");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_plan_policies_plans_plan_code",
                schema: "public",
                table: "ai_plan_policies",
                column: "plan_code",
                principalSchema: "public",
                principalTable: "plans",
                principalColumn: "code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tenants_plans_plan",
                schema: "public",
                table: "tenants",
                column: "plan",
                principalSchema: "public",
                principalTable: "plans",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_plan_policies_plans_plan_code",
                schema: "public",
                table: "ai_plan_policies");

            migrationBuilder.DropForeignKey(
                name: "FK_tenants_plans_plan",
                schema: "public",
                table: "tenants");

            migrationBuilder.DropTable(
                name: "plans",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_tenants_plan",
                schema: "public",
                table: "tenants");
        }
    }
}
