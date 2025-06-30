using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Prospect.Unreal.Assets.UnrealTypes;

namespace Prospect.Unreal.Assets.DataTables
{
    public class MapInfoCollection : Dictionary<string, MapInfo>
    {
    }

    public class MapInfo
    {
        [JsonProperty("m_persistentMap")]
        public AssetReference PersistentMap { get; set; }

        [JsonProperty("m_playerStartClusterRadius")]
        public float PlayerStartClusterRadius { get; set; }

        [JsonProperty("m_playerStartClusterCooldown")]
        public float PlayerStartClusterCooldown { get; set; }

        [JsonProperty("m_maxScoreAllowed")]
        public int MaxScoreAllowed { get; set; }

        [JsonProperty("m_playerStartScoreRules")]
        public List<PlayerStartScoreRule> PlayerStartScoreRules { get; set; } = new();

        [JsonProperty("m_VFXNiagaraMapInfo")]
        public VFXMapInfo VFXMapInfo { get; set; }

        [JsonProperty("m_difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("m_hasVoid")]
        public bool HasVoid { get; set; }

        [JsonProperty("m_containsAlienForge")]
        public bool ContainsAlienForge { get; set; }

        [JsonProperty("m_isVisible")]
        public bool IsVisible { get; set; }

        [JsonProperty("m_name")]
        public LocalizedText Name { get; set; }

        [JsonProperty("m_description")]
        public LocalizedText Description { get; set; }

        [JsonProperty("m_tooltip")]
        public LocalizedText Tooltip { get; set; }

        [JsonProperty("m_image")]
        public AssetReference Image { get; set; }

        [JsonProperty("m_mapSelectionImage")]
        public AssetReference MapSelectionImage { get; set; }

        [JsonProperty("m_hologramMaterial")]
        public AssetReference HologramMaterial { get; set; }

        public string GetPersistentMapPath()
        {
            return PersistentMap?.AssetPathName?.Replace("/Game/", "");
        }

        public bool IsPlayerSpawnValid(Vector3 spawnLocation, List<Vector3> existingPlayerLocations)
        {
            foreach (var existingLocation in existingPlayerLocations)
            {
                var distance = Vector3.Distance(spawnLocation, existingLocation);
                
                foreach (var rule in PlayerStartScoreRules)
                {
                    if (distance < rule.Radius)
                    {
                        return false; // Too close to existing player
                    }
                }
            }
            
            return true;
        }

        public int CalculateSpawnScore(Vector3 spawnLocation, List<Vector3> existingPlayerLocations)
        {
            int totalScore = 0;
            
            foreach (var existingLocation in existingPlayerLocations)
            {
                var distance = Vector3.Distance(spawnLocation, existingLocation);
                
                foreach (var rule in PlayerStartScoreRules)
                {
                    if (distance < rule.Radius)
                    {
                        totalScore += rule.ScorePerPlayerInRadius;
                    }
                }
            }
            
            return totalScore;
        }
    }

    public class PlayerStartScoreRule
    {
        [JsonProperty("m_radius")]
        public float Radius { get; set; }

        [JsonProperty("m_scorePerPlayerInRadius")]
        public int ScorePerPlayerInRadius { get; set; }
    }

    public class VFXMapInfo
    {
        [JsonProperty("StormOccusionCenter01")]
        public Vector3 StormOcclusionCenter01 { get; set; }

        [JsonProperty("StormOcclusionRadius01")]
        public float StormOcclusionRadius01 { get; set; }

        [JsonProperty("StormOccusionCenter02")]
        public Vector3 StormOcclusionCenter02 { get; set; }

        [JsonProperty("StormOccusionRadius02")]
        public float StormOcclusionRadius02 { get; set; }
    }

    public class LocalizedText
    {
        [JsonProperty("Namespace")]
        public string Namespace { get; set; }

        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("SourceString")]
        public string SourceString { get; set; }

        [JsonProperty("LocalizedString")]
        public string LocalizedString { get; set; }

        [JsonProperty("TableId")]
        public string TableId { get; set; }

        [JsonProperty("CultureInvariantString")]
        public string CultureInvariantString { get; set; }

        public string GetDisplayText()
        {
            return LocalizedString ?? SourceString ?? CultureInvariantString ?? Key ?? "";
        }
    }

    public class AssetReference
    {
        [JsonProperty("AssetPathName")]
        public string AssetPathName { get; set; }

        [JsonProperty("SubPathString")]
        public string SubPathString { get; set; }

        public bool IsValid => !string.IsNullOrEmpty(AssetPathName) && AssetPathName != "None";
    }
} 