using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildFlow.Api.Migrations.AppDb
{
    /// <inheritdoc />
    public partial class ProcurementIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the legacy tables as a recoverable archive. Copy rows before relinking.
            migrationBuilder.Sql("""
                INSERT INTO "Warehouses" ("Id", "Name", "Location", "CreatedAt", "UpdatedAt")
                SELECT "Id", "Name", "Location", "CreatedAt", "UpdatedAt" FROM "Warehouse"
                ON CONFLICT ("Id") DO NOTHING;
                INSERT INTO "Materials" ("Id", "Name", "Category", "Unit", "UnitPrice", "CurrentStock", "ReservedStock", "WarehouseId", "CreatedAt", "UpdatedAt", "IsArchived")
                SELECT "Id", "Name", "Category", "Unit", "UnitPrice", "CurrentStock", "ReservedStock", "WarehouseId", "CreatedAt", "UpdatedAt", false FROM "Material"
                ON CONFLICT ("Id") DO NOTHING;
                """);


            migrationBuilder.DropForeignKey(
                name: "FK_SupplierMaterials_Material_MaterialId",
                table: "SupplierMaterials");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Suppliers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Suppliers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Suppliers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "SupplierQuotations",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "SupplierQuotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SupplierQuotations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialId",
                table: "SupplierQuotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "SupplierQuotations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SupplierQuotations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "SupplierQuotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidUntil",
                table: "SupplierQuotations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "SupplierMaterials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SupplierMaterials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "SupplierMaterials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "PurchaseRequests",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetLimit",
                table: "PurchaseRequests",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "PurchaseRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialId",
                table: "PurchaseRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "PurchaseRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "PurchaseRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PurchaseRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "PurchaseRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "PurchaseOrders",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuotationId",
                table: "PurchaseOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "PurchaseOrders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Deliveries",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Deliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StockReceivedAt",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Deliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_MaterialId",
                table: "SupplierQuotations",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_MaterialId",
                table: "PurchaseRequests",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_ProjectId",
                table: "PurchaseRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PurchaseRequestId",
                table: "PurchaseOrders",
                column: "PurchaseRequestId",
                unique: true,
                filter: "\"Status\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_QuotationId",
                table: "PurchaseOrders",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_PurchaseOrderId",
                table: "Deliveries",
                column: "PurchaseOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_PurchaseOrders_PurchaseOrderId",
                table: "Deliveries",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_PurchaseRequests_PurchaseRequestId",
                table: "PurchaseOrders",
                column: "PurchaseRequestId",
                principalTable: "PurchaseRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_SupplierQuotations_QuotationId",
                table: "PurchaseOrders",
                column: "QuotationId",
                principalTable: "SupplierQuotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequests_Materials_MaterialId",
                table: "PurchaseRequests",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequests_Projects_ProjectId",
                table: "PurchaseRequests",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierMaterials_Materials_MaterialId",
                table: "SupplierMaterials",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierQuotations_Materials_MaterialId",
                table: "SupplierQuotations",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_PurchaseOrders_PurchaseOrderId",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_PurchaseRequests_PurchaseRequestId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_SupplierQuotations_QuotationId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequests_Materials_MaterialId",
                table: "PurchaseRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequests_Projects_ProjectId",
                table: "PurchaseRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierMaterials_Materials_MaterialId",
                table: "SupplierMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierQuotations_Materials_MaterialId",
                table: "SupplierQuotations");

            migrationBuilder.DropIndex(
                name: "IX_SupplierQuotations_MaterialId",
                table: "SupplierQuotations");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_MaterialId",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_ProjectId",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_PurchaseRequestId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_QuotationId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_PurchaseOrderId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SupplierQuotations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SupplierQuotations");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "SupplierQuotations");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "SupplierQuotations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SupplierQuotations");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SupplierQuotations");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "SupplierQuotations");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "SupplierMaterials");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SupplierMaterials");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "SupplierMaterials");

            migrationBuilder.DropColumn(
                name: "BudgetLimit",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "StockReceivedAt",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Deliveries");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "SupplierQuotations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "PurchaseRequests",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "PurchaseOrders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "Deliveries",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierMaterials_Material_MaterialId",
                table: "SupplierMaterials",
                column: "MaterialId",
                principalTable: "Material",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
