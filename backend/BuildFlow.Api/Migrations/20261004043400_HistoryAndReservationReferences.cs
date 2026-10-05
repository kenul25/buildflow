using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildFlow.Api.Migrations.BuildFlowDb
{
    /// <inheritdoc />
    public partial class HistoryAndReservationReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReversalOfId",
                table: "StockMovements",
                column: "ReversalOfId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressUpdates_CorrectionOfId",
                table: "ProgressUpdates",
                column: "CorrectionOfId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ProjectId",
                table: "InventoryReservations",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryReservations_Projects_ProjectId",
                table: "InventoryReservations",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressUpdates_ProgressUpdates_CorrectionOfId",
                table: "ProgressUpdates",
                column: "CorrectionOfId",
                principalTable: "ProgressUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StockMovements_ReversalOfId",
                table: "StockMovements",
                column: "ReversalOfId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryReservations_Projects_ProjectId",
                table: "InventoryReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgressUpdates_ProgressUpdates_CorrectionOfId",
                table: "ProgressUpdates");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StockMovements_ReversalOfId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ReversalOfId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_ProgressUpdates_CorrectionOfId",
                table: "ProgressUpdates");

            migrationBuilder.DropIndex(
                name: "IX_InventoryReservations_ProjectId",
                table: "InventoryReservations");
        }
    }
}
