using Microsoft.EntityFrameworkCore;
using UrlShortenerMvc.Data;
using UrlShortenerMvc.Models;

namespace UrlShortenerMvc.Services.ClickTracking;

public sealed class ClickWorker(
    IClickQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ClickWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var click = await queue.DequeueAsync(stoppingToken);
            try
            {
                await ProcessClickAsync(click, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Save click to database failed. ");
                // Production implementation should also consider retry/dead-letter handling.
            }
        }
    }

    private async Task ProcessClickAsync(ClickEvent click, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Clicks.Add(new Click
        {
            LinkId = click.LinkId,
            Timestamp = click.Timestamp,
            Referrer = click.Referrer,
            UserAgent = click.UserAgent,
            Country = click.Country
        });

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE Links
             SET ClickCount = ClickCount + 1
             WHERE Id = {click.LinkId}
             """,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}