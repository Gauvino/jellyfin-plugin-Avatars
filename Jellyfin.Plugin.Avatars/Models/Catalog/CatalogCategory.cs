namespace Jellyfin.Plugin.Avatars.Models.Catalog;

/// <summary>
/// One category surfaced in the avatar picker tab strip.
/// Matches the <c>categories[]</c> entries in <c>index.json</c>.
/// </summary>
public class CatalogCategory
{
    /// <summary>Gets or sets the slug-style id (lowercase, hyphenated).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the user-facing label.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the Material-icons name used in the tab strip.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Gets or sets the sort order; lower values render first.</summary>
    public int SortOrder { get; set; }
}
