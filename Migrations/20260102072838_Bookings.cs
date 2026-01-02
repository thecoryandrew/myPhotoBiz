using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myPhotoBiz.Migrations
{
    /// <inheritdoc />
    public partial class Bookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: true),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DetailedDescription = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BasePrice = table.Column<double>(type: "decimal(18,2)", nullable: false),
                    DiscountedPrice = table.Column<double>(type: "decimal(18,2)", nullable: true),
                    DurationHours = table.Column<double>(type: "REAL", nullable: false),
                    IncludedPhotos = table.Column<int>(type: "INTEGER", nullable: false),
                    IncludesPrints = table.Column<bool>(type: "INTEGER", nullable: false),
                    NumberOfPrints = table.Column<int>(type: "INTEGER", nullable: true),
                    IncludesAlbum = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludesDigitalGallery = table.Column<bool>(type: "INTEGER", nullable: false),
                    NumberOfLocations = table.Column<int>(type: "INTEGER", nullable: false),
                    OutfitChanges = table.Column<int>(type: "INTEGER", nullable: true),
                    IncludedFeatures = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    CoverImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookingReference = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ClientProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    PhotographerProfileId = table.Column<int>(type: "INTEGER", nullable: true),
                    ServicePackageId = table.Column<int>(type: "INTEGER", nullable: true),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PreferredDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AlternativeDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PreferredStartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    EstimatedDurationHours = table.Column<double>(type: "REAL", nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SpecialRequirements = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ContactName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EstimatedPrice = table.Column<double>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DeclineReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConfirmedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeclinedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PhotoShootId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingRequests_ClientProfiles_ClientProfileId",
                        column: x => x.ClientProfileId,
                        principalTable: "ClientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingRequests_PhotoShoots_PhotoShootId",
                        column: x => x.PhotoShootId,
                        principalTable: "PhotoShoots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BookingRequests_PhotographerProfiles_PhotographerProfileId",
                        column: x => x.PhotographerProfileId,
                        principalTable: "PhotographerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BookingRequests_ServicePackages_ServicePackageId",
                        column: x => x.ServicePackageId,
                        principalTable: "ServicePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PackageAddOns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServicePackageId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Price = table.Column<double>(type: "decimal(18,2)", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsStandalone = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageAddOns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageAddOns_ServicePackages_ServicePackageId",
                        column: x => x.ServicePackageId,
                        principalTable: "ServicePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotographerAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhotographerProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRecurring = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecurringDayOfWeek = table.Column<int>(type: "INTEGER", nullable: true),
                    IsBooked = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBlocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BookingRequestId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotographerAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotographerAvailabilities_BookingRequests_BookingRequestId",
                        column: x => x.BookingRequestId,
                        principalTable: "BookingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PhotographerAvailabilities_PhotographerProfiles_PhotographerProfileId",
                        column: x => x.PhotographerProfileId,
                        principalTable: "PhotographerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_CreatedAt",
                table: "Activities",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_EntityType",
                table: "Activities",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_UserId",
                table: "Activities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequest_PreferredDate",
                table: "BookingRequests",
                column: "PreferredDate");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequest_Reference",
                table: "BookingRequests",
                column: "BookingReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequest_Status",
                table: "BookingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_ClientProfileId",
                table: "BookingRequests",
                column: "ClientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_PhotographerProfileId",
                table: "BookingRequests",
                column: "PhotographerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_PhotoShootId",
                table: "BookingRequests",
                column: "PhotoShootId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_ServicePackageId",
                table: "BookingRequests",
                column: "ServicePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageAddOn_IsStandalone",
                table: "PackageAddOns",
                column: "IsStandalone");

            migrationBuilder.CreateIndex(
                name: "IX_PackageAddOns_ServicePackageId",
                table: "PackageAddOns",
                column: "ServicePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotographerAvailabilities_BookingRequestId",
                table: "PhotographerAvailabilities",
                column: "BookingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotographerAvailability_Photographer_StartTime",
                table: "PhotographerAvailabilities",
                columns: new[] { "PhotographerProfileId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackage_Category",
                table: "ServicePackages",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackage_DisplayOrder",
                table: "ServicePackages",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackage_IsActive",
                table: "ServicePackages",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "PackageAddOns");

            migrationBuilder.DropTable(
                name: "PhotographerAvailabilities");

            migrationBuilder.DropTable(
                name: "BookingRequests");

            migrationBuilder.DropTable(
                name: "ServicePackages");
        }
    }
}
