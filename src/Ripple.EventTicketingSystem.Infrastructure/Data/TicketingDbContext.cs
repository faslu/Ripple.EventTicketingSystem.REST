using Microsoft.EntityFrameworkCore;
using Ripple.EventTicketingSystem.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ripple.EventTicketingSystem.Infrastructure.Data
{
    public class TicketingDbContext : DbContext
    {
        public TicketingDbContext(
            DbContextOptions<TicketingDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events => Set<Event>();

        public DbSet<PricingTier> PricingTiers => Set<PricingTier>();

        public DbSet<Ticket> Tickets => Set<Ticket>();
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureEvent(modelBuilder);
            ConfigurePricingTier(modelBuilder);
            ConfigureTicket(modelBuilder);
        }

        private static void ConfigureEvent(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Event>();

            entity.ToTable("Events");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.Venue)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(x => x.TotalCapacity)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasMany(x => x.PricingTiers)
                .WithOne(x => x.Event)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Tickets)
                .WithOne(x => x.Event)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.NoAction);
        }

        private static void ConfigurePricingTier(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<PricingTier>();

            entity.ToTable("PricingTiers");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Price)
                .HasPrecision(10, 2);

            entity.Property(x => x.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasIndex(x => x.EventId);
        }

        private static void ConfigureTicket(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Ticket>();

            entity.ToTable("Tickets");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.CustomerName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.CustomerEmail)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(x => x.UnitPrice)
                .HasPrecision(10, 2);

            entity.Property(x => x.TotalAmount)
                .HasPrecision(21, 2)
                .HasComputedColumnSql(
                    "[Quantity] * [UnitPrice]",
                    stored: true);

            entity.Property(x => x.PurchaseDate)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasIndex(x => x.EventId);

            entity.HasIndex(x => x.PricingTierId);

            entity.HasIndex(x => x.PurchaseDate);

            entity.HasOne(x => x.PricingTier)
                .WithMany(x => x.Tickets)
                .HasForeignKey(x => x.PricingTierId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
