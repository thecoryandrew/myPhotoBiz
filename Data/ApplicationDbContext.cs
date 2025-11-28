using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Data
{
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
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureIdentitySchema(modelBuilder);
            ConfigureInvoiceRelationships(modelBuilder);
            ConfigurePhotoRelationships(modelBuilder);
            ConfigureGalleryRelationships(modelBuilder);
            ConfigureDecimalConversions(modelBuilder);
            ConfigureIndexes(modelBuilder);


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

            // Invoice <-> Client (N:1)
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Client)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.ClientId)
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

            // Photo <-> Gallery (N:1)
            modelBuilder.Entity<Photo>()
                .HasOne(p => p.Gallery)
                .WithMany(g => g.Photos)
                .HasForeignKey(p => p.GalleryId)
                .OnDelete(DeleteBehavior.SetNull);

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
                .HasIndex(g => g.ClientCode)
                .IsUnique()
                .HasDatabaseName("IX_Gallery_ClientCode");

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
                .HasIndex(p => p.GalleryId)
                .HasDatabaseName("IX_Photo_GalleryId");

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
                .HasIndex(i => i.ClientId)
                .HasDatabaseName("IX_Invoice_ClientId");

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
        }
    }
}