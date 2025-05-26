using Microsoft.Extensions.Options;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Services.CloudScript.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class TokenGenerator
{
    public static int TokenSequenceId = 0;
    // Header is static - base64url encoded {}
    private static readonly string Header = "e30";

    public static string vxGenerateToken(string key, string issuer, int exp, string vxa, string f, string t)
    {
        TokenSequenceId++;
        var claims = new Claims
        {
            iss = issuer,
            exp = exp,
            vxa = vxa,
            vxi = TokenSequenceId,
            f = f,
            t = t
        };

        List<string> segments = new List<string>();
        segments.Add(Header);

        // Encode payload
        var claimsString = JsonSerializer.Serialize(claims);
        var encodedClaims = Base64URLEncode(claimsString);

        // Join segments to prepare for signing
        segments.Add(encodedClaims);
        string toSign = String.Join(".", segments);

        // Sign token with key and SHA256
        string sig = SHA256Hash(key, toSign);
        segments.Add(sig);

        // Join all 3 parts of token with . and return
        string token = String.Join(".", segments);

        return token;
    }

    private static string Base64URLEncode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        // Remove padding at the end
        var encodedString = Convert.ToBase64String(plainTextBytes).TrimEnd('=');
        // Substitute URL-safe characters
        string urlEncoded = encodedString.Replace("+", "-").Replace("/", "_");

        return urlEncoded;
    }

    private static string SHA256Hash(string secret, string message)
    {
        var encoding = new System.Text.ASCIIEncoding();
        byte[] keyByte = encoding.GetBytes(secret);
        byte[] messageBytes = encoding.GetBytes(message);
        // The instance of HMACSHA256 is constructed and disposed in this method because it is not thread safe
        using var hmacsha256 = new HMACSHA256(keyByte);
        byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
        var hashString = Convert.ToBase64String(hashmessage).TrimEnd('=');
        string urlEncoded = hashString.Replace("+", "-").Replace("/", "_");

        return urlEncoded;
    }

    public class Claims
    {
        public string iss { get; set; }
        public int exp { get; set; }
        public string vxa { get; set; }
        public int vxi { get; set; }
        public string f { get; set; }
        public string t { get; set; }
        public string sub { get; set; }
    }
}

[CloudScriptFunction("VivoxLogin")]
public class VivoxLogin : ICloudScriptFunction<FYVivoxLoginTokenRequest, FYVivoxJoinData>
{
    private readonly VivoxConfig _settings;

    public VivoxLogin(IOptions<VivoxConfig> settings)
    {
        _settings = settings.Value;
    }

    public async Task<FYVivoxJoinData> ExecuteAsync(FYVivoxLoginTokenRequest request)
    {
        var channel = $"sip:confctl-g-{_settings.Issuer}.testchannel@mtu1xp.vivox.com"; // e - echo channel, g - global, p - positional
        var exp = (int)DateTimeOffset.UtcNow.AddSeconds(90).ToUnixTimeSeconds();
        var token = TokenGenerator.vxGenerateToken(_settings.Key, _settings.Issuer, exp, "login", request.Username, channel);

        return new FYVivoxJoinData
        {
            Request = new Models.Data.FYVivoxJoinTokenRequest{
                Username = request.Username,
                Channel = channel,
                HasAudio = false,
                HasText = true,
                ChannelType = 2,
                ChannelID = "testchannel",
            },
            Token = token
        };
    }
}