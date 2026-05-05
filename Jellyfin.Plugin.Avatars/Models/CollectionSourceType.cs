namespace Jellyfin.Plugin.Avatars.Models;

/// <summary>
/// Supported source types for the admin Collection Importer.
/// </summary>
public enum CollectionSourceType
{
    /// <summary>Direct ZIP archive download from a URL.</summary>
    ZipUrl,

    /// <summary>GitHub repository in <c>owner/name[#branch][:path]</c> form.</summary>
    GitHubRepo,

    /// <summary>JSON manifest URL listing remote image URLs (jf-avatars schema).</summary>
    ManifestUrl,
}
