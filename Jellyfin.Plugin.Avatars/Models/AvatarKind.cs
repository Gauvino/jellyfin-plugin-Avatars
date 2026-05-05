namespace Jellyfin.Plugin.Avatars.Models;

/// <summary>
/// Source of an avatar tracked by the plugin.
/// </summary>
public enum AvatarKind
{
    /// <summary>Built-in avatar shipped with the plugin DLL.</summary>
    BuiltIn,

    /// <summary>Avatar uploaded by an admin via the dashboard.</summary>
    Uploaded,

    /// <summary>Avatar pulled in from an admin-imported external collection.</summary>
    Imported,
}
