using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Jellyfin.Plugin.Avatars.Configuration;
using Jellyfin.Plugin.Avatars.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Avatars.Services;

/// <summary>
/// One-shot hosted service that migrates state from <c>cedev-1/jellyfin-plugin-GetAvatar</c>
/// (this fork's upstream) into the v3 Avatars schema. Idempotent: each invocation no-ops once
/// <see cref="PluginConfiguration.SchemaVersion"/> reaches 3.
/// </summary>
/// <remarks>
/// Non-destructive: the legacy XML file (<c>Jellyfin.Plugin.GetAvatar.xml</c>) and avatar
/// directory (<c>{PluginConfigurationsPath}/GetAvatar/avatars/</c>) are left untouched, so a
/// user who decides to roll back can re-install GetAvatar and resume their previous state.
/// </remarks>
public class LegacyMigrationHostedService : IHostedService
{
    private const string LegacyConfigFileName = "Jellyfin.Plugin.GetAvatar.xml";
    private const string LegacyAvatarDirName = "GetAvatar";
    private const string LegacyAvatarSubDir = "avatars";
    private const int CurrentSchemaVersion = 3;

    private readonly IApplicationPaths _appPaths;
    private readonly UploadedAvatarService _uploadedService;
    private readonly ILogger<LegacyMigrationHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyMigrationHostedService"/> class.
    /// </summary>
    /// <param name="appPaths">The Jellyfin application paths.</param>
    /// <param name="uploadedService">The uploaded-avatar service (used to access its target directory).</param>
    /// <param name="logger">The logger.</param>
    public LegacyMigrationHostedService(
        IApplicationPaths appPaths,
        UploadedAvatarService uploadedService,
        ILogger<LegacyMigrationHostedService> logger)
    {
        _appPaths = appPaths;
        _uploadedService = uploadedService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            RunMigration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Legacy GetAvatar migration failed; continuing without it");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void RunMigration()
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return;
        }

        if (plugin.Configuration.SchemaVersion >= CurrentSchemaVersion)
        {
            return;
        }

        var legacyConfigPath = Path.Combine(_appPaths.PluginConfigurationsPath, LegacyConfigFileName);
        if (!File.Exists(legacyConfigPath))
        {
            _logger.LogInformation("No legacy GetAvatar config found; marking schema as v{Version}", CurrentSchemaVersion);
            plugin.Configuration.SchemaVersion = CurrentSchemaVersion;
            plugin.SaveConfiguration();
            return;
        }

        _logger.LogInformation("Legacy GetAvatar config detected at {Path} — migrating to v{Version}", legacyConfigPath, CurrentSchemaVersion);

        LegacyConfig? legacy;
        try
        {
            using var stream = File.OpenRead(legacyConfigPath);
            var serializer = new XmlSerializer(typeof(LegacyConfig));
            legacy = serializer.Deserialize(stream) as LegacyConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not deserialize legacy config; aborting migration to avoid data loss");
            return;
        }

        if (legacy is null)
        {
            plugin.Configuration.SchemaVersion = CurrentSchemaVersion;
            plugin.SaveConfiguration();
            return;
        }

        var legacyAvatarDir = Path.Combine(_appPaths.PluginConfigurationsPath, LegacyAvatarDirName, LegacyAvatarSubDir);
        var migratedAvatars = MigrateAvatars(legacy.AvailableAvatars ?? new List<LegacyAvatarInfo>(), legacyAvatarDir);
        var migratedMappings = MigrateUserMappings(legacy.UserAvatars ?? new List<LegacyUserAvatarMapping>());

        plugin.Configuration.UploadedAvatars.AddRange(migratedAvatars);
        plugin.Configuration.UserAvatars.AddRange(migratedMappings);
        plugin.Configuration.SchemaVersion = CurrentSchemaVersion;
        plugin.SaveConfiguration();

