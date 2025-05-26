using System.Text.Json.Serialization;

public class TitleDataContractInfo {
    [JsonPropertyName("Name")]
    public string Name { get; set; }
    [JsonPropertyName("Faction")]
    public string Faction { get; set; }
    [JsonPropertyName("ReputationIncrease")]
    public int ReputationIncrease { get; set; }
    [JsonPropertyName("Rewards")]
    public TitleDataContractInfoReward[] Rewards { get; set; }
    [JsonPropertyName("Objectives")]
    public TitleDataContractInfoObjective[] Objectives { get; set; }
    [JsonPropertyName("UnlockData")]
    public TitleDataContractInfoUnlockData UnlockData { get; set; }
    [JsonPropertyName("IsMainContract")]
    public bool IsMainContract { get; set; }
    [JsonPropertyName("Difficulty")]
    public EYContractDifficulty Difficulty { get; set; }
}

public class TitleDataContractInfoReward {
    [JsonPropertyName("ItemID")]
    public string ItemID { get; set; }
    [JsonPropertyName("Amount")]
    public int Amount { get; set; }
}

public class TitleDataContractInfoObjective {
    [JsonPropertyName("Type")]
    public EYContractObjectiveType Type { get; set; }
    [JsonPropertyName("MaxProgress")]
    public int MaxProgress { get; set; }
    [JsonPropertyName("ItemToOwn")]
    public string ItemToOwn { get; set; }
    [JsonPropertyName("KillConditions")]
    public TitleDataContractInfoObjectiveKillConditions? KillConditions { get; set; }
}

public class TitleDataContractInfoObjectiveKillConditions
{
    [JsonPropertyName("KillTarget")]
    public EYKillTypeAction KillTarget { get; set; }
    [JsonPropertyName("AllowedWeaponCategories")]
    public EYDeviceCategory[] AllowedWeaponCategories { get; set; }
    [JsonPropertyName("AllowedSpecificWeapons")]
    public string[] AllowedSpecificWeapons { get; set; }
    [JsonPropertyName("SpecificAIEnemyTypeToKill")]
    public EYEnemyType SpecificAIEnemyTypeToKill { get; set; }
    [JsonPropertyName("MapName")]
    public string MapName { get; set; }
    [JsonPropertyName("OnlyDuringStorm")]
    public bool OnlyDuringStorm { get; set; }
}

public class TitleDataContractInfoUnlockData {
    [JsonPropertyName("Level")]
    public int Level { get; set; }
    [JsonPropertyName("Contracts")]
    public HashSet<string> Contracts { get; set; }
}

public enum EYContractObjectiveType {
	Invalid = 0,
	Kills = 1,
	OwnNumOfItem = 2,
	DeadDrop = 3,
	VisitArea = 4,
    LootContainer = 5,
    FactionLevel = 6,
    CompletedMission = 7,
	MAX = 8
}

public enum EYContractDifficulty
{
	Invalid  = 0,
	Easy     = 1,
	Medium   = 2,
	Hard     = 3,
	MAX      = 4
};

public enum EYDeviceCategory
{
    AssaultRifle = 0,
	Pistol       = 1,
	SMG          = 2,
	SniperRifle  = 3,
	HeavyWeapon  = 4,
	Shotgun      = 5,
	BurstRifle   = 6,
	Exotic       = 7,
	MissileLauncher = 8,
	Scanner      = 9,
	All          = 10,
	INVALID      = 11,
	MAX          = 12
};

public enum EYKillTypeAction
{
    Invalid      = 0,
	Players      = 1,
	Creatures    = 2,
	All          = 3,
	MAX          = 4
};

public enum EYEnemyType
{
    None              = 0,
	DebugAutomationTest = 1,
	DirtBeast_Melee   = 2,
	DirtBeast_RangedShort = 3,
	DirtBeast_RangedMedium = 4,
	DirtBeast_RangedLong = 5,
	DirtBeast_MeleeHeavy = 6,
	DirtBeast_RangedHeavy = 7,
	DirtBeast_FlyingHeavy = 8,
	DirtBeast_Boss    = 9,
	Orobot_Melee      = 10,
	Orobot_RangedShort = 11,
	Orobot_RangedMedium = 12,
	Orobot_Walker     = 13,
	Orobot_Platform   = 14,
	Plunderbot_RangedShort = 15,
	Plunderbot_RangedMedium = 16,
	Plunderbot_RangedLong = 17,
	GlowBeetle_Blast  = 18,
	GlowBeetle_Acid   = 19,
	GlowBeetle_Summon = 20,
	Strider           = 21,
	Rattler           = 22,
	Weremole          = 23,
	Crusher           = 24,
	Howler = 25,
	MAX   = 26
};