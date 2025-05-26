using System.Text.Json.Serialization;

public class TitleDataDailyCrateInfo {
    [JsonPropertyName("Name")]
    public string Name { get; set; }
    [JsonPropertyName("Level")]
    public int Level { get; set; }
    [JsonPropertyName("RewardGrants")]
    public TitleDataDailyCrateInfoRewardGrants[] RewardGrants { get; set; }
}

public class TitleDataDailyCrateInfoRewardGrants {
    [JsonPropertyName("Pool")]
    public TitleDataDailyCrateInfoPoolItem[] Pool { get; set; }
    [JsonPropertyName("Amount")]
    public int Amount { get; set; }
}

public class TitleDataDailyCrateInfoPoolItem {
    [JsonPropertyName("Name")]
    public string Name { get; set; }
    [JsonPropertyName("Weight")]
    public float Weight { get; set; }
    [JsonPropertyName("Amount")]
    public int Amount { get; set; }
}
