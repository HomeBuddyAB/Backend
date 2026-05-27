using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBuddy_API.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedSuspension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FeedSuspendedAt",
                table: "ProductGroups",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeedSuspended",
                table: "ProductGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeedSuspendedAt",
                table: "ProductGroups");

            migrationBuilder.DropColumn(
                name: "IsFeedSuspended",
                table: "ProductGroups");
        }
    }
}
