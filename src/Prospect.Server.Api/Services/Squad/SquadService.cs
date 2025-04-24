using Microsoft.Extensions.Options;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Models.Data;
using Prospect.Server.Api.Services.UserData;
using System.Text.Json;

namespace Prospect.Server.Api.Services.Squad;

public class SquadService
{
    private readonly ILogger<SquadService> _logger;
    private readonly UserDataService _userDataService;
    private readonly TitleDataService _titleDataService;
    
    // Dictionary to store active squads in memory
    private readonly Dictionary<string, SquadData> _activeSquads = new();

    public SquadService(ILogger<SquadService> logger, UserDataService userDataService, TitleDataService titleDataService)
    {
        _logger = logger;
        _userDataService = userDataService;
        _titleDataService = titleDataService;
    }

    // Create a new squad with the given leader
    public async Task<SquadData> CreateSquadAsync(string leaderId, string displayName)
    {
        var squad = new SquadData
        {
            LeaderId = leaderId,
            Members = new List<SquadMember>
            {
                new SquadMember
                {
                    UserId = leaderId,
                    DisplayName = displayName,
                    IsReady = false
                }
            }
        };

        // Store the squad in memory
        _activeSquads[squad.SquadId] = squad;

        // Update the player's squad state
        await UpdatePlayerSquadStateAsync(leaderId, squad.SquadId);

        return squad;
    }

    // Get a squad by ID
    public SquadData GetSquad(string squadId)
    {
        if (_activeSquads.TryGetValue(squadId, out var squad))
        {
            return squad;
        }

        return null;
    }

    // Add a player to a squad
    public async Task<bool> AddPlayerToSquadAsync(string squadId, string userId, string displayName)
    {
        if (!_activeSquads.TryGetValue(squadId, out var squad))
        {
            return false;
        }

        // Check if the player is already in the squad
        if (squad.Members.Any(m => m.UserId == userId))
        {
            return true;
        }

        // Add the player to the squad
        squad.Members.Add(new SquadMember
        {
            UserId = userId,
            DisplayName = displayName,
            IsReady = false
        });

        // Update the player's squad state
        await UpdatePlayerSquadStateAsync(userId, squadId);

        return true;
    }

    // Remove a player from a squad
    public async Task<bool> RemovePlayerFromSquadAsync(string squadId, string userId)
    {
        if (!_activeSquads.TryGetValue(squadId, out var squad))
        {
            return false;
        }

        // Remove the player from the squad
        var member = squad.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
        {
            return false;
        }

        squad.Members.Remove(member);

        // Update the player's squad state
        await UpdatePlayerSquadStateAsync(userId, "");

        // If there are no members left, delete the squad
        if (squad.Members.Count == 0)
        {
            _activeSquads.Remove(squadId);
            return true;
        }

        // If the leader left, assign a new leader
        if (squad.LeaderId == userId && squad.Members.Count > 0)
        {
            squad.LeaderId = squad.Members[0].UserId;
        }

        return true;
    }

    // Set a player's ready status
    public bool SetPlayerReady(string squadId, string userId, bool isReady)
    {
        if (!_activeSquads.TryGetValue(squadId, out var squad))
        {
            return false;
        }

        var member = squad.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
        {
            return false;
        }

        member.IsReady = isReady;

        // Check if all members are ready
        squad.AllReady = squad.Members.All(m => m.IsReady);

        return true;
    }

    // Set a player's deploy flow status
    public bool SetPlayerInDeployFlow(string squadId, string userId, bool isInDeployFlow)
    {
        if (!_activeSquads.TryGetValue(squadId, out var squad))
        {
            return false;
        }

        var member = squad.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
        {
            return false;
        }

        member.IsInDeployFlow = isInDeployFlow;

        // If any member is in deploy flow, the squad is in deploy flow
        squad.InDeployFlow = squad.Members.Any(m => m.IsInDeployFlow);

        return true;
    }

    // Set the map for a squad
    public bool SetSquadMap(string squadId, string mapName)
    {
        if (!_activeSquads.TryGetValue(squadId, out var squad))
        {
            return false;
        }

        squad.MapName = mapName;
        return true;
    }

    // Start matchmaking for a squad
    public bool StartMatchmaking(string squadId)
    {
        if (!_activeSquads.TryGetValue(squadId, out var squad))
        {
            return false;
        }

        squad.MatchmakingState = 1; // Matchmaking
        return true;
    }

    // Complete matchmaking for a squad
    public bool CompleteMatchmaking(string squadId, string sessionId)
    {
        if (!_activeSquads.TryGetValue(squadId, out var squad))
        {
            return false;
        }

        squad.MatchmakingState = 2; // Matched
        squad.SessionId = sessionId;
        return true;
    }

