using Prospect.Unreal.Core;
using Prospect.Unreal.Runtime;
using Prospect.Server.Game.Services;
using Serilog;
using System.IO;

namespace Prospect.Server.Game;

internal static class Program
{
    private const float TickRate = (1000.0f / 60.0f) / 1000.0f;
    
    private static readonly ILogger Logger = Log.ForContext(typeof(Program));
    private static readonly PeriodicTimer Tick = new PeriodicTimer(TimeSpan.FromSeconds(TickRate));
    
    public static async Task Main()
    {
        Console.CancelKeyPress += (_, e) =>
        {
            Tick.Dispose();
            e.Cancel = true;
        };
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({SourceContext,-52}) {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        
        Logger.Information("Starting Prospect.Server.Game");

        // Check for asset loading test mode
        var testMode = Environment.GetEnvironmentVariable("ASSET_TEST_MODE");
        if (!string.IsNullOrEmpty(testMode))
        {
            await RunAssetLoadingTestAsync();
            return;
        }

        // Initialize new asset loading system
        var assetsBasePath = Environment.GetEnvironmentVariable("ASSETS_PATH") ?? 
                            Path.Combine(Directory.GetCurrentDirectory(), "Exports");
        
        GameDataService gameDataService = null;
        try
        {
            gameDataService = new GameDataService(assetsBasePath);
            await gameDataService.InitializeAsync();
            gameDataService.LogConfiguration();
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to initialize new asset system, falling back to legacy loader");
        }
        
        // Get server configuration from environment or use defaults
        var serverPort = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "7777");
        var requestedMap = Environment.GetEnvironmentVariable("DEFAULT_MAP") ?? "Map01";
        var requestedGameMode = Environment.GetEnvironmentVariable("GAME_MODE") ?? "LOOP";
        
        // Load map data from new asset system or fallback to legacy
        string mapPath;
        string gameMode;
        
        if (gameDataService != null && gameDataService.IsMapSupported(requestedMap))
        {
            var mapInfo = gameDataService.GetMapInfo(requestedMap);
            var gameModeInfo = gameDataService.GetGameModeTuning(requestedGameMode);
            
            mapPath = "/Game/" + mapInfo.GetPersistentMapPath();
            gameMode = gameModeInfo?.IsLoopMode == true ? "/Script/Prospect/YGameMode_Loop" : "/Script/Prospect/YGameMode_Station";
            
            Logger.Information("Using new asset system:");
            Logger.Information("  Map: {MapName} -> {MapPath}", requestedMap, mapPath);
            Logger.Information("  Game Mode: {GameMode} -> {GameModePath}", requestedGameMode, gameMode);
            Logger.Information("  Player Start Rules: {RuleCount} configured", mapInfo.PlayerStartScoreRules.Count);
        }
        else
        {
            // Fallback to legacy asset loader
            Logger.Information("Using legacy asset loader");
            var assetLoader = new ProspectAssetLoader();
            
            try
            {
                if (assetLoader.IsMapAvailable(requestedMap))
                {
                    mapPath = assetLoader.GetMapPath(requestedMap);
                    gameMode = assetLoader.GetGameMode(requestedMap);
                    var spawnPoints = assetLoader.GetSpawnPoints(requestedMap);
                    
                    Logger.Information("Loaded map '{MapName}' with {SpawnCount} spawn points", 
                        requestedMap, spawnPoints.Length);
                }
                else
                {
                    Logger.Warning("Requested map '{MapName}' not available, falling back to Station", requestedMap);
                    mapPath = assetLoader.GetMapPath("Station");
                    gameMode = assetLoader.GetGameMode("Station");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load map data, falling back to hardcoded values");
                mapPath = "/Game/Maps/MP/Station/Station_P";
                gameMode = "/Script/Prospect/YGameMode_Station";
            }
            
            var availableMaps = assetLoader.GetAvailableMaps();
            Logger.Information("Available maps: {Maps}", string.Join(", ", availableMaps));
        }
        
        Logger.Information("Final Server Configuration: Port={Port}, Map={Map}, GameMode={GameMode}", 
            serverPort, mapPath, gameMode);

        // Prospect:
        //  Map:        /Game/Maps/MP/Station/Station_P
        //  GameMode:   /Script/Prospect/YGameMode_Station
        
        var worldUrl = new FUrl
        {
            Map = mapPath,
            Port = serverPort,
            // GameMode = gameMode
        };
        
        await using (var world = new ProspectWorld())
        {
            world.SetGameInstance(new UGameInstance());
            world.SetGameMode(worldUrl);
            world.InitializeActorsForPlay(worldUrl, true);
            
            Logger.Information("Starting server on port {Port}", serverPort);
            if (world.Listen())
            {
                Logger.Information("Server started successfully and listening for connections");
            }
            else
            {
                Logger.Error("Failed to start server");
                return;
            }
        
            while (await Tick.WaitForNextTickAsync())
            {
                world.Tick(TickRate);
            }
        }
        
        Logger.Information("Shutting down");
    }

    private static async Task RunAssetLoadingTestAsync()
    {
        Logger.Information("Running Asset Loading Test...");
        
        var assetsBasePath = Environment.GetEnvironmentVariable("ASSETS_PATH") ?? 
                            Path.Combine(Directory.GetCurrentDirectory(), "Exports");
        
        var test = new AssetLoadingTest();
        await test.RunTestAsync(assetsBasePath);
        
        Logger.Information("Asset Loading Test completed. Press any key to exit...");
        Console.ReadKey();
    }
}