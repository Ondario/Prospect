using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client;

public class FGetAccountInfoRequest
{
    /// <summary>
    /// User email address attached to their account
    /// </summary>
    [JsonPropertyName("Email")]
    public string? Email { get; set; }
    
    /// <summary>
    /// Unique PlayFab identifier of the user whose info is being requested
    /// </summary>
    [JsonPropertyName("PlayFabId")]
    public string? PlayFabId { get; set; }
    
    /// <summary>
    /// Title-specific username that uniquely identifies a user
    /// </summary>
    [JsonPropertyName("TitleDisplayName")]
    public string? TitleDisplayName { get; set; }
    
    /// <summary>
    /// PlayFab username for the account
    /// </summary>
    [JsonPropertyName("Username")]
    public string? Username { get; set; }
} 