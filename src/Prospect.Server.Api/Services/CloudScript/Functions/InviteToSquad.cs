using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYInviteToSquadRequest
{
    [JsonPropertyName("targetUserId")]
    public string TargetUserId { get; set; }
}

public class FYInviteToSquadResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("inviteId")]
    public string InviteId { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}

[CloudScriptFunction("InviteToSquad")]
public class InviteToSquad : ICloudScriptFunction<FYInviteToSquadRequest, FYInviteToSquadResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;
    private readonly Services.UserData.UserDataService _userDataService;

    public InviteToSquad(IHttpContextAccessor httpContextAccessor, SquadService squadService, Services.UserData.UserDataService userDataService)
    {
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
        _userDataService = userDataService;
    }

    public async Task<FYInviteToSquadResponse> ExecuteAsync(FYInviteToSquadRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }

        var userId = context.User.FindAuthUserId();
        
        // Get user's display name
        var userData = await _userDataService.FindAsync(userId, userId, new List<string> { "DisplayName" });
        string displayName = "Player";
        if (userData.TryGetValue("DisplayName", out var displayNameData))
        {
            displayName = displayNameData.Value;
        }

        // Check if player is in a squad
        var squad = await _squadService.GetPlayerSquadAsync(userId);
        if (squad == null)
        {
            return new FYInviteToSquadResponse
            {
                Success = false,
                Error = "Player is not in a squad"
            };
        }

        // Check if player is the squad leader
        if (squad.LeaderId != userId)
        {
            return new FYInviteToSquadResponse
            {
                Success = false,
                Error = "Only the squad leader can invite players"
            };
        }

        // Create the invite
        var invite = await _squadService.CreateSquadInviteAsync(userId, displayName, request.TargetUserId, squad.SquadId);
        if (invite == null)
        {
            return new FYInviteToSquadResponse
            {
                Success = false,
                Error = "Failed to create invite"
            };
        }

        return new FYInviteToSquadResponse
        {
            Success = true,
            InviteId = invite.InviteId
        };
    }
}