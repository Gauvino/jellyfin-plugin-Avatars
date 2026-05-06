using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.Avatars.Models.Catalog;
using Xunit;

namespace Jellyfin.Plugin.Avatars.Tests;

public class CatalogManifestTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Deserializes_full_manifest()
    {
        const string json = """
        {
          "version": "1.0.0",
          "source": "kalibrado/jf-avatars-images",
          "categories": [
            { "id": "netflix", "displayName": "Netflix", "icon": "movie", "sortOrder": 30 }
          ],
          "avatars": [
            { "id": "netflix/sample", "categoryId": "netflix", "displayName": "Sample",
              "fileName": "Netflix/sample.webp", "license": "unknown", "credit": "kalibrado" }
          ]
        }
        """;

        var manifest = JsonSerializer.Deserialize<CatalogManifest>(json, _options);

        Assert.NotNull(manifest);
        Assert.Equal("1.0.0", manifest!.Version);
        Assert.Single(manifest.Categories);
        Assert.Equal("netflix", manifest.Categories[0].Id);
        Assert.Equal(30, manifest.Categories[0].SortOrder);
        Assert.Single(manifest.Avatars);
        Assert.Equal("netflix/sample", manifest.Avatars[0].Id);
    }

    [Fact]
    public void Empty_collections_default_to_empty_lists()
    {
        var manifest = new CatalogManifest();
        Assert.Empty(manifest.Categories);
        Assert.Empty(manifest.Avatars);
    }

    [Fact]
    public void Categories_and_avatars_round_trip_through_json()
    {
        var manifest = new CatalogManifest { Version = "2.0.0", Source = "test" };
        manifest.Categories.Add(new CatalogCategory { Id = "a", DisplayName = "A", SortOrder = 1 });
        manifest.Categories.Add(new CatalogCategory { Id = "b", DisplayName = "B", SortOrder = 2 });
        manifest.Avatars.Add(new CatalogAvatar { Id = "a/x", CategoryId = "a", FileName = "A/x.webp" });

        var json = JsonSerializer.Serialize(manifest, _options);
        var copy = JsonSerializer.Deserialize<CatalogManifest>(json, _options);

        Assert.NotNull(copy);
        Assert.Equal("2.0.0", copy!.Version);
        Assert.Equal(2, copy.Categories.Count);
        Assert.Equal("a", copy.Categories.First().Id);
        Assert.Single(copy.Avatars);
    }
}
