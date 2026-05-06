using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Avatars.Models;
using Jellyfin.Plugin.Avatars.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Avatars.Controllers;

/// <summary>
/// Read-only listing of everything a user can pick from: built-in categories,
/// uploaded avatars (admin pool), and avatars contributed by imported collections
/// (wired alongside the importer).
/// </summary>
[ApiController]
[Route("Avatars/Catalog")]
[Authorize]
public class CatalogController : ControllerBase
{
    private const string UploadedCategoryId = "uploaded";

    private readonly UploadedAvatarService _uploadedService;
    private readonly BuiltInCatalogService _builtInService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogController"/> class.
    /// </summary>
    /// <param name="uploadedService">The uploaded-avatar service.</param>
    /// <param name="builtInService">The built-in catalog service.</param>
    public CatalogController(
        UploadedAvatarService uploadedService,
        BuiltInCatalogService builtInService)
    {
        _uploadedService = uploadedService;
        _builtInService = builtInService;
    }

    /// <summary>
    /// Gets the list of categories surfaced to users.
    /// </summary>
    /// <returns>A list of category descriptors with avatar counts.</returns>
    [HttpGet("Categories")]
    public ActionResult<IEnumerable<object>> GetCategories()
    {
        var disabled = Plugin.Instance?.Configuration.DisabledBuiltInIds ?? new List<string>();
        var disabledSet = new HashSet<string>(disabled, System.StringComparer.OrdinalIgnoreCase);

        var builtIn = _builtInService.GetCategories()
            .Select(c => new
            {
                id = c.Id,
                displayName = c.DisplayName,
                icon = c.Icon,
                sortOrder = c.SortOrder,
                count = _builtInService.GetAvatars(c.Id).Count(a => !disabledSet.Contains(a.Id)),
            })
            .Where(c => c.count > 0)
            .Cast<object>()
            .ToList();

        // Always surface the uploads tab, even when empty, so admins can see where uploads land.
        builtIn.Add(new
        {
            id = UploadedCategoryId,
            displayName = "Uploaded",
            icon = "upload",
            sortOrder = 1000,
            count = _uploadedService.GetAll().Count,
        });

        return Ok(builtIn);
    }

    /// <summary>
    /// Gets the avatars in a category, or all enabled built-ins + uploads when no category is given.
    /// </summary>
    /// <param name="categoryId">Optional category id; when omitted returns everything.</param>
    /// <returns>A list of avatar descriptors with their image URLs.</returns>
    [HttpGet("Avatars")]
    public ActionResult<IEnumerable<object>> GetAvatars([FromQuery] string? categoryId = null)
    {
        var disabled = Plugin.Instance?.Configuration.DisabledBuiltInIds ?? new List<string>();
        var disabledSet = new HashSet<string>(disabled, System.StringComparer.OrdinalIgnoreCase);

        var results = new List<object>();

        if (string.IsNullOrEmpty(categoryId)
            || !string.Equals(categoryId, UploadedCategoryId, System.StringComparison.OrdinalIgnoreCase))
        {
            var builtIn = _builtInService.GetAvatars(categoryId)
                .Where(a => !disabledSet.Contains(a.Id))
                .Select(a => new
                {
                    kind = nameof(AvatarKind.BuiltIn),
                    id = a.Id,
                    displayName = a.DisplayName,
                    categoryId = a.CategoryId,
                    url = $"/Avatars/Image/{nameof(AvatarKind.BuiltIn)}/{a.Id}",
                });
            results.AddRange(builtIn);
        }

        if (string.IsNullOrEmpty(categoryId)
            || string.Equals(categoryId, UploadedCategoryId, System.StringComparison.OrdinalIgnoreCase))
        {
            var uploaded = _uploadedService.GetAll().Select(a => new
            {
                kind = nameof(AvatarKind.Uploaded),
                id = a.Id,
                displayName = a.DisplayName,
                categoryId = UploadedCategoryId,
                url = $"/Avatars/Image/{nameof(AvatarKind.Uploaded)}/{a.Id}",
            });
            results.AddRange(uploaded);
        }

        return Ok(results);
    }
}
