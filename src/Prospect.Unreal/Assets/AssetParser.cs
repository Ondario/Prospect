using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Prospect.Unreal.Assets.UnrealTypes;
using Prospect.Unreal.Assets.DataTables;

namespace Prospect.Unreal.Assets
{
    /// <summary>
    /// Core parser for Unreal Engine JSON assets extracted via FModel
    /// Handles large file parsing and asset reference resolution
    /// </summary>
    public class AssetParser
    {
        private static readonly ILogger Logger = Log.ForContext<AssetParser>();
        private readonly string _assetsBasePath;
        private readonly Dictionary<string, UObject> _loadedAssets;
        
        public AssetParser(string assetsBasePath = "Exports")
        {
            _assetsBasePath = assetsBasePath;
            _loadedAssets = new Dictionary<string, UObject>();
        }

        /// <summary>
        /// Load a JSON asset file and parse it into UObject representation
        /// </summary>
        /// <param name="assetPath">Relative path from Prospect/Content/ (e.g., "Maps/MP/MAP01/MP_Map01_P")</param>
        /// <returns>Parsed UObject or null if failed</returns>
        public async Task<UObject> LoadAssetAsync(string assetPath)
        {
            try
            {
                // Check if already loaded
                if (_loadedAssets.TryGetValue(assetPath, out var cachedAsset))
                {
                    return cachedAsset;
                }

                // Construct full file path
                var fullPath = Path.Combine(_assetsBasePath, "Prospect", "Content", $"{assetPath}.json");
                
                if (!File.Exists(fullPath))
                {
                    Logger.Warning("Asset file not found: {AssetPath}", fullPath);
                    return null;
                }

                Logger.Information("Loading asset: {AssetPath} ({Size} MB)", 
                    assetPath, new FileInfo(fullPath).Length / (1024.0 * 1024.0));

                // Read and parse JSON
                var jsonContent = await File.ReadAllTextAsync(fullPath);
                var jsonObject = JToken.Parse(jsonContent);

                // Parse based on asset structure
                UObject asset = null;
                
                if (jsonObject is JArray jsonArray)
                {
                    Logger.Debug("Processing JSON array with {Count} exports for {AssetPath}", 
                        jsonArray.Count, assetPath);
                    
                    // For Level assets, try to find the main Level object first
                    if (assetPath.StartsWith("Maps/", StringComparison.OrdinalIgnoreCase))
                    {
                        asset = FindLevelInArray(jsonArray, assetPath);
                    }
                    
                    // If no level found or not a map asset, use first object
                    if (asset == null && jsonArray.Count > 0)
                    {
                        asset = ParseUnrealObject(jsonArray[0], assetPath);
                    }
                }
                else if (jsonObject is JObject singleObject)
                {
                    // Single object
                    asset = ParseUnrealObject(singleObject, assetPath);
                }

                if (asset != null)
                {
                    _loadedAssets[assetPath] = asset;
                    Logger.Information("Successfully loaded asset: {AssetPath} as {AssetType}", 
                        assetPath, asset.GetType().Name);
                }
                else
                {
                    Logger.Warning("Failed to parse asset: {AssetPath}", assetPath);
                }

                return asset;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load asset: {AssetPath}", assetPath);
                return null;
            }
        }

        /// <summary>
        /// Find the main Level object in a JSON array of exports
        /// </summary>
        private UObject FindLevelInArray(JArray jsonArray, string assetPath)
        {
            UObject bestCandidate = null;
            int bestScore = 0;

            foreach (var exportToken in jsonArray)
            {
                if (!(exportToken is JObject exportObject))
                    continue;

                var type = exportObject["Type"]?.ToString();
                var className = exportObject["Class"]?.ToString();
                var name = exportObject["Name"]?.ToString();

                int score = 0;

                // Scoring system to find the best Level candidate
                if (type == "World" || type == "Level") score += 100;
                if (!string.IsNullOrEmpty(className))
                {
                    var classLower = className.ToLowerInvariant();
                    if (classLower.Contains("world")) score += 50;
                    if (classLower.Contains("level")) score += 40;
                    if (classLower.Contains("uworld")) score += 60;
                }
                if (exportObject["PersistentLevel"] != null) score += 30;
                if (exportObject["StreamingLevels"] != null) score += 20;
                if (exportObject["WorldSettings"] != null) score += 20;
                if (exportObject["Actors"] != null) score += 15;
                if (!string.IsNullOrEmpty(name) && name.Contains("PersistentLevel")) score += 25;

                Logger.Debug("Export candidate: Type='{Type}', Class='{Class}', Name='{Name}', Score={Score}", 
                    type ?? "null", className ?? "null", name ?? "null", score);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = ParseUnrealObject(exportObject, assetPath);
                }
            }

