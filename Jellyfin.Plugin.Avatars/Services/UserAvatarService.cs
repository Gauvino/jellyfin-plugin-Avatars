using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.Avatars.Configuration;
using Jellyfin.Plugin.Avatars.Models;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Avatars.Services;

/// <summary>
/// Sets and clears the per-user avatar selection. Branches on <see cref="AvatarKind"/>
/// to resolve the source path from the appropriate provider service.
/// </summary>
public class UserAvatarService
{
    private readonly IUserManager _userManager;
    private readonly IApplicationPaths _appPaths;
    private readonly UploadedAvatarService _uploadedService;
    private readonly BuiltInCatalogService _builtInService;
    private readonly ILogger<UserAvatarService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAvatarService"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="appPaths">Jellyfin application paths.</param>
    /// <param name="uploadedService">Provider for <see cref="AvatarKind.Uploaded"/> sources.</param>
    /// <param name="builtInService">Provider for <see cref="AvatarKind.BuiltIn"/> sources.</param>
    /// <param name="logger">The logger.</param>
    public UserAvatarService(
        IUserManager userManager,
        IApplicationPaths appPaths,
        UploadedAvatarService uploadedService,
        BuiltInCatalogService builtInService,
        ILogger<UserAvatarService> logger)
    {
        _userManager = userManager;
        _appPaths = appPaths;
        _uploadedService = uploadedService;
        _builtInService = builtInService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the currently selected avatar mapping for a user, or <c>null</c>.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>The mapping or <c>null</c>.</returns>
    public UserAvatarMapping? GetMapping(Guid userId)
    {
        return Plugin.Instance?.Configuration.UserAvatars
            .FirstOrDefault(m => MatchesUser(m, userId));
    }

    /// <summary>
    /// Sets a user's profile image to the avatar identified by <paramref name="kind"/>+<paramref name="avatarId"/>.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="kind">The source kind.</param>
    /// <param name="avatarId">The avatar id within that source.</param>
    /// <returns>A task that completes when the user record has been updated.</returns>
    /// <exception cref="ArgumentException">Thrown when the user or avatar does not resolve.</exception>
    public async Task SetAsync(Guid userId, AvatarKind kind, string avatarId)
    {
        var user = _userManager.GetUserById(userId)
            ?? throw new ArgumentException($"User not found: {userId}", nameof(userId));

        var sourcePath = ResolveSourcePath(kind, avatarId)
            ?? throw new ArgumentException($"Avatar not found: kind={kind}, id={avatarId}", nameof(avatarId));

        var ext = Path.GetExtension(sourcePath);
        var userDataPath = Path.Combine(_appPaths.DataPath, "users", userId.ToString("N"));
        Directory.CreateDirectory(userDataPath);

        // Built-in / imported avatar ids are namespaced as "{categoryId}/{slug}" — replace
        // path separators so the filename stays a single segment.
        var safeId = avatarId.Replace('/', '_').Replace('\\', '_');
        var timestamp = DateTime.UtcNow.Ticks;
        var profileImagePath = Path.Combine(userDataPath, $"profile_avatar_{safeId}_{timestamp}{ext}");

        var oldProfileImagePath = user.ProfileImage?.Path;

        await CleanupOldProfileFilesAsync(userDataPath, oldProfileImagePath).ConfigureAwait(false);

        try
        {
            File.Copy(sourcePath, profileImagePath, overwrite: true);

            if (user.ProfileImage is null)
            {
                user.ProfileImage = new ImageInfo(profileImagePath);
            }
            else
            {
                user.ProfileImage.Path = profileImagePath;
            }

            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(oldProfileImagePath)
                && !string.Equals(oldProfileImagePath, profileImagePath, StringComparison.OrdinalIgnoreCase)
                && File.Exists(oldProfileImagePath))
            {
                try
                {
                    File.Delete(oldProfileImagePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete old profile image at {Path}", oldProfileImagePath);
                }
            }
        }
        catch
        {
            if (File.Exists(profileImagePath))
            {
                try
                {
                    File.Delete(profileImagePath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Could not clean up orphan {Path} after failed Set", profileImagePath);
                }
            }

            throw;
        }

        UpsertMapping(userId, kind, avatarId);
        _logger.LogInformation("Set avatar {Kind}:{Id} for user {User}", kind, avatarId, user.Username);
    }

    /// <summary>
    /// Clears a user's avatar selection and removes the profile image file.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns><c>true</c> if the user existed and the change was applied.</returns>
    public async Task<bool> RemoveAsync(Guid userId)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return false;
        }

        var oldProfileImagePath = user.ProfileImage?.Path;
        user.ProfileImage = null;
        await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(oldProfileImagePath) && File.Exists(oldProfileImagePath))
        {
            try
            {
                File.Delete(oldProfileImagePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete profile image {Path}", oldProfileImagePath);
            }
        }

        var plugin = Plugin.Instance;
        if (plugin is not null)
        {
            var removed = plugin.Configuration.UserAvatars.RemoveAll(m => MatchesUser(m, userId));
            if (removed > 0)
            {
                plugin.SaveConfiguration();
            }
        }

        _logger.LogInformation("Removed avatar for user {User}", user.Username);
        return true;
    }

