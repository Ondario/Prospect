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
        // Check for null or empty squadId
        if (string.IsNullOrEmpty(request.SquadId) || request.SquadId == "_")
        {
            _logger.LogInformation("TryGetCompleteSquadInfo called with invalid squad ID: {SquadId}", request?.SquadId ?? "null");
            return new FYTryGetCompleteSquadInfoResponse
            {
                Success = false,
                Error = "Invalid squad ID"
            };
        }

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
        _logger.LogInformation("TryGetCompleteSquadInfo called by {UserId} for squad {SquadId}", userId, request.SquadId);

        var squad = _squadService.GetSquad(request.SquadId);
        if (squad == null)
        {
            _logger.LogWarning("Squad {SquadId} not found", request.SquadId);
            return new FYTryGetCompleteSquadInfoResponse
            {
                Success = false,
                Error = "Squad not found"
            };
        }

        bool isUserLeader = userId == squad.LeaderId;

        return new FYTryGetCompleteSquadInfoResponse
        {
            Success = true,
            Squad = squad,
            Members = squad.Members,
            IsUserLeader = isUserLeader
        };
    }
}