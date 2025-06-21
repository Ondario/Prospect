using System.Text.Json.Serialization;
using Prospect.Server.Api.Services.CloudScript;
using Microsoft.Extensions.Configuration;

public class FYMatchConnectionData {
    [JsonPropertyName("addr")]
    public string Addr { get; set; }
    [JsonPropertyName("sessionId")]
    public string SessionID { get; set; }
    [JsonPropertyName("serverId")]
    public string ServerID { get; set; }
    [JsonPropertyName("connectSinglePlayer")]
    public bool ConnectSinglePlayer { get; set; }
    [JsonPropertyName("isMatch")]
    public bool IsMatch { get; set; }
}

public class RequestServerSessionStateRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionID { get; set; }
    [JsonPropertyName("amountRequests")]
    public int AmountRequests { get; set; }
}

public class RequestServerSessionStateResponse
{
    [JsonPropertyName("connectionData")]
    public FYMatchConnectionData ConnectionData { get; set; }
    [JsonPropertyName("retryCounter")]
    public int RetryCounter { get; set; }
    [JsonPropertyName("canGoToSession")]
    public bool CanGoToSession { get; set; }
    [JsonPropertyName("shouldCancel")]
    public bool ShouldCancel { get; set; }
}

[CloudScriptFunction("RequestServerSessionState")]
public class RequestServerSessionStateFunction : ICloudScriptFunction<RequestServerSessionStateRequest, RequestServerSessionStateResponse>
{
    private readonly ILogger<RequestServerSessionStateFunction> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestServerSessionStateFunction(
        ILogger<RequestServerSessionStateFunction> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RequestServerSessionStateResponse> ExecuteAsync(RequestServerSessionStateRequest request)
    {
        _logger.LogInformation("Processing server session state");

        // Get client IP address
        var context = _httpContextAccessor.HttpContext;
        var clientIp = context?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        // Get Steam host ID from configuration
        //Add a property in your config.json like ("ServerPlayerLanIp": "the ip that the server is on")
        var serverPlayerLanIp = _configuration["ServerPlayerLanIp"] ?? "192.168.86.25";
            //Add a property in your config.json like ("steamHostId": "your steam id here")
        var steamHostId = _configuration["SteamHostId"] ?? "76561198288988180"; // fallback if not set

        // Determine if this is the server player (localhost) or a LAN client
        bool isServerPlayer = clientIp == "127.0.0.1" || clientIp == "::1" || clientIp == "localhost" || clientIp == serverPlayerLanIp;

        string addr;
        if (isServerPlayer) {
            // Server player - return map name to load locally
            addr = "/Game/Maps/MP/MAP01/MP_Map01_P?listen?blsLanMatch"; // You can make this dynamic based on the sessionId if needed
            _logger.LogInformation("Server player detected (IP: {ClientIp}), returning map name: {MapName}", clientIp, addr);
        } else {
            // LAN/Steam client - return Steam host's SteamID
            addr = $"steam.{steamHostId}";
            _logger.LogInformation("LAN/Steam client detected (IP: {ClientIp}), returning Steam host ID: {SteamHostId}", clientIp, steamHostId);
        }

        return new RequestServerSessionStateResponse
        {
            CanGoToSession = true,
            ConnectionData = new FYMatchConnectionData {
                Addr = addr,
                ConnectSinglePlayer = false,
                IsMatch = true,
                ServerID = "testserver",
                SessionID = request.SessionID,
            },
            ShouldCancel = false,
            RetryCounter = 1,
        };
    }
}
