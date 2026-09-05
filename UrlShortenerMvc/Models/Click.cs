namespace UrlShortenerMvc.Models;

public class Click
{
    public long Id { get; set; }
    public Guid LinkId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Referrer { get; set; }
    public string? UserAgent { get; set; }
    public string? Country { get; set; }
}