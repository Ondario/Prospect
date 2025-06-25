using Prospect.Unreal.Core;
using Prospect.Unreal.Runtime;
using Serilog;

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

        // Get server configuration from environment or use defaults
        var serverPort = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "7777");
        var defaultMap = Environment.GetEnvironmentVariable("DEFAULT_MAP") ?? "Station"; // Default to Station lobby
        var gameMode = Environment.GetEnvironmentVariable("GAME_MODE") ?? "/Script/Prospect/YGameMode_Station";
        
        // Map name validation and conversion
        if (!defaultMap.StartsWith("/Game/Maps/"))
        {
            // Convert short map names to full paths
            defaultMap = defaultMap switch
            {
                "BrightSands" => "/Game/Maps/MP/BrightSands/BrightSands_P",
                "CrescentFalls" => "/Game/Maps/MP/CrescentFalls/CrescentFalls_P",
                "TharisIsland" => "/Game/Maps/MP/TharisIsland/TharisIsland_P",
                "Station" => "/Game/Maps/MP/Station/Station_P", // Station lobby
                _ => "/Game/Maps/MP/Station/Station_P" // Default to Station lobby
            };
        }
        
        Logger.Information("Server Configuration: Port={Port}, Map={Map}, GameMode={GameMode}", 
            serverPort, defaultMap, gameMode);

        // Prospect:
        //  Map:        /Game/Maps/MP/Station/Station_P
        //  GameMode:   /Script/Prospect/YGameMode_Station
        
        var worldUrl = new FUrl
        {
            Map = defaultMap,
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
}