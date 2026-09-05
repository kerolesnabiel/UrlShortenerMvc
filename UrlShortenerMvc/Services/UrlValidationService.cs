using System.Text.RegularExpressions;

namespace UrlShortenerMvc.Services;

public interface IUrlValidationService
{
    Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default);
}

public sealed record UrlValidationResult(bool IsValid, string? NormalizedUrl = null, string? ErrorMessage = null);

internal class UrlValidationService(IHttpContextAccessor httpContextAccessor) : IUrlValidationService
{
    public Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Task.FromResult(
                new UrlValidationResult(false, ErrorMessage: "Please enter a URL."));

        url = url.Trim();

        if (url.Length > 2048)
            return Task.FromResult(new UrlValidationResult
                (false, ErrorMessage: "The URL is too long."));

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return Task.FromResult(new UrlValidationResult
                (false, ErrorMessage: "Please enter a valid absolute URL."));

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return Task.FromResult(new UrlValidationResult
                (false, ErrorMessage: "Only HTTP and HTTPS URLs are allowed."));

        if (Regex.IsMatch(uri.Scheme, "^(javascript|data|file|vbscript)$", RegexOptions.IgnoreCase))
            return Task.FromResult(new UrlValidationResult
                (false, ErrorMessage: "This URL scheme is not allowed."));

        if (!string.IsNullOrEmpty(uri.UserInfo))
            return Task.FromResult(new UrlValidationResult
                (false, ErrorMessage: "URLs containing embedded credentials are not allowed."));

        if (IsOwnDomain(uri))
            return Task.FromResult(new UrlValidationResult
                (false, ErrorMessage: "You cannot shorten a URL belonging to this service."));

        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        var normalizedUrl = builder.Uri.AbsoluteUri;

        return Task.FromResult(new UrlValidationResult(true, normalizedUrl));
    }

    private bool IsOwnDomain(Uri uri)
    {
        var host = httpContextAccessor.HttpContext?.Request.Host.Host;
        return !string.IsNullOrWhiteSpace(host) &&
            string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase);
    }
}