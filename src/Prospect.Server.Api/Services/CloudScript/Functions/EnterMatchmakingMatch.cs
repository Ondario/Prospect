using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Hubs;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using Prospect.Server.Api.Services.UserData;

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
            
        // Validate map name format
        string mapName = request.MapName;
        if (!mapName.StartsWith("/Game/Maps/"))
        {
            // Fix map path format if needed
            if (mapName == "BrightSands")
                mapName = "/Game/Maps/MP/BrightSands/BrightSands_P";
            else if (mapName == "CrescentFalls")
                mapName = "/Game/Maps/MP/CrescentFalls/CrescentFalls_P";
            else if (mapName == "TharisIsland")
                mapName = "/Game/Maps/MP/TharisIsland/TharisIsland_P";
            
            _logger.LogInformation("[MATCH] Corrected map name to: {MapName}", mapName);
        }

        // Generate unique session ID
        string sessionId = Guid.NewGuid().ToString();
        
        // Send SignalR notification - CRITICAL for matchmaking!
        try {
            _logger.LogInformation("[MATCH] Broadcasting matchmaking success notification");
            
            // IMPORTANT: We must broadcast to ALL clients
            await _hubContext.Clients.All.SendAsync("OnSquadMatchmakingSuccess", 
                new OnSquadMatchmakingSuccessMessage {
                    Success = true,
                    SessionID = mapName,
                    SquadID = request.SquadId ?? "_"
                });
                
            _logger.LogInformation("[MATCH] Successfully broadcast matchmaking notification");
        } catch (Exception ex) {
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
            SessionId = sessionId,
        };
    }
}