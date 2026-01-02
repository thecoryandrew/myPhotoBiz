using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myPhotoBiz.Migrations
{
    /// <inheritdoc />
    public partial class UserConsolidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // STEP 1: Create new tables first (without FK constraints that depend on existing data)
            migrationBuilder.CreateTable(
                name: "ClientProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotographerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Bio = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Specialties = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PortfolioUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotographerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotographerProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // STEP 2: Migrate existing Clients data to ClientProfiles
            // This creates ClientProfiles from existing Clients, preserving the same Id
            migrationBuilder.Sql(@"
                INSERT INTO ClientProfiles (Id, UserId, PhoneNumber, Address, Notes, CreatedDate, UpdatedDate)
                SELECT c.Id, c.UserId, c.PhoneNumber, c.Address, COALESCE(c.Notes, ''), c.CreatedDate, c.UpdatedDate
                FROM Clients c
                WHERE c.UserId IS NOT NULL AND c.UserId != '';
            ");

            // STEP 3: Add new columns to existing tables (with nullable to allow migration)
            migrationBuilder.AddColumn<int>(
                name: "ClientProfileId",
                table: "PhotoShoots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhotographerProfileId",
                table: "PhotoShoots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientProfileId",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "GallerySessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientProfileId",
                table: "Contracts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientProfileId",
                table: "ClientBadges",
                type: "INTEGER",
                nullable: true);

            // STEP 4: Migrate FK values from old ClientId to new ClientProfileId
            // The ClientProfile.Id matches the old Client.Id from our INSERT above
            migrationBuilder.Sql(@"
                UPDATE PhotoShoots SET ClientProfileId = ClientId WHERE ClientId IS NOT NULL;
                UPDATE Invoices SET ClientProfileId = ClientId WHERE ClientId IS NOT NULL;
                UPDATE Contracts SET ClientProfileId = ClientId WHERE ClientId IS NOT NULL;
                UPDATE ClientBadges SET ClientProfileId = ClientId WHERE ClientId IS NOT NULL;
            ");

            // For GallerySessions, get UserId from the associated Gallery's client (if any)
            // Or we can set a default admin user - for now, we'll leave it null and clean up later

            // STEP 5: Create GalleryAccesses table
            migrationBuilder.CreateTable(
                name: "GalleryAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GalleryId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    GrantedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanDownload = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanProof = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanOrder = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GalleryAccesses_ClientProfiles_ClientProfileId",
                        column: x => x.ClientProfileId,
                        principalTable: "ClientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GalleryAccesses_Galleries_GalleryId",
                        column: x => x.GalleryId,
                        principalTable: "Galleries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // STEP 6: Remove old columns from Gallery
            migrationBuilder.DropIndex(
                name: "IX_Gallery_ClientCode",
                table: "Galleries");

            migrationBuilder.DropColumn(
                name: "ClientCode",
                table: "Galleries");

            migrationBuilder.DropColumn(
                name: "ClientPassword",
                table: "Galleries");

            migrationBuilder.AlterColumn<string>(
                name: "LogoPath",
                table: "Galleries",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            // STEP 7: For Albums and Photos, we need to handle the FK migration carefully
            // Since SQLite doesn't support ALTER COLUMN well, we'll use a workaround
            // First, add the new ClientProfileId column
            migrationBuilder.AddColumn<int>(
                name: "ClientProfileId",
                table: "Albums",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientProfileId",
                table: "Photos",
                type: "INTEGER",
                nullable: true);

            // Copy data
            migrationBuilder.Sql(@"
                UPDATE Albums SET ClientProfileId = ClientId WHERE ClientId IS NOT NULL;
                UPDATE Photos SET ClientProfileId = ClientId WHERE ClientId IS NOT NULL;
            ");

            // STEP 8: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_PhotoShoots_ClientProfileId",
                table: "PhotoShoots",
                column: "ClientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoShoots_PhotographerProfileId",
                table: "PhotoShoots",
                column: "PhotographerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_ClientProfileId",
                table: "Invoices",
                column: "ClientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_GallerySession_UserId",
                table: "GallerySessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ClientProfileId",
                table: "Contracts",
                column: "ClientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientBadges_ClientProfileId",
                table: "ClientBadges",
                column: "ClientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Albums_ClientProfileId",
                table: "Albums",
                column: "ClientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_ClientProfileId",
                table: "Photos",
                column: "ClientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfile_UserId",
                table: "ClientProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAccess_Gallery_ClientProfile",
                table: "GalleryAccesses",
                columns: new[] { "GalleryId", "ClientProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAccesses_ClientProfileId",
                table: "GalleryAccesses",
                column: "ClientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotographerProfile_UserId",
                table: "PhotographerProfiles",
                column: "UserId",
                unique: true);

            // STEP 9: Add FK constraints for new ClientProfileId columns
            migrationBuilder.AddForeignKey(
                name: "FK_Albums_ClientProfiles_ClientProfileId",
                table: "Albums",
                column: "ClientProfileId",
                principalTable: "ClientProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_ClientProfiles_ClientProfileId",
                table: "Photos",
                column: "ClientProfileId",
                principalTable: "ClientProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientBadges_ClientProfiles_ClientProfileId",
                table: "ClientBadges",
                column: "ClientProfileId",
                principalTable: "ClientProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_ClientProfiles_ClientProfileId",
                table: "Contracts",
                column: "ClientProfileId",
                principalTable: "ClientProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GallerySessions_AspNetUsers_UserId",
                table: "GallerySessions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_ClientProfiles_ClientProfileId",
                table: "Invoices",
                column: "ClientProfileId",
                principalTable: "ClientProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoShoots_ClientProfiles_ClientProfileId",
                table: "PhotoShoots",
                column: "ClientProfileId",
                principalTable: "ClientProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoShoots_PhotographerProfiles_PhotographerProfileId",
                table: "PhotoShoots",
                column: "PhotographerProfileId",
                principalTable: "PhotographerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Note: We're keeping the old ClientId columns for now to preserve data
            // They can be removed in a future migration after verification
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove FK constraints
            migrationBuilder.DropForeignKey(name: "FK_PhotoShoots_PhotographerProfiles_PhotographerProfileId", table: "PhotoShoots");
            migrationBuilder.DropForeignKey(name: "FK_PhotoShoots_ClientProfiles_ClientProfileId", table: "PhotoShoots");
            migrationBuilder.DropForeignKey(name: "FK_Invoices_ClientProfiles_ClientProfileId", table: "Invoices");
            migrationBuilder.DropForeignKey(name: "FK_GallerySessions_AspNetUsers_UserId", table: "GallerySessions");
            migrationBuilder.DropForeignKey(name: "FK_Contracts_ClientProfiles_ClientProfileId", table: "Contracts");
            migrationBuilder.DropForeignKey(name: "FK_ClientBadges_ClientProfiles_ClientProfileId", table: "ClientBadges");
            migrationBuilder.DropForeignKey(name: "FK_Photos_ClientProfiles_ClientProfileId", table: "Photos");
            migrationBuilder.DropForeignKey(name: "FK_Albums_ClientProfiles_ClientProfileId", table: "Albums");

            // Drop indexes
            migrationBuilder.DropIndex(name: "IX_PhotographerProfile_UserId", table: "PhotographerProfiles");
            migrationBuilder.DropIndex(name: "IX_GalleryAccesses_ClientProfileId", table: "GalleryAccesses");
            migrationBuilder.DropIndex(name: "IX_GalleryAccess_Gallery_ClientProfile", table: "GalleryAccesses");
            migrationBuilder.DropIndex(name: "IX_ClientProfile_UserId", table: "ClientProfiles");
            migrationBuilder.DropIndex(name: "IX_Photos_ClientProfileId", table: "Photos");
            migrationBuilder.DropIndex(name: "IX_Albums_ClientProfileId", table: "Albums");
            migrationBuilder.DropIndex(name: "IX_ClientBadges_ClientProfileId", table: "ClientBadges");
            migrationBuilder.DropIndex(name: "IX_Contracts_ClientProfileId", table: "Contracts");
            migrationBuilder.DropIndex(name: "IX_GallerySession_UserId", table: "GallerySessions");
            migrationBuilder.DropIndex(name: "IX_Invoice_ClientProfileId", table: "Invoices");
            migrationBuilder.DropIndex(name: "IX_PhotoShoots_PhotographerProfileId", table: "PhotoShoots");
            migrationBuilder.DropIndex(name: "IX_PhotoShoots_ClientProfileId", table: "PhotoShoots");

            // Drop new columns
            migrationBuilder.DropColumn(name: "ClientProfileId", table: "Photos");
            migrationBuilder.DropColumn(name: "ClientProfileId", table: "Albums");
            migrationBuilder.DropColumn(name: "ClientProfileId", table: "ClientBadges");
            migrationBuilder.DropColumn(name: "ClientProfileId", table: "Contracts");
            migrationBuilder.DropColumn(name: "UserId", table: "GallerySessions");
            migrationBuilder.DropColumn(name: "ClientProfileId", table: "Invoices");
            migrationBuilder.DropColumn(name: "PhotographerProfileId", table: "PhotoShoots");
            migrationBuilder.DropColumn(name: "ClientProfileId", table: "PhotoShoots");

            // Drop new tables
            migrationBuilder.DropTable(name: "GalleryAccesses");
            migrationBuilder.DropTable(name: "PhotographerProfiles");
            migrationBuilder.DropTable(name: "ClientProfiles");

            // Restore Gallery columns
            migrationBuilder.AlterColumn<string>(
                name: "LogoPath",
                table: "Galleries",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientCode",
                table: "Galleries",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientPassword",
                table: "Galleries",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Gallery_ClientCode",
                table: "Galleries",
                column: "ClientCode",
                unique: true);
        }
    }
}
