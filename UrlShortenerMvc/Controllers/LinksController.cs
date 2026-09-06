using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UrlShortenerMvc.Services;
using UrlShortenerMvc.ViewModels;

namespace UrlShortenerMvc.Controllers;

[Authorize]
public class LinksController(
    IUrlValidationService urlValidationService,
    ILinkService linkService) : Controller
{
    private const int PageSize = 30;

    [HttpGet("/Links")]
    public async Task<IActionResult> Index(string? search, int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Challenge();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var model = await linkService.GetUserLinksAsync(userId.Value, search, baseUrl,
            pageNumber, PageSize, cancellationToken);

        return View(model);
    }

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

        var userId = GetCurrentUserId();

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Challenge();

        var link = await linkService.GetLinkAsync(id, userId, cancellationToken);

        if (link is null)
            return NotFound();

        link.IsActive = false;
        await linkService.UpdateLinkAsync(link, cancellationToken);

        TempData["SuccessMessage"] = "The link has been disabled.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Challenge();


        var link = await linkService.GetLinkAsync(id, userId, cancellationToken);

        if (link is null)
            return NotFound();

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value <= DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "This link has expired. Change its expiration before enabling it.";
            return RedirectToAction(nameof(Edit), new { id = link.Id });
        }


        link.IsActive = true;
        await linkService.UpdateLinkAsync(link, cancellationToken);

        TempData["SuccessMessage"] = "The link has been enabled.";

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete()
    {
        throw new NotImplementedException();
    }

    public IActionResult Edit()
    {
        throw new NotImplementedException();
    }

    private string BuildShortUrl(string shortCode)
    {
        return $"{Request.Scheme}://{Request.Host}/{shortCode}";
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}