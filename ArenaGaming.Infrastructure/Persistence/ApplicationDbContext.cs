using ArenaGaming.Core.Domain;
using ArenaGaming.Core.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace ArenaGaming.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<Move> Moves { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationPreferences> NotificationPreferences { get; set; }

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

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.IsRead).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );
        });

        modelBuilder.Entity<NotificationPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.GameEvents).IsRequired();
            entity.Property(e => e.SocialEvents).IsRequired();
            entity.Property(e => e.SoundEffects).IsRequired();
            entity.Property(e => e.Volume).IsRequired();
            entity.Property(e => e.EmailNotifications).IsRequired();
            entity.Property(e => e.PushNotifications).IsRequired();
            entity.Property(e => e.TournamentAlerts).IsRequired();
            entity.Property(e => e.PlayerActions).IsRequired();
            entity.Property(e => e.SystemUpdates).IsRequired();
            
            // Create unique index on UserId
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }
} 