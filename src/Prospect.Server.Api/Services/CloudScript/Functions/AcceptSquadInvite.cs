using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYAcceptSquadInviteRequest
{
    [JsonPropertyName("inviteId")]
    public string InviteId { get; set; }
}

public class FYAcceptSquadInviteResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("squadId")]
    public string SquadId { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}

[CloudScriptFunction("AcceptSquadInvite")]
public class AcceptSquadInvite : ICloudScriptFunction<FYAcceptSquadInviteRequest, FYAcceptSquadInviteResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;

    public AcceptSquadInvite(IHttpContextAccessor httpContextAccessor, SquadService squadService)
    {
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
    }

    public async Task<FYAcceptSquadInviteResponse> ExecuteAsync(FYAcceptSquadInviteRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }

        var userId = context.User.FindAuthUserId();
        
        // Check if player is already in a squad
        var existingSquad = await _squadService.GetPlayerSquadAsync(userId);
        if (existingSquad != null)
        {
            return new FYAcceptSquadInviteResponse
            {
                Success = false,
                Error = "Player is already in a squad"
            };
        }

        // Accept the invite
        var success = await _squadService.AcceptSquadInviteAsync(userId, request.InviteId);
        if (!success)
        {
            return new FYAcceptSquadInviteResponse
            {
                Success = false,
                Error = "Failed to accept invite"
            };
        }

        // Get the player's squad after accepting the invite
        var squad = await _squadService.GetPlayerSquadAsync(userId);
        if (squad == null)
        {
            return new FYAcceptSquadInviteResponse
            {
                Success = false,
                Error = "Failed to join squad"
            };
        }

        return new FYAcceptSquadInviteResponse
        {
            Success = true,
            SquadId = squad.SquadId
        };
    }
}