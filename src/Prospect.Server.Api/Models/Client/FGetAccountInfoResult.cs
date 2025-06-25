using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client;

public class FGetAccountInfoResult
{
    /// <summary>
    /// User account information for the requested user
    /// </summary>
    [JsonPropertyName("AccountInfo")]
    public FUserAccountInfo? AccountInfo { get; set; }
}

public class FUserAccountInfo
{
    /// <summary>
    /// User's display name
    /// </summary>
    [JsonPropertyName("TitleDisplayName")]
    public string? TitleDisplayName { get; set; }
    
    /// <summary>
    /// Unique PlayFab identifier of the user
    /// </summary>
    [JsonPropertyName("PlayFabId")]
    public string? PlayFabId { get; set; }
    
    /// <summary>
    /// Unique identifier for the user's account
    /// </summary>
    [JsonPropertyName("Username")]
    public string? Username { get; set; }
    
    /// <summary>
    /// User's email address
    /// </summary>
    [JsonPropertyName("Email")]
    public string? Email { get; set; }
    
    /// <summary>
    /// Timestamp for when the user account was created
    /// </summary>
    [JsonPropertyName("Created")]
    public DateTime? Created { get; set; }
    
    /// <summary>
    /// Timestamp for when the user last logged in
    /// </summary>
    [JsonPropertyName("LastLogin")]
    public DateTime? LastLogin { get; set; }
} 