    /// <summary>
    /// Re-applies user avatars where the on-disk profile image is missing
    /// (e.g. after a Jellyfin data wipe). Mappings whose source no longer
    /// resolves are dropped from configuration.
    /// </summary>
    /// <returns>The number of profile images successfully restored.</returns>
    public async Task<int> ValidateAsync()
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return 0;
        }

        var repaired = 0;
        var toRemove = new List<UserAvatarMapping>();

        foreach (var mapping in plugin.Configuration.UserAvatars.ToList())
        {
            if (!Guid.TryParse(mapping.UserId, out var userId))
            {
                toRemove.Add(mapping);
                continue;
            }

            var user = _userManager.GetUserById(userId);
            if (user is null)
            {
                toRemove.Add(mapping);
                continue;
            }

            var profileExists = user.ProfileImage?.Path is { Length: > 0 } path && File.Exists(path);
            if (profileExists)
            {
                continue;
            }

            if (ResolveSourcePath(mapping.Kind, mapping.AvatarId) is null)
            {
                toRemove.Add(mapping);
                if (user.ProfileImage is not null)
                {
                    user.ProfileImage = null;
                    await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
                }

                continue;
            }

            try
            {
                await SetAsync(userId, mapping.Kind, mapping.AvatarId).ConfigureAwait(false);
                repaired++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not repair avatar for user {User}", user.Username);
            }
        }

        foreach (var mapping in toRemove)
        {
            plugin.Configuration.UserAvatars.Remove(mapping);
        }

        if (toRemove.Count > 0)
        {
            plugin.SaveConfiguration();
        }

        return repaired;
    }

    /// <summary>
    /// Removes <c>profile_*</c> files inside each user's data directory that are not
    /// the current <c>ProfileImage.Path</c>.
    /// </summary>
    /// <returns>The number of orphan files deleted.</returns>
    public int CleanOrphans()
    {
        var deleted = 0;
        foreach (var user in _userManager.Users)
        {
            var userDataPath = Path.Combine(_appPaths.DataPath, "users", user.Id.ToString("N"));
            if (!Directory.Exists(userDataPath))
            {
                continue;
            }

            var current = user.ProfileImage?.Path;
            foreach (var file in Directory.GetFiles(userDataPath, "profile_*"))
            {
                if (string.Equals(file, current, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not delete orphan {File}", file);
                }
            }
        }

        return deleted;
    }

    private static bool MatchesUser(UserAvatarMapping mapping, Guid userId)
    {
        return string.Equals(mapping.UserId, userId.ToString("N"), StringComparison.OrdinalIgnoreCase)
            || string.Equals(mapping.UserId, userId.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private string? ResolveSourcePath(AvatarKind kind, string avatarId)
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

    private void UpsertMapping(Guid userId, AvatarKind kind, string avatarId)
    {
        var plugin = Plugin.Instance ?? throw new InvalidOperationException("Plugin not initialized");
        var key = userId.ToString("N");

        var mapping = plugin.Configuration.UserAvatars.FirstOrDefault(m => MatchesUser(m, userId));

        if (mapping is null)
        {
            plugin.Configuration.UserAvatars.Add(new UserAvatarMapping
            {
                UserId = key,
                Kind = kind,
                AvatarId = avatarId,
            });
        }
        else
        {
            mapping.Kind = kind;
            mapping.AvatarId = avatarId;
            mapping.UserId = key;
        }

        plugin.SaveConfiguration();
    }

    private async Task CleanupOldProfileFilesAsync(string userDataPath, string? currentProfilePath)
    {
        if (!Directory.Exists(userDataPath))
        {
            return;
        }

        var profileFiles = Directory.GetFiles(userDataPath, "profile_*");
        foreach (var file in profileFiles)
        {
            if (string.Equals(file, currentProfilePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        using var stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    }
                    catch (IOException)
                    {
                        return;
                    }

                    File.Delete(file);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not delete old profile file (may be in use): {File}", file);
            }
        }
    }
}
