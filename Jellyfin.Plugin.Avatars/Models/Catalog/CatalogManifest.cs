using System.Collections.Generic;

namespace Jellyfin.Plugin.Avatars.Models.Catalog;

/// <summary>
/// Root document of the embedded built-in catalog.
/// Deserialized at startup by <c>BuiltInCatalogService</c> from <c>index.json</c>.
/// </summary>
public class CatalogManifest
{
    /// <summary>Gets or sets the catalog version (semver-style). Bumped each time the bundled set changes.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the upstream source description.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets the list of categories in display order.</summary>
    public List<CatalogCategory> Categories { get; } = new();

    /// <summary>Gets the flat list of avatars (each carries its own <c>CategoryId</c>).</summary>
    public List<CatalogAvatar> Avatars { get; } = new();
}
