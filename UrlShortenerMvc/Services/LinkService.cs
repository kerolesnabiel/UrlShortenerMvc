using Microsoft.EntityFrameworkCore;
using UrlShortenerMvc.Data;
using UrlShortenerMvc.Models;

namespace UrlShortenerMvc.Services;

public interface ILinkService
{
    Task<Link> CreateAsync(string originalUrl, Guid? userId, DateTime? expiresAt, CancellationToken cancellationToken = default);
}

internal class LinkService(
    IShortCodeGenerator shortCodeGenerator,
    ApplicationDbContext dbContext) : ILinkService
{
    public async Task<Link> CreateAsync(string originalUrl, Guid? userId, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        const byte maxAttempts = 10;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var length = Random.Shared.Next(6, 9);
            var shortCode = shortCodeGenerator.Generate(length);
            var alreadyExists = await dbContext.Links.AnyAsync(x => x.ShortCode == shortCode, cancellationToken);

            if (alreadyExists)
                continue;

            var link = new Link
            {
                ShortCode = shortCode,
                OriginalUrl = originalUrl,
                UserId = userId,
                ExpiresAt = expiresAt,
            };

            dbContext.Links.Add(link);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return link;
            }
            catch (DbUpdateException)
            {
                dbContext.Entry(link).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique short code. Please try again.");
    }
}