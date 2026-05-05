using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.Avatars.Models;
using Jellyfin.Plugin.Avatars.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Avatars.Controllers;

/// <summary>
/// Admin endpoints for managing the uploaded avatar pool.
/// </summary>
[ApiController]
[Route("Avatars/Upload")]
[Authorize(Policy = "RequiresElevation")]
public class UploadController : ControllerBase
{
    private const long _maxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly UploadedAvatarService _uploadedService;
    private readonly ILogger<UploadController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadController"/> class.
    /// </summary>
    /// <param name="uploadedService">The uploaded-avatar service.</param>
    /// <param name="logger">The logger.</param>
    public UploadController(UploadedAvatarService uploadedService, ILogger<UploadController> logger)
    {
        _uploadedService = uploadedService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a new avatar to the pool.
    /// </summary>
    /// <param name="file">The image file.</param>
    /// <returns>The newly created avatar descriptor.</returns>
    [HttpPost]
    public async Task<IActionResult> UploadAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        if (file.Length > _maxFileSizeBytes)
        {
            return BadRequest($"File exceeds the {_maxFileSizeBytes / (1024 * 1024)} MB limit");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(ext))
        {
            return BadRequest("Unsupported file type. Allowed: " + string.Join(", ", _allowedExtensions));
        }

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms).ConfigureAwait(false);
            var avatar = await _uploadedService.UploadAsync(file.FileName, ms.ToArray()).ConfigureAwait(false);

            return Ok(new
            {
                kind = nameof(AvatarKind.Uploaded),
                id = avatar.Id,
                displayName = avatar.DisplayName,
                addedAt = avatar.DateAdded,
                url = $"/Avatars/Image/{nameof(AvatarKind.Uploaded)}/{avatar.Id}",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {FileName}", file.FileName);
            return StatusCode(500, "Failed to upload avatar");
        }
    }

    /// <summary>
    /// Removes an uploaded avatar from the pool. Mappings pointing at it are cleaned up,
    /// but users keep their existing profile image until they pick another one.
    /// </summary>
    /// <param name="avatarId">The id of the avatar to delete.</param>
    /// <returns><see cref="OkResult"/> on success.</returns>
    [HttpDelete("{avatarId}")]
    public IActionResult Delete(string avatarId)
    {
        if (!_uploadedService.Delete(avatarId))
        {
            return NotFound();
        }

        return Ok(new { message = "Avatar deleted" });
    }
}
