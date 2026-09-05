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
            });
        }
    }
}
