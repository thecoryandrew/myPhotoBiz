
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

        public DbSet<Client> Clients => Set<Client>();
        public DbSet<PhotoShoot> PhotoShoots => Set<PhotoShoot>();
        public DbSet<Album> Albums => Set<Album>();
        public DbSet<Photo> Photos => Set<Photo>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Invoice <-> InvoiceItems
            modelBuilder.Entity<Invoice>()
           .HasMany(i => i.InvoiceItems)
           .WithOne(ii => ii.Invoice)
           .HasForeignKey(ii => ii.InvoiceId)
           .OnDelete(DeleteBehavior.Cascade);

            // Invoice <-> Client
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Client)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            // Invoice <-> PhotoShoot
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.PhotoShoot)
                .WithMany(ps => ps.Invoices)
                .HasForeignKey(i => i.PhotoShootId)
                .OnDelete(DeleteBehavior.SetNull);

            // Album <-> PhotoShoot
            modelBuilder.Entity<Album>()
                .HasOne(a => a.PhotoShoot)
                .WithMany(ps => ps.Albums)
                .HasForeignKey(a => a.PhotoShootId)
                .OnDelete(DeleteBehavior.Cascade);

            // Photo <-> Album
            modelBuilder.Entity<Photo>()
                .HasOne(p => p.Album)
                .WithMany(a => a.Photos)
                .HasForeignKey(p => p.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Convert decimal to double for SQLite compatibility
            modelBuilder.Entity<Invoice>()
                .Property(i => i.Amount)
                .HasConversion<double>();

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Tax)
                .HasConversion<double>();

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.UnitPrice)
                .HasConversion<double>();
        }
    }
}

