using System.Collections.Generic;
using Jellyfin.Plugin.Avatars.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Avatars.Configuration
{
    /// <summary>
    /// Persisted plugin configuration.
    /// </summary>
    /// <remarks>
    /// <para>Schema progression:</para>
    /// <list type="bullet">
    ///   <item><description>v0/v1 (legacy GetAvatar): <c>AvailableAvatars</c> + <c>UserAvatars</c>. Read by <c>LegacyMigrationHostedService</c> via its own ad-hoc XML DTOs — never deserialized into <see cref="PluginConfiguration"/>.</description></item>
    ///   <item><description>v3 (current): <see cref="UploadedAvatars"/>, <see cref="ImportedCollections"/>, <see cref="DisabledBuiltInIds"/>, <see cref="CatalogVersion"/>, <see cref="SchemaVersion"/>.</description></item>
    /// </list>
    /// </remarks>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            UploadedAvatars = new List<UploadedAvatar>();
            ImportedCollections = new List<ImportedCollection>();
            UserAvatars = new List<UserAvatarMapping>();
            DisabledBuiltInIds = new List<string>();
            CatalogVersion = string.Empty;
        }

        /// <summary>
        /// Gets or sets the persisted schema version. <c>0</c> = legacy GetAvatar XML on disk
        /// awaiting migration, <c>3</c> = current. Bumped by <c>LegacyMigrationHostedService</c>.
        /// </summary>
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets the version of the embedded built-in catalog last extracted to disk.
        /// Used by <c>BuiltInCatalogService</c> to detect when the shipped catalog was upgraded
        /// and the on-disk extraction needs refreshing.
        /// </summary>
        public string CatalogVersion { get; set; }

        /// <summary>
        /// Gets or sets the avatars uploaded via the admin dashboard.
        /// </summary>
        public List<UploadedAvatar> UploadedAvatars { get; set; }

        /// <summary>
        /// Gets or sets the collections imported from external URLs (ZIP / GitHub / manifest).
        /// </summary>
        public List<ImportedCollection> ImportedCollections { get; set; }

        /// <summary>
        /// Gets or sets the list of user avatar selections.
        /// </summary>
        public List<UserAvatarMapping> UserAvatars { get; set; }

        /// <summary>
        /// Gets or sets the IDs of built-in catalog entries the admin has chosen to hide
        /// from users (matches <c>CatalogAvatar.Id</c> in the embedded index).
        /// </summary>
        public List<string> DisabledBuiltInIds { get; set; }
    }
}
