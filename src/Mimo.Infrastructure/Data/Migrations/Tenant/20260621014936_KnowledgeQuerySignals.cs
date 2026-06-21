using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mimo.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class KnowledgeQuerySignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "knowledge_query_signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    query_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    top_similarity = table.Column<double>(type: "double precision", nullable: true),
                    match_count = table.Column<int>(type: "integer", nullable: false),
                    knowledge_gap = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_query_signals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_signals_conversation",
                table: "knowledge_query_signals",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_signals_gap_created",
                table: "knowledge_query_signals",
                columns: new[] { "knowledge_gap", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "knowledge_query_signals");
        }
    }
}
