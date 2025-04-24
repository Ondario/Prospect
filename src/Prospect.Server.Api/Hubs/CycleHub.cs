using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Services.Auth.Extensions;
using System.Collections.Concurrent;

namespace Prospect.Server.Api.Hubs;

public class CycleHub : Hub
{
    private readonly ILogger<CycleHub> _logger;
    
    // Store connection IDs for each user
    private static readonly ConcurrentDictionary<string, string> _userConnectionMap = new();

    public CycleHub(ILogger<CycleHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            // Try to get user ID if authenticated
            string userId = null;
            try
            {
                if (Context.User != null && Context.User.Identity?.IsAuthenticated == true)
                {
                    userId = Context.User.FindAuthUserId();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unable to get user ID from context: {Error}", ex.Message);
                // Continue without user ID - the connection is still valid
            }

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} connected with connection ID {ConnectionId}", 
                    userId, Context.ConnectionId);
                
                // Store the connection ID for the user
                _userConnectionMap[userId] = Context.ConnectionId;
                
                // Add the connection to user group for direct messaging
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            else
            {
                _logger.LogInformation("Anonymous connection {ConnectionId}", Context.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        try
        {
            // Try to get user ID if authenticated
            string userId = null;
            try
            {
                if (Context.User != null && Context.User.Identity?.IsAuthenticated == true)
                {
                    userId = Context.User.FindAuthUserId();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unable to get user ID during disconnect: {Error}", ex.Message);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} disconnected with connection ID {ConnectionId}", 
                    userId, Context.ConnectionId);
                
                // Remove the connection ID for the user
                _userConnectionMap.TryRemove(userId, out _);
                
                // Remove connection from user group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            else
            {
                _logger.LogInformation("Anonymous connection {ConnectionId} disconnected", Context.ConnectionId);
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
        if (string.IsNullOrEmpty(userId))
            return null;
            
        _userConnectionMap.TryGetValue(userId, out var connectionId);
        return connectionId;
    }
}