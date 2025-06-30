using Prospect.Unreal.Core;
using Prospect.Unreal.Runtime;
using Prospect.Unreal.Assets.UnrealTypes;
using Serilog;
using System;
using System.Collections.Generic;

namespace Prospect.Unreal.Net.Actors;

/// <summary>
/// Prospect-specific PlayerController implementation
/// Handles player state management, movement validation, and game-specific logic
/// </summary>
public class YPlayerController : APlayerController
{
    private static readonly ILogger Logger = Log.ForContext<YPlayerController>();
    
    /// <summary>
    /// Current player state
    /// </summary>
    public EPlayerState PlayerState { get; private set; }
    
    /// <summary>
    /// Player's current position in world space
    /// </summary>
    public Vector3 PlayerPosition { get; private set; }
    
    /// <summary>
    /// Player's current rotation
    /// </summary>
    public Quaternion PlayerRotation { get; private set; }
    
    /// <summary>
    /// Player's movement velocity
    /// </summary>
    public Vector3 PlayerVelocity { get; private set; }
    
    /// <summary>
    /// Whether the player is currently moving
    /// </summary>
    public bool IsMoving => PlayerVelocity.Magnitude > 0.1f;
    
    /// <summary>
    /// Player's spawn point reference
    /// </summary>
    public Vector3? SpawnPoint { get; private set; }
    
    /// <summary>
    /// Time when player last moved (for idle detection)
    /// </summary>
    public DateTime LastMovementTime { get; private set; }
    
    /// <summary>
    /// Maximum movement speed (units per second)
    /// </summary>
    public float MaxMovementSpeed { get; set; } = 357.0f; // Default from player tuning
    
    /// <summary>
    /// Sprint speed multiplier
    /// </summary>
    public float SprintMultiplier { get; set; } = 1.91f; // Default from player tuning
    
    /// <summary>
    /// Whether player is currently sprinting
    /// </summary>
    public bool IsSprinting { get; private set; }
    
    /// <summary>
    /// Movement validation parameters
    /// </summary>
    public MovementValidationParams MovementValidation { get; set; }
    
    public YPlayerController()
    {
        PlayerState = EPlayerState.Uninitialized;
        PlayerPosition = new Vector3(0, 0, 0);
        PlayerRotation = new Quaternion(0, 0, 0, 1);
        PlayerVelocity = new Vector3(0, 0, 0);
        LastMovementTime = DateTime.UtcNow;
        MovementValidation = new MovementValidationParams();
        
        Logger.Debug("YPlayerController created");
    }
    
    /// <summary>
    /// Initialize the player controller with spawn point
    /// </summary>
    public void InitializePlayer(Vector3 spawnPoint)
    {
        SpawnPoint = spawnPoint;
        PlayerPosition = spawnPoint;
        PlayerState = EPlayerState.Spawning;
        LastMovementTime = DateTime.UtcNow;
        
        Logger.Information("Player initialized at spawn point: ({X:F1}, {Y:F1}, {Z:F1})", 
            spawnPoint.X, spawnPoint.Y, spawnPoint.Z);
    }
    
    /// <summary>
    /// Complete player spawning process
    /// </summary>
    public void CompleteSpawning()
    {
        if (PlayerState == EPlayerState.Spawning)
        {
            PlayerState = EPlayerState.Playing;
            Logger.Information("Player spawning completed, now in Playing state");
        }
    }
    
