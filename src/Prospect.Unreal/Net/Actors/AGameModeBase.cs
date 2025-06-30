using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Math;
using Prospect.Unreal.Core.Objects;
using Prospect.Unreal.Runtime;
using System.Collections.Generic;

namespace Prospect.Unreal.Net.Actors;

public class AGameModeBase : AInfo
{
    public AGameModeBase()
    {
        OptionsString = string.Empty;
    }
    
    /// <summary>
    ///     Save options string and parse it when needed
    /// </summary>
    public string OptionsString { get; set; }
    
    public AGameSession? GameSession { get; set; }

    public virtual void InitGame(string mapName, string options, out string errorMessage)
    {
        // Default error.
        errorMessage = string.Empty;
        
        // Find world.
        var world = GetWorld();
        
        // Save Options for future use
        OptionsString = options;

        var spawnInfo = new FActorSpawnParameters
        {
            Instigator = GetInstigator(),
            ObjectFlags = EObjectFlags.RF_Transient
        };

        // GameSession = world.SpawnActor();
    }

    public virtual void InitGameState()
    {
        throw new NotImplementedException();
    }

    public void PreLogin(string options, string address, FUniqueNetIdRepl uniqueId, out string? errorMessage)
    {
        // Login unique id must match server expected unique id type OR No unique id could mean game doesn't use them
        errorMessage = null;
    }

    public APlayerController? Login(UPlayer newPlayer, ENetRole inRemoteRole, string portal, string options, FUniqueNetIdRepl uniqueId, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (GameSession == null)
        {
            errorMessage = "Failed to spawn player controller, GameSession is null";
            return null;
        }

        errorMessage = GameSession.ApproveLogin(options);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            return null;
        }
        
        // Create YPlayerController for Prospect-specific functionality
        var world = GetWorld();
        if (world == null)
        {
            errorMessage = "Failed to get world for player spawning";
            return null;
        }
        
        // TODO: Use proper PlayerController class selection based on game mode
        // For now, always spawn YPlayerController
        var spawnParams = new FActorSpawnParameters
        {
            ObjectFlags = EObjectFlags.RF_Transient,
            bNoFail = true
        };
        
        // Spawn the YPlayerController
        var playerController = world.SpawnActor<YPlayerController>(GUClassArray.StaticClass<YPlayerController>(), spawnParams);
        
        if (playerController == null)
        {
            errorMessage = "Failed to spawn YPlayerController";
            return null;
        }
        
        return playerController;
    }

    public void PostLogin(APlayerController newPlayer)
    {
        if (newPlayer is YPlayerController yPlayerController)
        {
            // Get spawn point from world
            var world = GetWorld();
            var spawnTransform = world?.GetPlayerSpawnTransform();
            
            if (spawnTransform?.Location != null)
            {
                var spawnPoint = new Prospect.Unreal.Assets.UnrealTypes.Vector3(
                    spawnTransform.Location.X,
                    spawnTransform.Location.Y,
                    spawnTransform.Location.Z
                );
                
                yPlayerController.InitializePlayer(spawnPoint);
                yPlayerController.CompleteSpawning();
                
                // Log successful player spawn
                Serilog.Log.Information("Player spawned successfully at ({X:F1}, {Y:F1}, {Z:F1})", 
                    spawnPoint.X, spawnPoint.Y, spawnPoint.Z);
            }
            else
            {
                Serilog.Log.Warning("No valid spawn point found, using default position");
                yPlayerController.InitializePlayer(new Prospect.Unreal.Assets.UnrealTypes.Vector3(0, 0, 100));
                yPlayerController.CompleteSpawning();
            }
        }
        else
        {
            Serilog.Log.Warning("Player controller is not YPlayerController, skipping Prospect-specific initialization");
        }
    }
}