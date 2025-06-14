using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYEnterMatchmakingResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("maintenanceMode")] // TODO: Legacy param?
    public bool MaintenanceMode { get; set; }
    [JsonPropertyName("blocker")]
	public int Blocker { get; set; } // EYMatchmakingBlocker
    [JsonPropertyName("isMatchTravel")]
	public bool IsMatchTravel { get; set; }
    [JsonPropertyName("singleplayerStation")]
    public bool SingleplayerStation { get; set; }
    [JsonPropertyName("numAttempts")]
	public int NumAttempts { get; set; }
    [JsonPropertyName("sessionId")]
	public string SessionId { get; set; }
    [JsonPropertyName("errorMessage")]
	public string ErrorMessage { get; set; }
    [JsonPropertyName("address")]
    public string Address { get; set; }
    [JsonPropertyName("port")]
    public int Port { get; set; }
}

public class FYEnterMatchAzureFunctionResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("sharedIndex")]
    public int ShardIndex { get; set; }

    [JsonPropertyName("singleplayerStation")]
    public bool SingleplayerStation { get; set; }

    [JsonPropertyName("maintenanceMode")]
    public bool MaintenanceMode { get; set; }

    [JsonPropertyName("sessionTimeJoinDelay")]
    public float SessionTimeJoinDelay { get; set; }
}