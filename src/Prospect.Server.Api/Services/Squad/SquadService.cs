using Prospect.Server.Api.Models.Data;
using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Hubs;
using System.Collections.Concurrent;

namespace Prospect.Server.Api.Services.Squad
{
    public class SquadService
    {
        private readonly ILogger<SquadService> _logger;
        private readonly IHubContext<CycleHub> _hubContext;
        private readonly Services.UserData.UserDataService _userDataService;
        
        // Using ConcurrentDictionary for thread safety
        private readonly ConcurrentDictionary<string, SquadData> _squads = new();
        private readonly ConcurrentDictionary<string, PlayerSquadState> _playerSquadStates = new();

        public SquadService(
            ILogger<SquadService> logger,
            IHubContext<CycleHub> hubContext,
            Services.UserData.UserDataService userDataService)
        {
            _logger = logger;
            _hubContext = hubContext;
            _userDataService = userDataService;
            // Start periodic orphaned squad cleanup
            StartOrphanedSquadCleanup();
        }

        public async Task<SquadData> CreateSquadAsync(string leaderId, string leaderDisplayName)
        {
            _logger.LogInformation("Creating squad for leader {LeaderId}", leaderId);
            
            // Check if player is already in a squad
            var existingSquad = await GetPlayerSquadAsync(leaderId);
            if (existingSquad != null)
            {
                _logger.LogInformation("Player {LeaderId} is already in squad {SquadId}", leaderId, existingSquad.SquadId);
                return existingSquad;
            }
            
            // Create new squad
            var squad = new SquadData
            {
                SquadId = Guid.NewGuid().ToString(),
                LeaderId = leaderId,
                Members = new List<SquadMember>
                {
                    new SquadMember
                    {
                        UserId = leaderId,
                        DisplayName = leaderDisplayName,
                        IsConnected = true
                    }
                }
            };
            
            // Store the squad in memory only
            _squads[squad.SquadId] = squad;
            
            // Update player's squad state
            var playerState = await GetPlayerSquadStateAsync(leaderId);
            playerState.SquadId = squad.SquadId;
            await SavePlayerSquadStateAsync(leaderId, playerState);
            
            _logger.LogInformation("Created squad {SquadId} for leader {LeaderId}", squad.SquadId, leaderId);
            
            return squad;
        }

        public SquadData GetSquad(string squadId)
        {
            // Handle null or empty squad ID
            if (string.IsNullOrEmpty(squadId) || squadId == "_")
            {
                _logger.LogInformation("GetSquad called with null or empty squad ID");
                return null;
            }

            // Try to get the squad from memory only
            _squads.TryGetValue(squadId, out var squad);
            return squad;
        }
        
        public async Task<SquadData> GetPlayerSquadAsync(string userId)
        {
            var playerState = await GetPlayerSquadStateAsync(userId);
            if (string.IsNullOrEmpty(playerState.SquadId))
            {
                return null;
            }
            
            return GetSquad(playerState.SquadId);
        }
        
        // Method used by GetSquadInvites.cs
        public async Task<List<SquadInvite>> GetPlayerSquadInvitesAsync(string userId)
        {
            var playerState = await GetPlayerSquadStateAsync(userId);
            return playerState.Invites;
        }
        
