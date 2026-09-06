using System.ComponentModel.DataAnnotations;

namespace UrlShortenerMvc.ViewModels;

public class EditLinkViewModel
{
    public Guid Id { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    [Display(Name = "Expiration")] public int? ExpirationDays { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public long ClickCount { get; set; }
}
