using Microsoft.EntityFrameworkCore;
using TaskManager.Models;

namespace TaskManager.Data;

public class  TasksDbContext(DbContextOptions<TasksDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(255);
            entity.Property(t => t.Description);
            entity.Property(t => t.IsCompleted).IsRequired();
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("now()"); ;
            entity.Property(t => t.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(t => t.UserId).IsRequired();
            entity.HasIndex(t => t.UserId);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Token).IsRequired();
            entity.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(r => r.RevokedAt).IsRequired(false);
            entity.Property(r => r.ExpiresAt).IsRequired();
            entity.Property(r => r.IsRevoked).IsRequired();
            entity.HasIndex(r => r.Token).IsUnique();
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}