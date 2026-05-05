# Jellyfin Plugin — Avatars

Pick an avatar from a built-in categorized gallery, upload your own, or import collections from external URLs. A modern fork of [cedev-1/jellyfin-plugin-GetAvatar](https://github.com/cedev-1/jellyfin-plugin-GetAvatar) extended with the [kalibrado/jf-avatars](https://github.com/kalibrado/jf-avatars) catalog and a category UX.

![banner](./assets/banner.png)

## Features

- **Built-in catalog** — embedded in the plugin DLL, ~300 avatars across multiple categories (animals, anime, gaming, nature, abstract, etc.). No internet required at runtime.
- **Upload custom avatars** — admins can add their own from the dashboard.
- **Import external collections** — admins can pull in additional avatars from a ZIP URL, a GitHub repo (`owner/name#branch:path`), or a manifest JSON. Auto-sync optional.
- **Per-user picker** with category tabs, search, and grid view. Selection persists across sessions.
- **Migration from GetAvatar** — if you previously used `cedev-1/jellyfin-plugin-GetAvatar`, your uploaded avatars and user mappings are migrated automatically on first start.

## Installation

1. Open your Jellyfin Dashboard -> **Plugins** -> **Catalog** -> Settings
2. Add this manifest URL:

   ```
   https://gauvino.github.io/jellyfin-plugin-Avatars/manifest.json
   ```

3. Install **Avatars** from the catalog. Restart Jellyfin.

## Configuration

### Admin (Dashboard -> Plugins -> Avatars)

- **Catalog tab** — enable/disable individual categories or specific built-in avatars.
- **Uploads tab** — drop PNG/JPG/WebP files to add to the pool.
- **Collections tab** — add external collections by URL with attribution.

### User (User Settings -> Avatar)

- Browse categories, search, click an avatar to apply.
- "Remove avatar" button restores the default.

## Built-in catalog credit

The default avatar collection ships from [kalibrado/jf-avatars-images](https://github.com/kalibrado/jf-avatars-images) — see [`ATTRIBUTIONS.md`](./ATTRIBUTIONS.md) for full credit. Image rights belong to their original creators.

## Development

```bash
dotnet build Jellyfin.Plugin.Avatars/Jellyfin.Plugin.Avatars.csproj -c Release
# Output: Jellyfin.Plugin.Avatars/bin/Release/net9.0/Jellyfin.Plugin.Avatars.dll
```

## License

[MIT](./LICENSE) — preserves the original cedev-1/GetAvatar license.
