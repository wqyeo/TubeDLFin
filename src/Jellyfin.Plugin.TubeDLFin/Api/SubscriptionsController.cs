using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeDLFin.Detection;
using Jellyfin.Plugin.TubeDLFin.Subscriptions;
using Jellyfin.Plugin.TubeDLFin.Tools;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TubeDLFin.Api;

/// <summary>
/// Subscriptions to channels and playlists. Administrators only.
/// </summary>
[ApiController]
[Route(PluginIdentity.RoutePrefix + "/Subscriptions")]
[Authorize(Policy = Policies.RequiresElevation)]
[Produces(MediaTypeNames.Application.Json)]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionsController"/> class.
    /// </summary>
    /// <param name="subscriptions">The subscriptions.</param>
    public SubscriptionsController(ISubscriptionService subscriptions)
    {
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
    }

    /// <summary>
    /// Lists the subscriptions by name.
    /// </summary>
    /// <param name="cancellationToken">The request's cancellation token.</param>
    /// <response code="200">The subscriptions.</response>
    /// <returns>Every subscription.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionInfo>>> GetSubscriptions(CancellationToken cancellationToken) =>
        Ok(await _subscriptions.GetAllAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Subscribes to a channel or playlist. Its current videos are listed and won't be downloaded;
    /// later uploads will.
    /// </summary>
    /// <param name="request">The link, name, interval and download options.</param>
    /// <param name="cancellationToken">The request's cancellation token. Closing the page stops yt-dlp.</param>
    /// <response code="201">The new subscription.</response>
    /// <response code="400">A value is invalid, or this is already subscribed to; the reasons are in <c>detail</c>.</response>
    /// <response code="422">yt-dlp refused the link or it isn't a list of videos; the reason is in <c>detail</c>.</response>
    /// <response code="502">yt-dlp's output could not be read.</response>
    /// <response code="503">yt-dlp is not installed or not working.</response>
    /// <response code="504">yt-dlp did not finish in time.</response>
    /// <returns>The subscription.</returns>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<SubscriptionInfo>> AddSubscription([FromBody] SubscriptionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var result = await _subscriptions.AddAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.Subscription is not { } subscription)
            {
                return Problem(string.Join(" ", result.Errors), statusCode: StatusCodes.Status400BadRequest);
            }

            return Created(new Uri($"{PluginIdentity.RoutePrefix}/Subscriptions/{subscription.Id:N}", UriKind.Relative), subscription);
        }
        catch (ToolNotReadyException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (ProbeFailedException ex)
        {
            return Problem(ex.Message, statusCode: ProbeFailureStatus.For(ex.Reason));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(ProbeFailureStatus.ClientClosedRequest);
        }
    }

    /// <summary>
    /// Changes a subscription's name, check interval or download options.
    /// </summary>
    /// <param name="id">The subscription's id.</param>
    /// <param name="update">The values to change; others are kept.</param>
    /// <param name="cancellationToken">The request's cancellation token.</param>
    /// <response code="200">The subscription after the change.</response>
    /// <response code="400">A value is invalid; the reasons are in <c>detail</c>.</response>
    /// <response code="404">There is no subscription with that id.</response>
    /// <returns>The subscription.</returns>
    [HttpPut("{id}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionInfo>> UpdateSubscription([FromRoute] Guid id, [FromBody] SubscriptionUpdate update, CancellationToken cancellationToken) =>
        Respond(await _subscriptions.UpdateAsync(id, update, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Deletes a subscription. Downloaded files and queued downloads are kept.
    /// </summary>
    /// <param name="id">The subscription's id.</param>
    /// <param name="cancellationToken">The request's cancellation token.</param>
    /// <response code="204">Deleted.</response>
    /// <response code="404">There is no subscription with that id.</response>
    /// <returns>Nothing.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteSubscription([FromRoute] Guid id, CancellationToken cancellationToken) =>
        await _subscriptions.DeleteAsync(id, cancellationToken).ConfigureAwait(false) ? NoContent() : NotFound();

    /// <summary>
    /// Checks a subscription now and queues its new uploads. Problems are reported in the
    /// subscription's <c>LastError</c>, not as an error response.
    /// </summary>
    /// <param name="id">The subscription's id.</param>
    /// <param name="cancellationToken">The request's cancellation token.</param>
    /// <response code="200">The subscription after the check.</response>
    /// <response code="404">There is no subscription with that id.</response>
    /// <returns>The subscription.</returns>
    [HttpPost("{id}/Check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionInfo>> CheckSubscription([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await _subscriptions.CheckNowAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Pauses a subscription's checks.
    /// </summary>
    /// <param name="id">The subscription's id.</param>
    /// <param name="cancellationToken">The request's cancellation token.</param>
    /// <response code="200">The subscription after the change.</response>
    /// <response code="404">There is no subscription with that id.</response>
    /// <returns>The subscription.</returns>
    [HttpPost("{id}/Pause")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionInfo>> PauseSubscription([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await _subscriptions.SetPausedAsync(id, paused: true, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Resumes a subscription's checks. If a check was missed while paused, it runs on the
    /// scheduled task's next run.
    /// </summary>
    /// <param name="id">The subscription's id.</param>
    /// <param name="cancellationToken">The request's cancellation token.</param>
    /// <response code="200">The subscription after the change.</response>
    /// <response code="404">There is no subscription with that id.</response>
    /// <returns>The subscription.</returns>
    [HttpPost("{id}/Resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionInfo>> ResumeSubscription([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await _subscriptions.SetPausedAsync(id, paused: false, cancellationToken).ConfigureAwait(false));

    private ActionResult<SubscriptionInfo> Respond(SubscriptionResult result) => result switch
    {
        { Subscription: { } subscription } => Ok(subscription),
        { IsNotFound: true } => NotFound(),
        _ => Problem(string.Join(" ", result.Errors), statusCode: StatusCodes.Status400BadRequest),
    };
}
