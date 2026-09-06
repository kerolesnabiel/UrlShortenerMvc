namespace UrlShortenerMvc.ViewModels;

public class LinkDetailsViewModel
{
    public Guid Id { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long TotalClicks { get; set; }
    public DateTime? LastClickAt { get; set; }
    public IReadOnlyList<ClickChartPointViewModel> ClicksOverTime { get; set; } = [];
    public IReadOnlyList<CountryAnalyticsViewModel> Countries { get; set; } = [];
    public IReadOnlyList<ReferrerAnalyticsViewModel> Referrers { get; set; } = [];
    public IReadOnlyList<DeviceAnalyticsViewModel> Devices { get; set; } = [];
    public IReadOnlyList<BrowserAnalyticsViewModel> Browsers { get; set; } = [];
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    public bool IsAvailable => IsActive && !IsExpired;
}

public class ClickChartPointViewModel
{
    public DateTime Date { get; set; }
    public long Clicks { get; set; }
}

public class CountryAnalyticsViewModel
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public long Clicks { get; set; }
    public double Percentage { get; set; }

    public string FlagEmoji => CountryCode.Length == 2
        ? string.Concat(CountryCode.ToUpperInvariant()
            .Select(c => char.ConvertFromUtf32(127397 + c)))
        : "🌍";
}

public class ReferrerAnalyticsViewModel
{
    public string Name { get; set; } = string.Empty;
    public long Clicks { get; set; }
    public double Percentage { get; set; }
}

public class DeviceAnalyticsViewModel
{
    public string Name { get; set; } = string.Empty;
    public long Clicks { get; set; }
    public double Percentage { get; set; }
}

public class BrowserAnalyticsViewModel
{
    public string Name { get; set; } = string.Empty;
    public long Clicks { get; set; }
    public double Percentage { get; set; }
}
