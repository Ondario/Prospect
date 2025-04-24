using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYCreateSquadRequest
{
    // Empty request
}

public class FYCreateSquadResponse
{
    [JsonPropertyName("squadId")]
    public string SquadId { get; set; }
    
    [JsonPropertyName("leaderId")]
    public string LeaderId { get; set; }
    
    [JsonPropertyName("members")]
    public List<Prospect.Server.Api.Models.Data.SquadMember> Members { get; set; }
}

[CloudScriptFunction("CreateSquad")]
public class CreateSquad : ICloudScriptFunction<FYCreateSquadRequest, FYCreateSquadResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;
    private readonly Services.UserData.UserDataService _userDataService;

    public CreateSquad(IHttpContextAccessor httpContextAccessor, SquadService squadService, Services.UserData.UserDataService userDataService)
    {
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
        _userDataService = userDataService;
    }

    public async Task<FYCreateSquadResponse> ExecuteAsync(FYCreateSquadRequest request)
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

        // Check if player is already in a squad
        var existingSquad = await _squadService.GetPlayerSquadAsync(userId);
        if (existingSquad != null)
        {
            return new FYCreateSquadResponse
            {
                SquadId = existingSquad.SquadId,
                LeaderId = existingSquad.LeaderId,
                Members = existingSquad.Members
            };
        }

        // Create a new squad
        var squad = await _squadService.CreateSquadAsync(userId, displayName);

        return new FYCreateSquadResponse
        {
            SquadId = squad.SquadId,
            LeaderId = squad.LeaderId,
            Members = squad.Members
        };
    }
}