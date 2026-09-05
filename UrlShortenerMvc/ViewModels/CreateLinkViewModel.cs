using System.ComponentModel.DataAnnotations;

namespace UrlShortenerMvc.ViewModels;

public class CreateLinkViewModel
{
    [Required(ErrorMessage = "Please enter a URL.")]
    [Display(Name = "Destination URL")]
    public string OriginalUrl { get; set; } = string.Empty;
    public string? ShortUrl { get; set; }
    [Display(Name = "Expiration")]
    public int? ExpirationDays { get; set; }
    public bool IsAnonymous { get; set; }
    public bool CreatedSuccessfully =>
        !string.IsNullOrWhiteSpace(ShortUrl);
}