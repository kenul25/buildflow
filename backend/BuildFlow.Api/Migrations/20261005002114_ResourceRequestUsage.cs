using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildFlow.Api.Migrations.BuildFlowDb
{
    /// <inheritdoc />
    public partial class ResourceRequestUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResourceCount",
                table: "ResourceRequestItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResourceCount",
                table: "ResourceRequestItems");
        }
    }
}
