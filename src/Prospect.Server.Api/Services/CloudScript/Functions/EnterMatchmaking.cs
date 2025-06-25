using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
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

    public EnterMatchmakingFunction(
        ILogger<EnterMatchmakingFunction> logger,
        IHubContext<CycleHub> hubContext,
        IHttpContextAccessor httpContextAccessor,
        SquadService squadService = null)
    {
        _logger = logger;
        _hubContext = hubContext;
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
    }

    public async Task<FYEnterMatchAzureFunctionResult> ExecuteAsync(FYEnterMatchAzureFunction request)
    {
        var userId = _httpContextAccessor?.HttpContext?.User?.FindAuthUserId() ?? "unknown";

        _logger.LogInformation(
            "[MATCH] EnterMatchmaking called by {UserId} with MapName={MapName}, SquadId={SquadId}",
            userId, request.MapName, request.SquadId ?? "null");

        _logger.LogInformation("[MATCH] Incoming raw map name: {MapName}", request.MapName);
        string mapName = NormalizeMapName(request.MapName);

        // Check if this is a squad matchmaking request
        bool isSquad = !string.IsNullOrEmpty(request.SquadId) && request.SquadId != "_";
        _logger.LogInformation("[MATCH] Is Squad Request: {IsSquad}", isSquad);

        // Set SingleplayerStation to false to route directly to a match
        var result = new FYEnterMatchAzureFunctionResult
        {
            Success = true,
            ErrorMessage = "",
            SingleplayerStation = false,
            Address = mapName,
            MaintenanceMode = false,
            Port = 0, // Set to 0 for Steam P2P - Steam handles networking automatically
        };

        _logger.LogInformation("[MATCH] Returning result: Success={Success}, Address={Address}, Port={Port}, SingleplayerStation={SingleplayerStation}",
            result.Success, result.Address, result.Port, result.SingleplayerStation);

        return result;
    }

    private static string NormalizeMapName(string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName))
            return "/Game/Maps/MP/MAP01/MP_Map01_P";

        if (mapName.StartsWith("/Game/Maps/"))
            return mapName;

        return mapName switch
        {
            "BrightSands" or "Map01" => "/Game/Maps/MP/MAP01/MP_Map01_P",
            "CrescentFalls" or "Map02" => "/Game/Maps/MP/MAP02/MP_Map02_P",
            "TharisIsland" or "Map03" => "/Game/Maps/MP/MAP03/MP_Map03_P",
            _ => "/Game/Maps/MP/MAP01/MP_Map01_P"
        };
    }
}