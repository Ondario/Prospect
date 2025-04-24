using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Hubs;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYSquadMemberReadyForMatchRequest
{
    [JsonPropertyName("isReady")]
    public bool IsReady { get; set; }
}

public class FYSquadMemberReadyForMatchResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}

[CloudScriptFunction("SquadMemberReadyForMatch")]
public class SquadMemberReadyForMatch : ICloudScriptFunction<FYSquadMemberReadyForMatchRequest, FYSquadMemberReadyForMatchResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;
    private readonly IHubContext<CycleHub> _hubContext;

    public SquadMemberReadyForMatch(IHttpContextAccessor httpContextAccessor, SquadService squadService, IHubContext<CycleHub> hubContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
        _hubContext = hubContext;
    }

    public async Task<FYSquadMemberReadyForMatchResponse> ExecuteAsync(FYSquadMemberReadyForMatchRequest request)
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
            return new FYSquadMemberReadyForMatchResponse
            {
                Success = false,
                Error = "Player is not in a squad"
            };
        }

        // Set player's ready status
        var success = _squadService.SetPlayerReady(squad.SquadId, userId, request.IsReady);
        if (!success)
        {
            return new FYSquadMemberReadyForMatchResponse
            {
                Success = false,
                Error = "Failed to set ready status"
            };
        }

        // Update the squad
        squad = _squadService.GetSquad(squad.SquadId);

        // Notify squad members of the status change
        foreach (var member in squad.Members)
        {
            await _hubContext.Clients.User(member.UserId).SendAsync("SquadMemberStatusChanged", new
            {
                SquadId = squad.SquadId,
                UserId = userId,
                IsReady = request.IsReady,
                AllReady = squad.AllReady
            });
        }

        return new FYSquadMemberReadyForMatchResponse
        {
            Success = true
        };
    }
}