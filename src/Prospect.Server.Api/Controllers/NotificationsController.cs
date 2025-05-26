using System.Net.Mime;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Hubs;

namespace Prospect.Server.Api.Controllers;

public class NotifyRequest
{
    [JsonPropertyName("type")]
    public string MessageType { get; set; }

    [JsonPropertyName("data")]
    public object MessageData { get; set; }
}

public class ServerReadyMessageData
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("sessionId")]
    public string SessionID { get; set; }
    [JsonPropertyName("squadId")]
    public string SquadID { get; set; }
}


[Route("Notifications")]
[ApiController]
public class NotificationsController : Controller
{
    private readonly IHubContext<CycleHub> _hubContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(ILogger<NotificationsController> logger, IHubContext<CycleHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    [HttpPost("TestNotify")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<IActionResult> TestNotify(NotifyRequest request)
    {
        // TODO: To test Notifications_DT
        _logger.LogDebug("Received test notification event {MessageType}", request.MessageType);
        await _hubContext.Clients.All.SendAsync(request.MessageType, request.MessageData);

        return StatusCode(200);
    }

    [HttpPost("ServerReady")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<IActionResult> ServerReady(ServerReadyMessageData request)
    {
        _logger.LogDebug("Received test notification event OnSquadMatchmakingSuccess");
        await _hubContext.Clients.All.SendAsync("OnSquadMatchmakingSuccess", request);

        return StatusCode(200);
    }
}