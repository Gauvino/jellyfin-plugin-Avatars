# Attributions

## Code

This plugin is a fork of **[cedev-1/jellyfin-plugin-GetAvatar](https://github.com/cedev-1/jellyfin-plugin-GetAvatar)** (MIT). The original author's commits are preserved in the git history.

The category-selector UX is inspired by **[kalibrado/jf-avatars](https://github.com/kalibrado/jf-avatars)** (MIT).

The C# project structure and CI/CD pipeline mirror **[intro-skipper/intro-skipper](https://github.com/intro-skipper/intro-skipper)** (GPL-3.0, code patterns only — no GPL code is included).

## Default avatar catalog

The default built-in avatar collection embedded in `Resources/avatars-builtin.zip` is sourced from **[kalibrado/jf-avatars-images](https://github.com/kalibrado/jf-avatars-images)** at the snapshot referenced in this commit.

The kalibrado/jf-avatars-images repository does not declare an explicit license at the time of vendoring. We credit kalibrado as the curator and acknowledge that individual images therein originate from various third-party sources whose rights remain with their creators. We use these images in good faith for non-commercial display in self-hosted Jellyfin instances. **Any image owner who wishes their work removed should open an issue** — we will remove and re-release promptly.

## Imported collections (runtime)

When admins use the **Collections importer** to add external collections, the responsibility for respecting source licenses falls on the admin. The plugin records the `LicenseNotice` field provided at import time and surfaces it in the admin UI for transparency.
