using Prospect.Unreal.Runtime;
using Prospect.Unreal.Assets;
using Prospect.Unreal.Assets.UnrealTypes;
using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Math;
using Prospect.Unreal.Net.Actors;
using Prospect.Server.Game.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Alias the two ULevel types to avoid naming conflicts
using AssetULevel = Prospect.Unreal.Assets.UnrealTypes.ULevel;
using RuntimeULevel = Prospect.Unreal.Core.ULevel;
using AssetUActor = Prospect.Unreal.Assets.UnrealTypes.UActor;
using RuntimeAActor = Prospect.Unreal.Net.Actors.AActor;
using Vector3 = Prospect.Unreal.Assets.UnrealTypes.Vector3;

namespace Prospect.Server.Game;

public class ProspectWorld : UWorld
{
    private static readonly ILogger Logger = Log.ForContext<ProspectWorld>();
    
    private AssetParser? _assetParser;
    private GameDataService? _gameDataService;
    private AssetULevel? _loadedAssetLevel;
    private RuntimeULevel? _runtimeLevel;
    private List<AssetUActor> _playerStartActors;
    private GridStreamingManager _gridStreamingManager;

    public ProspectWorld()
    {
        _playerStartActors = new List<AssetUActor>();
        _gridStreamingManager = new GridStreamingManager();
    }

