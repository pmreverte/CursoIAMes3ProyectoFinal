using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sprint2.Models;

namespace Sprint2.Data
{
/// <summary>
/// Represents the database context for the application, handling database operations and configurations.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TodoTask> Tasks { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

    /// <summary>
    /// Configures the model relationships and properties using the ModelBuilder.
    /// </summary>
    /// <param name="modelBuilder">The ModelBuilder used to configure the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the TodoTask entity
            modelBuilder.Entity<TodoTask>(entity =>
            {
                entity.ToTable("Tasks");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(500);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Changes).HasMaxLength(4000);
                entity.Property(e => e.UserId).HasMaxLength(450);
                entity.Property(e => e.UserName).HasMaxLength(256);
                entity.Property(e => e.Timestamp).IsRequired();
            });
        }
    }
}
