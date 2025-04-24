using System.Text.Json.Serialization;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class StationRequest
{
    [JsonPropertyName("optimalRegion")]
    public string OptimalRegion { get; set; }
    
    [JsonPropertyName("isMatch")]
    public bool IsMatch { get; set; }
    
    [JsonPropertyName("mapName")]
    public string MapName { get; set; }
    
    [JsonPropertyName("squad_id")]
    public string SquadId { get; set; }
    
    [JsonPropertyName("bypassMaintenanceMode")]
    public bool BypassMaintenanceMode { get; set; }
    
    [JsonPropertyName("loginNonce")]
    public string LoginNonce { get; set; }
}

[CloudScriptFunction("EnterMatchmakingStation")]
public class EnterMatchmakingStationFunction : ICloudScriptFunction<StationRequest, FYEnterMatchmakingResult>
{
    private readonly ILogger<EnterMatchmakingStationFunction> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public EnterMatchmakingStationFunction(ILogger<EnterMatchmakingStationFunction> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Task<FYEnterMatchmakingResult> ExecuteAsync(StationRequest request)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindAuthUserId() ?? "unknown";
        
        _logger.LogInformation(
            "[MATCH] EnterMatchmakingStation called by {UserId} with MapName={MapName}, IsMatch={IsMatch}, SquadId={SquadId}",
            userId, request.MapName, request.IsMatch, request.SquadId);
        
        // If isMatch is true, this is actually trying to enter a game map, not the station
        if (request.IsMatch)
        {
            _logger.LogInformation("[MATCH] Redirecting to game map from station: {MapName}", request.MapName);
            
            return Task.FromResult(new FYEnterMatchmakingResult
            {
                Success = true,
                ErrorMessage = "",
                SingleplayerStation = false, // This will route to a game map
                NumAttempts = 1,
                Blocker = 0,
                IsMatchTravel = true,
                SessionId = Guid.NewGuid().ToString()
            });
        }
        
        // Normal station travel
        _logger.LogInformation("[MATCH] Traveling to station");
        
        return Task.FromResult(new FYEnterMatchmakingResult
        {
            Success = true,
            ErrorMessage = "",
            SingleplayerStation = true, // This will route to station
            NumAttempts = 0,
            Blocker = 0,
            IsMatchTravel = false,
            SessionId = ""
        });
    }
}