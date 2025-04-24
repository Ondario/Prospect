using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYGetSquadInfoRequest
{
    // Empty request
}

public class FYGetSquadInfoResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("squadId")]
    public string SquadId { get; set; }
    
    [JsonPropertyName("leaderId")]
    public string LeaderId { get; set; }
    
    [JsonPropertyName("members")]
    public List<Prospect.Server.Api.Models.Data.SquadMember> Members { get; set; }
    
    [JsonPropertyName("mapName")]
    public string MapName { get; set; }
    
    [JsonPropertyName("allReady")]
    public bool AllReady { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}

[CloudScriptFunction("GetSquadInfo")]
public class GetSquadInfo : ICloudScriptFunction<FYGetSquadInfoRequest, FYGetSquadInfoResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;

    public GetSquadInfo(IHttpContextAccessor httpContextAccessor, SquadService squadService)
    {
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
    }

    public async Task<FYGetSquadInfoResponse> ExecuteAsync(FYGetSquadInfoRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }

        var userId = context.User.FindAuthUserId();
        
        // Check if player is in a squad
        var squad = await _squadService.GetPlayerSquadAsync(userId);
        if (squad == null)
        {
            return new FYGetSquadInfoResponse
            {
                Success = false,
                Error = "Player is not in a squad"
            };
        }

        return new FYGetSquadInfoResponse
        {
            Success = true,
            SquadId = squad.SquadId,
            LeaderId = squad.LeaderId,
            Members = squad.Members,
            MapName = squad.MapName,
            AllReady = squad.AllReady
        };
    }
}