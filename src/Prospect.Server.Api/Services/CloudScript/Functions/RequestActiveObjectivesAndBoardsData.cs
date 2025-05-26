using System.Collections;
using System.Diagnostics.Contracts;
using System.Text.Json;
using Prospect.Server.Api.Models.Data;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.CloudScript.Models.Data;
using Prospect.Server.Api.Services.UserData;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("RequestActiveObjectivesAndBoardsData")]
public class RequestActiveObjectivesAndBoardsData : ICloudScriptFunction<FYQueryFactionProgressionRequest, FYGetPlayerContractsResult>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserDataService _userDataService;
    private readonly TitleDataService _titleDataService;
    private readonly string[] factions = ["Korolev", "Osiris", "ICA"]; // TODO: Order is important
    private readonly EYContractDifficulty[] difficulties = [EYContractDifficulty.Easy, EYContractDifficulty.Medium, EYContractDifficulty.Hard];
    private readonly Random rnd = new Random();

    public RequestActiveObjectivesAndBoardsData(IHttpContextAccessor httpContextAccessor, UserDataService userDataService, TitleDataService titleDataService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userDataService = userDataService;
        _titleDataService = titleDataService;
    }

    public async Task<FYGetPlayerContractsResult> ExecuteAsync(FYQueryFactionProgressionRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return new FYGetPlayerContractsResult{};
        }
        var userId = context.User.FindAuthUserId();

        var userData = await _userDataService.FindAsync(userId, userId, new List<string>{ "ContractsActive", "JobBoardsData", "FactionProgressionKorolev", "FactionProgressionICA", "FactionProgressionOsiris" });
        var contractsActive = JsonSerializer.Deserialize<FYGetActiveContractsResult>(userData["ContractsActive"].Value);
        var jobBoardsData = JsonSerializer.Deserialize<JobBoardsData>(userData["JobBoardsData"].Value);

        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var lastBoardRefreshTimeUtc = jobBoardsData.LastBoardRefreshTimeUtc;
        var boards = jobBoardsData.Boards;
        // The boards are updated once in 3 hours
        if (now >= lastBoardRefreshTimeUtc.Seconds + 60 * 60 * 3) {
            var titleData = _titleDataService.Find(new List<string> { "Contracts", "Jobs", "LevelData" });

            var contracts = JsonSerializer.Deserialize<Dictionary<string, TitleDataContractInfo>>(titleData["Contracts"]);
            var jobs = JsonSerializer.Deserialize<Dictionary<string, Dictionary<EYContractDifficulty, string[]>>>(titleData["Jobs"]);
            var levelsData = JsonSerializer.Deserialize<Dictionary<string, int[]>>(titleData["LevelData"]);
            var currentQuests = new Dictionary<string, Dictionary<EYContractDifficulty, bool>> {
                ["Korolev"] = new Dictionary<EYContractDifficulty, bool> { [EYContractDifficulty.Easy] = false, [EYContractDifficulty.Medium] = false, [EYContractDifficulty.Hard] = false },
                ["Osiris"] = new Dictionary<EYContractDifficulty, bool> { [EYContractDifficulty.Easy] = false, [EYContractDifficulty.Medium] = false, [EYContractDifficulty.Hard] = false },
                ["ICA"] = new Dictionary<EYContractDifficulty, bool> { [EYContractDifficulty.Easy] = false, [EYContractDifficulty.Medium] = false, [EYContractDifficulty.Hard] = false },
            };

            foreach (var contract in contractsActive.Contracts) {
                var contractData = contracts[contract.ContractID];
                if (contractData.IsMainContract) {
                    continue;
                }
                currentQuests[contractData.Faction][contractData.Difficulty] = true;
            }

            for (var i = 0; i < factions.Length; i++) {
                var faction = factions[i];
                var factionProgression = JsonSerializer.Deserialize<int>(userData[$"FactionProgression{faction}"].Value);
                var levels = levelsData[faction];
                var level = levels.Length - Array.FindIndex(levels, (int xp) => {
                    return factionProgression >= xp;
                });

                for (var j = 0; j < difficulties.Length; j++) {
                    var difficulty = difficulties[j];
                    if (currentQuests[faction][difficulty]) {
                        continue;
                    }
                    var factionJobs = jobs[faction];
                    var jobsAvailable = Array.FindAll(factionJobs[difficulty], (string jobId) => {
                        return level >= contracts[jobId].UnlockData.Level;
                    });
                    string jobId = jobsAvailable.Length > 0 ? jobsAvailable[rnd.Next(jobsAvailable.Length)] : factionJobs[difficulty][0];
                    boards[i].Contracts[j] = new FYFactionContractData { ContractId = jobId };
                }
            }
            lastBoardRefreshTimeUtc.Seconds = now;

            await _userDataService.UpdateAsync(
                userId, userId,
                new Dictionary<string, string> { ["JobBoardsData"] = JsonSerializer.Serialize(jobBoardsData) }
            );
        }

        return new FYGetPlayerContractsResult
        {
            UserId = userId,
            Error = null,
            ActiveContracts = contractsActive.Contracts,
            FactionsContracts = new FYFactionsContractsData
            {
                Boards = boards,
                LastBoardRefreshTimeUtc = lastBoardRefreshTimeUtc
            },
            RefreshHours24UtcFromBackend = 12 // Doesn't seem to work
        };
    }
}