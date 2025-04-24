using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYTryGetCompleteSquadInfoRequest
{
    [JsonPropertyName("squadId")]
    public string SquadId { get; set; }
}

public class FYTryGetCompleteSquadInfoResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("squad")]
    public Prospect.Server.Api.Models.Data.SquadData Squad { get; set; }
    
    [JsonPropertyName("error")]
    public string Error { get; set; }
}

[CloudScriptFunction("TryGetCompleteSquadInfo")]
public class TryGetCompleteSquadInfo : ICloudScriptFunction<FYTryGetCompleteSquadInfoRequest, FYTryGetCompleteSquadInfoResponse>
{
    private readonly SquadService _squadService;

    public TryGetCompleteSquadInfo(SquadService squadService)
    {
        _squadService = squadService;
    }

    public Task<FYTryGetCompleteSquadInfoResponse> ExecuteAsync(FYTryGetCompleteSquadInfoRequest request)
    {
        // Get the squad by ID
        var squad = _squadService.GetSquad(request.SquadId);
        if (squad == null)
        {
            return Task.FromResult(new FYTryGetCompleteSquadInfoResponse
            {
                Success = false,
                Error = "Squad not found"
            });
        }

        return Task.FromResult(new FYTryGetCompleteSquadInfoResponse
        {
            Success = true,
            Squad = squad
        });
    }
}