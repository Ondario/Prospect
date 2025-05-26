using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYVivoxJoinTokenRequest
{
    [JsonPropertyName("userName")]
    public string Username { get; set; }

    [JsonPropertyName("channel")]
    public string Channel { get; set; }

    [JsonPropertyName("hasText")]
    public bool HasText { get; set; }

    [JsonPropertyName("hasAudio")]
    public bool HasAudio { get; set; }

    [JsonPropertyName("channelType")]
    public string ChannelType { get; set; }

    [JsonPropertyName("channelId")]
    public string ChannelID { get; set; }
}

[CloudScriptFunction("VivoxJoin")]
public class VivoxJoin : ICloudScriptFunction<FYVivoxJoinTokenRequest, object>
{
    private readonly VivoxConfig _settings;

    public VivoxJoin(IOptions<VivoxConfig> settings)
    {
        _settings = settings.Value;
    }

    private int ReturnChannelType(string type) {
        switch (type) {
            case "GLOBAL": return 2;
            case "WHISPER": return 3;
            case "SQUAD": return 4;
            case "TEAM": return 5;
            case "MATCH": return 6;
            default: return 2;
        }
    }

    public async Task<object> ExecuteAsync(FYVivoxJoinTokenRequest request)
    {
        var exp = (int)DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds();
        var token = TokenGenerator.vxGenerateToken(_settings.Key, _settings.Issuer, exp, "join", request.Username, request.Channel);

        return new FYVivoxJoinData
        {
            Request = new Models.Data.FYVivoxJoinTokenRequest{
                Username = request.Username,
                // TODO: Client requests sip:confctl-d-issuer._MATCH!p-6000-2000-1.000-2@mtu1xp.vivox.com, but session ID contains
                // sip:confctl-d-issuer._MATCH!p-6000-2000-1.000-1@mtu1xp.vivox.com.
                // Server session need to report Vivox channel ID? Or need to construct proper session ID?
                Channel = request.Channel,
                HasAudio = request.HasAudio,
                HasText = request.HasText,
                ChannelType = ReturnChannelType(request.ChannelType),
                ChannelID = request.ChannelID,
            },
            Token = token
        };
    }
}