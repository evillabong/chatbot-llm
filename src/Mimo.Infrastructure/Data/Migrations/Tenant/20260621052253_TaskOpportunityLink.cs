using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mimo.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class TaskOpportunityLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "opportunity_id",
                table: "tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_opportunity",
                table: "tasks",
                column: "opportunity_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tasks_opportunity",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "opportunity_id",
                table: "tasks");
        }
    }
}
