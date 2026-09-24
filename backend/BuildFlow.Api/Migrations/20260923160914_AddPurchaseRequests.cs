using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BuildFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaterialName = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    EstimatedUnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedTotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    RequiredByDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_SupplierId",
                table: "SupplierQuotations",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierQuotations_Suppliers_SupplierId",
                table: "SupplierQuotations",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierQuotations_Suppliers_SupplierId",
                table: "SupplierQuotations");

            migrationBuilder.DropTable(
                name: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupplierQuotations_SupplierId",
                table: "SupplierQuotations");
        }
    }
}
