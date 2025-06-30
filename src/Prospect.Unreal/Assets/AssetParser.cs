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

                // Parse based on asset type
                UObject asset = null;
                
                if (jsonObject is JArray jsonArray)
                {
                    // Multiple objects in array - handle first one for now
                    if (jsonArray.Count > 0)
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
                    Logger.Information("Successfully loaded asset: {AssetPath}", assetPath);
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

            // Determine object type and create appropriate instance
            switch (type)
            {
                case "World":
                case "Level":
                    return ParseLevel(jsonObject, assetPath);
                
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
                    return ParseGenericObject(jsonObject, assetPath);
            }
        }

        /// <summary>
        /// Parse a Level/World object
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
            
            // Look for actors in various possible locations
            if (jsonObject["Actors"] is JArray actorsArray)
            {
                foreach (var actorToken in actorsArray)
                {
                    var actor = ParseActor(actorToken as JObject, assetPath);
                    if (actor != null)
                        actors.Add(actor);
                }
            }

            level.Actors = actors;

            // Parse streaming levels
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

            return level;
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
} 