using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UrlShortenerMvc.Models;

namespace UrlShortenerMvc.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
    {
        public DbSet<Link> Links => Set<Link>();
        public DbSet<Click> Clicks => Set<Click>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Link>(entity =>
            {
                entity.Property(x => x.ShortCode)
                    .HasMaxLength(8)
                    .IsUnicode(false)
                    .IsRequired();

                entity.HasIndex(x => x.ShortCode)
                    .IsUnique();

                entity.Property(x => x.OriginalUrl)
                    .HasMaxLength(2048)
                    .IsRequired();

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(x => x.Clicks)
                    .WithOne()
                    .HasForeignKey(x => x.LinkId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Click>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.Timestamp)
                    .HasColumnType("datetime2")
                    .IsRequired();

                entity.Property(x => x.Referrer)
                    .HasMaxLength(500)
                    .IsUnicode()
                    .IsRequired(false);

                entity.Property(x => x.UserAgent)
                    .HasMaxLength(500)
                    .IsUnicode()
                    .IsRequired(false);

                entity.Property(x => x.Country)
                    .HasColumnType("char(2)")
                    .IsFixedLength()
                    .IsUnicode(false)
                    .IsRequired(false);

                entity.HasIndex(x => new { x.LinkId, x.Timestamp });
            });
        }
    }
}
