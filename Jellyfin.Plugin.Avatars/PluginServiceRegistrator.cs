using Jellyfin.Plugin.Avatars.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.Avatars
{
    /// <summary>
    /// Service registrator for the Avatars plugin.
    /// </summary>
    /// <remarks>
    /// Registration order is significant: <see cref="UploadedAvatarService"/> must be registered before
    /// <see cref="UserAvatarService"/> (the latter depends on the former), and the
    /// <see cref="AvatarValidationService"/> hosted service must come after both. The
    /// future <c>LegacyMigrationHostedService</c> (chunk #7) will be registered before
    /// <see cref="AvatarValidationService"/> so the migration runs first on cold start.
    /// </remarks>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<BuiltInCatalogService>();
            serviceCollection.AddSingleton<UploadedAvatarService>();
            serviceCollection.AddSingleton<UserAvatarService>();

            serviceCollection.AddSingleton<IStartupFilter, ScriptInjectorStartup>();
            serviceCollection.AddHostedService<AvatarValidationService>();
        }
    }
}
