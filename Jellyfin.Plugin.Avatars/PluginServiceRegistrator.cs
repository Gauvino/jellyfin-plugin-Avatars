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
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<AvatarService>();
            serviceCollection.AddSingleton<IStartupFilter, ScriptInjectorStartup>();
            serviceCollection.AddHostedService<AvatarValidationService>();
        }
    }
}
