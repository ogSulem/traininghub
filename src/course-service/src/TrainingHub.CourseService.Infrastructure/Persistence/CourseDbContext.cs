using Microsoft.EntityFrameworkCore;
using TrainingHub.CourseService.Domain.Entities;

namespace TrainingHub.CourseService.Infrastructure.Persistence;

public class CourseDbContext : DbContext
{
    public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Description).IsRequired().HasMaxLength(2000);

            builder.OwnsOne(c => c.Period, periodBuilder =>
            {
                periodBuilder.Property(p => p.Start).HasColumnName("StartsAt");
                periodBuilder.Property(p => p.End).HasColumnName("EndsAt");
            });

            builder.HasMany(c => c.Assignments)
                .WithOne()
                .HasForeignKey(a => a.CourseId);
        });

        modelBuilder.Entity<Assignment>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
            builder.Property(a => a.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void NormalizeDateTimesToUtc()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType == typeof(DateTime) && property.CurrentValue is DateTime dt)
                {
                    if (dt.Kind == DateTimeKind.Local)
                    {
                        property.CurrentValue = dt.ToUniversalTime();
                    }
                    else if (dt.Kind == DateTimeKind.Unspecified)
                    {
                        property.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    }
                }
                else if (property.Metadata.ClrType == typeof(DateTime?) && property.CurrentValue is DateTime nullableDt)
                {
                    if (nullableDt.Kind == DateTimeKind.Local)
                    {
                        property.CurrentValue = nullableDt.ToUniversalTime();
                    }
                    else if (nullableDt.Kind == DateTimeKind.Unspecified)
                    {
                        property.CurrentValue = DateTime.SpecifyKind(nullableDt, DateTimeKind.Utc);
                    }
                }
            }
        }
    }
}
