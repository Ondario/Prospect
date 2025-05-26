using System.Text.Json;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.CloudScript.Models.Data;
using Prospect.Server.Api.Services.UserData;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("UpdateRetentionBonus")]
public class UpdateRetentionBonus : ICloudScriptFunction<FYRetentionBonusRequest, FYRetentionBonusResult>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserDataService _userDataService;
    private readonly TitleDataService _titleDataService;

    public UpdateRetentionBonus(IHttpContextAccessor httpContextAccessor, UserDataService userDataService, TitleDataService titleDataService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userDataService = userDataService;
        _titleDataService = titleDataService;
    }

    public async Task<FYRetentionBonusResult> ExecuteAsync(FYRetentionBonusRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }
        var userId = context.User.FindAuthUserId();
        var userData = await _userDataService.FindAsync(
            userId, userId,
            new List<string>{"RetentionBonus", "Inventory"}
        );

        var retentionBonus = JsonSerializer.Deserialize<FYRetentionProgress>(userData["RetentionBonus"].Value);
        if (retentionBonus.ClaimedAll) {
            return new FYRetentionBonusResult
            {
                UserId = userId,
                Error = "",
                PlayerData = retentionBonus,
            };
        }

        var currentMidnight = (int)new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).ToUnixTimeSeconds();
        var nextClaimTime = retentionBonus.LastClaimTime.Seconds + 60 * 60 * 24;
        if (currentMidnight < nextClaimTime) {
            return new FYRetentionBonusResult
            {
                UserId = userId,
                Error = "",
                PlayerData = retentionBonus,
            };
        }

        var titleData = _titleDataService.Find(new List<string>{"Blueprints", "RetentionBonusData"});
        var bonusData = JsonSerializer.Deserialize<Dictionary<string, TitleDataRetentionBonusInfo>>(titleData["RetentionBonusData"]);

        retentionBonus.DaysClaimed++;
        retentionBonus.LastClaimTime.Seconds = currentMidnight;
        retentionBonus.ClaimedAll = retentionBonus.DaysClaimed >= 14;

        // TODO: Currently only CB2 bonuses are considered
        var bonuses = bonusData["CB2"];
        var inventory = JsonSerializer.Deserialize<List<FYCustomItemInfo>>(userData["Inventory"].Value);
        var rewardItemID = bonuses.Rewards[retentionBonus.DaysClaimed - 1];
        if (rewardItemID != "None") {
            var blueprints = JsonSerializer.Deserialize<Dictionary<string, TitleDataBlueprintInfo>>(titleData["Blueprints"]);
            var itemInfo = blueprints[rewardItemID];
            inventory.Add(new FYCustomItemInfo{
                ItemId = Guid.NewGuid().ToString(),
                Amount = 1,
                BaseItemId = rewardItemID,
                Durability = itemInfo.DurabilityMax,
                Insurance = "",
                InsuranceOwnerPlayfabId = "",
                ModData = new FYModItems { M = [] },
                Origin = new FYItemOriginBackend { G = "", P = "", T = "" },
                InsuredAttachmentId = "",
                PrimaryVanityId = 0,
                SecondaryVanityId = 0,
                RolledPerks = [],
            });
        }

        await _userDataService.UpdateAsync(
            userId, userId,
            new Dictionary<string, string>{
                ["RetentionBonus"] = JsonSerializer.Serialize(retentionBonus),
                ["Inventory"] = JsonSerializer.Serialize(inventory),
            }
        );

        // TODO: This request does not update inventory items. Maybe a SignalR event must be sent?
        return new FYRetentionBonusResult
        {
            UserId = userId,
            Error = "",
            PlayerData = retentionBonus,
        };
    }
}