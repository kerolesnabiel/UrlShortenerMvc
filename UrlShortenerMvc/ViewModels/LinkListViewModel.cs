namespace UrlShortenerMvc.ViewModels;

public class LinkListViewModel
{
    public IReadOnlyList<LinkItemViewModel> Links { get; set; } =
        Array.Empty<LinkItemViewModel>();

    public string? Search { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }

    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class LinkItemViewModel
{
    public Guid Id { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public long ClickCount { get; set; }

    public bool IsExpired =>
        ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    public bool IsAvailable =>
        IsActive && !IsExpired;
}