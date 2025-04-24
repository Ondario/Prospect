using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Prospect.Server.Api.Hubs;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.Squad;
using Prospect.Server.Api.Services.UserData;
using Prospect.Server.Api.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("EnterMatchmakingMatch")]
public class EnterMatchmakingMatchFunction : ICloudScriptFunction<FYEnterMatchAzureFunction, FYEnterMatchmakingResult>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHubContext<CycleHub> _hubContext;
    private readonly UserDataService _userDataService;
    private readonly TitleDataService _titleDataService;
    private readonly SquadService _squadService;

    public EnterMatchmakingMatchFunction(
        IHubContext<CycleHub> hubContext, 
        IHttpContextAccessor httpContextAccessor, 
        UserDataService userDataService, 
        TitleDataService titleDataService,
        SquadService squadService)
    {
        _httpContextAccessor = httpContextAccessor;
        _hubContext = hubContext;
        _userDataService = userDataService;
        _titleDataService = titleDataService;
        _squadService = squadService;
    }

    public async Task<FYEnterMatchmakingResult> ExecuteAsync(FYEnterMatchAzureFunction request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }

        var userId = context.User.FindAuthUserId();
        var titleData = _titleDataService.Find(new List<string>{"Contracts"});
        var contracts = JsonSerializer.Deserialize<Dictionary<string, TitleDataContractInfo>>(titleData["Contracts"]);

        var userData = await _userDataService.FindAsync(
            userId, userId,
            new List<string>{"ContractsActive", "Inventory"}
        );
        var inventory = JsonSerializer.Deserialize<List<FYCustomItemInfo>>(userData["Inventory"].Value);
        var contractsActive = JsonSerializer.Deserialize<FYGetActiveContractsResult>(userData["ContractsActive"].Value);
        
        // Update contract progress for delivery quests
        foreach (var contractActive in contractsActive.Contracts) {
            if (!contracts.TryGetValue(contractActive.ContractID, out var contract)) {
                continue;
            }
            for (var i = 0; i < contract.Objectives.Length; i++) {
                var objective = contract.Objectives[i];
                if (objective.Type != EYContractObjectiveType.OwnNumOfItem) {
                    continue;
                }
                int remaining = objective.MaxProgress;
                foreach (var item in inventory) {
                    if (item.BaseItemId != objective.ItemToOwn) {
                        continue;
                    }
                    remaining -= item.Amount;
                    if (remaining <= 0) {
                        remaining = 0;
                        break;
                    }
                }
                contractActive.Progress[i] = objective.MaxProgress - remaining;
            }
        }

        await _userDataService.UpdateAsync(
            userId, userId,
            new Dictionary<string, string>{
                ["ContractsActive"] = JsonSerializer.Serialize(contractsActive),
            }
        );

        // For squad matchmaking
        if (!string.IsNullOrEmpty(request.SquadId) && request.SquadId != "_")
        {
            var squad = _squadService.GetSquad(request.SquadId);
            if (squad != null)
            {
                try
                {
                    // Ensure we have a session ID for the squad
                    if (string.IsNullOrEmpty(squad.SessionId))
                    {
                        // Generate a session ID for the squad
                        squad.SessionId = Guid.NewGuid().ToString();
                        
                        // Complete matchmaking for the squad
                        _squadService.CompleteMatchmaking(squad.SquadId, squad.SessionId);
                    }
                    
                    // Notify all squad members about matchmaking success
                    await _hubContext.Clients.Group($"squad_{squad.SquadId}").SendAsync("OnSquadMatchmakingSuccess", new OnSquadMatchmakingSuccessMessage {
                        Success = true,
                        SessionID = request.MapName,
                        SquadID = squad.SquadId
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in squad match entry: {ex.Message}");
                }
                
                return new FYEnterMatchmakingResult
                {
                    Success = true,
                    ErrorMessage = "",
                    SingleplayerStation = false, // Important: False to actually enter a match
                    NumAttempts = 1,
                    Blocker = 0,
                    IsMatchTravel = true,
                    SessionId = squad.SessionId
                };
            }
        }

        // Solo player
        try
        {
            await _hubContext.Clients.User(userId).SendAsync("OnSquadMatchmakingSuccess", new OnSquadMatchmakingSuccessMessage {
                Success = true,
                SessionID = request.MapName,
                SquadID = request.SquadId ?? "_"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending solo match entry notification: {ex.Message}");
        }

        return new FYEnterMatchmakingResult
        {
            Success = true,
            ErrorMessage = "",
            SingleplayerStation = false, // VERY IMPORTANT: Must be false to enter match
            NumAttempts = 1,
            Blocker = 0,
            IsMatchTravel = true,
            SessionId = Guid.NewGuid().ToString()
        };
    }
}