        _logger.LogInformation(
            "Migration complete: {Avatars} uploaded avatar(s), {Mappings} user mapping(s) imported from GetAvatar",
            migratedAvatars.Count,
            migratedMappings.Count);
    }

    private List<UploadedAvatar> MigrateAvatars(IList<LegacyAvatarInfo> legacyEntries, string legacyAvatarDir)
    {
        var migrated = new List<UploadedAvatar>(legacyEntries.Count);
        if (legacyEntries.Count == 0)
        {
            return migrated;
        }

        Directory.CreateDirectory(_uploadedService.UploadDirectory);

        foreach (var entry in legacyEntries)
        {
            if (string.IsNullOrEmpty(entry.Id) || string.IsNullOrEmpty(entry.FileName))
            {
                continue;
            }

            var srcPath = Path.Combine(legacyAvatarDir, entry.FileName);
            if (!File.Exists(srcPath))
            {
                _logger.LogWarning("Legacy avatar {Id} ({File}) not found on disk; skipping", entry.Id, entry.FileName);
                continue;
            }

            var destPath = Path.Combine(_uploadedService.UploadDirectory, entry.FileName);
            try
            {
                if (!File.Exists(destPath))
                {
                    File.Copy(srcPath, destPath, overwrite: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to copy legacy avatar {Src} to {Dest}", srcPath, destPath);
                continue;
            }

            string sha;
            try
            {
                using var fs = File.OpenRead(destPath);
                sha = Convert.ToHexString(SHA256.HashData(fs)).ToLowerInvariant();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not hash migrated file {Path}; using empty checksum", destPath);
                sha = string.Empty;
            }

            migrated.Add(new UploadedAvatar
            {
                Id = entry.Id,
                FileName = entry.FileName,
                Sha256 = sha,
                DisplayName = string.IsNullOrEmpty(entry.Name) ? Path.GetFileNameWithoutExtension(entry.FileName) : entry.Name,
                DateAdded = entry.DateAdded == default ? DateTime.UtcNow : entry.DateAdded,
            });
        }

        return migrated;
    }

    private static List<UserAvatarMapping> MigrateUserMappings(IList<LegacyUserAvatarMapping> legacyMappings)
    {
        return legacyMappings
            .Where(m => !string.IsNullOrEmpty(m.UserId) && !string.IsNullOrEmpty(m.AvatarId))
            .Select(m => new UserAvatarMapping
            {
                UserId = m.UserId,
                AvatarId = m.AvatarId,
                Kind = AvatarKind.Uploaded,
            })
            .ToList();
    }

    /// <summary>
    /// XML-serializable shape that mirrors cedev-1/GetAvatar v1.5.x's persisted config.
    /// Public to satisfy <see cref="XmlSerializer"/>; never referenced outside the migration.
    /// </summary>
    [XmlRoot("PluginConfiguration")]
    public class LegacyConfig
    {
        /// <summary>Gets or sets the legacy avatar pool list.</summary>
        public List<LegacyAvatarInfo>? AvailableAvatars { get; set; }

        /// <summary>Gets or sets the legacy user-avatar mapping list.</summary>
        public List<LegacyUserAvatarMapping>? UserAvatars { get; set; }
    }

    /// <summary>
    /// XML-serializable counterpart of cedev-1/GetAvatar's <c>AvatarInfo</c>.
    /// </summary>
    public class LegacyAvatarInfo
    {
        /// <summary>Gets or sets the avatar id.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the avatar display name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the on-disk filename in the legacy avatar pool.</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>Gets or sets the upload timestamp.</summary>
        public DateTime DateAdded { get; set; }
    }

    /// <summary>
    /// XML-serializable counterpart of cedev-1/GetAvatar's <c>UserAvatarMapping</c>.
    /// </summary>
    public class LegacyUserAvatarMapping
    {
        /// <summary>Gets or sets the user id.</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>Gets or sets the avatar id (refers to a <see cref="LegacyAvatarInfo.Id"/>).</summary>
        public string AvatarId { get; set; } = string.Empty;
    }
}
