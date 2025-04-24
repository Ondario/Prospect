using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Data;

public class SquadData
{
    [JsonPropertyName("squadId")]
    public string SquadId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("leaderId")]
    public string LeaderId { get; set; }

    [JsonPropertyName("members")]
    public List<SquadMember> Members { get; set; } = new List<SquadMember>();

    [JsonPropertyName("mapName")]
    public string MapName { get; set; } = "";

    [JsonPropertyName("deployTime")]
    public int DeployTime { get; set; } = 0; // Unix timestamp

    [JsonPropertyName("allReady")]
    public bool AllReady { get; set; } = false;

    [JsonPropertyName("inDeployFlow")]
    public bool InDeployFlow { get; set; } = false;

    [JsonPropertyName("matchmakingState")]
    public int MatchmakingState { get; set; } = 0; // 0 = not matchmaking, 1 = matchmaking, 2 = matched

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = "";
}

public class SquadMember
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("isReady")]
    public bool IsReady { get; set; } = false;

    [JsonPropertyName("isInDeployFlow")]
    public bool IsInDeployFlow { get; set; } = false;

    [JsonPropertyName("isConnected")]
    public bool IsConnected { get; set; } = true;
}

public class SquadInvite
{
    [JsonPropertyName("inviteId")]
    public string InviteId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("fromUserId")]
    public string FromUserId { get; set; }

    [JsonPropertyName("fromDisplayName")]
    public string FromDisplayName { get; set; }

    [JsonPropertyName("toUserId")]
    public string ToUserId { get; set; }

    [JsonPropertyName("squadId")]
    public string SquadId { get; set; }

    [JsonPropertyName("createdAt")]
    public int CreatedAt { get; set; } = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}

public class PlayerSquadState
{
    [JsonPropertyName("squadId")]
    public string SquadId { get; set; } = "";
    
    [JsonPropertyName("invites")]
    public List<SquadInvite> Invites { get; set; } = new List<SquadInvite>();
}