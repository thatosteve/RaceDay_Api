using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RaceDay.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ROLES
            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("Roles");
                e.HasKey(r => r.RoleId);
                e.Property(r => r.RoleName).HasMaxLength(20).IsRequired();
                e.HasIndex(r => r.RoleName).IsUnique();

                e.HasData(
                    new Role { RoleId = 1, RoleName = "Organiser" },
                    new Role { RoleId = 2, RoleName = "Participant" }
                );
            });

            // USERS
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasKey(u => u.UserId);
                e.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
                e.Property(u => u.LastName).HasMaxLength(50).IsRequired();
                e.Property(u => u.Email).HasMaxLength(150).IsRequired();
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
                e.Property(u => u.PhoneNumber).HasMaxLength(20);
                e.Property(u => u.ProfileImageUrl).HasMaxLength(500);
                e.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(u => u.Role)
                 .WithMany(r => r.Users)
                 .HasForeignKey(u => u.RoleId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // EVENTS
            modelBuilder.Entity<Event>(e =>
            {
                e.ToTable("Events");
                e.HasKey(ev => ev.EventId);
                e.Property(ev => ev.Name).HasMaxLength(150).IsRequired();
                e.Property(ev => ev.Description).HasMaxLength(1000);
                e.Property(ev => ev.Location).HasMaxLength(200).IsRequired();
                e.Property(ev => ev.DistanceKm).HasColumnType("decimal(6,2)");
                e.Property(ev => ev.EventType).HasMaxLength(10).IsRequired();
                e.Property(ev => ev.BannerImageUrl).HasMaxLength(500);
                e.Property(ev => ev.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(ev => ev.Organiser)
                 .WithMany(u => u.OrganisedEvents)
                 .HasForeignKey(ev => ev.OrganiserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // CATEGORIES
            modelBuilder.Entity<Category>(e =>
            {
                e.ToTable("Categories");
                e.HasKey(c => c.CategoryId);
                e.Property(c => c.Name).HasMaxLength(100).IsRequired();
                e.Property(c => c.CategoryType).HasMaxLength(20).IsRequired();
                e.Property(c => c.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(c => c.Event)
                 .WithMany(ev => ev.Categories)
                 .HasForeignKey(c => c.EventId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ENROLMENTS
            modelBuilder.Entity<Enrolment>(e =>
            {
                e.ToTable("Enrolments");
                e.HasKey(en => en.EnrolmentId);
                e.Property(en => en.Status).HasMaxLength(20).HasDefaultValue("Pending");
                e.Property(en => en.EnrolledAt).HasDefaultValueSql("GETDATE()");

                e.HasIndex(en => new { en.UserId, en.EventId }).IsUnique();

                e.HasOne(en => en.User)
                 .WithMany(u => u.Enrolments)
                 .HasForeignKey(en => en.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(en => en.Event)
                 .WithMany(ev => ev.Enrolments)
                 .HasForeignKey(en => en.EventId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(en => en.Category)
                 .WithMany(c => c.Enrolments)
                 .HasForeignKey(en => en.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // RESULTS  (one-to-one with Enrolments)
            modelBuilder.Entity<Result>(e =>
            {
                e.ToTable("Results");
                e.HasKey(r => r.ResultId);
                e.Property(r => r.FinishTime).HasColumnType("time");
                e.Property(r => r.RecordedAt).HasDefaultValueSql("GETDATE()");

                e.HasIndex(r => r.EnrolmentId).IsUnique();

                e.HasOne(r => r.Enrolment)
                 .WithOne(en => en.Result)
                 .HasForeignKey<Result>(r => r.EnrolmentId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
