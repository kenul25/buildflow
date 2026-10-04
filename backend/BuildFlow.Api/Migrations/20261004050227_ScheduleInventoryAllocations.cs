using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildFlow.Api.Migrations.BuildFlowDb
{
    /// <inheritdoc />
    public partial class ScheduleInventoryAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleId",
                table: "InventoryReservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ScheduleId",
                table: "InventoryReservations",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryReservations_WorkSchedules_ScheduleId",
                table: "InventoryReservations",
                column: "ScheduleId",
                principalTable: "WorkSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryReservations_WorkSchedules_ScheduleId",
                table: "InventoryReservations");

            migrationBuilder.DropIndex(
                name: "IX_InventoryReservations_ScheduleId",
                table: "InventoryReservations");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "InventoryReservations");
        }
    }
}
