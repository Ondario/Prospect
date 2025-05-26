using System.Text.Json;
using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Data;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models.Data;
using Prospect.Server.Api.Services.UserData;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYClaimCompletedActiveContractRewardsRequest {
    [JsonPropertyName("contractId")]
	public string ContractID { get; set; }
    [JsonPropertyName("contractsToUnlock")]
	public string[] ContractsToUnlock { get; set; }
}

public class FYClaimCompletedActiveContractRewardsResult {
    [JsonPropertyName("userId")]
	public string UserID { get; set; }
    [JsonPropertyName("error")]
	public string Error { get; set; }
    [JsonPropertyName("claimedContractId")]
	public string ClaimedContractID { get; set; }
    [JsonPropertyName("newContractIdOnBoard")]
	public string NewContractIdOnBoard { get; set; }
    [JsonPropertyName("changedCurrencies")]
	public FYCurrencyItem[] ChangedCurrencies { get; set; }
    [JsonPropertyName("itemsGranted")]
	public List<FYCustomItemInfo> ItemsGranted { get; set; }
    [JsonPropertyName("itemsUpdatedOrRemoved")]
	public List<FYCustomItemInfo> ItemsUpdatedOrRemoved { get; set; }
    [JsonPropertyName("playerFactionProgressData")]
	public FYPlayerFactionProgressData PlayerFactionProgressData { get; set; }
    [JsonPropertyName("contractsActivated")]
	public List<FYActiveContractPlayerData> ContractsActivated { get; set; }
    [JsonPropertyName("status")]
	public int Status { get; set; } // EYClaimContractRewardsStatus
    [JsonPropertyName("updatedSeasonXp")]
	public int UpdatedSeasonXp { get; set; }
}

public class FYGetCompletedContractsResult {
    [JsonPropertyName("contractsIds")]
	public List<string> ContractsIDs { get; set; }
};

