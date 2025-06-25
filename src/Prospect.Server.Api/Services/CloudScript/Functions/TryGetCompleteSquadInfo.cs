using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Data;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.Squad;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYTryGetCompleteSquadInfoRequest
{
    [JsonPropertyName("squadId")]
    public string SquadId { get; set; }
}

public class FYTryGetCompleteSquadInfoResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
    
    [JsonPropertyName("squad")]
    public SquadData Squad { get; set; }
    
    [JsonPropertyName("members")]
    public List<SquadMember> Members { get; set; }
    
    [JsonPropertyName("isUserLeader")]
    public bool IsUserLeader { get; set; }
}

[CloudScriptFunction("TryGetCompleteSquadInfo")]
public class TryGetCompleteSquadInfo : ICloudScriptFunction<FYTryGetCompleteSquadInfoRequest, FYTryGetCompleteSquadInfoResponse>
{
    private readonly ILogger<TryGetCompleteSquadInfo> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;

    public TryGetCompleteSquadInfo(
        ILogger<TryGetCompleteSquadInfo> logger,
        IHttpContextAccessor httpContextAccessor,
        SquadService squadService)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
    }

    public async Task<FYTryGetCompleteSquadInfoResponse> ExecuteAsync(FYTryGetCompleteSquadInfoRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return new FYTryGetCompleteSquadInfoResponse
            {
                Success = false,
                Error = "No HTTP context"
            };
        }

        var userId = context.User.FindAuthUserId();

        // If squadId is missing/invalid, try to auto-create a squad for solo players
        if (string.IsNullOrEmpty(request.SquadId) || request.SquadId == "_")
        {
            // Check if player is already in a squad
            var existingSquad = await _squadService.GetPlayerSquadAsync(userId);
            if (existingSquad != null)
            {
                return new FYTryGetCompleteSquadInfoResponse
                {
                    Success = true,
                    Squad = existingSquad,
                    Members = existingSquad.Members,
                    IsUserLeader = userId == existingSquad.LeaderId
                };
            }

            // Fetch display name for squad creation
            var userDataService = (Services.UserData.UserDataService)context.RequestServices.GetService(typeof(Services.UserData.UserDataService));
            string displayName = "Player";
            if (userDataService != null)
            {
                var userData = await userDataService.FindAsync(userId, userId, new List<string> { "DisplayName" });
                if (userData.TryGetValue("DisplayName", out var displayNameData))
                {
                    displayName = displayNameData.Value;
                }
            }

            // Create squad for solo player
            var squad = await _squadService.CreateSquadAsync(userId, displayName);
            return new FYTryGetCompleteSquadInfoResponse
            {
                Success = true,
                Squad = squad,
                Members = squad.Members,
                IsUserLeader = true
            };
        }

        _logger.LogInformation("TryGetCompleteSquadInfo called by {UserId} for squad {SquadId}", userId, request.SquadId);

        var foundSquad = _squadService.GetSquad(request.SquadId);
        if (foundSquad == null)
        {
            _logger.LogWarning("Squad {SquadId} not found", request.SquadId);
            return new FYTryGetCompleteSquadInfoResponse
            {
                Success = false,
                Error = "Squad not found"
            };
        }

        bool isUserLeader = userId == foundSquad.LeaderId;

        return new FYTryGetCompleteSquadInfoResponse
        {
            Success = true,
            Squad = foundSquad,
            Members = foundSquad.Members,
            IsUserLeader = isUserLeader
        };
    }
}