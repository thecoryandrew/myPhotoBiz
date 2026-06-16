using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myPhotoBiz.Migrations
{
    /// <inheritdoc />
    public partial class MoneyPrecisionSoftDeleteIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Activity_UserId",
                table: "Activities");

            migrationBuilder.RenameIndex(
                name: "IX_Photos_ClientProfileId",
                table: "Photos",
                newName: "IX_Photo_ClientProfileId");

            migrationBuilder.AlterColumn<long>(
                name: "DurationHours",
                table: "ServicePackages",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<long>(
                name: "Price",
                table: "PrintPricings",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<long>(
                name: "TotalPrice",
                table: "PrintOrders",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<long>(
                name: "UnitPrice",
                table: "PrintItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<long>(
                name: "Price",
                table: "PhotoShoots",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Contracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ClientProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<long>(
                name: "EstimatedDurationHours",
                table: "BookingRequests",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAccess_ExpiryDate",
                table: "GalleryAccesses",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_UserId_CreatedAt",
                table: "Activities",
                columns: new[] { "UserId", "CreatedAt" });

            // Decimals are now stored as integer hundredths (read back as value / 100).
            // Existing rows hold whole-currency/whole-hour values, so scale them up by 100.
            UpdateDecimals(migrationBuilder, newValueSql: "CAST(ROUND(\"{0}\" * 100) AS INTEGER)");
        }

        private static readonly (string Table, string Column)[] DecimalColumns =
        {
            ("Invoices", "Amount"),
            ("Invoices", "Tax"),
            ("InvoiceItems", "UnitPrice"),
            ("PhotoShoots", "Price"),
            ("PrintPricings", "Price"),
            ("PrintItems", "UnitPrice"),
            ("PrintOrders", "TotalPrice"),
            ("ServicePackages", "BasePrice"),
            ("ServicePackages", "DiscountedPrice"),
            ("ServicePackages", "DurationHours"),
            ("PackageAddOns", "Price"),
            ("BookingRequests", "EstimatedPrice"),
            ("BookingRequests", "EstimatedDurationHours"),
            ("PhotographerProfiles", "HourlyRate"),
        };

        private static void UpdateDecimals(MigrationBuilder migrationBuilder, string newValueSql)
        {
            foreach (var (table, column) in DecimalColumns)
            {
                var expr = string.Format(newValueSql, column);
                migrationBuilder.Sql(
                    $"UPDATE \"{table}\" SET \"{column}\" = {expr} WHERE \"{column}\" IS NOT NULL;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the hundredths scaling first so the values are back in whole-currency form
            // before the column types revert. Division preserves the fractional component.
            UpdateDecimals(migrationBuilder, newValueSql: "(\"{0}\" / 100.0)");

            migrationBuilder.DropIndex(
                name: "IX_GalleryAccess_ExpiryDate",
                table: "GalleryAccesses");

            migrationBuilder.DropIndex(
                name: "IX_Activity_UserId_CreatedAt",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ClientProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_Photo_ClientProfileId",
                table: "Photos",
                newName: "IX_Photos_ClientProfileId");

            migrationBuilder.AlterColumn<double>(
                name: "DurationHours",
                table: "ServicePackages",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "PrintPricings",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "TotalPrice",
                table: "PrintOrders",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "UnitPrice",
                table: "PrintItems",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "PhotoShoots",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "EstimatedDurationHours",
                table: "BookingRequests",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_UserId",
                table: "Activities",
                column: "UserId");
        }
    }
}
