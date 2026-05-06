using Jellyfin.Plugin.Avatars.Models;
using Xunit;

namespace Jellyfin.Plugin.Avatars.Tests;

public class AvatarKindTests
{
    [Theory]
    [InlineData(AvatarKind.BuiltIn, "BuiltIn")]
    [InlineData(AvatarKind.Uploaded, "Uploaded")]
    [InlineData(AvatarKind.Imported, "Imported")]
    public void Stable_string_round_trip(AvatarKind kind, string expected)
    {
        Assert.Equal(expected, kind.ToString());
        Assert.True(System.Enum.TryParse<AvatarKind>(expected, out var parsed));
        Assert.Equal(kind, parsed);
    }

    [Fact]
    public void Default_user_mapping_is_uploaded()
    {
        var mapping = new Configuration.UserAvatarMapping();
        Assert.Equal(AvatarKind.Uploaded, mapping.Kind);
    }
}
