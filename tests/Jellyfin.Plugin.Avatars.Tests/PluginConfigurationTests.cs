using Jellyfin.Plugin.Avatars.Configuration;
using Jellyfin.Plugin.Avatars.Models;
using Xunit;

namespace Jellyfin.Plugin.Avatars.Tests;

public class PluginConfigurationTests
{
    [Fact]
    public void Fresh_config_initializes_empty_collections()
    {
        var cfg = new PluginConfiguration();

        Assert.Equal(0, cfg.SchemaVersion);
        Assert.Equal(string.Empty, cfg.CatalogVersion);
        Assert.NotNull(cfg.UploadedAvatars);
        Assert.Empty(cfg.UploadedAvatars);
        Assert.NotNull(cfg.ImportedCollections);
        Assert.Empty(cfg.ImportedCollections);
        Assert.NotNull(cfg.UserAvatars);
        Assert.Empty(cfg.UserAvatars);
        Assert.NotNull(cfg.DisabledBuiltInIds);
        Assert.Empty(cfg.DisabledBuiltInIds);
    }

    [Fact]
    public void Round_trip_preserves_added_state()
    {
        var cfg = new PluginConfiguration
        {
            SchemaVersion = 3,
            CatalogVersion = "1.0.0",
        };
        cfg.UploadedAvatars.Add(new UploadedAvatar { Id = "u1", FileName = "u1.png", Sha256 = "abc" });
        cfg.UserAvatars.Add(new UserAvatarMapping { UserId = "user-1", Kind = AvatarKind.BuiltIn, AvatarId = "netflix/sample" });
        cfg.DisabledBuiltInIds.Add("xbox-one/foo");

        Assert.Equal(3, cfg.SchemaVersion);
        Assert.Equal("1.0.0", cfg.CatalogVersion);
        Assert.Single(cfg.UploadedAvatars);
        Assert.Equal("u1", cfg.UploadedAvatars[0].Id);
        Assert.Single(cfg.UserAvatars);
        Assert.Equal(AvatarKind.BuiltIn, cfg.UserAvatars[0].Kind);
        Assert.Contains("xbox-one/foo", cfg.DisabledBuiltInIds);
    }
}
