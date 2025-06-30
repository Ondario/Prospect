using System.Collections.Generic;
using Newtonsoft.Json;

namespace Prospect.Unreal.Assets.DataTables
{
    public class GameModeTuningCollection : Dictionary<string, GameModeTuning>
    {
    }

    public class GameModeTuning
    {
        [JsonProperty("m_gameplayTagsModifiedXP")]
        public List<string> GameplayTagsModifiedXP { get; set; } = new();

        [JsonProperty("m_xpModificationOnGameplayTagsMultiplier")]
        public float XpModificationOnGameplayTagsMultiplier { get; set; }

        [JsonProperty("m_questVictoryConditionQuestList")]
        public int QuestVictoryConditionQuestList { get; set; }

        [JsonProperty("m_addQuestTimerDelay")]
        public float AddQuestTimerDelay { get; set; }

        [JsonProperty("m_amountOfHardQuests")]
        public int AmountOfHardQuests { get; set; }

        [JsonProperty("m_amountOfMediumQuests")]
        public int AmountOfMediumQuests { get; set; }

        [JsonProperty("m_scoreSharingMultiplier")]
        public float ScoreSharingMultiplier { get; set; }

        [JsonProperty("m_currencySharingMultiplier")]
        public float CurrencySharingMultiplier { get; set; }

        [JsonProperty("m_xpSharingMultiplier")]
        public float XpSharingMultiplier { get; set; }

        [JsonProperty("m_pactBreakInteractionTime")]
        public float PactBreakInteractionTime { get; set; }

        [JsonProperty("m_heatmapEnabled")]
        public bool HeatmapEnabled { get; set; }

        [JsonProperty("m_ownPlayerHeatScore")]
        public float OwnPlayerHeatScore { get; set; }

        [JsonProperty("m_pactMateHeatScore")]
        public float PactMateHeatScore { get; set; }

        [JsonProperty("m_nonPlayerHeatScore")]
        public float NonPlayerHeatScore { get; set; }

        [JsonProperty("m_playerHeatScore")]
        public float PlayerHeatScore { get; set; }

        [JsonProperty("m_vehicleHeatScore")]
        public float VehicleHeatScore { get; set; }

        [JsonProperty("m_recentlyDealtDamageScore")]
        public float RecentlyDealtDamageScore { get; set; }

        [JsonProperty("m_recentlyDealDamageHeatMapTimeSpan")]
        public float RecentlyDealDamageHeatMapTimeSpan { get; set; }

        [JsonProperty("m_sessionTimeoutCallbackRewards")]
        public float SessionTimeoutCallbackRewards { get; set; }

        [JsonProperty("m_sessionManagesResources")]
        public bool SessionManagesResources { get; set; }

        [JsonProperty("m_interactionMultiplier")]
        public float InteractionMultiplier { get; set; }

        [JsonProperty("m_interactionTraceSphere")]
        public float InteractionTraceSphere { get; set; }

        [JsonProperty("m_checkRecursiveTrace")]
        public bool CheckRecursiveTrace { get; set; }

        [JsonProperty("m_prioritizeNonPlayerCharacters")]
        public bool PrioritizeNonPlayerCharacters { get; set; }

        [JsonProperty("m_useSessionTimerShutdown")]
        public bool UseSessionTimerShutdown { get; set; }

        [JsonProperty("m_damageOutlineHostilePlayersDuration")]
        public float DamageOutlineHostilePlayersDuration { get; set; }

        [JsonProperty("m_DBNOCharges")]
        public int DBNOCharges { get; set; }

        [JsonProperty("m_rowName")]
        public string RowName { get; set; }

        // Helper properties for specific game modes
        public bool IsLoopMode => RowName == "LOOP";
        public bool IsSoloTrainingMode => RowName == "SOLOTRAININGMATCH";
        public bool IsEventMode => RowName == "EVENT";
        public bool IsSandboxMode => RowName == "SANDBOX";
        public bool IsStationMode => RowName == "STATION";

        public bool AllowsScoreSharing => ScoreSharingMultiplier > 0.0f;
        public bool AllowsCurrencySharing => CurrencySharingMultiplier > 0.0f;
        public bool AllowsXpSharing => XpSharingMultiplier > 0.0f;
        public bool HasTimerShutdown => UseSessionTimerShutdown;
    }
} 