using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models;

/// <summary>
/// Message sent to clients when squad matchmaking succeeds
/// </summary>
public class OnSquadMatchmakingSuccessMessage
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("sessionId")]
    public string SessionID { get; set; }
    
    [JsonPropertyName("squadId")]
    public string SquadID { get; set; }
}