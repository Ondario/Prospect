using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYGetSquadInvitesRequest
{
    // Empty request
}

public class FYGetSquadInvitesResponse
{
    [JsonPropertyName("invites")]
    public List<Prospect.Server.Api.Models.Data.SquadInvite> Invites { get; set; }
}

[CloudScriptFunction("GetSquadInvites")]
public class GetSquadInvites : ICloudScriptFunction<FYGetSquadInvitesRequest, FYGetSquadInvitesResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SquadService _squadService;

    public GetSquadInvites(IHttpContextAccessor httpContextAccessor, SquadService squadService)
    {
        _httpContextAccessor = httpContextAccessor;
        _squadService = squadService;
    }

    public async Task<FYGetSquadInvitesResponse> ExecuteAsync(FYGetSquadInvitesRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }

        var userId = context.User.FindAuthUserId();
        
        // Get player's squad invites
        var invites = await _squadService.GetPlayerSquadInvitesAsync(userId);

        return new FYGetSquadInvitesResponse
        {
            Invites = invites
        };
    }
}