        // Method used by InviteToSquad.cs - updated to support 4 parameters
        public async Task<SquadInvite> CreateSquadInviteAsync(string fromUserId, string toUserId, string fromUserDisplayName, string toUserDisplayName)
        {
            var fromPlayerState = await GetPlayerSquadStateAsync(fromUserId);
            if (string.IsNullOrEmpty(fromPlayerState.SquadId))
            {
                return null;
            }
            
            var squad = GetSquad(fromPlayerState.SquadId);
            if (squad == null)
            {
                return null;
            }
            
            // Check if user is already in this squad
            if (squad.Members.Any(m => m.UserId == toUserId))
            {
                return null;
            }
            
            // Create invite
            var invite = new SquadInvite
            {
                InviteId = Guid.NewGuid().ToString(),
                FromUserId = fromUserId,
                FromDisplayName = fromUserDisplayName,
                ToUserId = toUserId,
                SquadId = squad.SquadId,
                CreatedAt = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            // Add invite to target player's state
            var toPlayerState = await GetPlayerSquadStateAsync(toUserId);
            toPlayerState.Invites.Add(invite);
            await SavePlayerSquadStateAsync(toUserId, toPlayerState);
            
            // Notify target player of invite via SignalR
            var connectionId = CycleHub.GetConnectionIdForUser(toUserId);
            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("SquadInviteReceived", new
                {
                    InviteId = invite.InviteId,
                    FromUserId = invite.FromUserId,
                    FromDisplayName = invite.FromDisplayName,
                    SquadId = invite.SquadId
                });
            }
            
            return invite;
        }
        
        // Method used by LeaveSquad.cs - updated to support 2 parameters
        public async Task<bool> RemovePlayerFromSquadAsync(string userId, string squadId)
        {
            PlayerSquadState userState;
            
            // If squadId is not provided, get it from player state
            if (string.IsNullOrEmpty(squadId))
            {
                userState = await GetPlayerSquadStateAsync(userId);
                squadId = userState.SquadId;
                
                if (string.IsNullOrEmpty(squadId))
                {
                    _logger.LogWarning("RemovePlayerFromSquadAsync: No squadId for user {UserId}", userId);
                    return false;
                }
            }
            else
            {
                // Get player state
                userState = await GetPlayerSquadStateAsync(userId);
            }
            
            var squad = GetSquad(squadId);
            if (squad == null)
            {
                _logger.LogWarning("RemovePlayerFromSquadAsync: Squad {SquadId} not found for user {UserId}", squadId, userId);
                // Defensive: clear player state anyway
                userState.SquadId = "";
                await SavePlayerSquadStateAsync(userId, userState);
                return false;
            }
            
            // Remove member from squad
            var member = squad.Members.FirstOrDefault(m => m.UserId == userId);
            if (member != null)
            {
                squad.Members.Remove(member);
                _logger.LogInformation("User {UserId} left squad {SquadId}", userId, squadId);
            }
            else
            {
                _logger.LogWarning("RemovePlayerFromSquadAsync: Member {UserId} not found in squad {SquadId}", userId, squadId);
            }
            
            // Clear player squad ID
            userState.SquadId = "";
            await SavePlayerSquadStateAsync(userId, userState);
            _logger.LogInformation("Cleared PlayerSquadState for user {UserId}", userId);
            
            // If this was the leader, assign a new leader or delete the squad
            if (userId == squad.LeaderId)
            {
                if (squad.Members.Count > 0)
                {
                    squad.LeaderId = squad.Members[0].UserId;
                    _logger.LogInformation("Assigned new leader {LeaderId} for squad {SquadId}", squad.LeaderId, squadId);
                }
                else
                {
                    _squads.TryRemove(squad.SquadId, out _);
                    _logger.LogInformation("Squad {SquadId} destroyed (no members left)", squad.SquadId);
                    return true;
                }
            }
            // If no members left, destroy the squad
            if (squad.Members.Count == 0)
            {
                _squads.TryRemove(squad.SquadId, out _);
                _logger.LogInformation("Squad {SquadId} destroyed (no members left)", squad.SquadId);
                return true;
            }
            
            // Notify remaining squad members
            await NotifySquadUpdatedAsync(squad);
            
            return true;
        }
        
