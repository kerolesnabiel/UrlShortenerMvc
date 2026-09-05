using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using UrlShortenerMvc.Data;
using UrlShortenerMvc.Models;
using UrlShortenerMvc.Services;

namespace UrlShortenerMvc.Controllers;

public class RedirectController(
    IDistributedCache cache,
    ApplicationDbContext dbContext,
    IGeoIpService geoIpService) : Controller
{
    [HttpGet("/{shortCode}")]
    public async Task<IActionResult> Index(string shortCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            return NotFound();

        var cacheKey = $"link:{shortCode}";
        LinkCacheItem? link = null;
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
            link = JsonSerializer.Deserialize<LinkCacheItem>(cached);

        if (link is null)
        {
            var dbLink = await dbContext.Links
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ShortCode == shortCode && x.IsActive, cancellationToken);

            if (dbLink is null || (dbLink.ExpiresAt.HasValue && dbLink.ExpiresAt.Value <= DateTime.UtcNow))
                return NotFound();

            link = new LinkCacheItem(dbLink.Id, dbLink.OriginalUrl, dbLink.UserId.HasValue, dbLink.ExpiresAt);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(link), cacheOptions, cancellationToken);
        }

        if (link.TracksClicks)
        {
            var click = new Click
            {
                LinkId = link.Id,
                Timestamp = DateTime.UtcNow,
                Referrer = Request.Headers.Referer.FirstOrDefault(),
                UserAgent = Request.Headers.UserAgent.FirstOrDefault(),
                Country = geoIpService.GetCountry(HttpContext.Connection.RemoteIpAddress)
            };

            dbContext.Clicks.Add(click);

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE Links
                SET ClickCount = ClickCount + 1
                WHERE Id = {link.Id}
                """,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Redirect(link.OriginalUrl);
    }

    private sealed record LinkCacheItem(Guid Id, string OriginalUrl, bool TracksClicks, DateTime? ExpiresAt);
}