using Microsoft.EntityFrameworkCore;

namespace SafeParallelism.Concurrency;

public class IssueDbContext : DbContext
{
    public IssueDbContext(DbContextOptions<IssueDbContext> options) : base(options)
    {
    }

    public DbSet<Issue> Issues { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Issue>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Title).IsUnique();

            entity.Property(e => e.Description).HasMaxLength(2000);
                  
            entity.Property(e => e.RowVersion)
                  .IsRowVersion()
                  .IsConcurrencyToken();
        });

        base.OnModelCreating(modelBuilder);
    }
}