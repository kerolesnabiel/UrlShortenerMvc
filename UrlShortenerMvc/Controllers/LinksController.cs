using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UrlShortenerMvc.Services;
using UrlShortenerMvc.ViewModels;

namespace UrlShortenerMvc.Controllers;

public class LinksController(
    IUrlValidationService urlValidationService,
    ILinkService linkService) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Create()
    {
        return View(new CreateLinkViewModel() { IsAnonymous = User.Identity?.IsAuthenticated != true });
    }


    [AllowAnonymous]
    [EnableRateLimiting("CreateLink")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLinkViewModel model, CancellationToken cancellationToken)
    {
        model.IsAnonymous = User.Identity?.IsAuthenticated != true;

        if (!ModelState.IsValid)
            return View(model);

        var uriValidationResult = await urlValidationService.ValidateAsync(model.OriginalUrl, cancellationToken);
        if (!uriValidationResult.IsValid)
        {
            ModelState.AddModelError(nameof(model.OriginalUrl),
                uriValidationResult.ErrorMessage ?? "The URL is invalid.");
            return View(model);
        }

        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var parsedUserId))
                return Unauthorized();
            userId = parsedUserId;
        }

        DateTime? expiresAt = null;
        if (model.ExpirationDays.HasValue)
        {
            var days = model.ExpirationDays.Value;
            if (days is not (1 or 7 or 30))
            {
                ModelState.AddModelError(nameof(model.ExpirationDays), "Invalid expiration period.");
                return View(model);
            }

            expiresAt = DateTime.UtcNow.AddDays(days);
        }

        var link = await linkService.CreateAsync(
            uriValidationResult.NormalizedUrl!,
            userId,
            expiresAt,
            cancellationToken);

        model.ShortUrl = BuildShortUrl(link.ShortCode);

        return View(model);
    }

    private string BuildShortUrl(string shortCode)
    {
        return $"{Request.Scheme}://{Request.Host}/{shortCode}";
    }
}