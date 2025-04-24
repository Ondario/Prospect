using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Hubs;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using Prospect.Server.Api.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("EnterMatchmaking")]
public class EnterMatchmakingFunction : ICloudScriptFunction<FYEnterMatchAzureFunction, FYEnterMatchAzureFunctionResult>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHubContext<CycleHub> _hubContext;
    private readonly SquadService _squadService;

    public EnterMatchmakingFunction(IHttpContextAccessor httpContextAccessor, IHubContext<CycleHub> hubContext, SquadService squadService)
    {
        _httpContextAccessor = httpContextAccessor;
        _hubContext = hubContext;
        _squadService = squadService;
    }

    public async Task<FYEnterMatchAzureFunctionResult> ExecuteAsync(FYEnterMatchAzureFunction request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }

        var userId = context.User.FindAuthUserId();
        
        // For squad matchmaking
        if (!string.IsNullOrEmpty(request.SquadId) && request.SquadId != "_")
        {
            var squad = _squadService.GetSquad(request.SquadId);
            if (squad != null)
            {
                try
                {
                    // Start matchmaking for the squad
                    _squadService.StartMatchmaking(squad.SquadId);
                    
                    // Generate a session ID for the squad
                    var sessionId = Guid.NewGuid().ToString();
                    
                    // Complete matchmaking for the squad
                    _squadService.CompleteMatchmaking(squad.SquadId, sessionId);
                    
                    // Notify all squad members about matchmaking success
                    foreach (var member in squad.Members)
                    {
                        await _hubContext.Clients.User(member.UserId).SendAsync("OnSquadMatchmakingSuccess", new OnSquadMatchmakingSuccessMessage {
                            Success = true,
                            SessionID = request.MapName, // Use the requested map
                            SquadID = squad.SquadId
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in squad matchmaking: {ex.Message}");
                }
                
                return new FYEnterMatchAzureFunctionResult
                {
                    Success = true,
                    ErrorMessage = "",
                    SingleplayerStation = false, // Important: False to actually enter a match
                    Address = request.MapName,
                    MaintenanceMode = false,
                    Port = 7777,
                };
            }
        }

        // Solo player - just use whatever map was requested
        try
        {
            // The client expects this notification
            await _hubContext.Clients.User(userId).SendAsync("OnSquadMatchmakingSuccess", new OnSquadMatchmakingSuccessMessage {
                Success = true,
                SessionID = request.MapName,
                SquadID = request.SquadId ?? "_"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending solo matchmaking notification: {ex.Message}");
        }

        return new FYEnterMatchAzureFunctionResult
        {
            Success = true,
            ErrorMessage = "",
            // THIS IS CRITICAL: False means it will enter a map, True means it will go to station
            SingleplayerStation = false,
            Address = request.MapName,
            MaintenanceMode = false,
            Port = 7777,
        };
    }
}