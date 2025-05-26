using System.Text.Json;
using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Data;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models.Data;
using Prospect.Server.Api.Services.UserData;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

public class FYClaimGeneratorIncomeClientResult {
    [JsonPropertyName("userId")]
	public string UserId { get; set; }
    [JsonPropertyName("error")]
	public string Error { get; set; }
    [JsonPropertyName("generatorId")]
	public string GeneratorID { get; set; }
    [JsonPropertyName("changedCurrenciesBalances")]
	public FYCurrencyItem[] ChangedCurrenciesBalances { get; set; }
    [JsonPropertyName("grantedItems")]
	public List<FYCustomItemInfo> GrantedItems { get; set; }
    [JsonPropertyName("lastClaimTime")]
	public FYTimestamp LastClaimTime { get; set; }
};

public class FYClaimGeneratorIncomeClientRequest {
    [JsonPropertyName("generatorId")]
	public string GeneratorID { get; set; }
};

public class FYPassiveGenerator {
    [JsonPropertyName("generatorId")]
    public string GeneratorID { get; set; }
    [JsonPropertyName("lastClaimTime")]
	public FYTimestamp LastClaimTime { get; set; }
}

[CloudScriptFunction("ClaimGeneratorIncomeClient")]
public class ClaimGeneratorIncomeClient : ICloudScriptFunction<FYClaimGeneratorIncomeClientRequest, FYClaimGeneratorIncomeClientResult>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserDataService _userDataService;
    private readonly TitleDataService _titleDataService;

    static readonly Random random = new();

    private TitleDataDailyCrateInfoPoolItem[] PickItems(TitleDataDailyCrateInfoPoolItem[] items, int numPicks)
    {
        var itemList = new (TitleDataDailyCrateInfoPoolItem item, float cumulativeWeight)[items.Length];
        float totalWeight = 0;

        for (int i = 0; i < items.Length; i++) {
            var item = items[i];
            totalWeight += item.Weight;
            itemList[i] = (item, totalWeight);
        }

        var selectedItems = new TitleDataDailyCrateInfoPoolItem[numPicks];

        for (int i = 0; i < numPicks; i++) {
            float pick = (float)random.NextDouble() * totalWeight;
            int left = 0, right = itemList.Length - 1;

            while (left < right) {
                int mid = (left + right) / 2;
                if (itemList[mid].cumulativeWeight < pick) {
                    left = mid + 1;
                } else {
                    right = mid;
                }
            }

            selectedItems[i] = itemList[left].item;
        }

        return selectedItems;
    }

    public ClaimGeneratorIncomeClient(IHttpContextAccessor httpContextAccessor, UserDataService userDataService, TitleDataService titleDataService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userDataService = userDataService;
        _titleDataService = titleDataService;
    }

    public async Task<FYClaimGeneratorIncomeClientResult> ExecuteAsync(FYClaimGeneratorIncomeClientRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }
        var userId = context.User.FindAuthUserId();
        var userData = await _userDataService.FindAsync(
            userId, userId,
            new List<string>{"Generators__2021_09_09", "CharacterTechTreeBonuses", "Balance", "Inventory"}
        );

        var userGenerators = JsonSerializer.Deserialize<FYPassiveGenerator[]>(userData["Generators__2021_09_09"].Value);
        var generator = Array.Find(userGenerators, item => item.GeneratorID == request.GeneratorID);
        if (generator == null) {
            return new FYClaimGeneratorIncomeClientResult {
                UserId = userId,
                Error = "Invalid generator ID",
            };
        }

        var balance = JsonSerializer.Deserialize<PlayerBalance>(userData["Balance"].Value);
        var inventory = JsonSerializer.Deserialize<List<FYCustomItemInfo>>(userData["Inventory"].Value);
        var techTreeBonuses = JsonSerializer.Deserialize<CharacterTechTreeBonuses>(userData["CharacterTechTreeBonuses"].Value);

        var titleData = _titleDataService.Find(new List<string>{"PassiveGenerators", "DailyCrateSetups", "Blueprints"});
        var passiveGenerators = JsonSerializer.Deserialize<Dictionary<string, TitleDataPassiveGeneratorsInfo>>(titleData["PassiveGenerators"]);

        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        FYCurrencyItem[] changedCurrencies = [];
        List<FYCustomItemInfo> grantedItems = [];
        switch (generator.GeneratorID) {
            case "playerquarters_gen_kmarks": {
                var rate = techTreeBonuses.KmarksRate;
                if (rate == 0) {
                    return new FYClaimGeneratorIncomeClientResult
                    {
                        UserId = userId,
                        Error = "K-Marks Generator Upgrade is not constructed",
                    };
                }

                var generatorData = passiveGenerators[generator.GeneratorID];
                var hours = (now - generator.LastClaimTime.Seconds) / (60 * generatorData.BaseGenIntervalMinutes);
                var cap = techTreeBonuses.KmarksCap + generatorData.BaseCap;
                var claimableAmount = Math.Min(hours * rate, cap);
                if (claimableAmount == 0) {
                    return new FYClaimGeneratorIncomeClientResult
                    {
                        UserId = userId,
                        Error = "Generator is not ready yet",
                    };
                }

                balance.SoftCurrency += claimableAmount;
                changedCurrencies = [new FYCurrencyItem { CurrencyName = "SoftCurrency", Amount = balance.SoftCurrency }];
                break;
            }
            case "playerquarters_gen_aurum": {
                var rate = techTreeBonuses.AurumRate;
                if (rate == 0) {
                    return new FYClaimGeneratorIncomeClientResult
                    {
                        UserId = userId,
                        Error = "Aurum Generator Upgrade is not constructed",
                    };
                }

                var generatorData = passiveGenerators[generator.GeneratorID];
                var hours = (now - generator.LastClaimTime.Seconds) / (60 * generatorData.BaseGenIntervalMinutes);
                var cap = techTreeBonuses.AurumCap + generatorData.BaseCap;
                var claimableAmount = (int)Math.Min(hours * rate, cap);
                if (claimableAmount == 0) {
                    return new FYClaimGeneratorIncomeClientResult
                    {
                        UserId = userId,
                        Error = "Generator is not ready yet",
                    };
                }

                balance.HardCurrency += claimableAmount;
                changedCurrencies = [new FYCurrencyItem { CurrencyName = "Aurum", Amount = balance.HardCurrency }];
                break;
            }
            case "playerquarters_gen_crate": {
                var crateTier = techTreeBonuses.CrateTier;
                if (crateTier == 0) {
                    return new FYClaimGeneratorIncomeClientResult
                    {
                        UserId = userId,
                        Error = "Crate Generator Upgrade is not constructed",
                    };
                }

                var generatorData = passiveGenerators[generator.GeneratorID];
                var nextClaimTime = generator.LastClaimTime.Seconds + 60 * generatorData.BaseGenIntervalMinutes;
                if (now < nextClaimTime) {
                    return new FYClaimGeneratorIncomeClientResult
                    {
                        UserId = userId,
                        Error = "Generator is not ready yet",
                    };
                }

                var dailyCrateSetups = JsonSerializer.Deserialize<TitleDataDailyCrateInfo[]>(titleData["DailyCrateSetups"]);
                var blueprints = JsonSerializer.Deserialize<Dictionary<string, TitleDataBlueprintInfo>>(titleData["Blueprints"]);
                var rewardsPool = dailyCrateSetups[crateTier - 1];

                foreach (var grant in rewardsPool.RewardGrants) {
                    var items = PickItems(grant.Pool, grant.Amount);
                    // TODO: Stackable? GrantedItems doesn't imply granted OR updated.
                    foreach (var item in items) {
                        var itemInfo = blueprints[item.Name];
                        var grantedItem = new FYCustomItemInfo {
                            ItemId = Guid.NewGuid().ToString(),
                            Amount = item.Amount,
                            BaseItemId = item.Name,
                            Durability = itemInfo.DurabilityMax,
                            Insurance = "",
                            InsuranceOwnerPlayfabId = "",
                            ModData = new FYModItems { M = [] },
                            Origin = new FYItemOriginBackend { G = "", P = "", T = "" },
                            InsuredAttachmentId = "",
                            PrimaryVanityId = 0,
                            SecondaryVanityId = 0,
                            RolledPerks = [],
                        };
                        inventory.Add(grantedItem);
                        grantedItems.Add(grantedItem);
                    }
                }
                break;
            }
            default: {
                return new FYClaimGeneratorIncomeClientResult
                {
                    UserId = userId,
                    Error = $"Unknown generator ID: {request.GeneratorID}",
                };
            }
        }

        generator.LastClaimTime.Seconds = now;

        await _userDataService.UpdateAsync(
            userId, userId,
            new Dictionary<string, string>{
                ["Inventory"] = JsonSerializer.Serialize(inventory),
                ["Balance"] = JsonSerializer.Serialize(balance),
                ["Generators__2021_09_09"] = JsonSerializer.Serialize(userGenerators),
            }
        );

        return new FYClaimGeneratorIncomeClientResult
        {
            UserId = userId,
            Error = "",
            GeneratorID = request.GeneratorID,
            ChangedCurrenciesBalances = changedCurrencies,
            GrantedItems = grantedItems,
            LastClaimTime = generator.LastClaimTime,
        };
    }
}