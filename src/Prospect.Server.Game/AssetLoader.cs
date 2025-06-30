using System.Text.Json;

namespace Prospect.Server.Game;

public class ProspectAssetLoader
{
    private readonly string _assetsPath;
    private readonly Dictionary<string, MapData> _loadedMaps = new();

    public ProspectAssetLoader(string assetsPath = "assets")
    {
        _assetsPath = assetsPath;
    }

    public MapData LoadMap(string mapName)
    {
        if (_loadedMaps.TryGetValue(mapName, out var cachedMap))
        {
            return cachedMap;
        }

        var mapFilePath = Path.Combine(_assetsPath, "maps", mapName, $"{mapName}_P.json");
        
        if (!File.Exists(mapFilePath))
        {
            throw new FileNotFoundException($"Map data file not found: {mapFilePath}");
        }

        try
        {
            var jsonContent = File.ReadAllText(mapFilePath);
            var mapData = JsonSerializer.Deserialize<MapData>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (mapData == null)
            {
                throw new InvalidOperationException($"Failed to deserialize map data: {mapFilePath}");
            }

            _loadedMaps[mapName] = mapData;
            return mapData;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error loading map '{mapName}': {ex.Message}", ex);
        }
    }

    public SpawnPoint[] GetSpawnPoints(string mapName)
    {
        var mapData = LoadMap(mapName);
        return mapData.SpawnPoints ?? Array.Empty<SpawnPoint>();
    }

    public string GetMapPath(string mapName)
    {
        var mapData = LoadMap(mapName);
        return mapData.MapPath;
    }

    public string GetGameMode(string mapName)
    {
        var mapData = LoadMap(mapName);
        return mapData.GameMode;
    }

    public bool IsMapAvailable(string mapName)
    {
        var mapFilePath = Path.Combine(_assetsPath, "maps", mapName, $"{mapName}_P.json");
        return File.Exists(mapFilePath);
    }

    public string[] GetAvailableMaps()
    {
        var mapsDirectory = Path.Combine(_assetsPath, "maps");
        if (!Directory.Exists(mapsDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(mapsDirectory)
            .Select(dir => new DirectoryInfo(dir).Name)
            .Where(IsMapAvailable)
            .ToArray();
    }
}

public class MapData
{
    public string MapName { get; set; } = string.Empty;
    public string MapPath { get; set; } = string.Empty;
    public string GameMode { get; set; } = string.Empty;
    public SpawnPoint[]? SpawnPoints { get; set; }
    public WorldBounds? WorldBounds { get; set; }
    public MapMetadata? Metadata { get; set; }
}

public class SpawnPoint
{
    public Vector3 Location { get; set; }
    public Rotator Rotation { get; set; }
    public string Type { get; set; } = "PlayerStart";
    public int TeamId { get; set; }
}

public class Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public class Rotator
{
    public float Pitch { get; set; }
    public float Yaw { get; set; }
    public float Roll { get; set; }
}

public class WorldBounds
{
    public Vector3? Min { get; set; }
    public Vector3? Max { get; set; }
}

public class MapMetadata
{
    public int MaxPlayers { get; set; }
    public string DefaultGameMode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
} 