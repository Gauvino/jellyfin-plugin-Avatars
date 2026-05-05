using System;

namespace Jellyfin.Plugin.Avatars.Models;

/// <summary>
/// An avatar uploaded by an admin to the local plugin pool.
/// Successor to <c>Configuration.AvatarInfo</c> with content-hash for deduplication.
/// </summary>
public class UploadedAvatar
{
    /// <summary>Gets or sets the stable identifier (GUID) used in the per-user mapping.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the on-disk filename inside the upload pool (relative).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the hex-encoded SHA-256 of the file body, used to detect duplicate uploads.</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name shown in the admin and user UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp the file was added to the pool.</summary>
    public DateTime DateAdded { get; set; }
}
