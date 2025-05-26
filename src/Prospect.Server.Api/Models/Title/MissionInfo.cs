using System.Text.Json.Serialization;

public class TitleDataMissionInfo {
    [JsonPropertyName("MissionId")]
    public string MissionId { get; set; }
    [JsonPropertyName("NextMissionRowHandle")]
    public UDataTable NextMissionRowHandle { get; set; }
    [JsonPropertyName("OnboardingRewards")]
    public UDataTable OnboardingRewards { get; set; }
}

public class TitleDataMissionRewardInfo {
    [JsonPropertyName("MissionRewardId")]
    public string MissionRewardId { get; set; }
    [JsonPropertyName("RewardEntries")]
    public TitleDataMissionRewardEntry[] RewardEntries { get; set; }
}

public class TitleDataMissionRewardEntry {
    [JsonPropertyName("RewardType")]
    public int RewardType { get; set; }
    [JsonPropertyName("RewardRowHandle")]
    public UDataTable RewardRowHandle { get; set; }
    [JsonPropertyName("RewardAmount")]
    public int RewardAmount { get; set; }
}

/*
enum class EYRewardType : uint8_t
{
	EYRewardType__None             = 0,
	EYRewardType__Currency         = 1,
	EYRewardType__Material         = 2,
	EYRewardType__Reputation       = 3,
	EYRewardType__SeasonXP         = 4,
	EYRewardType__ProspectorLevel  = 5,
	EYRewardType__Ammo             = 6,
	EYRewardType__Weapon           = 7,
	EYRewardType__Consumable       = 8,
	EYRewardType__Armor            = 9,
	EYRewardType__Bag              = 10,
	EYRewardType__MAX              = 11
};
*/