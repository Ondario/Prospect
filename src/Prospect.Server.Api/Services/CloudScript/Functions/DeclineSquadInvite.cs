using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYDeclineSquadInviteRequest
{
    [JsonPropertyName("inviteId")]
    public string InviteId { get; set; }
}

public class FYDeclineSquadInviteResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}

[CloudScriptFunction("DeclineSquadInvite")]
public class DeclineSquadInvite : ICloudScriptFunction<FYDeclineSquadInviteRequest, FYDeclineSquadInviteResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;

    public DeclineSquadInvite(IHttpContextAccessor httpContextAccessor, SquadService squadService)
    {
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
    }

    public async Task<FYDeclineSquadInviteResponse> ExecuteAsync(FYDeclineSquadInviteRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }

        var userId = context.User.FindAuthUserId();
        
        // Decline the invite
        var success = await _squadService.DeclineSquadInviteAsync(userId, request.InviteId);
        if (!success)
        {
            return new FYDeclineSquadInviteResponse
            {
                Success = false,
                Error = "Failed to decline invite"
            };
        }

        return new FYDeclineSquadInviteResponse
        {
            Success = true
        };
    }
}