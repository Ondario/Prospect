using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Hubs;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYSquadMemberStartingDeployFlowRequest
{
    [JsonPropertyName("inDeployFlow")]
    public bool InDeployFlow { get; set; }
}

public class FYSquadMemberStartingDeployFlowResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}

[CloudScriptFunction("SquadMemberStartingDeployFlow")]
public class SquadMemberStartingDeployFlow : ICloudScriptFunction<FYSquadMemberStartingDeployFlowRequest, FYSquadMemberStartingDeployFlowResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;
    private readonly IHubContext<CycleHub> _hubContext;

    public SquadMemberStartingDeployFlow(IHttpContextAccessor httpContextAccessor, SquadService squadService, IHubContext<CycleHub> hubContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
        _hubContext = hubContext;
    }

    public async Task<FYSquadMemberStartingDeployFlowResponse> ExecuteAsync(FYSquadMemberStartingDeployFlowRequest request)
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
            return new FYSquadMemberStartingDeployFlowResponse
            {
                Success = false,
                Error = "Player is not in a squad"
            };
        }

        // Set player's deploy flow status
        var success = _squadService.SetPlayerInDeployFlow(squad.SquadId, userId, request.InDeployFlow);
        if (!success)
        {
            return new FYSquadMemberStartingDeployFlowResponse
            {
                Success = false,
                Error = "Failed to set deploy flow status"
            };
        }

        // Update the squad
        squad = _squadService.GetSquad(squad.SquadId);

        // Notify squad members of the status change
        foreach (var member in squad.Members)
        {
            await _hubContext.Clients.User(member.UserId).SendAsync("SquadMemberDeployFlowChanged", new
            {
                SquadId = squad.SquadId,
                UserId = userId,
                InDeployFlow = request.InDeployFlow,
                SquadInDeployFlow = squad.InDeployFlow
            });
        }

        return new FYSquadMemberStartingDeployFlowResponse
        {
            Success = true
        };
    }
}