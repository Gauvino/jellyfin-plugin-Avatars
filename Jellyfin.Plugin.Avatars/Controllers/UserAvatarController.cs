using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.Avatars.Models;
using Jellyfin.Plugin.Avatars.Models.Requests;
using Jellyfin.Plugin.Avatars.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Avatars.Controllers;

/// <summary>
/// Endpoints that mutate or read a single user's avatar selection.
/// </summary>
[ApiController]
[Route("Avatars/User")]
[Authorize]
public class UserAvatarController : ControllerBase
{
    private readonly UserAvatarService _userAvatars;
    private readonly IUserManager _userManager;
    private readonly ILogger<UserAvatarController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAvatarController"/> class.
    /// </summary>
    /// <param name="userAvatars">The per-user avatar service.</param>
    /// <param name="userManager">The Jellyfin user manager.</param>
    /// <param name="logger">The logger.</param>
    public UserAvatarController(
        UserAvatarService userAvatars,
        IUserManager userManager,
        ILogger<UserAvatarController> logger)
    {
        _userAvatars = userAvatars;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Sets the avatar for the calling user (or for another user when the caller is admin).
    /// </summary>
    /// <param name="request">The request body.</param>
    /// <returns>An ack JSON object.</returns>
    [HttpPost("Set")]
    public async Task<IActionResult> SetAsync([FromBody] SetAvatarRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.AvatarId))
        {
            return BadRequest("avatarId is required");
        }

        if (!TryResolveCallerId(out var callerId))
        {
            return Unauthorized();
        }

        var targetId = callerId;
        if (!string.IsNullOrEmpty(request.UserId))
        {
            if (!Guid.TryParse(request.UserId, out var parsed))
            {
                return BadRequest("Invalid user id");
            }

            targetId = parsed;
            if (targetId != callerId && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }
        }

        try
        {
            await _userAvatars.SetAsync(targetId, request.Kind, request.AvatarId).ConfigureAwait(false);
            return Ok(new { message = "Avatar set" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set avatar for user {User}", targetId);
            return StatusCode(500, "Failed to set avatar");
        }
    }

    /// <summary>
    /// Clears the avatar for a target user (admin only).
    /// </summary>
    /// <param name="request">The request body.</param>
    /// <returns>An ack JSON object.</returns>
    [HttpPost("Remove")]
    [Authorize(Policy = "RequiresElevation")]
    public async Task<IActionResult> RemoveAsync([FromBody] RemoveAvatarRequest request)
    {
        if (!Guid.TryParse(request?.UserId, out var userId))
        {
            return BadRequest("Invalid user id");
        }

        var ok = await _userAvatars.RemoveAsync(userId).ConfigureAwait(false);
        return ok ? Ok(new { message = "Avatar cleared" }) : NotFound();
    }

    /// <summary>
    /// Gets the current selection for a user, or 404 if none.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>The mapping descriptor.</returns>
    [HttpGet("{userId}")]
    [AllowAnonymous]
    public IActionResult Get(string userId)
    {
        if (!Guid.TryParse(userId, out var parsed))
        {
            return BadRequest("Invalid user id");
        }

        var mapping = _userAvatars.GetMapping(parsed);
        if (mapping is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            userId = mapping.UserId,
            kind = mapping.Kind.ToString(),
            avatarId = mapping.AvatarId,
            url = $"/Avatars/Image/{mapping.Kind}/{mapping.AvatarId}",
        });
    }

    private bool TryResolveCallerId(out Guid userId)
    {
        userId = Guid.Empty;

        var idClaim = User.Claims.FirstOrDefault(c =>
            c.Type == "userId"
            || c.Type == "sub"
            || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (idClaim is not null && Guid.TryParse(idClaim.Value, out var parsed))
        {
            userId = parsed;
            return true;
        }

        var nameClaim = User.Claims.FirstOrDefault(c =>
            c.Type == "name"
            || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        if (nameClaim is null)
        {
            return false;
        }

        var user = _userManager.GetUserByName(nameClaim.Value);
        if (user is null)
        {
            return false;
        }

        userId = user.Id;
        return true;
    }
}
