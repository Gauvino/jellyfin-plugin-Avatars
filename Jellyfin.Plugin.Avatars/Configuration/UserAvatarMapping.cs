using Jellyfin.Plugin.Avatars.Models;

namespace Jellyfin.Plugin.Avatars.Configuration
{
    /// <summary>
    /// Represents a user's avatar selection.
    /// </summary>
    public class UserAvatarMapping
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the avatar source kind. Defaults to <see cref="AvatarKind.Uploaded"/>
        /// so legacy mappings (pre-v2) deserialize as uploaded avatars.
        /// </summary>
        public AvatarKind Kind { get; set; } = AvatarKind.Uploaded;

        /// <summary>
        /// Gets or sets the avatar ID. Refers to <c>UploadedAvatar.Id</c>, a built-in
        /// catalog id, or an imported-collection avatar id depending on <see cref="Kind"/>.
        /// </summary>
        public string AvatarId { get; set; } = string.Empty;
    }
}
