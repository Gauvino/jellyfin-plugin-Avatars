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
    /// Registration order matters: <see cref="UploadedAvatarService"/> must be registered before
    /// <see cref="UserAvatarService"/> since the latter depends on the former. The legacy
    /// <c>AvatarService</c> remains side-by-side until the controller refactor (chunk #5)
    /// retires it; the legacy startup filter and validation service likewise stay until
    /// their replacements are wired in.
    /// </remarks>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            // v2 services
            serviceCollection.AddSingleton<UploadedAvatarService>();
            serviceCollection.AddSingleton<UserAvatarService>();

            // Legacy (will be removed after controller refactor)
            serviceCollection.AddSingleton<AvatarService>();

            // Web injection + startup hosted services
            serviceCollection.AddSingleton<IStartupFilter, ScriptInjectorStartup>();
            serviceCollection.AddHostedService<AvatarValidationService>();
        }
    }
}
