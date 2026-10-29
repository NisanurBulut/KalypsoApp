using Kalypso.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Kalypso.Api.Data;

public sealed class ShortLink
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string OriginalUrl { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public long Clicks { get; set; }
}

public sealed class KalypsoDbContext(DbContextOptions<KalypsoDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> Links => Set<ShortLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortLink>(link =>
        {
            link.HasIndex(l => l.Code).IsUnique();
            link.Property(l => l.Code).HasMaxLength(LinkRules.MaxCodeLength).IsRequired();
            link.Property(l => l.OriginalUrl).HasMaxLength(LinkRules.MaxUrlLength).IsRequired();
        });
    }
}