    /// <summary>
    /// Initialize the world with asset loading capabilities
    /// </summary>
    /// <param name="assetsBasePath">Path to the Exports folder containing game assets</param>
    /// <param name="gameDataService">Service providing game configuration data</param>
    public void InitializeAssetSystem(string assetsBasePath, GameDataService? gameDataService = null)
    {
        try
        {
            _assetParser = new AssetParser(assetsBasePath);
            _gameDataService = gameDataService;
            
            Logger.Information("ProspectWorld asset system initialized with base path: {AssetsBasePath}", assetsBasePath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize asset system");
            throw;
        }
    }

    /// <summary>
    /// Load a level into the world (specifically designed for MAP01)
    /// </summary>
    /// <param name="levelPath">Path to the level asset (e.g., "Maps/MP/MAP01/MP_Map01_P")</param>
    /// <returns>True if level loaded successfully</returns>
    public async Task<bool> LoadLevelAsync(string levelPath)
    {
        if (_assetParser == null)
        {
            Logger.Error("Asset system not initialized. Call InitializeAssetSystem() first.");
            return false;
        }

        try
        {
            Logger.Information("Loading level: {LevelPath}", levelPath);
            
            // Load the level using AssetParser
            _loadedAssetLevel = await _assetParser.LoadLevelAsync(levelPath);
            
            if (_loadedAssetLevel == null)
            {
                Logger.Error("Failed to load level: {LevelPath}", levelPath);
                return false;
            }

            Logger.Information("Successfully loaded level: {LevelName} with {ActorCount} actors", 
                _loadedAssetLevel.Name, _loadedAssetLevel.Actors.Count);

            // Extract PlayerStart actors
            await ExtractPlayerStartActorsAsync();
            
            // Initialize grid-based streaming
            await InitializeGridStreamingAsync(levelPath);
            
            // Convert asset level to runtime level
            _runtimeLevel = ConvertAssetLevelToRuntimeLevel(_loadedAssetLevel);
            
            // Add level to world's levels list
            AddLevel(_runtimeLevel);
            
            // Set as persistent level if it's the first/main level
            if (PersistentLevel == null)
            {
                SetPersistentLevel(_runtimeLevel);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load level: {LevelPath}", levelPath);
            return false;
        }
    }

    /// <summary>
    /// Extract and validate PlayerStart actors from the loaded level
    /// </summary>
    private async Task ExtractPlayerStartActorsAsync()
    {
        if (_loadedAssetLevel == null)
            return;

        // Find all PlayerStart actors
        var playerStarts = _loadedAssetLevel.FindActorsByType("PlayerStart");
        
        Logger.Information("Found {PlayerStartCount} PlayerStart actors in level", playerStarts.Count);
        
        if (playerStarts.Count == 0)
        {
            Logger.Warning("No PlayerStart actors found in level! Players will not be able to spawn.");
            return;
        }

        // Validate PlayerStart positions using map configuration
        var validatedStarts = new List<UActor>();
        
        if (_gameDataService != null)
        {
            var mapInfo = _gameDataService.GetMapInfo("Map01");
            if (mapInfo != null)
            {
                validatedStarts = ValidatePlayerStartPlacements(playerStarts, mapInfo);
            }
            else
            {
                Logger.Warning("No map configuration found for Map01, using all PlayerStart actors");
                validatedStarts = playerStarts;
            }
        }
        else
        {
            validatedStarts = playerStarts;
        }

        _playerStartActors = validatedStarts;
        
        Logger.Information("Validated {ValidPlayerStartCount} PlayerStart actors for spawning", 
            _playerStartActors.Count);

        // Log sample PlayerStart positions
        for (int i = 0; i < Math.Min(5, _playerStartActors.Count); i++)
        {
            var start = _playerStartActors[i];
            if (start.Transform?.Translation != null)
            {
                Logger.Information("  PlayerStart {Index}: ({X:F1}, {Y:F1}, {Z:F1})", 
                    i + 1, 
                    start.Transform.Translation.X, 
                    start.Transform.Translation.Y, 
                    start.Transform.Translation.Z);
            }
        }
    }

    /// <summary>
    /// Validate PlayerStart placements against map configuration rules
    /// </summary>
    private List<AssetUActor> ValidatePlayerStartPlacements(List<AssetUActor> playerStarts, Prospect.Unreal.Assets.DataTables.MapInfo mapInfo)
    {
        var validStarts = new List<AssetUActor>();
        var clusterRadius = mapInfo.PlayerStartClusterRadius;
        
        Logger.Information("Validating PlayerStart placements with cluster radius: {ClusterRadius}", clusterRadius);
        
        foreach (var start in playerStarts)
        {
            if (start.Transform?.Translation == null)
            {
                Logger.Warning("PlayerStart has no transform, skipping: {StartName}", start.Name);
                continue;
            }

            // Check if this start is too close to already validated starts (basic clustering prevention)
            bool isValid = true;
            foreach (var validStart in validStarts)
            {
                if (validStart.Transform?.Translation != null)
                {
                    var distance = Prospect.Unreal.Assets.UnrealTypes.Vector3.Distance(start.Transform.Translation, validStart.Transform.Translation);
                    if (distance < clusterRadius)
                    {
                        isValid = false;
                        break;
                    }
                }
            }

            if (isValid)
            {
                validStarts.Add(start);
            }
        }

        Logger.Information("Cluster validation: {ValidCount}/{TotalCount} PlayerStarts passed clustering rules", 
            validStarts.Count, playerStarts.Count);

        return validStarts;
    }

    /// <summary>
    /// Initialize grid-based streaming system for MAP01 (A-J rows × 0-9 columns)
    /// </summary>
    private async Task InitializeGridStreamingAsync(string levelPath)
    {
        try
        {
            Logger.Information("Initializing grid-based streaming for MAP01...");
            
            // Initialize the grid streaming manager for MAP01's A-J/0-9 system
            await _gridStreamingManager.InitializeAsync(levelPath, _assetParser);
            
            Logger.Information("Grid streaming system initialized successfully");
            
            // Expand PlayerStart search to include grid cells
            await ExpandPlayerStartSearchToGridCellsAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize grid streaming system");
            // Non-fatal - continue without grid streaming
        }
    }
    
    /// <summary>
    /// Expand PlayerStart search to include loaded grid cells
    /// </summary>
    private async Task ExpandPlayerStartSearchToGridCellsAsync()
    {
        Logger.Information("Expanding PlayerStart search to grid cells...");
        
        var loadedCells = _gridStreamingManager.GetLoadedGridCells();
        int additionalPlayerStarts = 0;
        
        foreach (var cellId in loadedCells)
        {
            var gridCell = await _gridStreamingManager.LoadGridCellAsync(cellId);
            if (gridCell != null)
            {
                var cellPlayerStarts = gridCell.FindActorsByType("PlayerStart");
                Logger.Information("Grid cell {CellId}: Found {PlayerStartCount} PlayerStart actors", 
                    cellId, cellPlayerStarts.Count);
                
                // Add new PlayerStarts that aren't duplicates
                foreach (var playerStart in cellPlayerStarts)
                {
                    if (!IsPlayerStartDuplicate(playerStart))
                    {
                        _playerStartActors.Add(playerStart);
                        additionalPlayerStarts++;
                    }
                }
            }
        }
        
        Logger.Information("Expanded PlayerStart search: Added {AdditionalCount} PlayerStarts from {CellCount} grid cells", 
            additionalPlayerStarts, loadedCells.Length);
        Logger.Information("Total PlayerStart actors available: {TotalCount}", _playerStartActors.Count);
    }
    
    /// <summary>
    /// Check if a PlayerStart is a duplicate based on position
    /// </summary>
    private bool IsPlayerStartDuplicate(AssetUActor newPlayerStart)
    {
        if (newPlayerStart.Transform?.Translation == null)
            return true;
            
        const float duplicateThreshold = 10.0f; // Units - very close positions considered duplicates
        
        foreach (var existingStart in _playerStartActors)
        {
            if (existingStart.Transform?.Translation != null)
            {
                var distance = Prospect.Unreal.Assets.UnrealTypes.Vector3.Distance(
                    newPlayerStart.Transform.Translation, 
                    existingStart.Transform.Translation);
                    
                if (distance < duplicateThreshold)
                {
                    return true; // Found a duplicate
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Get a suitable PlayerStart for spawning a new player
    /// </summary>
    /// <param name="existingPlayerPositions">Positions of currently connected players</param>
    /// <returns>Best PlayerStart actor for spawning</returns>
    public AssetUActor? GetPlayerSpawnPoint(List<Vector3> existingPlayerPositions)
    {
        if (_playerStartActors.Count == 0)
        {
            Logger.Warning("No PlayerStart actors available for spawning");
            return null;
        }

        // If no existing players, return the first spawn point
        if (existingPlayerPositions.Count == 0)
        {
            return _playerStartActors[0];
        }

        // Use distance-based scoring system from map configuration
        if (_gameDataService != null)
        {
            var mapInfo = _gameDataService.GetMapInfo("Map01");
            if (mapInfo != null)
            {
                return CalculateBestSpawnPoint(_playerStartActors, existingPlayerPositions, mapInfo);
            }
        }

        // Fallback: Find the spawn point furthest from existing players
        AssetUActor? bestSpawn = null;
        float maxMinDistance = 0;

        foreach (var spawn in _playerStartActors)
        {
            if (spawn.Transform?.Translation == null)
                continue;

            float minDistance = float.MaxValue;
            foreach (var playerPos in existingPlayerPositions)
            {
                var distance = Prospect.Unreal.Assets.UnrealTypes.Vector3.Distance(spawn.Transform.Translation, ConvertVector3(playerPos));
                minDistance = Math.Min(minDistance, distance);
            }

            if (minDistance > maxMinDistance)
            {
                maxMinDistance = minDistance;
                bestSpawn = spawn;
            }
        }

        Logger.Information("Selected spawn point with min distance {Distance:F1} from existing players", maxMinDistance);
        return bestSpawn;
    }

    /// <summary>
    /// Calculate the best spawn point using the map's scoring system
    /// </summary>
    private AssetUActor? CalculateBestSpawnPoint(List<AssetUActor> spawnPoints, List<Vector3> existingPlayerPositions, Prospect.Unreal.Assets.DataTables.MapInfo mapInfo)
    {
        AssetUActor? bestSpawn = null;
        float bestScore = float.MinValue;

        foreach (var spawn in spawnPoints)
        {
            if (spawn.Transform?.Translation == null)
                continue;

            float score = 0;
            
            // Apply scoring rules from map configuration
            foreach (var rule in mapInfo.PlayerStartScoreRules)
            {
                int playersInRadius = 0;
                foreach (var playerPos in existingPlayerPositions)
                {
                    var distance = Prospect.Unreal.Assets.UnrealTypes.Vector3.Distance(spawn.Transform.Translation, ConvertVector3(playerPos));
                    if (distance <= rule.Radius)
                    {
                        playersInRadius++;
                    }
                }
                
                // Negative score for each player in radius (prefer less crowded areas)
                score -= playersInRadius * rule.ScorePerPlayerInRadius;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestSpawn = spawn;
            }
        }

        Logger.Information("Selected spawn point with score: {Score:F1}", bestScore);
        return bestSpawn;
    }

    /// <summary>
    /// Get statistics about the currently loaded level
    /// </summary>
    public LevelLoadingStats GetLevelStats()
    {
        return new LevelLoadingStats
        {
            IsLevelLoaded = _loadedAssetLevel != null,
            LevelName = _loadedAssetLevel?.Name ?? "None",
            TotalActors = _loadedAssetLevel?.Actors.Count ?? 0,
            PlayerStartCount = _playerStartActors.Count,
            StreamingLevelsCount = _loadedAssetLevel?.StreamingLevels?.Count ?? 0,
            GridCellsLoaded = _gridStreamingManager.GetLoadedCellCount()
        };
    }

    /// <summary>
    /// Convert between Vector3 types
    /// </summary>
    private static Prospect.Unreal.Assets.UnrealTypes.Vector3 ConvertVector3(Prospect.Server.Game.Vector3 source)
    {
        return new Prospect.Unreal.Assets.UnrealTypes.Vector3(source.X, source.Y, source.Z);
    }

    /// <summary>
    /// Convert an asset level (with UActor) to a runtime level (with AActor)
    /// </summary>
    private RuntimeULevel ConvertAssetLevelToRuntimeLevel(AssetULevel assetLevel)
    {
        var runtimeLevel = new RuntimeULevel();
        
        // Note: Currently we don't need to convert actors since the runtime level
        // will be populated by the networking system. The asset level is used
        // for spawn point information and level metadata.
        
        Logger.Information("Converted asset level '{Name}' to runtime level", assetLevel.Name);
        return runtimeLevel;
    }

    private void AddLevel(RuntimeULevel level)
    {
        // Use the protected method from UWorld base class
        AddLevelToWorld(level);
    }

    private void SetPersistentLevel(RuntimeULevel level)
    {
        // Use the protected method from UWorld base class
        SetPersistentLevelInternal(level);
    }
    
    /// <summary>
    /// Override to provide Prospect-specific spawn point logic
    /// </summary>
    public override FTransform? GetPlayerSpawnTransform()
    {
        // Get existing player positions (simplified for now)
        var existingPlayerPositions = new List<Vector3>();
        
        // TODO: Collect actual player positions from connected players
        
        var spawnPoint = GetPlayerSpawnPoint(existingPlayerPositions);
        if (spawnPoint?.Transform?.Translation != null)
        {
            return new FTransform
            {
                Location = new FVector(
                    spawnPoint.Transform.Translation.X,
                    spawnPoint.Transform.Translation.Y,
                    spawnPoint.Transform.Translation.Z
                )
            };
        }
        
        // Fallback to base implementation
        return base.GetPlayerSpawnTransform();
    }
}

/// <summary>
/// Statistics about loaded level data
/// </summary>
public class LevelLoadingStats
{
    public bool IsLevelLoaded { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public int TotalActors { get; set; }
    public int PlayerStartCount { get; set; }
    public int StreamingLevelsCount { get; set; }
    public int GridCellsLoaded { get; set; }

    public override string ToString()
    {
        return $"Level: {LevelName} (Loaded: {IsLevelLoaded}, Actors: {TotalActors}, PlayerStarts: {PlayerStartCount}, StreamingLevels: {StreamingLevelsCount}, GridCells: {GridCellsLoaded})";
    }
}

/// <summary>
/// Manages grid-based streaming for MAP01 (A-J rows × 0-9 columns)
/// </summary>
public class GridStreamingManager
{
    private static readonly ILogger Logger = Log.ForContext<GridStreamingManager>();
    private readonly Dictionary<string, AssetULevel> _loadedGridCells;
    private AssetParser? _assetParser;
    private string _baseLevelPath = string.Empty;

    public GridStreamingManager()
    {
        _loadedGridCells = new Dictionary<string, AssetULevel>();
    }

    public async Task InitializeAsync(string levelPath, AssetParser? assetParser)
    {
        _assetParser = assetParser;
        _baseLevelPath = levelPath.Replace("MP_Map01_P", "");
        
        Logger.Information("Grid streaming manager initialized for base path: {BasePath}", _baseLevelPath);
        
        // Pre-load some core grid cells (optional)
        await PreloadCoreGridCellsAsync();
    }

    private async Task PreloadCoreGridCellsAsync()
    {
        // Pre-load central grid cells for immediate gameplay
        var coreGridCells = new[] { "E4", "E5", "F4", "F5" }; // Center of the map
        
        foreach (var cellId in coreGridCells)
        {
            await LoadGridCellAsync(cellId);
        }
    }

    public async Task<AssetULevel?> LoadGridCellAsync(string gridCellId)
    {
        if (_loadedGridCells.TryGetValue(gridCellId, out var existingCell))
        {
            return existingCell;
        }

        if (_assetParser == null)
        {
            Logger.Warning("Asset parser not available for grid cell loading");
            return null;
        }

        try
        {
            // Construct path to grid cell gameplay file (GP directory)
            var gridCellPath = $"{_baseLevelPath}GP/MP_Map01_{gridCellId}_GP";
            
            Logger.Information("Loading grid cell: {GridCellId} from {GridCellPath}", gridCellId, gridCellPath);
            
            var gridCell = await _assetParser.LoadLevelAsync(gridCellPath);
            if (gridCell != null)
            {
                _loadedGridCells[gridCellId] = gridCell;
                Logger.Information("Successfully loaded grid cell {GridCellId} with {ActorCount} actors", 
                    gridCellId, gridCell.Actors.Count);
            }
            
            return gridCell;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load grid cell: {GridCellId}", gridCellId);
            return null;
        }
    }

    public int GetLoadedCellCount()
    {
        return _loadedGridCells.Count;
    }

    public void UnloadGridCell(string gridCellId)
    {
        if (_loadedGridCells.Remove(gridCellId))
        {
            Logger.Information("Unloaded grid cell: {GridCellId}", gridCellId);
        }
    }

    public string[] GetLoadedGridCells()
    {
        return _loadedGridCells.Keys.ToArray();
    }
}