namespace UrlShortenerMvc.Models;

public class Link
{
    public Guid Id { get; set; }

    public string ShortCode { get; set; } = string.Empty;

    public string OriginalUrl { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public long ClickCount { get; set; } = 0L;

    public ICollection<Click> Clicks { get; set; } = [];
}