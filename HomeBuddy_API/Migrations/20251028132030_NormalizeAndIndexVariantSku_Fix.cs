using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBuddy_API.Migrations
{
    public partial class NormalizeAndIndexVariantSku_Fix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Add RowVersion column safely ---
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Inventories]')
      AND name = 'RowVersion'
)
BEGIN
    ALTER TABLE [dbo].[Inventories]
    ADD [RowVersion] rowversion NOT NULL;
END
");

            // --- Ensure ReferenceId column is NVARCHAR(450) for indexing ---
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[InventoryTransactions]')
      AND name = 'ReferenceId'
      AND (max_length = -1 OR max_length > 900)
)
BEGIN
    ALTER TABLE [dbo].[InventoryTransactions]
    ALTER COLUMN [ReferenceId] NVARCHAR(450) NULL;
END
");

            // --- Create composite index if it doesn't already exist ---
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_InventoryTransaction_InventoryId_Type_Ref'
      AND object_id = OBJECT_ID(N'[dbo].[InventoryTransactions]')
)
BEGIN
    CREATE INDEX [IX_InventoryTransaction_InventoryId_Type_Ref]
    ON [dbo].[InventoryTransactions] ([InventoryId], [TransactionType], [ReferenceId]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // --- Drop the composite index if it exists ---
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_InventoryTransaction_InventoryId_Type_Ref'
      AND object_id = OBJECT_ID(N'[dbo].[InventoryTransactions]')
)
BEGIN
    DROP INDEX [IX_InventoryTransaction_InventoryId_Type_Ref]
    ON [dbo].[InventoryTransactions];
END
");

            // --- Revert ReferenceId to NVARCHAR(MAX) only if needed ---
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[InventoryTransactions]')
      AND c.name = 'ReferenceId'
      AND t.name = 'nvarchar'
      AND (c.max_length = 900 OR c.max_length = 450)
)
BEGIN
    ALTER TABLE [dbo].[InventoryTransactions]
    ALTER COLUMN [ReferenceId] NVARCHAR(MAX) NULL;
END
");

            // --- Drop RowVersion column if it exists ---
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Inventories]')
      AND name = 'RowVersion'
)
BEGIN
    ALTER TABLE [dbo].[Inventories]
    DROP COLUMN [RowVersion];
END
");
        }
    }
}

