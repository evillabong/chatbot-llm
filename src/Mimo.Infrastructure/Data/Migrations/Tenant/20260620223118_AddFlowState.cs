using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mimo.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddFlowState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "flow_state",
                table: "conversations",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "flow_state",
                table: "conversations");
        }
    }
}
