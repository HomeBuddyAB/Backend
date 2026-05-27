using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBuddy_API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrphanDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOrphaned",
                table: "ProductGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OrphanedAt",
                table: "ProductGroups",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Orphaned",
                table: "ImportLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Reappeared",
                table: "ImportLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOrphaned",
                table: "ProductGroups");

            migrationBuilder.DropColumn(
                name: "OrphanedAt",
                table: "ProductGroups");

            migrationBuilder.DropColumn(
                name: "Orphaned",
                table: "ImportLogs");

            migrationBuilder.DropColumn(
                name: "Reappeared",
                table: "ImportLogs");
        }
    }
}
