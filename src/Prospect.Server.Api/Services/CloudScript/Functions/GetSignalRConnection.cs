using Microsoft.Extensions.Options;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("GetSignalRConnection")]
public class GetSignalRConnection : ICloudScriptFunction<FYGetSignalRConnection, FYGetSignalRConnectionResult>
{
    private readonly SignalRSettings _settings;

    public GetSignalRConnection(IOptions<SignalRSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<FYGetSignalRConnectionResult> ExecuteAsync(FYGetSignalRConnection request)
    {
        // The game client connects to SignalR only over HTTPS
        return Task.FromResult(new FYGetSignalRConnectionResult
        {
            Url = _settings.SignalRURL,
            AccessToken = _settings.SignalRAccessToken
        });
    }
}