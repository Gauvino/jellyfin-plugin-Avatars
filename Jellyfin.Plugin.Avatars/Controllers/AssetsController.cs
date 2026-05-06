using System;
using System.IO;
using System.Reflection;
using Jellyfin.Plugin.Avatars.Models;
using Jellyfin.Plugin.Avatars.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Avatars.Controllers;

/// <summary>
/// Streams binary plugin assets (images + the injected client script) without auth
/// so they can be referenced from <c>index.html</c> and from user-facing pages.
/// </summary>
[ApiController]
[Route("Avatars")]
[AllowAnonymous]
public class AssetsController : ControllerBase
{
    private readonly UploadedAvatarService _uploadedService;
    private readonly BuiltInCatalogService _builtInService;
    private readonly ILogger<AssetsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetsController"/> class.
    /// </summary>
    /// <param name="uploadedService">The uploaded-avatar provider.</param>
    /// <param name="builtInService">The built-in catalog provider.</param>
    /// <param name="logger">The logger.</param>
    public AssetsController(
        UploadedAvatarService uploadedService,
        BuiltInCatalogService builtInService,
        ILogger<AssetsController> logger)
    {
        _uploadedService = uploadedService;
        _builtInService = builtInService;
        _logger = logger;
    }

    /// <summary>
    /// Serves the JavaScript bundle injected into Jellyfin's web UI by
    /// <c>ScriptInjectorMiddleware</c>.
    /// </summary>
    /// <returns>The embedded <c>clientScript.js</c> file.</returns>
    [HttpGet("ClientScript")]
    [Produces("application/javascript")]
    public IActionResult GetClientScript()
    {
        const string ResourceName = "Jellyfin.Plugin.Avatars.Configuration.Web.clientScript.js";
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            _logger.LogError("Embedded client script not found: {Resource}", ResourceName);
            return NotFound();
        }

        return File(stream, "application/javascript");
    }

    /// <summary>
    /// Streams the avatar image bytes for a given <paramref name="kind"/> + <paramref name="avatarId"/>.
    /// </summary>
    /// <param name="kind">The avatar source kind.</param>
    /// <param name="avatarId">The avatar id within that source.</param>
    /// <returns>The image content with the appropriate <c>Content-Type</c>.</returns>
    [HttpGet("Image/{kind}/{*avatarId}")]
    public IActionResult GetImage(AvatarKind kind, string avatarId)
    {
        var path = ResolvePath(kind, avatarId);
        if (path is null || !System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream",
        };

        return File(System.IO.File.OpenRead(path), contentType);
    }

    private string? ResolvePath(AvatarKind kind, string avatarId)
    {
        return kind switch
        {
            AvatarKind.Uploaded => _uploadedService.TryGetPath(avatarId),
            AvatarKind.BuiltIn => _builtInService.TryGetPath(avatarId),

            // Wired when CollectionImportService lands.
            AvatarKind.Imported => null,

            _ => null,
        };
    }
}
