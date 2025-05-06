using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace EduMateBackend.Data;


public class EduMateDatabaseContext(DbContextOptions<EduMateDatabaseContext> options) : DbContext(options)
{
    public DbSet<User> users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("userid_sequence")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.id)
                .HasDefaultValueSql("nextval('userid_sequence')")
                .ValueGeneratedOnAdd();
            
            entity.Property(u => u.email)
                .IsRequired();
            entity.HasIndex(u => u.email)
                .IsUnique();
            
            entity.Property(u => u.username)
                .IsRequired();
            entity.HasIndex(u => u.username)
                .IsUnique();
            
            entity.Property(u => u.password)
                .IsRequired();
            entity.HasKey(u => u.id);
        });
    }
}

public class User
{
    [JsonIgnore]
    public int id { get; set; }
    [MinLength(8)]
    [MaxLength(255)]
    public string username { get; set; } = null!;
    [MaxLength(255)]
    public string email { get; set; } = null!;
    [MinLength(8)]
    [MaxLength(255)]
    public string password { get; set; } = null!;
}