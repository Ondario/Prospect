using Microsoft.Extensions.Options;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("VivoxSettings")]
public class VivoxSettings : ICloudScriptFunction<FYEmptyRequest, FYVivoxSettingsData>
{
    private readonly VivoxConfig _settings;

    public VivoxSettings(IOptions<VivoxConfig> settings)
    {
        _settings = settings.Value;
    }

    public Task<FYVivoxSettingsData> ExecuteAsync(FYEmptyRequest request)
    {
        return Task.FromResult(new FYVivoxSettingsData
        {
            VivoxDomain = _settings.Domain,
            VivoxIssuer = _settings.Issuer,
            VivoxServer = _settings.Server
        });
    }
}