using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace EduMateBackend.Data;


public class EduMateDatabaseContext(DbContextOptions<EduMateDatabaseContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Followers> FollowerRelationships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email)
                .IsRequired();
            entity.HasIndex(u => u.Email)
                .IsUnique();
            
            entity.Property(u => u.Username)
                .IsRequired();
            entity.HasIndex(u => u.Username)
                .IsUnique();
            
            entity.Property(u => u.Password)
                .IsRequired();
            //Creating Id as primary key
            entity.HasKey(u => u.Id);
        });

        modelBuilder.Entity<Followers>(e =>
        {
            e.HasKey(en => new { UserId = en.FollowerId, FriendId = en.FolloweeId });
            e.HasOne(en => en.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(en => en.Followee)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FolloweeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MinLength(8)]
    [MaxLength(255)]
    public string Username { get; set; } = string.Empty;
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    [MinLength(8)]
    [MaxLength(255)]
    public string Password { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual ICollection<Followers> Following { get; set; } = new List<Followers>();
   
    [JsonIgnore]
    public virtual ICollection<Followers> Followers { get; set; } = new List<Followers>();
}

public class Followers
{
    public Guid FollowerId { get; set; }  
    public User Follower { get; set; } = null!;
    
    public Guid FolloweeId { get; set; }
    public User Followee { get; set; } = null!;
}