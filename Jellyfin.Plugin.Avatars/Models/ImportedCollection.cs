using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Avatars.Models;

/// <summary>
/// Metadata for a collection of avatars pulled in from an external source via the
/// admin Collection Importer. Persisted in <c>PluginConfiguration.ImportedCollections</c>.
/// </summary>
public class ImportedCollection
{
    /// <summary>Gets or sets the stable identifier (GUID).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the free-text label chosen by the admin at import time.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider used when fetching this collection.</summary>
    public CollectionSourceType SourceType { get; set; }

    /// <summary>Gets or sets the canonicalised source URL (whitelist-validated <c>https://</c>).</summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the catalog category id under which this collection's avatars are listed.</summary>
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp of the initial import.</summary>
    public DateTime ImportedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the last successful sync (initial import counts).</summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>Gets or sets the HTTP <c>ETag</c> header from the last successful fetch, used to short-circuit syncs.</summary>
    public string? LastSyncEtag { get; set; }

    /// <summary>Gets or sets the last commit SHA, populated only when <see cref="SourceType"/> is <see cref="CollectionSourceType.GitHubRepo"/>.</summary>
    public string? LastSyncCommitSha { get; set; }

    /// <summary>Gets or sets a value indicating whether the validation hosted service auto-syncs this collection at startup.</summary>
    public bool AutoSync { get; set; }

    /// <summary>Gets or sets a value indicating whether the collection's avatars are hidden from users without removing them from disk.</summary>
    public bool Hidden { get; set; }

    /// <summary>Gets or sets the free-text licensing notice the admin captured at import time, surfaced in the admin UI for transparency.</summary>
    public string LicenseNotice { get; set; } = string.Empty;

    /// <summary>Gets or sets the IDs (matching <c>CatalogAvatar.Id</c>) of the avatars this collection contributed to the catalog.</summary>
    public List<string> AvatarIds { get; set; } = new();

    /// <summary>Gets or sets the total disk size of the extracted collection in bytes.</summary>
    public long TotalSizeBytes { get; set; }
}
