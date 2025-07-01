using System.Text.Json.Serialization;
using Prospect.Server.Api.Services.CloudScript;
using Microsoft.Extensions.Configuration;
using Prospect.Server.Api.Utils;
using Prospect.Server.Api.Services.ServerManagement;

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
    private readonly ServerManagementService _serverManagementService;

    public RequestServerSessionStateFunction(
        ILogger<RequestServerSessionStateFunction> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ServerManagementService serverManagementService)
    {
        _logger = logger;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _serverManagementService = serverManagementService;
    }

    public async Task<RequestServerSessionStateResponse> ExecuteAsync(RequestServerSessionStateRequest request)
    {
        _logger.LogInformation("[DEDICATED] Processing server session state request for session: {SessionID}", request.SessionID);

        // Extract map name from session ID (session ID is the map name from EnterMatchmakingMatch)
        string mapName = request.SessionID;
        
        // Get or create dedicated server instance for this session
        //var serverInstance = await _serverManagementService.GetOrCreateServerAsync(request.SessionID, mapName);
        
        //if (serverInstance.State == ServerState.Error)
        //{
        //    throw new InvalidOperationException($"Failed to create server instance for session {request.SessionID}");
        //}
        
        // Return the actual server connection details
        //string serverAddress = serverInstance.GetConnectionString();
        
        //_logger.LogInformation("[DEDICATED] Returning dedicated server connection: {ServerAddress} for map: {MapName}, session: {SessionID}", 
        //    serverAddress, mapName, request.SessionID);

        var response = new RequestServerSessionStateResponse
        {
            CanGoToSession = true,
            ConnectionData = new FYMatchConnectionData
            {
                Addr = "192.168.0.25", ///serverAddress,
                ConnectSinglePlayer = false,
                IsMatch = true,
                ServerID = "1", //serverInstance.ServerId,
                SessionID = request.SessionID,
            },
            ShouldCancel = false,
            RetryCounter = 1,
        };

        return response;
    }

    private async Task<string> EnsureServerInstanceAsync(string mapName, string sessionId)
    {
        _logger.LogInformation("[DEDICATED] Ensuring server instance for map: {MapName}, session: {SessionID}", mapName, sessionId);
        
        var serverInstance = await _serverManagementService.GetOrCreateServerAsync(sessionId, mapName);
        
        if (serverInstance.State == ServerState.Error)
        {
            throw new InvalidOperationException($"Failed to create server instance for session {sessionId}");
        }
        
        return serverInstance.ServerId;
    }
}
