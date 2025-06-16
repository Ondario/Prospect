using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Hubs;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("EnterMatchmaking")]
public class EnterMatchmakingFunction : ICloudScriptFunction<FYEnterMatchAzureFunction, FYEnterMatchAzureFunctionResult>
{
    private readonly ILogger<EnterMatchmakingFunction> _logger;
    private readonly IHubContext<CycleHub> _hubContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;
    private readonly MatchServerSettings _matchSettings;

    public EnterMatchmakingFunction(
        ILogger<EnterMatchmakingFunction> logger,
        IHubContext<CycleHub> hubContext, 
        IHttpContextAccessor httpContextAccessor,
        IOptions<MatchServerSettings> matchSettings,
        SquadService squadService = null)
    {
        _logger = logger;
        _hubContext = hubContext;
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
        _matchSettings = matchSettings.Value;
    }

    public async Task<FYEnterMatchAzureFunctionResult> ExecuteAsync(FYEnterMatchAzureFunction request)
    {
        var userId = _httpContextAccessor?.HttpContext?.User?.FindAuthUserId() ?? "unknown";
        
        _logger.LogInformation(
            "[MATCH] EnterMatchmaking called by {UserId} with MapName={MapName}, SquadId={SquadId}",
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
        
        // Check if this is a squad matchmaking request
        bool isSquad = !string.IsNullOrEmpty(request.SquadId) && request.SquadId != "_";
        _logger.LogInformation("[MATCH] Is Squad Request: {IsSquad}", isSquad);

        // Generate a unique session ID for this match
        string sessionId = Guid.NewGuid().ToString();
        
        // Return match connection details
        return new FYEnterMatchAzureFunctionResult
        {
            Success = true,
            ErrorMessage = "",
            SingleplayerStation = false,
            Address = _matchSettings.Host,
            Port = _matchSettings.Port,
            MaintenanceMode = false,
            SessionId = sessionId,
            ShardIndex = 0
        };
    }
}