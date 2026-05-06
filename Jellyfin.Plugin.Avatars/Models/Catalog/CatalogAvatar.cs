namespace Jellyfin.Plugin.Avatars.Models.Catalog;

/// <summary>
/// One avatar in the embedded built-in catalog.
/// Matches the <c>avatars[]</c> entries in <c>index.json</c>.
/// </summary>
public class CatalogAvatar
{
    /// <summary>Gets or sets the avatar id, namespaced as <c>{categoryId}/{slug}</c>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the category this avatar belongs to.</summary>
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>Gets or sets the user-facing label.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the path inside the extracted catalog directory (forward slashes).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the license identifier or free-text notice.</summary>
    public string License { get; set; } = string.Empty;

    /// <summary>Gets or sets the original creator/curator credit.</summary>
    public string Credit { get; set; } = string.Empty;
}
