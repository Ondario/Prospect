using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.Squad;
using System.Collections.Concurrent;

namespace Prospect.Server.Api.Hubs;

public class CycleHub : Hub
{
    private readonly ILogger<CycleHub> _logger;
    private readonly SquadService _squadService;
    
    // Store connection IDs for each user
    private static readonly ConcurrentDictionary<string, string> _userConnectionMap = new();

    public CycleHub(ILogger<CycleHub> logger, SquadService squadService)
    {
        _logger = logger;
        _squadService = squadService;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = Context.User.FindAuthUserId();
            _logger.LogInformation("User {UserId} connected with connection ID {ConnectionId}", userId, Context.ConnectionId);
            
            // Store the connection ID for the user
            _userConnectionMap[userId] = Context.ConnectionId;
            
            // Add the connection to the user's group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            // Check if user is in a squad
            var squad = await _squadService.GetPlayerSquadAsync(userId);
            if (squad != null)
            {
                // Add the connection to the squad group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"squad_{squad.SquadId}");
                
                // Update the user's connection status in the squad
                var member = squad.Members.FirstOrDefault(m => m.UserId == userId);
                if (member != null)
                {
                    member.IsConnected = true;
                    
                    // Notify squad members of the status change
                    await Clients.Group($"squad_{squad.SquadId}").SendAsync("SquadMemberConnectionChanged", new
                    {
                        SquadId = squad.SquadId,
                        UserId = userId,
                        IsConnected = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = Context.User.FindAuthUserId();
            _logger.LogInformation("User {UserId} disconnected with connection ID {ConnectionId}", userId, Context.ConnectionId);
            
            // Remove the connection ID for the user
            _userConnectionMap.TryRemove(userId, out _);
            
            // Remove the connection from the user's group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            // Check if user is in a squad
            var squad = await _squadService.GetPlayerSquadAsync(userId);
            if (squad != null)
            {
                // Remove the connection from the squad group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"squad_{squad.SquadId}");
                
                // Update the user's connection status in the squad
                var member = squad.Members.FirstOrDefault(m => m.UserId == userId);
                if (member != null)
                {
                    member.IsConnected = false;
                    
                    // Notify squad members of the status change
                    await Clients.Group($"squad_{squad.SquadId}").SendAsync("SquadMemberConnectionChanged", new
                    {
                        SquadId = squad.SquadId,
                        UserId = userId,
                        IsConnected = false
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisconnectedAsync");
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    // Helper method to get the connection ID for a user
    public static string GetConnectionIdForUser(string userId)
    {
        _userConnectionMap.TryGetValue(userId, out var connectionId);
        return connectionId;
    }
}