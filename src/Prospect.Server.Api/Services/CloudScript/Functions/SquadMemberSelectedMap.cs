using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Hubs;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYSquadMemberSelectedMapRequest
{
    [JsonPropertyName("mapName")]
    public string MapName { get; set; }
}

public class FYSquadMemberSelectedMapResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}

[CloudScriptFunction("SquadMemberSelectedMap")]
public class SquadMemberSelectedMap : ICloudScriptFunction<FYSquadMemberSelectedMapRequest, FYSquadMemberSelectedMapResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;
    private readonly IHubContext<CycleHub> _hubContext;

    public SquadMemberSelectedMap(IHttpContextAccessor httpContextAccessor, SquadService squadService, IHubContext<CycleHub> hubContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
        _hubContext = hubContext;
    }

    public async Task<FYSquadMemberSelectedMapResponse> ExecuteAsync(FYSquadMemberSelectedMapRequest request)
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
            return new FYSquadMemberSelectedMapResponse
            {
                Success = false,
                Error = "Player is not in a squad"
            };
        }

        // Check if player is the squad leader
        if (squad.LeaderId != userId)
        {
            return new FYSquadMemberSelectedMapResponse
            {
                Success = false,
                Error = "Only the squad leader can select the map"
            };
        }

        // Set squad map
        var success = _squadService.SetSquadMap(squad.SquadId, request.MapName);
        if (!success)
        {
            return new FYSquadMemberSelectedMapResponse
            {
                Success = false,
                Error = "Failed to set map"
            };
        }

        // Notify squad members of the map change
        foreach (var member in squad.Members)
        {
            await _hubContext.Clients.User(member.UserId).SendAsync("SquadMapChanged", new
            {
                SquadId = squad.SquadId,
                MapName = request.MapName
            });
        }

        return new FYSquadMemberSelectedMapResponse
        {
            Success = true
        };
    }
}