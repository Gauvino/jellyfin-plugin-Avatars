using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Jellyfin.Plugin.Avatars.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Avatars.Services;

/// <summary>
/// Manages avatars uploaded by admins via the dashboard.
/// Files live under <c>{PluginConfigurationsPath}/Avatars/uploaded/</c> and metadata
/// is persisted in <c>PluginConfiguration.UploadedAvatars</c>.
/// </summary>
public class UploadedAvatarService
{
    private readonly IApplicationPaths _appPaths;
    private readonly ILogger<UploadedAvatarService> _logger;
    private readonly string _uploadDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadedAvatarService"/> class.
    /// </summary>
    /// <param name="appPaths">The Jellyfin application paths.</param>
    /// <param name="logger">The logger.</param>
    public UploadedAvatarService(IApplicationPaths appPaths, ILogger<UploadedAvatarService> logger)
    {
        _appPaths = appPaths;
        _logger = logger;
        _uploadDirectory = Path.Combine(_appPaths.PluginConfigurationsPath, "Avatars", "uploaded");

        Directory.CreateDirectory(_uploadDirectory);
        _logger.LogInformation("Upload directory: {Path}", _uploadDirectory);
    }

    /// <summary>
    /// Gets the on-disk directory where uploaded avatars are stored.
    /// </summary>
    public string UploadDirectory => _uploadDirectory;

    /// <summary>
    /// Returns all uploaded avatars currently in the pool.
    /// </summary>
    /// <returns>A read-only snapshot of the uploaded list.</returns>
    public IReadOnlyList<UploadedAvatar> GetAll()
    {
        var list = Plugin.Instance?.Configuration.UploadedAvatars;
        return list is null ? Array.Empty<UploadedAvatar>() : list.ToArray();
    }

    /// <summary>
    /// Resolves the on-disk path for an uploaded avatar id.
    /// </summary>
    /// <param name="avatarId">The id of the avatar.</param>
    /// <returns>The absolute file path, or <c>null</c> if not found.</returns>
    public string? TryGetPath(string avatarId)
    {
        var avatar = Plugin.Instance?.Configuration.UploadedAvatars
            .FirstOrDefault(a => string.Equals(a.Id, avatarId, StringComparison.Ordinal));
        if (avatar is null)
        {
            return null;
        }

        var fullPath = Path.Combine(_uploadDirectory, avatar.FileName);
        return File.Exists(fullPath) ? fullPath : null;
    }

    /// <summary>
    /// Persists an uploaded image to the pool. Computes SHA-256 to deduplicate identical content
    /// — a re-upload of the same bytes returns the existing entry.
    /// </summary>
    /// <param name="originalFileName">The original filename (used for the display name and extension).</param>
    /// <param name="imageData">The raw image bytes.</param>
    /// <returns>The persisted <see cref="UploadedAvatar"/>.</returns>
    public async Task<UploadedAvatar> UploadAsync(string originalFileName, byte[] imageData)
    {
        ArgumentNullException.ThrowIfNull(imageData);

        var sha256 = Convert.ToHexString(SHA256.HashData(imageData)).ToLowerInvariant();

        var existing = Plugin.Instance?.Configuration.UploadedAvatars
            .FirstOrDefault(a => string.Equals(a.Sha256, sha256, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _logger.LogInformation("Duplicate upload detected (sha256={Sha}); returning existing {Id}", sha256, existing.Id);
            return existing;
        }

        var id = Guid.NewGuid().ToString("N");
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(ext))
        {
            ext = ".png";
        }

        var fileName = $"{id}{ext}";
        var filePath = Path.Combine(_uploadDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, imageData).ConfigureAwait(false);

        var avatar = new UploadedAvatar
        {
            Id = id,
            FileName = fileName,
            Sha256 = sha256,
            DisplayName = Path.GetFileNameWithoutExtension(originalFileName),
            DateAdded = DateTime.UtcNow,
        };

        var plugin = Plugin.Instance ?? throw new InvalidOperationException("Plugin not initialized");
        plugin.Configuration.UploadedAvatars.Add(avatar);
        plugin.SaveConfiguration();

        _logger.LogInformation("Uploaded avatar {Id} ({DisplayName}, sha={Sha})", id, avatar.DisplayName, sha256);
        return avatar;
    }

    /// <summary>
    /// Removes an uploaded avatar from the pool. Does not touch users currently using it
    /// — their profile image file is owned by Jellyfin and remains intact.
    /// </summary>
    /// <param name="avatarId">The id of the avatar to delete.</param>
    /// <returns><c>true</c> if the avatar existed and was removed, <c>false</c> otherwise.</returns>
    public bool Delete(string avatarId)
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return false;
        }

        var avatar = plugin.Configuration.UploadedAvatars
            .FirstOrDefault(a => string.Equals(a.Id, avatarId, StringComparison.Ordinal));
        if (avatar is null)
        {
            return false;
        }

        var filePath = Path.Combine(_uploadDirectory, avatar.FileName);
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file for uploaded avatar {Id} at {Path}", avatarId, filePath);
        }

        plugin.Configuration.UploadedAvatars.Remove(avatar);

        // Clear stale user mappings pointing at this uploaded id.
        plugin.Configuration.UserAvatars.RemoveAll(m =>
            m.Kind == AvatarKind.Uploaded
            && string.Equals(m.AvatarId, avatarId, StringComparison.Ordinal));

        plugin.SaveConfiguration();

        _logger.LogInformation("Deleted uploaded avatar {Id} ({DisplayName})", avatarId, avatar.DisplayName);
        return true;
    }
}
