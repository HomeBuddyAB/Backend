using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBuddy_API.Migrations
{
    /// <inheritdoc />
    public partial class AddImportLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemsStaged = table.Column<int>(type: "int", nullable: false),
                    ItemsUpdated = table.Column<int>(type: "int", nullable: false),
                    ItemsSkipped = table.Column<int>(type: "int", nullable: false),
                    AutoCategorized = table.Column<int>(type: "int", nullable: false),
                    Uncategorized = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportLogs");
        }
    }
}