            if (bestCandidate != null)
            {
                Logger.Debug("Selected Level candidate with score {Score} for {AssetPath}", bestScore, assetPath);
            }

            return bestCandidate;
        }

        /// <summary>
        /// Load a Level asset specifically (convenience method)
        /// </summary>
        /// <param name="levelPath">Path to level (e.g., "Maps/MP/MAP01/MP_Map01_P")</param>
        /// <returns>Parsed ULevel or null if failed</returns>
        public async Task<ULevel> LoadLevelAsync(string levelPath)
        {
            try
            {
                var asset = await LoadAssetAsync(levelPath);
                if (asset is ULevel level)
                {
                    return level;
                }

                Logger.Warning("Asset {LevelPath} is not a Level", levelPath);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load level: {LevelPath}", levelPath);
                return null;
            }
        }

        /// <summary>
        /// Load a DataTable from the DataTables directory
        /// </summary>
        public async Task<DataTable<T>> LoadDataTableAsync<T>(string tableName) where T : class
        {
            try
            {
                var assetPath = Path.Combine("DataTables", tableName);
                var asset = await LoadAssetAsync(assetPath);
                
                if (asset is DataTable<T> dataTable)
                {
                    return dataTable;
                }

                Logger.Warning("Asset {AssetPath} is not a DataTable", assetPath);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load DataTable: {TableName}", tableName);
                return null;
            }
        }

        /// <summary>
        /// Parse a JSON object into appropriate UObject type
        /// </summary>
        private UObject ParseUnrealObject(JToken jsonToken, string assetPath)
        {
            if (!(jsonToken is JObject jsonObject))
                return null;

            var type = jsonObject["Type"]?.ToString();
            var className = jsonObject["Class"]?.ToString();
            var name = jsonObject["Name"]?.ToString();

            // Enhanced logging for debugging
            Logger.Debug("Parsing asset {AssetPath}: Type='{Type}', Class='{Class}', Name='{Name}'", 
                assetPath, type ?? "null", className ?? "null", name ?? "null");

            // Enhanced Level/World detection
            if (IsLevelAsset(jsonObject, assetPath, type, className))
            {
                Logger.Debug("Detected Level asset: {AssetPath}", assetPath);
                return ParseLevel(jsonObject, assetPath);
            }

            // Determine object type and create appropriate instance
            switch (type)
            {
                case "DataTable":
                    return ParseDataTable(jsonObject, assetPath);
                
                case "StaticMesh":
                    return ParseStaticMesh(jsonObject, assetPath);
                
                case "Actor":
                case "Pawn":
                case "PlayerStart":
                    return ParseActor(jsonObject, assetPath);
                
                default:
                    // Generic UObject
                    Logger.Debug("Creating generic UObject for {AssetPath} with type '{Type}'", assetPath, type ?? "unknown");
                    return ParseGenericObject(jsonObject, assetPath);
            }
        }