        // Method used by SquadMemberReadyForMatch.cs
        public bool SetPlayerReady(string squadId, string userId, bool isReady)
        {
            var squad = GetSquad(squadId);
            if (squad == null)
            {
                _logger.LogWarning("SetPlayerReady: Squad {SquadId} not found", squadId);
                return false;
            }
            
            var member = squad.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
            {
                _logger.LogWarning("SetPlayerReady: Member {UserId} not found in squad {SquadId}", userId, squadId);
                return false;
            }
            
            member.IsReady = isReady;
            
            // Check if all members are ready
            squad.AllReady = squad.Members.All(m => m.IsReady);
            
            // Notify squad members
            NotifySquadUpdatedAsync(squad).Wait();
            
            return true;
        }
        
        // Method used by SquadMemberSelectedMap.cs
        public bool SetSquadMap(string squadId, string mapName)
        {
            var squad = GetSquad(squadId);
            if (squad == null)
            {
                _logger.LogWarning("SetSquadMap: Squad {SquadId} not found", squadId);
                return false;
            }
            
            squad.MapName = mapName;
            
            // Notify squad members
            NotifySquadUpdatedAsync(squad).Wait();
            
            return true;
        }
        
        // Method used by SquadMemberStartingDeployFlow.cs
        public bool SetPlayerInDeployFlow(string squadId, string userId, bool inDeployFlow)
        {
            var squad = GetSquad(squadId);
            if (squad == null)
            {
                _logger.LogWarning("SetPlayerInDeployFlow: Squad {SquadId} not found", squadId);
                return false;
            }
            
            var member = squad.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
            {
                _logger.LogWarning("SetPlayerInDeployFlow: Member {UserId} not found in squad {SquadId}", userId, squadId);
                return false;
            }
            
            member.IsInDeployFlow = inDeployFlow;
            
            // Update squad deploy flow state if all members are in deploy flow
            squad.InDeployFlow = squad.Members.All(m => m.IsInDeployFlow);
            
            // Notify squad members
            NotifySquadUpdatedAsync(squad).Wait();
            
            return true;
        }
        
        public async Task<bool> AcceptSquadInviteAsync(string userId, string inviteId)
        {
            var playerState = await GetPlayerSquadStateAsync(userId);
            
            // Find the invite
            var invite = playerState.Invites.FirstOrDefault(i => i.InviteId == inviteId);
            if (invite == null)
            {
                _logger.LogWarning("Invite {InviteId} not found for user {UserId}", inviteId, userId);
                return false;
            }
            
            // Get the squad
            var squad = GetSquad(invite.SquadId);
            if (squad == null)
            {
                _logger.LogWarning("Squad {SquadId} not found for invite {InviteId}", invite.SquadId, inviteId);
                return false;
            }
            
            // Get user data for display name
            var userData = await _userDataService.FindAsync(userId, userId, new List<string> { "DisplayName" });
            string displayName = "Player";
            if (userData.TryGetValue("DisplayName", out var displayNameData))
            {
                displayName = displayNameData.Value;
            }
            
            // Add member to squad
            squad.Members.Add(new SquadMember
            {
                UserId = userId,
                DisplayName = displayName,
                IsConnected = true
            });
            
            // Update player squad state
            playerState.SquadId = squad.SquadId;
            playerState.Invites.Remove(invite);
            await SavePlayerSquadStateAsync(userId, playerState);
            
            // Notify squad members
            await NotifySquadUpdatedAsync(squad);
            
            return true;
        }
        
        public async Task<bool> DeclineSquadInviteAsync(string userId, string inviteId)
        {
            var playerState = await GetPlayerSquadStateAsync(userId);
            
            // Find and remove the invite
            var invite = playerState.Invites.FirstOrDefault(i => i.InviteId == inviteId);
            if (invite == null)
            {
                return false;
            }
            
            playerState.Invites.Remove(invite);
            await SavePlayerSquadStateAsync(userId, playerState);
            
            return true;
        }
        
        public async Task<bool> LeaveSquadAsync(string userId)
        {
            return await RemovePlayerFromSquadAsync(userId, null);
        }
        
