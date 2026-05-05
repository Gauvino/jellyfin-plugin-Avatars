using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Avatars.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Avatars
{
    /// <summary>
    /// Hosted service that re-applies missing user avatars and prunes orphan profile images at startup.
    /// </summary>
    public class AvatarValidationService : IHostedService
    {
        private readonly UserAvatarService _userAvatars;
        private readonly ILogger<AvatarValidationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarValidationService"/> class.
        /// </summary>
        /// <param name="userAvatars">The per-user avatar service.</param>
        /// <param name="logger">The logger.</param>
        public AvatarValidationService(UserAvatarService userAvatars, ILogger<AvatarValidationService> logger)
        {
            _userAvatars = userAvatars;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Avatars validation service starting...");

                // Tiny grace period so the rest of Jellyfin's services are wired up
                // before we touch user records.
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);

                var repaired = await _userAvatars.ValidateAsync().ConfigureAwait(false);
                _logger.LogInformation("Avatar validation completed. Repaired {Count} mapping(s).", repaired);

                var deleted = _userAvatars.CleanOrphans();
                if (deleted > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} orphaned profile image(s).", deleted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during avatar validation at startup");
            }
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Avatars validation service stopping...");
            return Task.CompletedTask;
        }
    }
}
