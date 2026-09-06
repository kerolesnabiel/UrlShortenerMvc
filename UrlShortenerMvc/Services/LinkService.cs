using Microsoft.EntityFrameworkCore;
using UrlShortenerMvc.Data;
using UrlShortenerMvc.Models;
using UrlShortenerMvc.ViewModels;

namespace UrlShortenerMvc.Services;

public interface ILinkService
{
    Task<Link?> GetLinkAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);
    Task UpdateLinkAsync(Link link, CancellationToken cancellationToken = default);


    Task<Link> CreateAsync(string originalUrl, Guid? userId, DateTime? expiresAt,
        CancellationToken cancellationToken = default);

    Task<LinkListViewModel> GetUserLinksAsync(Guid userId, string? search, string baseUrl,
        int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
}

internal class LinkService(IShortCodeGenerator shortCodeGenerator, ApplicationDbContext dbContext) : ILinkService
{
    public async Task<Link?> GetLinkAsync(Guid id, Guid? userId, CancellationToken cancellationToken)
    {
        return await dbContext.Links.FirstOrDefaultAsync(x =>
            x.Id == id && (!userId.HasValue || x.UserId == userId.Value), cancellationToken);
    }

    public async Task UpdateLinkAsync(Link link, CancellationToken cancellationToken)
    {
        dbContext.Links.Update(link);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Link> CreateAsync(string originalUrl, Guid? userId, DateTime? expiresAt,
        CancellationToken cancellationToken)
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

    public async Task<LinkListViewModel> GetUserLinksAsync(Guid userId, string? search, string baseUrl,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            pageNumber = 1;

        search = search?.Trim();

        var query = dbContext.Links.AsNoTracking().Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.ShortCode.Contains(search) || x.OriginalUrl.Contains(search));

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        if (totalPages > 0 && pageNumber > totalPages)
            pageNumber = totalPages;

        var links = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LinkItemViewModel
            {
                Id = x.Id,
                ShortCode = x.ShortCode,
                ShortUrl = $"{baseUrl}/{x.ShortCode}",
                OriginalUrl = x.OriginalUrl,
                CreatedAt = x.CreatedAt,
                ExpiresAt = x.ExpiresAt,
                IsActive = x.IsActive,
                ClickCount = x.ClickCount
            })
            .ToListAsync(cancellationToken);

        return new LinkListViewModel
        {
            Links = links,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
}