        public async Task<SquadInvite> InviteToSquadAsync(string fromUserId, string toUserId)
        {
            // Get inviter display name
            var userData = await _userDataService.FindAsync(fromUserId, fromUserId, new List<string> { "DisplayName" });
            string fromDisplayName = "Player";
            if (userData.TryGetValue("DisplayName", out var displayNameData))
            {
                fromDisplayName = displayNameData.Value;
            }
            
            return await CreateSquadInviteAsync(fromUserId, toUserId, fromDisplayName, "");
        }
        
        public void StartMatchmaking(string squadId)
        {
            if (string.IsNullOrEmpty(squadId) || squadId == "_")
            {
                _logger.LogInformation("StartMatchmaking called with invalid squad ID: {SquadId}", squadId);
                return;
            }
            
            var squad = GetSquad(squadId);
            if (squad == null)
            {
                _logger.LogWarning("StartMatchmaking: Squad {SquadId} not found", squadId);
                return;
            }
            
            _logger.LogInformation("Starting matchmaking for squad {SquadId}", squadId);
            squad.MatchmakingState = 1; // 1 = matchmaking
        }
        
        public void CompleteMatchmaking(string squadId, string sessionId)
        {
            if (string.IsNullOrEmpty(squadId) || squadId == "_")
            {
                _logger.LogInformation("CompleteMatchmaking called with invalid squad ID: {SquadId}", squadId);
                return;
            }
            
            var squad = GetSquad(squadId);
            if (squad == null)
            {
                _logger.LogWarning("CompleteMatchmaking: Squad {SquadId} not found", squadId);
                return;
            }
            
            _logger.LogInformation("Completing matchmaking for squad {SquadId} with session {SessionId}", squadId, sessionId);
            squad.MatchmakingState = 2; // 2 = matched
            squad.SessionId = sessionId;
        }
        
        private async Task<PlayerSquadState> GetPlayerSquadStateAsync(string userId)
        {
            // Check if we already have it in memory
            if (_playerSquadStates.TryGetValue(userId, out var state))
            {
                return state;
            }
            
            // Try to load from user data
            try
            {
                var userData = await _userDataService.FindAsync(userId, userId, new List<string> { "SquadState" });
                if (userData.TryGetValue("SquadState", out var squadStateData))
                {
                    state = System.Text.Json.JsonSerializer.Deserialize<PlayerSquadState>(squadStateData.Value);
                    if (state != null)
                    {
                        _playerSquadStates[userId] = state;
                        return state;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading player squad state for {UserId}", userId);
            }
            
            // Create new state
            state = new PlayerSquadState();
            _playerSquadStates[userId] = state;
            return state;
        }
        
        private async Task SavePlayerSquadStateAsync(string userId, PlayerSquadState state)
        {
            // Update in memory
            _playerSquadStates[userId] = state;
            
            // Save to user data
            try
            {
                await _userDataService.UpdateAsync(
                    userId, 
                    userId, 
                    new Dictionary<string, string> { 
                        ["SquadState"] = System.Text.Json.JsonSerializer.Serialize(state) 
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving player squad state for {UserId}", userId);
            }
        }
        
        private async Task NotifySquadUpdatedAsync(SquadData squad)
        {
            foreach (var member in squad.Members)
            {
                var connectionId = CycleHub.GetConnectionIdForUser(member.UserId);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("SquadUpdated", squad);
                }
            }
        }

        // Periodic cleanup for orphaned squads
        private void StartOrphanedSquadCleanup()
        {
            var timer = new System.Threading.Timer(_ => CleanupOrphanedSquads(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private void CleanupOrphanedSquads()
        {
            foreach (var kvp in _squads.ToList())
            {
                var squadId = kvp.Key;
                var squad = kvp.Value;
                if (squad.Members == null || squad.Members.Count == 0)
                {
                    _squads.TryRemove(squadId, out _);
                    _logger.LogWarning("Orphaned squad {SquadId} cleaned up (no members)", squadId);
                }
            }
        }
    }
}