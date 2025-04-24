using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("EnterMatchmakingStation")]
public class EnterMatchmakingStationFunction : ICloudScriptFunction<FYEnterMatchAzureFunction, FYEnterMatchmakingResult>
{
    public Task<FYEnterMatchmakingResult> ExecuteAsync(FYEnterMatchAzureFunction request)
    {
        // Do NOT modify this - keep it exactly as in the original implementation
        return Task.FromResult(new FYEnterMatchmakingResult
        {
            Success = true,
            ErrorMessage = "",
            SingleplayerStation = true, // THIS MUST BE TRUE
            NumAttempts = 1,
            Blocker = 0,
            IsMatchTravel = false
        });
    }
}