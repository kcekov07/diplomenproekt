using EcoLoop.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcoLoop.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Store> Stores { get; set; } = null!;
        public DbSet<StoreImage> StoreImages { get; set; } = null!;
        public DbSet<StorePhone> StorePhones { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<News> News { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<EcoPoints> EcoPoints { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Optional: cascade delete configuration
            builder.Entity<StoreImage>()
                .HasOne(si => si.Store)
                .WithMany(s => s.Images)
                .HasForeignKey(si => si.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StorePhone>()
                .HasOne(sp => sp.Store)
                .WithMany(s => s.Phones)
                .HasForeignKey(sp => sp.StoreId)
                .OnDelete(DeleteBehavior.Cascade);  
        }
    }
}