[CloudScriptFunction("ClaimActiveContract")]
public class ClaimActiveContract : ICloudScriptFunction<FYClaimCompletedActiveContractRewardsRequest, FYClaimCompletedActiveContractRewardsResult>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserDataService _userDataService;
    private readonly TitleDataService _titleDataService;
    private readonly EYContractDifficulty[] difficulties = [EYContractDifficulty.Easy, EYContractDifficulty.Medium, EYContractDifficulty.Hard];
    private readonly Random rnd = new Random();

    public ClaimActiveContract(IHttpContextAccessor httpContextAccessor, UserDataService userDataService, TitleDataService titleDataService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userDataService = userDataService;
        _titleDataService = titleDataService;
    }

    public async Task<FYClaimCompletedActiveContractRewardsResult> ExecuteAsync(FYClaimCompletedActiveContractRewardsRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }
        var userId = context.User.FindAuthUserId();
        var titleData = _titleDataService.Find(new List<string>{ "Contracts", "Blueprints", "Jobs", "LevelData" });

        var contracts = JsonSerializer.Deserialize<Dictionary<string, TitleDataContractInfo>>(titleData["Contracts"]);
        if (!contracts.TryGetValue(request.ContractID, out var contract)) {
            return new FYClaimCompletedActiveContractRewardsResult
            {
                UserID = userId,
                Error = "Contract not found",
                Status = 13, // EYClaimContractRewardsStatus::WRONG_CONTRACT_ID
            };
        }

        var factionKey = "FactionProgression" + contract.Faction;
        var userData = await _userDataService.FindAsync(
            userId, userId,
            new List<string>{"ContractsActive", "ContractsOneTimeCompleted", "Balance", "Inventory", factionKey, "JobBoardsData" }
        );

        var factionProgression = JsonSerializer.Deserialize<int>(userData[factionKey].Value);
        var contractsActive = JsonSerializer.Deserialize<FYGetActiveContractsResult>(userData["ContractsActive"].Value);
        var contractsCompleted = JsonSerializer.Deserialize<FYGetCompletedContractsResult>(userData["ContractsOneTimeCompleted"].Value);
        var jobBoardsData = JsonSerializer.Deserialize<JobBoardsData>(userData["JobBoardsData"].Value);
        var balance = JsonSerializer.Deserialize<PlayerBalance>(userData["Balance"].Value);
        var inventory = JsonSerializer.Deserialize<List<FYCustomItemInfo>>(userData["Inventory"].Value);

        var targetContractIdx = contractsActive.Contracts.FindIndex(item => item.ContractID == contract.Name);
        if (targetContractIdx == -1) {
            return new FYClaimCompletedActiveContractRewardsResult
            {
                UserID = userId,
                Error = "Contract not active",
                Status = 8, // EYClaimContractRewardsStatus::NO_ACTIVE_CONTRACT
            };
        }

        var blueprints = JsonSerializer.Deserialize<Dictionary<string, TitleDataBlueprintInfo>>(titleData["Blueprints"]);

        var targetContract = contractsActive.Contracts[targetContractIdx];
        List<FYCustomItemInfo> itemsUpdatedOrRemoved = [];
        HashSet<string> deletedItemsIds = [];
        // TODO: Optimize
        for (var i = 0; i < contract.Objectives.Length; i++) {
            var objective = contract.Objectives[i];
            int remaining = objective.MaxProgress;
            if (objective.Type == EYContractObjectiveType.OwnNumOfItem) {
                // NOTE: The backend may perform item ownership validation
                // since it has the information about the player's items.
                foreach (var item in inventory) {
                    if (item.BaseItemId != objective.ItemToOwn) {
                        continue;
                    }
                    var localRemaining = remaining;
                    remaining -= item.Amount;
                    item.Amount -= localRemaining;
                    if (item.Amount <= 0) {
                        item.Amount = 0;
                        deletedItemsIds.Add(item.ItemId);
                    }
                    itemsUpdatedOrRemoved.Add(item);
                    if (remaining <= 0) {
                        break;
                    }
                }
            } else {
                // NOTE: A game server must write the correct progress
                // for other types of objectives.
                // The objectives are mapped by array index.
                remaining -= targetContract.Progress[i];
            }
            if (remaining > 0) {
                return new FYClaimCompletedActiveContractRewardsResult
                {
                    UserID = userId,
                    Error = "Objective not met",
                    Status = 16, // EYClaimContractRewardsStatus::PROGRESS_MISSING
                };
            }
        }

        // TODO: Optimize
        var newInventory = new List<FYCustomItemInfo>(inventory.Count);
        foreach (var item in inventory) {
            if (!deletedItemsIds.Contains(item.ItemId)) {
                newInventory.Add(item);
            }
        }

        List<FYCustomItemInfo> itemsGranted = new List<FYCustomItemInfo>();
        List<FYCurrencyItem> changedCurrencies = [];
        foreach (var reward in contract.Rewards) {
            if (reward.ItemID == "SoftCurrency") {
                balance.SoftCurrency += reward.Amount;
                changedCurrencies.Add(new FYCurrencyItem { CurrencyName = "SoftCurrency", Amount = balance.SoftCurrency });
            } else if (reward.ItemID == "Aurum") {
                balance.HardCurrency += reward.Amount;
                changedCurrencies.Add(new FYCurrencyItem { CurrencyName = "Aurum", Amount = balance.HardCurrency });
            } else if (reward.ItemID == "InsuranceCurrency") {
                balance.InsuranceCurrency += reward.Amount;
                changedCurrencies.Add(new FYCurrencyItem { CurrencyName = "InsuranceCurrency", Amount = balance.InsuranceCurrency });
            } else {
                // TODO: Inventory limit check

                var blueprintData = blueprints[reward.ItemID];
                var remainingAmount = reward.Amount; // Quest rewards are given individually

                // Populate incomplete stacks first
                foreach (var item in newInventory) {
                    if (item.BaseItemId != reward.ItemID || item.Amount >= blueprintData.MaxAmountPerStack) {
                        continue;
                    }
                    var remainingSpace = blueprintData.MaxAmountPerStack - item.Amount;
                    var amountToAdd = Math.Min(remainingAmount, remainingSpace);
                    item.Amount += amountToAdd;
                    remainingAmount -= amountToAdd;
                    itemsUpdatedOrRemoved.Add(item);
                    if (remainingAmount == 0) {
                        break;
                    }
                }

                while (remainingAmount > 0) {
                    var itemStackAmount = Math.Min(blueprintData.MaxAmountPerStack, remainingAmount);
                    var itemInfo = new FYCustomItemInfo{
                        ItemId = Guid.NewGuid().ToString(),
                        Amount = itemStackAmount,
                        BaseItemId = reward.ItemID,
                        Durability = blueprintData.DurabilityMax,
                        Insurance = "",
                        InsuranceOwnerPlayfabId = "",
                        ModData = new FYModItems { M = [] },
                        Origin = new FYItemOriginBackend { G = "", P = "", T = "" },
                        InsuredAttachmentId = "",
                        PrimaryVanityId = 0,
                        SecondaryVanityId = 0,
                        RolledPerks = [],
                    };
                    newInventory.Add(itemInfo);
                    itemsGranted.Add(itemInfo);
                    remainingAmount -= itemStackAmount;
                }
            }
        }

        factionProgression += contract.ReputationIncrease;
        contractsActive.Contracts.RemoveAt(targetContractIdx);
        // Main contracts can be completed only once.
        // Other contracts should be jobs.
        var newContractIdOnBoard = "";
        if (contract.IsMainContract) {
            contractsCompleted.ContractsIDs.Add(contract.Name);
        } else {
            var contractDifficulty = contract.Difficulty;

            var levelsData = JsonSerializer.Deserialize<Dictionary<string, int[]>>(titleData["LevelData"]);
            var levels = levelsData[contract.Faction];
            var jobs = JsonSerializer.Deserialize<Dictionary<string, Dictionary<EYContractDifficulty, string[]>>>(titleData["Jobs"]);
            var level = levels.Length - Array.FindIndex(levels, (int xp) => {
                return factionProgression >= xp;
            });

            var factionBoard = jobBoardsData.Boards.Find(c => c.FactionId == contract.Faction);
            var idx = Array.FindIndex(difficulties, d => d == contractDifficulty);
            var factionJobs = jobs[contract.Faction];
            var jobsAvailable = Array.FindAll(factionJobs[contractDifficulty], (string jobId) => {
                return level >= contracts[jobId].UnlockData.Level;
            });
            // If player completes a job, it means he has at least one unlocked anyway.
            newContractIdOnBoard = jobsAvailable[rnd.Next(jobsAvailable.Length)];
            factionBoard.Contracts[idx].ContractId = newContractIdOnBoard;
        }

        // TODO: Check for next unlock in contracts instead of relying on client
        var newContracts = new List<FYActiveContractPlayerData>();
        foreach (var contractIdToUnlock in request.ContractsToUnlock) {
            if (contractsCompleted.ContractsIDs.Contains(contractIdToUnlock)) {
                continue;
            }
            var contractToUnlock = contracts[contractIdToUnlock];
            var progress = new int[contractToUnlock.Objectives.Length];
            // NOTE: Automatically set max progress for player kill objectives.
            for (var i = 0; i < contractToUnlock.Objectives.Length; i++) {
                var objective = contractToUnlock.Objectives[i];
                if (objective.Type == EYContractObjectiveType.Kills && objective.KillConditions.KillTarget == EYKillTypeAction.Players) {
                    progress[i] = objective.MaxProgress;
                }
            }
            var newContract = new FYActiveContractPlayerData {
                ContractID = contractIdToUnlock,
                Progress = progress,
            };
            contractsActive.Contracts.Add(newContract);
            newContracts.Add(newContract);
        }

        await _userDataService.UpdateAsync(
            userId, userId,
            new Dictionary<string, string>{
                ["ContractsOneTimeCompleted"] = JsonSerializer.Serialize(contractsCompleted),
                ["ContractsActive"] = JsonSerializer.Serialize(contractsActive),
                ["Balance"] = JsonSerializer.Serialize(balance),
                ["Inventory"] = JsonSerializer.Serialize(newInventory),
                [factionKey] = JsonSerializer.Serialize(factionProgression),
                ["JobBoardsData"] = JsonSerializer.Serialize(jobBoardsData),
            }
        );

        return new FYClaimCompletedActiveContractRewardsResult
        {
            UserID = userId,
            Error = "",
            ClaimedContractID = contract.Name,
            ContractsActivated = newContracts,
            ItemsUpdatedOrRemoved = itemsUpdatedOrRemoved,
            ChangedCurrencies = changedCurrencies.ToArray(),
            ItemsGranted = itemsGranted,
            NewContractIdOnBoard = newContractIdOnBoard,
            PlayerFactionProgressData = new FYPlayerFactionProgressData {
                FactionID = contract.Faction,
                CurrentProgression = factionProgression,
            },
            Status = 18, // EYClaimContractRewardsStatus::OK
            UpdatedSeasonXp = 0 // TODO: Probably a separate season XP rate configured by server?
        };
    }
}