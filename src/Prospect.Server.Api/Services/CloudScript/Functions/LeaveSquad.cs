using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYLeaveSquadRequest
{
    // Empty request
}

public class FYLeaveSquadResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}

[CloudScriptFunction("LeaveSquad")]
public class LeaveSquad : ICloudScriptFunction<FYLeaveSquadRequest, FYLeaveSquadResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;

    public LeaveSquad(IHttpContextAccessor httpContextAccessor, SquadService squadService)
    {
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
    }

    public async Task<FYLeaveSquadResponse> ExecuteAsync(FYLeaveSquadRequest request)
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
            return new FYLeaveSquadResponse
            {
                Success = false,
                Error = "Player is not in a squad"
            };
        }

        // Leave the squad
        var success = await _squadService.RemovePlayerFromSquadAsync(squad.SquadId, userId);
        if (!success)
        {
            return new FYLeaveSquadResponse
            {
                Success = false,
                Error = "Failed to leave squad"
            };
        }

        return new FYLeaveSquadResponse
        {
            Success = true
        };
    }
}