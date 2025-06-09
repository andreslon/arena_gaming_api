using ArenaGaming.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ArenaGaming.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<Move> Moves { get; set; }
    public DbSet<Session> Sessions { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Board).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CurrentPlayerSymbol).IsRequired();
            entity.Property(e => e.PlayerId).IsRequired();
            entity.Property(e => e.EndedAt);
        });

        modelBuilder.Entity<Move>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GameId).IsRequired();
            entity.Property(e => e.Position).IsRequired();
            entity.Property(e => e.Symbol).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlayerId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
} 