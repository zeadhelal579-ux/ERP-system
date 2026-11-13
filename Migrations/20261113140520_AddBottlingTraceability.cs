using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_system.Migrations
{
    /// <inheritdoc />
    public partial class AddBottlingTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_Users_TestedBy",
                table: "LabResults");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_Products_ProductId",
                table: "ProductionBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_RawMaterials_RawMaterialId",
                table: "ProductionBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_Users_CreatedBy",
                table: "ProductionBatches");

            migrationBuilder.DropIndex(
                name: "IX_ShippingLogs_BatchId",
                table: "ShippingLogs");

            migrationBuilder.AddColumn<string>(
                name: "LotCode",
                table: "RawMaterials",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RawMaterials",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierLot",
                table: "RawMaterials",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "RawMaterials",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "kg");

            migrationBuilder.AddColumn<bool>(
                name: "IsHeld",
                table: "ProductionBatches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProductionBatches",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StartRequestId",
                table: "ProductionBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RawMaterialId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssuedBy = table.Column<int>(type: "int", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialIssues", x => x.Id);
                    table.CheckConstraint("CK_MaterialIssues_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_MaterialIssues_ProductionBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialIssues_RawMaterials_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialIssues_Users_IssuedBy",
                        column: x => x.IssuedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PalletDispatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TruckPlates = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShippedBy = table.Column<int>(type: "int", nullable: false),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PalletDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PalletDispatches_Users_ShippedBy",
                        column: x => x.ShippedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinishedPallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DispatchId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedPallets", x => x.Id);
                    table.CheckConstraint("CK_FinishedPallets_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_FinishedPallets_PalletDispatches_DispatchId",
                        column: x => x.DispatchId,
                        principalTable: "PalletDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinishedPallets_ProductionBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("UPDATE [RawMaterials] SET [LotCode] = CONCAT('LEGACY-', [Id]) WHERE [LotCode] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingLogs_BatchId",
                table: "ShippingLogs",
                column: "BatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_LotCode",
                table: "RawMaterials",
                column: "LotCode",
                unique: true,
                filter: "[LotCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_BatchNumber",
                table: "ProductionBatches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_StartRequestId",
                table: "ProductionBatches",
                column: "StartRequestId",
                unique: true,
                filter: "[StartRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedPallets_BatchId",
                table: "FinishedPallets",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_FinishedPallets_Code",
                table: "FinishedPallets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinishedPallets_DispatchId",
                table: "FinishedPallets",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_BatchId",
                table: "MaterialIssues",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_IssuedBy",
                table: "MaterialIssues",
                column: "IssuedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_RawMaterialId_BatchId",
                table: "MaterialIssues",
                columns: new[] { "RawMaterialId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_RequestId",
                table: "MaterialIssues",
                column: "RequestId",
                unique: true,
                filter: "[RequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PalletDispatches_RequestId",
                table: "PalletDispatches",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PalletDispatches_ShippedBy",
                table: "PalletDispatches",
                column: "ShippedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_Users_TestedBy",
                table: "LabResults",
                column: "TestedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Products_ProductId",
                table: "ProductionBatches",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_RawMaterials_RawMaterialId",
                table: "ProductionBatches",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Users_CreatedBy",
                table: "ProductionBatches",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_Users_TestedBy",
                table: "LabResults");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_Products_ProductId",
                table: "ProductionBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_RawMaterials_RawMaterialId",
                table: "ProductionBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBatches_Users_CreatedBy",
                table: "ProductionBatches");

            migrationBuilder.DropTable(
                name: "FinishedPallets");

            migrationBuilder.DropTable(
                name: "MaterialIssues");

            migrationBuilder.DropTable(
                name: "PalletDispatches");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ShippingLogs_BatchId",
                table: "ShippingLogs");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_LotCode",
                table: "RawMaterials");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBatches_BatchNumber",
                table: "ProductionBatches");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBatches_StartRequestId",
                table: "ProductionBatches");

            migrationBuilder.DropColumn(
                name: "LotCode",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "SupplierLot",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "IsHeld",
                table: "ProductionBatches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProductionBatches");

            migrationBuilder.DropColumn(
                name: "StartRequestId",
                table: "ProductionBatches");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingLogs_BatchId",
                table: "ShippingLogs",
                column: "BatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_Users_TestedBy",
                table: "LabResults",
                column: "TestedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Products_ProductId",
                table: "ProductionBatches",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_RawMaterials_RawMaterialId",
                table: "ProductionBatches",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBatches_Users_CreatedBy",
                table: "ProductionBatches",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
