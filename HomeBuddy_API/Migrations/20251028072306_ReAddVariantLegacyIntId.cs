using Microsoft.EntityFrameworkCore.Migrations;

public partial class ReAddVariantLegacyIntId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "LegacyIntId",
            table: "Variants",
            type: "int",
            nullable: true);

        // Backfill — choose ONE of the two blocks below.

        // A) If Variants.Id is still INT at this point:
        // migrationBuilder.Sql("UPDATE Variants SET LegacyIntId = Id");

        //B) If Variants.Id is already GUID, but you still have the old int somewhere
        // (e.g., an archive/temp table), backfill from that mapping instead.
        // Example:
        migrationBuilder.Sql(@"
          UPDATE v
          SET v.LegacyIntId = m.LegacyIntId
           FROM Variants v
           JOIN VariantIdMap m ON m.GuidId = v.Id
         ");

        // Make the column required and unique for safe joins
        migrationBuilder.AlterColumn<int>(
            name: "LegacyIntId",
            table: "Variants",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Variants_LegacyIntId",
            table: "Variants",
            column: "LegacyIntId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Variants_LegacyIntId", table: "Variants");
        migrationBuilder.DropColumn(name: "LegacyIntId", table: "Variants");
    }
}
