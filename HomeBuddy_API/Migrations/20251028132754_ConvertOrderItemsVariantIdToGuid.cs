using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBuddy_API.Migrations
{
    public partial class ConvertOrderItemsVariantIdToGuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Drop any existing foreign key (if it exists)
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Variants_VariantId'
)
ALTER TABLE [dbo].[OrderItems] DROP CONSTRAINT [FK_OrderItems_Variants_VariantId];
");

            // 2) Drop the int column
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[OrderItems]')
      AND name = 'VariantId'
      AND system_type_id = 56 -- int
)
ALTER TABLE [dbo].[OrderItems] DROP COLUMN [VariantId];
");

            // 3) Add the correct GUID column
            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "OrderItems",
                type: "uniqueidentifier",
                nullable: true);

            // 4) Create FK to Variants.Id
            migrationBuilder.Sql(@"
ALTER TABLE [dbo].[OrderItems]
ADD CONSTRAINT [FK_OrderItems_Variants_VariantId]
FOREIGN KEY ([VariantId]) REFERENCES [dbo].[Variants]([Id])
ON DELETE NO ACTION ON UPDATE NO ACTION;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1) Drop the FK
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Variants_VariantId'
)
ALTER TABLE [dbo].[OrderItems] DROP CONSTRAINT [FK_OrderItems_Variants_VariantId];
");

            // 2) Drop the GUID column
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[OrderItems]')
      AND name = 'VariantId'
      AND system_type_id = 36 -- uniqueidentifier
)
ALTER TABLE [dbo].[OrderItems] DROP COLUMN [VariantId];
");

            // 3) Re-add as int
            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "OrderItems",
                type: "int",
                nullable: true);
        }
    }
}
