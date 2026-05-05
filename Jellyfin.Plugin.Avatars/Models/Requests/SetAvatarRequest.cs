namespace Jellyfin.Plugin.Avatars.Models.Requests;

/// <summary>
/// Request body for <c>POST /Avatars/User/Set</c>.
/// </summary>
public class SetAvatarRequest
{
    /// <summary>
    /// Gets or sets the avatar source kind. Defaults to <see cref="AvatarKind.Uploaded"/>
    /// for backward compatibility with v1 client scripts that did not send a kind.
    /// </summary>
    public AvatarKind Kind { get; set; } = AvatarKind.Uploaded;

    /// <summary>
    /// Gets or sets the avatar id within the chosen <see cref="Kind"/>.
    /// </summary>
    public string AvatarId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target user id (optional). When omitted, the call defaults
    /// to the authenticated caller. Setting this requires admin elevation.
    /// </summary>
    public string? UserId { get; set; }
}
