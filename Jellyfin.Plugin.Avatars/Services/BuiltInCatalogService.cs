using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Jellyfin.Plugin.Avatars.Models.Catalog;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Avatars.Services;

/// <summary>
/// Owns the embedded built-in avatar catalog. The catalog ships as a zip
/// resource inside the .dll; this service extracts it lazily to
/// <c>{DataPath}/avatars-builtin/</c> on first access (or whenever the bundled
/// version differs from <c>PluginConfiguration.CatalogVersion</c>) and exposes
/// in-memory accessors that mirror the on-disk layout.
/// </summary>
public class BuiltInCatalogService
{
    private const string EmbeddedResourceName = "Jellyfin.Plugin.Avatars.Resources.avatars-builtin.zip";
    private const string IndexFileName = "index.json";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IApplicationPaths _appPaths;
    private readonly ILogger<BuiltInCatalogService> _logger;
    private readonly string _extractDir;
    private readonly object _initLock = new();

    private CatalogManifest? _manifest;
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuiltInCatalogService"/> class.
    /// </summary>
    /// <param name="appPaths">The Jellyfin application paths.</param>
    /// <param name="logger">The logger.</param>
    public BuiltInCatalogService(IApplicationPaths appPaths, ILogger<BuiltInCatalogService> logger)
    {
        _appPaths = appPaths;
        _logger = logger;
        _extractDir = Path.Combine(_appPaths.DataPath, "avatars-builtin");
    }

    /// <summary>
    /// Gets the directory where the catalog zip is extracted.
    /// </summary>
    public string ExtractDirectory => _extractDir;

    /// <summary>
    /// Returns the categories declared by the bundled catalog, ordered by <c>SortOrder</c>.
    /// </summary>
    /// <returns>A read-only snapshot of the category list.</returns>
    public IReadOnlyList<CatalogCategory> GetCategories()
    {
        EnsureInitialized();
        return _manifest is null
            ? Array.Empty<CatalogCategory>()
            : _manifest.Categories.OrderBy(c => c.SortOrder).ToArray();
    }

    /// <summary>
    /// Returns the avatars in a category, or all built-in avatars when <paramref name="categoryId"/> is <c>null</c>.
    /// </summary>
    /// <param name="categoryId">Optional category id filter.</param>
    /// <returns>A snapshot of matching avatars.</returns>
    public IReadOnlyList<CatalogAvatar> GetAvatars(string? categoryId = null)
    {
        EnsureInitialized();
        if (_manifest is null)
        {
            return Array.Empty<CatalogAvatar>();
        }

        if (string.IsNullOrEmpty(categoryId))
        {
            return _manifest.Avatars.ToArray();
        }

        return _manifest.Avatars
            .Where(a => string.Equals(a.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Resolves the on-disk path of a built-in avatar id.
    /// </summary>
    /// <param name="avatarId">The avatar id (matches <c>CatalogAvatar.Id</c>).</param>
    /// <returns>The absolute file path, or <c>null</c> if not found.</returns>
    public string? TryGetPath(string avatarId)
    {
        EnsureInitialized();
        if (_manifest is null)
        {
            return null;
        }

        var entry = _manifest.Avatars.FirstOrDefault(a =>
            string.Equals(a.Id, avatarId, StringComparison.Ordinal));
        if (entry is null)
        {
            return null;
        }

        var path = Path.Combine(_extractDir, entry.FileName.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path) ? path : null;
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_initLock)
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                _manifest = ExtractIfNeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize built-in catalog");
                _manifest = null;
            }
            finally
            {
                _initialized = true;
            }
        }
    }

    private CatalogManifest? ExtractIfNeeded()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var resource = assembly.GetManifestResourceStream(EmbeddedResourceName);
        if (resource is null)
        {
            _logger.LogWarning("Embedded catalog resource not found: {Resource}", EmbeddedResourceName);
            return null;
        }

        // Buffer the resource so we can rewind it twice (peek index.json, then extract).
        using var buffered = new MemoryStream();
        resource.CopyTo(buffered);

        var shippedVersion = PeekShippedVersion(buffered);
        if (string.IsNullOrEmpty(shippedVersion))
        {
            _logger.LogWarning("Embedded catalog has no usable index.json version");
            return null;
        }

        var configured = Plugin.Instance?.Configuration.CatalogVersion ?? string.Empty;
        var indexPath = Path.Combine(_extractDir, IndexFileName);
        var alreadyExtracted = File.Exists(indexPath)
            && string.Equals(configured, shippedVersion, StringComparison.Ordinal);

        if (!alreadyExtracted)
        {
            _logger.LogInformation(
                "Extracting built-in catalog v{Shipped} to {Dir} (was v{Configured})",
                shippedVersion,
                _extractDir,
                string.IsNullOrEmpty(configured) ? "<none>" : configured);

            ExtractTo(_extractDir, buffered);

            if (Plugin.Instance is { } plugin)
            {
                plugin.Configuration.CatalogVersion = shippedVersion;
                plugin.SaveConfiguration();
            }
        }

        return ReadManifestFromDisk(indexPath);
    }

    private static string PeekShippedVersion(MemoryStream zipBuffer)
    {
        zipBuffer.Position = 0;
        using var archive = new ZipArchive(zipBuffer, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry(IndexFileName);
        if (entry is null)
        {
            return string.Empty;
        }

        using var entryStream = entry.Open();
        var doc = JsonSerializer.Deserialize<JsonDocument>(entryStream, _jsonOptions);
        if (doc is null)
        {
            return string.Empty;
        }

        return doc.RootElement.TryGetProperty("version", out var versionEl)
            && versionEl.ValueKind == JsonValueKind.String
                ? versionEl.GetString() ?? string.Empty
                : string.Empty;
    }

    private static void ExtractTo(string targetDir, MemoryStream zipBuffer)
    {
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, recursive: true);
        }

        Directory.CreateDirectory(targetDir);
        zipBuffer.Position = 0;

        using var archive = new ZipArchive(zipBuffer, ZipArchiveMode.Read, leaveOpen: true);
        foreach (var entry in archive.Entries)
        {
            // Sanitize: refuse path traversal attempts.
            var entryFullName = entry.FullName.Replace('\\', '/');
            if (entryFullName.Contains("..", StringComparison.Ordinal))
            {
                continue;
            }

            var destPath = Path.GetFullPath(Path.Combine(targetDir, entryFullName));
            if (!destPath.StartsWith(Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Skip directory-only entries.
            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            using var entryStream = entry.Open();
            using var outStream = File.Create(destPath);
            entryStream.CopyTo(outStream);
        }
    }

    private static CatalogManifest? ReadManifestFromDisk(string indexPath)
    {
        if (!File.Exists(indexPath))
        {
            return null;
        }

        using var stream = File.OpenRead(indexPath);
        return JsonSerializer.Deserialize<CatalogManifest>(stream, _jsonOptions);
    }
}