    // Create a squad invite
    public async Task<SquadInvite> CreateSquadInviteAsync(string fromUserId, string fromDisplayName, string toUserId, string squadId)
    {
        // Check if the squad exists
        if (!_activeSquads.TryGetValue(squadId, out _))
        {
            return null;
        }

        // Create the invite
        var invite = new SquadInvite
        {
            FromUserId = fromUserId,
            FromDisplayName = fromDisplayName,
            ToUserId = toUserId,
            SquadId = squadId
        };

        // Store the invite in the target player's data
        var userData = await _userDataService.FindAsync(toUserId, toUserId, new List<string> { "SquadState" });
        
        PlayerSquadState squadState;
        if (!userData.TryGetValue("SquadState", out var squadStateData))
        {
            squadState = new PlayerSquadState();
        }
        else
        {
            squadState = JsonSerializer.Deserialize<PlayerSquadState>(squadStateData.Value);
        }

        squadState.Invites.Add(invite);

        await _userDataService.UpdateAsync(
            toUserId, 
            toUserId,
            new Dictionary<string, string> { ["SquadState"] = JsonSerializer.Serialize(squadState) }
        );

        return invite;
    }

    // Accept a squad invite
    public async Task<bool> AcceptSquadInviteAsync(string userId, string inviteId)
    {
        // Get the player's squad state
        var userData = await _userDataService.FindAsync(userId, userId, new List<string> { "SquadState" });
        
        if (!userData.TryGetValue("SquadState", out var squadStateData))
        {
            return false;
        }

        var squadState = JsonSerializer.Deserialize<PlayerSquadState>(squadStateData.Value);
        var invite = squadState.Invites.FirstOrDefault(i => i.InviteId == inviteId);
        
        if (invite == null)
        {
            return false;
        }

        // Remove the invite
        squadState.Invites.Remove(invite);

        // Get user display name
        var userProfile = await _userDataService.FindAsync(userId, userId, new List<string> { "DisplayName" });
        string displayName = "Player";
        if (userProfile.TryGetValue("DisplayName", out var displayNameData))
        {
            displayName = displayNameData.Value;
        }

        // Add the player to the squad
        var success = await AddPlayerToSquadAsync(invite.SquadId, userId, displayName);
        
        // Update the player's squad state
        await _userDataService.UpdateAsync(
            userId, 
            userId,
            new Dictionary<string, string> { ["SquadState"] = JsonSerializer.Serialize(squadState) }
        );

        return success;
    }

    // Decline a squad invite
    public async Task<bool> DeclineSquadInviteAsync(string userId, string inviteId)
    {
        // Get the player's squad state
        var userData = await _userDataService.FindAsync(userId, userId, new List<string> { "SquadState" });
        
        if (!userData.TryGetValue("SquadState", out var squadStateData))
        {
            return false;
        }

        var squadState = JsonSerializer.Deserialize<PlayerSquadState>(squadStateData.Value);
        var invite = squadState.Invites.FirstOrDefault(i => i.InviteId == inviteId);
        
        if (invite == null)
        {
            return false;
        }

        // Remove the invite
        squadState.Invites.Remove(invite);

        // Update the player's squad state
        await _userDataService.UpdateAsync(
            userId, 
            userId,
            new Dictionary<string, string> { ["SquadState"] = JsonSerializer.Serialize(squadState) }
        );

        return true;
    }

    // Helper method to update a player's squad state
    private async Task UpdatePlayerSquadStateAsync(string userId, string squadId)
    {
        var userData = await _userDataService.FindAsync(userId, userId, new List<string> { "SquadState" });
        
        PlayerSquadState squadState;
        if (!userData.TryGetValue("SquadState", out var squadStateData))
        {
            squadState = new PlayerSquadState();
        }
        else
        {
            squadState = JsonSerializer.Deserialize<PlayerSquadState>(squadStateData.Value);
        }

        squadState.SquadId = squadId;

        await _userDataService.UpdateAsync(
            userId, 
            userId,
            new Dictionary<string, string> { ["SquadState"] = JsonSerializer.Serialize(squadState) }
        );
    }

    // Get a player's squad
    public async Task<SquadData> GetPlayerSquadAsync(string userId)
    {
        var userData = await _userDataService.FindAsync(userId, userId, new List<string> { "SquadState" });
        
        if (!userData.TryGetValue("SquadState", out var squadStateData))
        {
            return null;
        }

        var squadState = JsonSerializer.Deserialize<PlayerSquadState>(squadStateData.Value);
        
        if (string.IsNullOrEmpty(squadState.SquadId))
        {
            return null;
        }

        return GetSquad(squadState.SquadId);
    }

    // Get a player's squad invites
    public async Task<List<SquadInvite>> GetPlayerSquadInvitesAsync(string userId)
    {
        var userData = await _userDataService.FindAsync(userId, userId, new List<string> { "SquadState" });
        
        if (!userData.TryGetValue("SquadState", out var squadStateData))
        {
            return new List<SquadInvite>();
        }

        var squadState = JsonSerializer.Deserialize<PlayerSquadState>(squadStateData.Value);
        return squadState.Invites;
    }
}