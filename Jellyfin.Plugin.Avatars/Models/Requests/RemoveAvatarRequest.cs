namespace Jellyfin.Plugin.Avatars.Models.Requests;

/// <summary>
/// Request body for <c>POST /Avatars/User/Remove</c>.
/// </summary>
public class RemoveAvatarRequest
{
    /// <summary>Gets or sets the user id whose avatar should be cleared.</summary>
    public string UserId { get; set; } = string.Empty;
}
