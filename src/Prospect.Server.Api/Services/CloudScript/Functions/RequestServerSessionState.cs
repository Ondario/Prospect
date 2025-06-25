using System.Text.Json.Serialization;
using Prospect.Server.Api.Services.CloudScript;
using Microsoft.Extensions.Configuration;
using Prospect.Server.Api.Utils;

public class FYMatchConnectionData
{
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

        _logger.LogInformation("[MATCH] Incoming raw session/map name: {SessionID}", request.SessionID);
        //Add a property in your config.json like ("ServerPlayerLanIp": "the ip that the server is on")
        var serverPlayerLanIp = _configuration["ServerPlayerLanIp"] ?? "192.168.86.25";

        // Determine if this is the server player (localhost) or a LAN client
        bool isServerPlayer = clientIp == "127.0.0.1" || clientIp == "::1" || clientIp == "localhost" || clientIp == serverPlayerLanIp;

        string addr;
        if (isServerPlayer)
        {
            // Server player - return map name to load locally
            // The session ID is actually the map name from EnterMatchmakingMatch
            string mapName = request.SessionID;
            addr = $"{mapName}?listen?bIsLanMatch=1";
            _logger.LogInformation("Server player detected (IP: {ClientIp}), using session ID as map: {SessionID}, returning: {MapName}", clientIp, request.SessionID, addr);
        }
        else
        {
            // All clients connect to the dedicated server
            addr = $"{serverPlayerLanIp}{mapName}";
            _logger.LogInformation("Client detected (IP: {ClientIp}), returning dedicated server: {ServerAddress}", clientIp, addr);
        }

        var response = new RequestServerSessionStateResponse
        {
            CanGoToSession = true,
            ConnectionData = new FYMatchConnectionData
            {
                Addr = addr,
                ConnectSinglePlayer = false,
                IsMatch = true,
                ServerID = "testserver",
                SessionID = request.SessionID,
            },
            ShouldCancel = false,
            RetryCounter = 1,
        };

        _logger.LogInformation("Returning connection data: Addr={Addr}, SessionID={SessionID}, IsMatch={IsMatch}",
            response.ConnectionData.Addr, response.ConnectionData.SessionID, response.ConnectionData.IsMatch);

        return response;
    }
}
