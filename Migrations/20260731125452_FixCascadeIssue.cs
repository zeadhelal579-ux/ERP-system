using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_system.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShippingLogs_Users_ShippedBy",
                table: "ShippingLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingLogs_Users_ShippedBy",
                table: "ShippingLogs",
                column: "ShippedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShippingLogs_Users_ShippedBy",
                table: "ShippingLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingLogs_Users_ShippedBy",
                table: "ShippingLogs",
                column: "ShippedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
