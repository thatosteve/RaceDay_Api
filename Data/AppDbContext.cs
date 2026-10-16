using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

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
            // ---------------- ROLES ----------------
            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("ROLES");
                e.HasKey(r => r.RoleId).HasName("PK_ROLES");
                e.Property(r => r.RoleId).HasColumnName("role_id").ValueGeneratedOnAdd();
                e.Property(r => r.RoleName)
                    .HasColumnName("role_name")
                    .HasColumnType("varchar(20)")
                    .IsRequired();

                e.HasIndex(r => r.RoleName)
                    .IsUnique()
                    .HasDatabaseName("UQ_ROLES_role_name");

                e.HasData(
                    new Role { RoleId = 1, RoleName = "Organiser" },
                    new Role { RoleId = 2, RoleName = "Participant" });
            });

            // ---------------- USERS ----------------
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("USERS");
                e.HasKey(u => u.UserId).HasName("PK_USERS");

                e.Property(u => u.UserId).HasColumnName("user_id").ValueGeneratedOnAdd();
                e.Property(u => u.RoleId).HasColumnName("role_id");
                e.Property(u => u.FirstName).HasColumnName("first_name").HasColumnType("varchar(50)").IsRequired();
                e.Property(u => u.LastName).HasColumnName("last_name").HasColumnType("varchar(50)").IsRequired();
                e.Property(u => u.Email).HasColumnName("email").HasColumnType("varchar(150)").IsRequired();
                e.Property(u => u.PasswordHash).HasColumnName("password_hash").HasColumnType("varchar(255)").IsRequired();
                e.Property(u => u.PhoneNumber).HasColumnName("phone_number").HasColumnType("varchar(20)");
                e.Property(u => u.DateOfBirth).HasColumnName("date_of_birth").HasColumnType("date");
                e.Property(u => u.ProfileImageUrl).HasColumnName("profile_image_url").HasColumnType("varchar(500)");
                e.Property(u => u.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                e.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_USERS_email");

                e.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .HasConstraintName("FK_USERS_ROLES")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- EVENTS ----------------
            modelBuilder.Entity<Event>(e =>
            {
                e.ToTable("EVENTS", t =>
                    t.HasCheckConstraint("CK_EVENTS_TYPE", "event_type IN ('run', 'walk', 'cycle')"));

                e.HasKey(ev => ev.EventId).HasName("PK_EVENTS");

                e.Property(ev => ev.EventId).HasColumnName("event_id").ValueGeneratedOnAdd();
                e.Property(ev => ev.OrganiserId).HasColumnName("organiser_id");
                e.Property(ev => ev.Name).HasColumnName("name").HasColumnType("varchar(150)").IsRequired();
                e.Property(ev => ev.Description).HasColumnName("description").HasColumnType("varchar(1000)");
                e.Property(ev => ev.EventDate).HasColumnName("event_date").HasColumnType("datetime").IsRequired();
                e.Property(ev => ev.Location).HasColumnName("location").HasColumnType("varchar(200)").IsRequired();
                e.Property(ev => ev.DistanceKm).HasColumnName("distance_km").HasColumnType("decimal(6,2)");
                e.Property(ev => ev.EventType).HasColumnName("event_type").HasColumnType("varchar(10)").IsRequired();
                e.Property(ev => ev.BannerImageUrl).HasColumnName("banner_image_url").HasColumnType("varchar(500)");
                e.Property(ev => ev.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                e.HasOne(ev => ev.Organiser)
                    .WithMany(u => u.OrganisedEvents)
                    .HasForeignKey(ev => ev.OrganiserId)
                    .HasConstraintName("FK_EVENTS_USERS")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- CATEGORIES ----------------
            modelBuilder.Entity<Category>(e =>
            {
                e.ToTable("CATEGORIES", t =>
                    t.HasCheckConstraint("CK_CATEGORIES_TYPE", "category_type IN ('Age', 'Distance')"));

                e.HasKey(c => c.CategoryId).HasName("PK_CATEGORIES");

                e.Property(c => c.CategoryId).HasColumnName("category_id").ValueGeneratedOnAdd();
                e.Property(c => c.EventId).HasColumnName("event_id");
                e.Property(c => c.Name).HasColumnName("name").HasColumnType("varchar(100)").IsRequired();
                e.Property(c => c.CategoryType).HasColumnName("category_type").HasColumnType("varchar(20)").IsRequired();
                e.Property(c => c.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                e.HasOne(c => c.Event)
                    .WithMany(ev => ev.Categories)
                    .HasForeignKey(c => c.EventId)
                    .HasConstraintName("FK_CATEGORIES_EVENTS")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- ENROLMENTS ----------------
            modelBuilder.Entity<Enrolment>(e =>
            {
                e.ToTable("ENROLMENTS", t =>
                    t.HasCheckConstraint("CK_ENROLMENTS_STATUS", "status IN ('Pending', 'Confirmed')"));

                e.HasKey(en => en.EnrolmentId).HasName("PK_ENROLMENTS");

                e.Property(en => en.EnrolmentId).HasColumnName("enrolment_id").ValueGeneratedOnAdd();
                e.Property(en => en.UserId).HasColumnName("user_id");
                e.Property(en => en.EventId).HasColumnName("event_id");
                e.Property(en => en.CategoryId).HasColumnName("category_id");
                e.Property(en => en.Status)
                    .HasColumnName("status")
                    .HasColumnType("varchar(20)")
                    .HasDefaultValue("Pending")
                    .IsRequired();
                e.Property(en => en.EnrolledAt)
                    .HasColumnName("enrolled_at")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                // UQ_ENROLMENTS (user_id, event_id)
                e.HasIndex(en => new { en.UserId, en.EventId })
                    .IsUnique()
                    .HasDatabaseName("UQ_ENROLMENTS");

                e.HasOne(en => en.User)
                    .WithMany(u => u.Enrolments)
                    .HasForeignKey(en => en.UserId)
                    .HasConstraintName("FK_ENROLMENTS_USERS")
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(en => en.Event)
                    .WithMany(ev => ev.Enrolments)
                    .HasForeignKey(en => en.EventId)
                    .HasConstraintName("FK_ENROLMENTS_EVENTS")
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(en => en.Category)
                    .WithMany(c => c.Enrolments)
                    .HasForeignKey(en => en.CategoryId)
                    .HasConstraintName("FK_ENROLMENTS_CATEGORIES")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- RESULTS ----------------
            modelBuilder.Entity<Result>(e =>
            {
                e.ToTable("RESULTS");
                e.HasKey(r => r.ResultId).HasName("PK_RESULTS");

                e.Property(r => r.ResultId).HasColumnName("result_id").ValueGeneratedOnAdd();
                e.Property(r => r.EnrolmentId).HasColumnName("enrolment_id");
                e.Property(r => r.FinishTime).HasColumnName("finish_time").HasColumnType("time");
                e.Property(r => r.FinishPosition).HasColumnName("finish_position");
                e.Property(r => r.RecordedAt)
                    .HasColumnName("recorded_at")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()");

                e.HasIndex(r => r.EnrolmentId)
                    .IsUnique()
                    .HasDatabaseName("UQ_RESULTS_enrolment_id");

                e.HasOne(r => r.Enrolment)
                    .WithOne(en => en.Result)
                    .HasForeignKey<Result>(r => r.EnrolmentId)
                    .HasConstraintName("FK_RESULTS_ENROLMENTS")
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}