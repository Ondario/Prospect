using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Hubs;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using Prospect.Server.Api.Services.UserData;
using Prospect.Server.Api.Utils;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class OnSquadMatchmakingSuccessMessage
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("sessionId")]
    public string SessionID { get; set; }

    [JsonPropertyName("squadId")]
    public string SquadID { get; set; }
}

[CloudScriptFunction("EnterMatchmakingMatch")]
public class EnterMatchmakingMatchFunction : ICloudScriptFunction<FYEnterMatchAzureFunction, FYEnterMatchmakingResult>
{
    private readonly ILogger<EnterMatchmakingMatchFunction> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHubContext<CycleHub> _hubContext;
    private readonly UserDataService _userDataService;
    private readonly TitleDataService _titleDataService;
    private readonly SquadService _squadService;

    public EnterMatchmakingMatchFunction(
        ILogger<EnterMatchmakingMatchFunction> logger,
        IHubContext<CycleHub> hubContext,
        IHttpContextAccessor httpContextAccessor,
        UserDataService userDataService,
        TitleDataService titleDataService,
        SquadService squadService = null)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _hubContext = hubContext;
        _userDataService = userDataService;
        _titleDataService = titleDataService;
        _squadService = squadService;
    }

    public async Task<FYEnterMatchmakingResult> ExecuteAsync(FYEnterMatchAzureFunction request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            _logger.LogError("[MATCH] No HTTP context available in EnterMatchmakingMatch");
            throw new CloudScriptException("CloudScript was not called within a http request");
        }

        var userId = context.User.FindAuthUserId();

        _logger.LogInformation(
            "[MATCH] EnterMatchmakingMatch called by {UserId} with MapName={MapName}, SquadId={SquadId}",
            userId, request.MapName, request.SquadId ?? "null");

        // Assume the incoming map name is already normalized
        string mapName = request.MapName;

        // Use the normalized map name as the session ID
        string sessionId = mapName;

        // Send SignalR notification - CRITICAL for matchmaking!
        try
        {
            _logger.LogInformation("[MATCH] Sending matchmaking success notification to the requesting client only");

            // Send only to the requesting client
            var connectionId = CycleHub.GetConnectionIdForUser(userId);
            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("OnSquadMatchmakingSuccess",
                    new OnSquadMatchmakingSuccessMessage
                    {
                        Success = true,
                        SessionID = sessionId, // Now the map name
                        SquadID = request.SquadId ?? "_"
                    });
            }
            // Squad logic for future use:
            // If you want to notify all squad members, uncomment and use the following:
            /*
            var squad = await _squadService.GetPlayerSquadAsync(userId);
            if (squad != null)
            {
                foreach (var member in squad.Members)
                {
                    var memberConnectionId = CycleHub.GetConnectionIdForUser(member.UserId);
                    if (!string.IsNullOrEmpty(memberConnectionId))
                    {
                        await _hubContext.Clients.Client(memberConnectionId).SendAsync("OnSquadMatchmakingSuccess", 
                            new OnSquadMatchmakingSuccessMessage {
                                Success = true,
                                SessionID = mapName,
                                SquadID = squad.SquadId
                            });
                    }
                }
            }
            */
            _logger.LogInformation("[MATCH] Successfully sent matchmaking notification to the client");
        }
        catch (Exception ex)
        {
            _logger.LogError("[MATCH] Failed to send matchmaking notification: {Error}", ex.Message);
        }

        // Return result that will trigger map travel
        return new FYEnterMatchmakingResult
        {
            Success = true,
            ErrorMessage = "",
            SingleplayerStation = false,  // CRITICAL: must be false to go to a match
            NumAttempts = 1,
            Blocker = 0,
            IsMatchTravel = true,  // CRITICAL: must be true
            SessionId = sessionId, // Now the map name
        };
    }
}