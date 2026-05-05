using System.Collections.Generic;
using Jellyfin.Plugin.Avatars.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Avatars.Configuration
{
    /// <summary>
    /// Represents the configuration settings for the Avatars plugin.
    /// </summary>
    /// <remarks>
    /// Schema layout:
    /// <list type="bullet">
    ///   <item><description>v1 (cedev-1/GetAvatar): only <see cref="AvailableAvatars"/> + <see cref="UserAvatars"/>.</description></item>
    ///   <item><description>v2-v3 (this plugin): introduces <see cref="UploadedAvatars"/>, <see cref="ImportedCollections"/>, <see cref="DisabledBuiltInIds"/>, <see cref="CatalogVersion"/>, <see cref="SchemaVersion"/>.</description></item>
    /// </list>
    /// <para><see cref="AvailableAvatars"/> is preserved during the v1->v3 migration window so the
    /// <c>LegacyMigrationHostedService</c> can read it without an XML compatibility shim.</para>
    /// </remarks>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            AvailableAvatars = new List<AvatarInfo>();
            UploadedAvatars = new List<UploadedAvatar>();
            ImportedCollections = new List<ImportedCollection>();
            UserAvatars = new List<UserAvatarMapping>();
            DisabledBuiltInIds = new List<string>();
            CatalogVersion = string.Empty;
        }

        /// <summary>
        /// Gets or sets the persisted schema version. <c>0</c> = legacy GetAvatar XML,
        /// <c>3</c> = current. Bumped by the migration hosted service.
        /// </summary>
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets the version of the embedded built-in catalog last extracted to disk.
        /// Used by <c>BuiltInCatalogService</c> to detect when the shipped catalog was upgraded
        /// and the on-disk extraction needs refreshing.
        /// </summary>
        public string CatalogVersion { get; set; }

        /// <summary>
        /// Gets or sets the legacy v1 avatar list. Migrated into <see cref="UploadedAvatars"/>
        /// by the migration service then left in place for safety.
        /// </summary>
        public List<AvatarInfo> AvailableAvatars { get; set; }

        /// <summary>
        /// Gets or sets the avatars uploaded via the admin dashboard (v2+ replacement for
        /// <see cref="AvailableAvatars"/>).
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
