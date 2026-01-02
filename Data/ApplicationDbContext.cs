using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Data
{
    // TODO: [HIGH-DATA] ClientProfile CASCADE delete is too aggressive - deletes all related data
    // TODO: [HIGH-DATA] Invoice SetNull on client delete orphans invoice records
    // TODO: [HIGH-DATA] Contract SetNull on photoshoot delete orphans contract records
    // TODO: [HIGH] Add soft delete support (IsDeleted flag) for Client, Invoice, Contract entities
    // TODO: [HIGH] PhotoShoot has dual photographer FKs (PhotographerId string + PhotographerProfileId int) - consolidate
    // TODO: [MEDIUM] Add missing indexes: GalleryAccess.ExpiryDate, Photo.ClientProfileId
    // TODO: [MEDIUM] Add unique constraint on ClientProfile email
    // TODO: [MEDIUM] Add CreatedBy/UpdatedBy audit fields to key entities
    // TODO: [FEATURE] Add Payment model for tracking payment history
    // TODO: [FEATURE] Add EmailTemplate model for customizable notifications
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        #region DbSets
        // Original DbSets
        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<PhotoShoot> PhotoShoots => Set<PhotoShoot>();
        public DbSet<Album> Albums => Set<Album>();
        public DbSet<Photo> Photos => Set<Photo>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<FileItem> Files => Set<FileItem>();

        // Gallery & Proofing DbSets
        public DbSet<Gallery> Galleries { get; set; }
        public DbSet<GallerySession> GallerySessions { get; set; }
        public DbSet<Proof> Proofs { get; set; }
        public DbSet<PrintOrder> PrintOrders { get; set; }
        public DbSet<PrintItem> PrintItems { get; set; }
        public DbSet<PrintPricing> PrintPricings { get; set; }

        // Authorization DbSets
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Badge DbSets
        public DbSet<Badge> Badges { get; set; }
        public DbSet<ClientBadge> ClientBadges { get; set; }

        // Profile DbSets (unified user management)
        public DbSet<ClientProfile> ClientProfiles { get; set; }
        public DbSet<PhotographerProfile> PhotographerProfiles { get; set; }
        public DbSet<GalleryAccess> GalleryAccesses { get; set; }

        // Activity tracking
        public DbSet<Activity> Activities { get; set; }

        // Booking system
        public DbSet<BookingRequest> BookingRequests { get; set; }
        public DbSet<PhotographerAvailability> PhotographerAvailabilities { get; set; }

        // Service packages
        public DbSet<ServicePackage> ServicePackages { get; set; }
        public DbSet<PackageAddOn> PackageAddOns { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureIdentitySchema(modelBuilder);
            ConfigureProfileRelationships(modelBuilder);
            ConfigureInvoiceRelationships(modelBuilder);
            ConfigurePhotoRelationships(modelBuilder);
            ConfigureGalleryRelationships(modelBuilder);
            ConfigureGalleryAccessRelationships(modelBuilder);
            ConfigureContractRelationships(modelBuilder);
            ConfigureBadgeRelationships(modelBuilder);
            ConfigureDecimalConversions(modelBuilder);
            ConfigureIndexes(modelBuilder);
            ConfigureBookingRelationships(modelBuilder);
            ConfigurePackageRelationships(modelBuilder);
        }

        /// <summary>
        /// Configure Identity tables for SQLite (without schemas)
        /// </summary>
        private void ConfigureIdentitySchema(ModelBuilder modelBuilder)
        {
            // SQLite doesn't support schemas, so we just configure table names
            // Don't use HasDefaultSchema for SQLite compatibility


            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens");
        }

        /// <summary>
        /// Configure Profile relationships (ClientProfile, PhotographerProfile 1:1 with ApplicationUser)
        /// </summary>
        private void ConfigureProfileRelationships(ModelBuilder modelBuilder)
        {
            // ClientProfile 1:1 with ApplicationUser
            modelBuilder.Entity<ClientProfile>()
                .HasOne(cp => cp.User)
                .WithOne(u => u.ClientProfile)
                .HasForeignKey<ClientProfile>(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientProfile>()
                .HasIndex(cp => cp.UserId)
                .IsUnique()
                .HasDatabaseName("IX_ClientProfile_UserId");

            // PhotographerProfile 1:1 with ApplicationUser
            modelBuilder.Entity<PhotographerProfile>()
                .HasOne(pp => pp.User)
                .WithOne(u => u.PhotographerProfile)
                .HasForeignKey<PhotographerProfile>(pp => pp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PhotographerProfile>()
                .HasIndex(pp => pp.UserId)
                .IsUnique()
                .HasDatabaseName("IX_PhotographerProfile_UserId");

            // PhotoShoot -> ClientProfile relationship
            modelBuilder.Entity<PhotoShoot>()
                .HasOne(ps => ps.ClientProfile)
                .WithMany(cp => cp.PhotoShoots)
                .HasForeignKey(ps => ps.ClientProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // PhotoShoot -> PhotographerProfile relationship (optional)
            modelBuilder.Entity<PhotoShoot>()
                .HasOne(ps => ps.PhotographerProfile)
                .WithMany(pp => pp.AssignedPhotoShoots)
                .HasForeignKey(ps => ps.PhotographerProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            // Album -> ClientProfile relationship
            modelBuilder.Entity<Album>()
                .HasOne(a => a.ClientProfile)
                .WithMany()
                .HasForeignKey(a => a.ClientProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <summary>
        /// Configure GalleryAccess relationships
        /// </summary>
        private void ConfigureGalleryAccessRelationships(ModelBuilder modelBuilder)
        {
            // GalleryAccess -> Gallery
            modelBuilder.Entity<GalleryAccess>()
                .HasOne(ga => ga.Gallery)
                .WithMany(g => g.Accesses)
                .HasForeignKey(ga => ga.GalleryId)
                .OnDelete(DeleteBehavior.Cascade);

            // GalleryAccess -> ClientProfile
            modelBuilder.Entity<GalleryAccess>()
                .HasOne(ga => ga.ClientProfile)
                .WithMany(cp => cp.GalleryAccesses)
                .HasForeignKey(ga => ga.ClientProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index on Gallery + ClientProfile combination
            modelBuilder.Entity<GalleryAccess>()
                .HasIndex(ga => new { ga.GalleryId, ga.ClientProfileId })
                .IsUnique()
                .HasDatabaseName("IX_GalleryAccess_Gallery_ClientProfile");

            // GallerySession -> User relationship
            modelBuilder.Entity<GallerySession>()
                .HasOne(gs => gs.User)
                .WithMany()
                .HasForeignKey(gs => gs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GallerySession>()
                .HasIndex(gs => gs.UserId)
                .HasDatabaseName("IX_GallerySession_UserId");
        }

        /// <summary>
        /// Configure Invoice-related relationships
        /// </summary>
        private void ConfigureInvoiceRelationships(ModelBuilder modelBuilder)
        {
            // Invoice <-> InvoiceItems (1:N)
            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.InvoiceItems)
                .WithOne(ii => ii.Invoice)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Invoice <-> ClientProfile (N:1)
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.ClientProfile)
                .WithMany(cp => cp.Invoices)
                .HasForeignKey(i => i.ClientProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            // Invoice <-> PhotoShoot (N:1)
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.PhotoShoot)
                .WithMany(ps => ps.Invoices)
                .HasForeignKey(i => i.PhotoShootId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        /// <summary>
        /// Configure Photo-related relationships
        /// </summary>
        private void ConfigurePhotoRelationships(ModelBuilder modelBuilder)
        {
            // Album <-> PhotoShoot (N:1)
            modelBuilder.Entity<Album>()
                .HasOne(a => a.PhotoShoot)
                .WithMany(ps => ps.Albums)
                .HasForeignKey(a => a.PhotoShootId)
                .OnDelete(DeleteBehavior.Cascade);

            // Photo <-> Album (N:1)
            modelBuilder.Entity<Photo>()
                .HasOne(p => p.Album)
                .WithMany(a => a.Photos)
                .HasForeignKey(p => p.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Photo properties
            modelBuilder.Entity<Photo>()
                .Property(p => p.FullImagePath)
                .IsRequired(false);

            modelBuilder.Entity<Photo>()
                .Property(p => p.ThumbnailPath)
                .IsRequired(false);

            modelBuilder.Entity<Photo>()
                .Property(p => p.FilePath)
                .IsRequired(false);
        }

        /// <summary>
        /// Configure Contract relationships
        /// </summary>
        private void ConfigureContractRelationships(ModelBuilder modelBuilder)
        {
            // Contract <-> ClientProfile (N:1)
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.ClientProfile)
                .WithMany(cp => cp.Contracts)
                .HasForeignKey(c => c.ClientProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            // Contract <-> PhotoShoot (N:1)
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.PhotoShoot)
                .WithMany(ps => ps.Contracts)
                .HasForeignKey(c => c.PhotoShootId)
                .OnDelete(DeleteBehavior.SetNull);

            // Contract <-> Badge (N:1)
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.BadgeToAward)
                .WithMany()
                .HasForeignKey(c => c.BadgeToAwardId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        /// <summary>
        /// Configure Badge and ClientBadge relationships
        /// </summary>
        private void ConfigureBadgeRelationships(ModelBuilder modelBuilder)
        {
            // ClientBadge <-> ClientProfile (N:1)
            modelBuilder.Entity<ClientBadge>()
                .HasOne(cb => cb.ClientProfile)
                .WithMany(cp => cp.ClientBadges)
                .HasForeignKey(cb => cb.ClientProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // ClientBadge <-> Badge (N:1)
            modelBuilder.Entity<ClientBadge>()
                .HasOne(cb => cb.Badge)
                .WithMany(b => b.ClientBadges)
                .HasForeignKey(cb => cb.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);

            // ClientBadge <-> Contract (N:1)
            modelBuilder.Entity<ClientBadge>()
                .HasOne(cb => cb.Contract)
                .WithMany(c => c.ClientBadges)
                .HasForeignKey(cb => cb.ContractId)
                .OnDelete(DeleteBehavior.SetNull);

            // Badge <-> Contract (auto-award configuration)
            modelBuilder.Entity<Badge>()
                .HasIndex(b => b.RequiredContractId);
        }

        /// <summary>
        /// Configure Gallery, Proofing, and Print Order relationships
        /// </summary>
        private void ConfigureGalleryRelationships(ModelBuilder modelBuilder)
        {
            // Gallery <-> GallerySession (1:N)
            modelBuilder.Entity<Gallery>()
                .HasMany(g => g.Sessions)
                .WithOne(gs => gs.Gallery)
                .HasForeignKey(gs => gs.GalleryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Gallery <-> Album (M:N)
            modelBuilder.Entity<Gallery>()
                .HasMany(g => g.Albums)
                .WithMany(a => a.Galleries)
                .UsingEntity<Dictionary<string, object>>(
                    "GalleryAlbum",
                    j => j.HasOne<Album>().WithMany().HasForeignKey("AlbumId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Gallery>().WithMany().HasForeignKey("GalleryId").OnDelete(DeleteBehavior.Cascade));

            // GallerySession <-> Proof (1:N)
            modelBuilder.Entity<GallerySession>()
                .HasMany(gs => gs.Proofs)
                .WithOne(p => p.Session)
                .HasForeignKey(p => p.GallerySessionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Photo <-> Proof (1:N)
            modelBuilder.Entity<Photo>()
                .HasMany(p => p.Proofs)
                .WithOne(pr => pr.Photo)
                .HasForeignKey(pr => pr.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);

            // GallerySession <-> PrintOrder (1:N) - Configure if PrintOrders navigation exists
            // modelBuilder.Entity<GallerySession>()
            //     .HasMany(gs => gs.PrintOrders)
            //     .WithOne(po => po.Session)
            //     .HasForeignKey(po => po.GallerySessionId)
            //     .OnDelete(DeleteBehavior.Restrict);

            // PrintOrder <-> PrintItem (1:N)
            modelBuilder.Entity<PrintOrder>()
                .HasMany(po => po.Items)
                .WithOne(pi => pi.PrintOrder)
                .HasForeignKey(pi => pi.PrintOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Photo <-> PrintItem (N:1)
            modelBuilder.Entity<PrintItem>()
                .HasOne(pi => pi.Photo)
                .WithMany()
                .HasForeignKey(pi => pi.PhotoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure value objects and defaults
            modelBuilder.Entity<Gallery>()
                .Property(g => g.BrandColor)
                .HasDefaultValue("#2c3e50");

            modelBuilder.Entity<Proof>()
                .Property(p => p.SelectedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<PrintOrder>()
                .Property(po => po.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<GallerySession>()
                .Property(gs => gs.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }

        /// <summary>
        /// Configure decimal to double conversion for SQLite compatibility
        /// </summary>
        private void ConfigureDecimalConversions(ModelBuilder modelBuilder)
        {
            // Invoice properties
            modelBuilder.Entity<Invoice>()
                .Property(i => i.Amount)
                .HasConversion<double>();

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Tax)
                .HasConversion<double>();

            // InvoiceItem properties
            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.UnitPrice)
                .HasConversion<double>();

            // PrintPricing properties
            modelBuilder.Entity<PrintPricing>()
                .Property(pp => pp.Price)
                .HasConversion<double>();

            // PrintItem properties
            modelBuilder.Entity<PrintItem>()
                .Property(pi => pi.UnitPrice)
                .HasConversion<double>();

            // PrintOrder properties
            modelBuilder.Entity<PrintOrder>()
                .Property(po => po.TotalPrice)
                .HasConversion<double>();
        }

        /// <summary>
        /// Configure indexes for query performance
        /// </summary>
        private void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            // Gallery indexes
            modelBuilder.Entity<Gallery>()
                .HasIndex(g => g.IsActive)
                .HasDatabaseName("IX_Gallery_IsActive");

            // GallerySession indexes
            modelBuilder.Entity<GallerySession>()
                .HasIndex(gs => gs.SessionToken)
                .IsUnique()
                .HasDatabaseName("IX_GallerySession_SessionToken");

            modelBuilder.Entity<GallerySession>()
                .HasIndex(gs => gs.GalleryId)
                .HasDatabaseName("IX_GallerySession_GalleryId");

            modelBuilder.Entity<GallerySession>()
                .HasIndex(gs => gs.CreatedDate)
                .HasDatabaseName("IX_GallerySession_CreatedDate");

            // Proof indexes
            modelBuilder.Entity<Proof>()
                .HasIndex(new[] { nameof(Proof.PhotoId), nameof(Proof.GallerySessionId) })
                .HasDatabaseName("IX_Proof_PhotoId_SessionId");

            modelBuilder.Entity<Proof>()
                .HasIndex(p => p.IsFavorite)
                .HasDatabaseName("IX_Proof_IsFavorite");

            modelBuilder.Entity<Proof>()
                .HasIndex(p => p.IsMarkedForEditing)
                .HasDatabaseName("IX_Proof_IsMarkedForEditing");

            // Photo indexes
            modelBuilder.Entity<Photo>()
                .HasIndex(p => p.DisplayOrder)
                .HasDatabaseName("IX_Photo_DisplayOrder");

            // PrintOrder indexes
            modelBuilder.Entity<PrintOrder>()
                .HasIndex(po => po.OrderNumber)
                .IsUnique()
                .HasDatabaseName("IX_PrintOrder_OrderNumber");

            modelBuilder.Entity<PrintOrder>()
                .HasIndex(po => po.Status)
                .HasDatabaseName("IX_PrintOrder_Status");

            modelBuilder.Entity<PrintOrder>()
                .HasIndex(po => po.CreatedDate)
                .HasDatabaseName("IX_PrintOrder_CreatedDate");

            // PrintPricing indexes
            modelBuilder.Entity<PrintPricing>()
                .HasIndex(new[] { nameof(PrintPricing.Size), nameof(PrintPricing.FinishType) })
                .IsUnique()
                .HasDatabaseName("IX_PrintPricing_Size_FinishType");

            // Invoice indexes
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.ClientProfileId)
                .HasDatabaseName("IX_Invoice_ClientProfileId");

            modelBuilder.Entity<RolePermission>()
                .ToTable("RolePermissions")
                .HasIndex(rp => new { rp.RoleId, rp.Permission })
                .IsUnique()
                .HasDatabaseName("IX_RolePermissions_RoleId_Permission");

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.PhotoShootId)
                .HasDatabaseName("IX_Invoice_PhotoShootId");

            // Album indexes
            modelBuilder.Entity<Album>()
                .HasIndex(a => a.PhotoShootId)
                .HasDatabaseName("IX_Album_PhotoShootId");

            // Client indexes
            modelBuilder.Entity<Client>()
                .HasIndex(c => c.Email)
                .IsUnique()
                .HasDatabaseName("IX_Client_Email");

            // Activity indexes
            modelBuilder.Entity<Activity>()
                .HasIndex(a => a.CreatedAt)
                .HasDatabaseName("IX_Activity_CreatedAt");

            modelBuilder.Entity<Activity>()
                .HasIndex(a => a.EntityType)
                .HasDatabaseName("IX_Activity_EntityType");

            modelBuilder.Entity<Activity>()
                .HasIndex(a => a.UserId)
                .HasDatabaseName("IX_Activity_UserId");
        }

        /// <summary>
        /// Configure Booking system relationships
        /// </summary>
        private void ConfigureBookingRelationships(ModelBuilder modelBuilder)
        {
            // BookingRequest -> ClientProfile (N:1)
            modelBuilder.Entity<BookingRequest>()
                .HasOne(br => br.ClientProfile)
                .WithMany()
                .HasForeignKey(br => br.ClientProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // BookingRequest -> PhotographerProfile (N:1, optional)
            modelBuilder.Entity<BookingRequest>()
                .HasOne(br => br.PhotographerProfile)
                .WithMany()
                .HasForeignKey(br => br.PhotographerProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            // BookingRequest -> ServicePackage (N:1, optional)
            modelBuilder.Entity<BookingRequest>()
                .HasOne(br => br.ServicePackage)
                .WithMany(sp => sp.BookingRequests)
                .HasForeignKey(br => br.ServicePackageId)
                .OnDelete(DeleteBehavior.SetNull);

            // BookingRequest -> PhotoShoot (1:1, optional - when converted)
            modelBuilder.Entity<BookingRequest>()
                .HasOne(br => br.PhotoShoot)
                .WithMany()
                .HasForeignKey(br => br.PhotoShootId)
                .OnDelete(DeleteBehavior.SetNull);

            // PhotographerAvailability -> PhotographerProfile (N:1)
            modelBuilder.Entity<PhotographerAvailability>()
                .HasOne(pa => pa.PhotographerProfile)
                .WithMany()
                .HasForeignKey(pa => pa.PhotographerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // PhotographerAvailability -> BookingRequest (N:1, optional)
            modelBuilder.Entity<PhotographerAvailability>()
                .HasOne(pa => pa.BookingRequest)
                .WithMany(br => br.AvailabilitySlots)
                .HasForeignKey(pa => pa.BookingRequestId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            modelBuilder.Entity<BookingRequest>()
                .HasIndex(br => br.BookingReference)
                .IsUnique()
                .HasDatabaseName("IX_BookingRequest_Reference");

            modelBuilder.Entity<BookingRequest>()
                .HasIndex(br => br.Status)
                .HasDatabaseName("IX_BookingRequest_Status");

            modelBuilder.Entity<BookingRequest>()
                .HasIndex(br => br.PreferredDate)
                .HasDatabaseName("IX_BookingRequest_PreferredDate");

            modelBuilder.Entity<PhotographerAvailability>()
                .HasIndex(pa => new { pa.PhotographerProfileId, pa.StartTime })
                .HasDatabaseName("IX_PhotographerAvailability_Photographer_StartTime");

            // Decimal conversions for SQLite
            modelBuilder.Entity<BookingRequest>()
                .Property(br => br.EstimatedPrice)
                .HasConversion<double>();

            modelBuilder.Entity<BookingRequest>()
                .Property(br => br.EstimatedDurationHours)
                .HasConversion<double>();
        }

        /// <summary>
        /// Configure Service Package relationships
        /// </summary>
        private void ConfigurePackageRelationships(ModelBuilder modelBuilder)
        {
            // PackageAddOn -> ServicePackage (N:1, optional for standalone add-ons)
            modelBuilder.Entity<PackageAddOn>()
                .HasOne(pa => pa.ServicePackage)
                .WithMany(sp => sp.AddOns)
                .HasForeignKey(pa => pa.ServicePackageId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            modelBuilder.Entity<ServicePackage>()
                .HasIndex(sp => sp.Category)
                .HasDatabaseName("IX_ServicePackage_Category");

            modelBuilder.Entity<ServicePackage>()
                .HasIndex(sp => sp.IsActive)
                .HasDatabaseName("IX_ServicePackage_IsActive");

            modelBuilder.Entity<ServicePackage>()
                .HasIndex(sp => sp.DisplayOrder)
                .HasDatabaseName("IX_ServicePackage_DisplayOrder");

            modelBuilder.Entity<PackageAddOn>()
                .HasIndex(pa => pa.IsStandalone)
                .HasDatabaseName("IX_PackageAddOn_IsStandalone");

            // Decimal conversions for SQLite
            modelBuilder.Entity<ServicePackage>()
                .Property(sp => sp.BasePrice)
                .HasConversion<double>();

            modelBuilder.Entity<ServicePackage>()
                .Property(sp => sp.DiscountedPrice)
                .HasConversion<double>();

            modelBuilder.Entity<ServicePackage>()
                .Property(sp => sp.DurationHours)
                .HasConversion<double>();

            modelBuilder.Entity<PackageAddOn>()
                .Property(pa => pa.Price)
                .HasConversion<double>();
        }
    }
}