    /// <summary>
    /// Update player movement with validation
    /// </summary>
    /// <param name="newPosition">New position to validate</param>
    /// <param name="newVelocity">New velocity vector</param>
    /// <param name="deltaTime">Time since last update</param>
    /// <returns>True if movement is valid</returns>
    public bool UpdateMovement(Vector3 newPosition, Vector3 newVelocity, float deltaTime)
    {
        if (PlayerState != EPlayerState.Playing)
        {
            Logger.Debug("Movement rejected: Player not in Playing state (current: {State})", PlayerState);
            return false;
        }
        
        // Validate movement speed
        var speed = newVelocity.Magnitude;
        var maxSpeed = IsSprinting ? MaxMovementSpeed * SprintMultiplier : MaxMovementSpeed;
        
        if (speed > maxSpeed * MovementValidation.SpeedTolerance)
        {
            Logger.Warning("Movement rejected: Speed {Speed:F1} exceeds maximum {MaxSpeed:F1}", speed, maxSpeed);
            return false;
        }
        
        // Validate position change (anti-teleport)
        var positionDelta = Vector3.Distance(PlayerPosition, newPosition);
        var maxPositionChange = maxSpeed * deltaTime * MovementValidation.PositionTolerance;
        
        if (positionDelta > maxPositionChange)
        {
            Logger.Warning("Movement rejected: Position change {Delta:F1} exceeds maximum {MaxChange:F1}", 
                positionDelta, maxPositionChange);
            return false;
        }
        
        // Update player state
        PlayerPosition = newPosition;
        PlayerVelocity = newVelocity;
        LastMovementTime = DateTime.UtcNow;
        
        Logger.Debug("Player movement updated: Position=({X:F1}, {Y:F1}, {Z:F1}), Speed={Speed:F1}", 
            PlayerPosition.X, PlayerPosition.Y, PlayerPosition.Z, speed);
        
        return true;
    }
    
    /// <summary>
    /// Set player sprinting state
    /// </summary>
    public void SetSprinting(bool sprinting)
    {
        if (IsSprinting != sprinting)
        {
            IsSprinting = sprinting;
            Logger.Debug("Player sprint state changed: {IsSprinting}", IsSprinting);
        }
    }
    
    /// <summary>
    /// Update player rotation
    /// </summary>
    public void UpdateRotation(Quaternion newRotation)
    {
        PlayerRotation = newRotation;
    }
    
    /// <summary>
    /// Check if player is idle (hasn't moved recently)
    /// </summary>
    public bool IsIdle(TimeSpan idleThreshold)
    {
        return DateTime.UtcNow - LastMovementTime > idleThreshold;
    }
    
    /// <summary>
    /// Disconnect the player
    /// </summary>
    public void DisconnectPlayer(string reason)
    {
        PlayerState = EPlayerState.Disconnected;
        Logger.Information("Player disconnected: {Reason}", reason);
    }
    
    /// <summary>
    /// Get player status summary
    /// </summary>
    public PlayerStatus GetPlayerStatus()
    {
        return new PlayerStatus
        {
            State = PlayerState,
            Position = PlayerPosition,
            Velocity = PlayerVelocity,
            IsMoving = IsMoving,
            IsSprinting = IsSprinting,
            LastMovementTime = LastMovementTime
        };
    }
}

/// <summary>
/// Player state enumeration
/// </summary>
public enum EPlayerState
{
    Uninitialized,
    Spawning,
    Playing,
    Dead,
    Spectating,
    Disconnected
}

/// <summary>
/// Movement validation parameters
/// </summary>
public class MovementValidationParams
{
    /// <summary>
    /// Speed tolerance multiplier (1.0 = exact, 1.1 = 10% tolerance)
    /// </summary>
    public float SpeedTolerance { get; set; } = 1.15f;
    
    /// <summary>
    /// Position change tolerance multiplier
    /// </summary>
    public float PositionTolerance { get; set; } = 1.25f;
    
    /// <summary>
    /// Whether to enable anti-cheat validation
    /// </summary>
    public bool EnableAntiCheat { get; set; } = true;
}

/// <summary>
/// Player status information
/// </summary>
public class PlayerStatus
{
    public EPlayerState State { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public bool IsMoving { get; set; }
    public bool IsSprinting { get; set; }
    public DateTime LastMovementTime { get; set; }
    
    public override string ToString()
    {
        return $"State: {State}, Position: ({Position.X:F1}, {Position.Y:F1}, {Position.Z:F1}), " +
               $"Moving: {IsMoving}, Sprinting: {IsSprinting}";
    }
}