        /// <summary>
        /// Enhanced detection for Level/World assets
        /// </summary>
        private bool IsLevelAsset(JObject jsonObject, string assetPath, string type, string className)
        {
            // Check explicit type fields
            if (type == "World" || type == "Level")
                return true;

            // Check class name patterns
            if (!string.IsNullOrEmpty(className))
            {
                var classLower = className.ToLowerInvariant();
                if (classLower.Contains("world") || classLower.Contains("level") || 
                    classLower.Contains("uworld") || classLower.Contains("ulevel"))
                    return true;
            }

            // Check asset path patterns (Maps typically contain levels)
            if (assetPath.StartsWith("Maps/", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Debug("Detected Maps path, assuming Level asset: {AssetPath}", assetPath);
                return true;
            }

            // Check for typical level properties
            if (jsonObject["PersistentLevel"] != null || 
                jsonObject["StreamingLevels"] != null ||
                jsonObject["LevelScriptActor"] != null ||
                jsonObject["WorldSettings"] != null)
            {
                Logger.Debug("Detected level properties in asset: {AssetPath}", assetPath);
                return true;
            }

            // Check for array of exports that might be a level structure
            if (jsonObject.Parent is JArray && assetPath.Contains("_P"))
            {
                // Unreal typically names levels with _P suffix (persistent)
                Logger.Debug("Detected array structure with _P suffix, assuming Level: {AssetPath}", assetPath);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parse a Level/World object - Enhanced for complex Unreal asset structures
        /// </summary>
        private ULevel ParseLevel(JObject jsonObject, string assetPath)
        {
            var level = new ULevel
            {
                Name = jsonObject["Name"]?.ToString() ?? Path.GetFileNameWithoutExtension(assetPath),
                Class = jsonObject["Class"]?.ToString(),
                AssetPath = assetPath
            };

            // Parse actors in the level
            var actors = new List<UActor>();
            
            // Enhanced actor extraction - check multiple possible locations
            ExtractActorsFromJson(jsonObject, actors, assetPath);

            // If this is part of an array structure, check parent array for more objects
            if (jsonObject.Parent is JArray parentArray)
            {
                Logger.Debug("Processing array structure with {Count} exports for {AssetPath}", 
                    parentArray.Count, assetPath);
                
                foreach (var exportToken in parentArray)
                {
                    if (exportToken is JObject exportObject && exportObject != jsonObject)
                    {
                        ExtractActorsFromJson(exportObject, actors, assetPath);
                    }
                }
            }

            level.Actors = actors;
            Logger.Information("Parsed Level {AssetPath} with {ActorCount} actors", assetPath, actors.Count);

            // Parse streaming levels
            ExtractStreamingLevels(jsonObject, level);

            return level;
        }

        /// <summary>
        /// Extract actors from various JSON structures
        /// </summary>
        private void ExtractActorsFromJson(JObject jsonObject, List<UActor> actors, string assetPath)
        {
            // Look for actors in various possible locations
            
            // Standard Actors array
            if (jsonObject["Actors"] is JArray actorsArray)
            {
                foreach (var actorToken in actorsArray)
                {
                    var actor = ParseActor(actorToken as JObject, assetPath);
                    if (actor != null)
                        actors.Add(actor);
                }
            }

            // PersistentLevel actors
            if (jsonObject["PersistentLevel"] is JObject persistentLevel)
            {
                ExtractActorsFromJson(persistentLevel, actors, assetPath);
            }

            // Properties that might contain actors
            if (jsonObject["Properties"] is JObject properties)
            {
                foreach (var property in properties)
                {
                    if (property.Value is JArray propertyArray)
                    {
                        foreach (var item in propertyArray)
                        {
                            if (item is JObject itemObject && IsActorObject(itemObject))
                            {
                                var actor = ParseActor(itemObject, assetPath);
                                if (actor != null)
                                    actors.Add(actor);
                            }
                        }
                    }
                    else if (property.Value is JObject propertyObject && IsActorObject(propertyObject))
                    {
                        var actor = ParseActor(propertyObject, assetPath);
                        if (actor != null)
                            actors.Add(actor);
                    }
                }
            }

            // Check if this object itself is an actor
            if (IsActorObject(jsonObject))
            {
                var actor = ParseActor(jsonObject, assetPath);
                if (actor != null)
                {
                    actors.Add(actor);
                }
            }
        }

        /// <summary>
        /// Check if a JSON object represents an actor
        /// </summary>
        private bool IsActorObject(JObject jsonObject)
        {
            var className = jsonObject["Class"]?.ToString();
            if (string.IsNullOrEmpty(className))
                return false;

            var classLower = className.ToLowerInvariant();
            return classLower.Contains("actor") || 
                   classLower.Contains("pawn") || 
                   classLower.Contains("playerstart") ||
                   classLower.Contains("character") ||
                   jsonObject["Transform"] != null || // Objects with transforms are often actors
                   jsonObject["RootComponent"] != null;
        }

        /// <summary>
        /// Extract streaming levels from JSON
        /// </summary>
        private void ExtractStreamingLevels(JObject jsonObject, ULevel level)
        {
            if (jsonObject["StreamingLevels"] is JArray streamingArray)
            {
                var streamingLevels = new List<string>();
                foreach (var streamingToken in streamingArray)
                {
                    var levelName = streamingToken["PackageName"]?.ToString();
                    if (!string.IsNullOrEmpty(levelName))
                        streamingLevels.Add(levelName);
                }
                level.StreamingLevels = streamingLevels;
            }
        }

        /// <summary>
        /// Parse a DataTable object
        /// </summary>
        private UObject ParseDataTable(JObject jsonObject, string assetPath)
        {
            // Create generic DataTable - specific types will be handled by LoadDataTableAsync<T>
            var dataTable = new UObject
            {
                Name = jsonObject["Name"]?.ToString(),
                Class = jsonObject["Class"]?.ToString(),
                AssetPath = assetPath
            };

            // Store raw rows data for later typed parsing
            if (jsonObject["Rows"] is JObject rowsObject)
            {
                dataTable.Properties["Rows"] = rowsObject;
            }

            return dataTable;
        }

        /// <summary>
        /// Parse an Actor object
        /// </summary>
        private UActor ParseActor(JObject jsonObject, string assetPath)
        {
            if (jsonObject == null) return null;

            var actor = new UActor
            {
                Name = jsonObject["Name"]?.ToString(),
                Class = jsonObject["Class"]?.ToString(),
                AssetPath = assetPath
            };

            // Parse transform
            if (jsonObject["Transform"] is JObject transformObject)
            {
                actor.Transform = ParseTransform(transformObject);
            }

            // Parse components
            if (jsonObject["Components"] is JArray componentsArray)
            {
                var components = new List<UActorComponent>();
                foreach (var componentToken in componentsArray)
                {
                    // Component parsing would go here
                    // For now, store as generic components
                }
                actor.Components = components;
            }

            return actor;
        }

        /// <summary>
        /// Parse a StaticMesh object
        /// </summary>
        private UObject ParseStaticMesh(JObject jsonObject, string assetPath)
        {
            return new UObject
            {
                Name = jsonObject["Name"]?.ToString(),
                Class = jsonObject["Class"]?.ToString(),
                AssetPath = assetPath
            };
        }

        /// <summary>
        /// Parse a generic UObject
        /// </summary>
        private UObject ParseGenericObject(JObject jsonObject, string assetPath)
        {
            var obj = new UObject
            {
                Name = jsonObject["Name"]?.ToString(),
                Class = jsonObject["Class"]?.ToString(),
                AssetPath = assetPath
            };

            // Store properties for later access
            if (jsonObject["Properties"] is JObject propertiesObject)
            {
                foreach (var property in propertiesObject)
                {
                    obj.Properties[property.Key] = property.Value;
                }
            }

            return obj;
        }

        /// <summary>
        /// Parse transform data from JSON
        /// </summary>
        private Transform ParseTransform(JObject transformObject)
        {
            var transform = new Transform();

            if (transformObject["Translation"] is JObject translation)
            {
                transform.Translation = new Vector3
                {
                    X = translation["X"]?.ToObject<float>() ?? 0f,
                    Y = translation["Y"]?.ToObject<float>() ?? 0f,
                    Z = translation["Z"]?.ToObject<float>() ?? 0f
                };
            }

            if (transformObject["Rotation"] is JObject rotation)
            {
                transform.Rotation = new Quaternion
                {
                    X = rotation["X"]?.ToObject<float>() ?? 0f,
                    Y = rotation["Y"]?.ToObject<float>() ?? 0f,
                    Z = rotation["Z"]?.ToObject<float>() ?? 0f,
                    W = rotation["W"]?.ToObject<float>() ?? 1f
                };
            }

            if (transformObject["Scale3D"] is JObject scale)
            {
                transform.Scale = new Vector3
                {
                    X = scale["X"]?.ToObject<float>() ?? 1f,
                    Y = scale["Y"]?.ToObject<float>() ?? 1f,
                    Z = scale["Z"]?.ToObject<float>() ?? 1f
                };
            }

            return transform;
        }

        /// <summary>
        /// Resolve an asset reference to actual asset path
        /// </summary>
        public string ResolveAssetReference(string objectPath)
        {
            // Convert Unreal asset path to file system path
            // Example: "/Game/Maps/MP/MAP01/MP_Map01_P.MP_Map01_P" -> "Maps/MP/MAP01/MP_Map01_P"
            
            if (string.IsNullOrEmpty(objectPath))
                return null;

            // Remove /Game/ prefix
            if (objectPath.StartsWith("/Game/"))
            {
                objectPath = objectPath.Substring(6);
            }

            // Remove the duplicate name at the end (common in Unreal references)
            var lastDot = objectPath.LastIndexOf('.');
            if (lastDot > 0)
            {
                var beforeDot = objectPath.Substring(0, lastDot);
                var afterDot = objectPath.Substring(lastDot + 1);
                
                // If the part after the dot matches the filename, remove it
                var filename = Path.GetFileNameWithoutExtension(beforeDot);
                if (afterDot == filename)
                {
                    objectPath = beforeDot;
                }
            }

            return objectPath;
        }

        /// <summary>
        /// Clear the asset cache
        /// </summary>
        public void ClearCache()
        {
            _loadedAssets.Clear();
            Logger.Information("Asset cache cleared");
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public (int LoadedAssets, long MemoryUsage) GetCacheStats()
        {
            // Rough memory estimation
            long memoryUsage = _loadedAssets.Count * 1024; // Base overhead per asset
            
            return (_loadedAssets.Count, memoryUsage);
        }
    }

    public static class ProspectConfig
    {
        public static bool AssetParserLoggingEnabled { get; private set; } = true;
        static ProspectConfig()
        {
            try
            {
                if (File.Exists("configuration.json"))
                {
                    var json = JObject.Parse(File.ReadAllText("configuration.json"));
                    AssetParserLoggingEnabled = json["AssetParserLoggingEnabled"]?.Value<bool>() ?? true;
                }
            }
            catch { /* Ignore config errors, default to enabled */ }
        }
    }
} 