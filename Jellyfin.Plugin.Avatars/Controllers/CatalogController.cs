using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Avatars.Models;
using Jellyfin.Plugin.Avatars.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Avatars.Controllers;

/// <summary>
/// Read-only listing of everything a user can pick from: built-in categories
/// (wired in chunk #6), uploaded avatars, and avatars contributed by imported
/// collections (wired alongside the importer).
/// </summary>
[ApiController]
[Route("Avatars/Catalog")]
[Authorize]
public class CatalogController : ControllerBase
{
    private readonly UploadedAvatarService _uploadedService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogController"/> class.
    /// </summary>
    /// <param name="uploadedService">The uploaded-avatar service.</param>
    public CatalogController(UploadedAvatarService uploadedService)
    {
        _uploadedService = uploadedService;
    }

    /// <summary>
    /// Gets the list of categories surfaced to users.
    /// </summary>
    /// <returns>A list of category descriptors.</returns>
    [HttpGet("Categories")]
    public ActionResult<IEnumerable<object>> GetCategories()
    {
        // Built-in categories arrive in chunk #6 with BuiltInCatalogService.
        // For now expose the synthetic "Uploaded" category so clients can render
        // a working tab strip during the transition.
        var uploadedCount = _uploadedService.GetAll().Count;
        var categories = new[]
        {
            new
            {
                id = "uploaded",
                displayName = "Uploaded",
                icon = "upload",
                sortOrder = 1000,
                count = uploadedCount,
            },
        };

        return Ok(categories);
    }

    /// <summary>
    /// Gets the avatars in a category, or all avatars if no category is supplied.
    /// </summary>
    /// <param name="categoryId">Optional category id; when omitted returns all kinds.</param>
    /// <returns>A list of avatar descriptors with their image URLs.</returns>
    [HttpGet("Avatars")]
    public ActionResult<IEnumerable<object>> GetAvatars([FromQuery] string? categoryId = null)
    {
        var includeUploaded = string.IsNullOrEmpty(categoryId)
            || string.Equals(categoryId, "uploaded", System.StringComparison.OrdinalIgnoreCase);

        if (!includeUploaded)
        {
            return Ok(System.Array.Empty<object>());
        }

        var uploaded = _uploadedService.GetAll()
            .Select(a => new
            {
                kind = nameof(AvatarKind.Uploaded),
                id = a.Id,
                displayName = a.DisplayName,
                categoryId = "uploaded",
                url = $"/Avatars/Image/{nameof(AvatarKind.Uploaded)}/{a.Id}",
                addedAt = a.DateAdded,
            });

        return Ok(uploaded);
    }
}
