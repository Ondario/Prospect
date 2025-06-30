using System;
using System.Collections.Generic;
using System.Linq;

namespace Prospect.Unreal.Assets.UnrealTypes
{
    /// <summary>
    /// Represents an Unreal Engine Actor
    /// Base class for all objects that can be placed in a level
    /// </summary>
    public class UActor : UObject
    {
        /// <summary>
        /// Transform information (position, rotation, scale)
        /// </summary>
        public Transform Transform { get; set; }

        /// <summary>
        /// Components attached to this actor
        /// </summary>
        public List<UActorComponent> Components { get; set; }

        /// <summary>
        /// Whether this actor is currently active/enabled
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Tags assigned to this actor for identification
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Layer this actor belongs to
        /// </summary>
        public string Layer { get; set; }

        /// <summary>
        /// Network replication settings
        /// </summary>
        public ActorReplicationInfo ReplicationInfo { get; set; }

        public UActor()
        {
            Transform = new Transform();
            Components = new List<UActorComponent>();
            Tags = new List<string>();
            ReplicationInfo = new ActorReplicationInfo();
        }

        /// <summary>
        /// Get the world position of this actor
        /// </summary>
        /// <returns>World position</returns>
        public Vector3 GetActorLocation()
        {
            return Transform?.Translation ?? Vector3.Zero;
        }

        /// <summary>
        /// Set the world position of this actor
        /// </summary>
        /// <param name="location">New world position</param>
        public void SetActorLocation(Vector3 location)
        {
            if (Transform == null)
                Transform = new Transform();
            Transform.Translation = location;
        }

        /// <summary>
        /// Get the rotation of this actor
        /// </summary>
        /// <returns>Actor rotation</returns>
        public Quaternion GetActorRotation()
        {
            return Transform?.Rotation ?? Quaternion.Identity;
        }

        /// <summary>
        /// Set the rotation of this actor
        /// </summary>
        /// <param name="rotation">New rotation</param>
        public void SetActorRotation(Quaternion rotation)
        {
            if (Transform == null)
                Transform = new Transform();
            Transform.Rotation = rotation;
        }

        /// <summary>
        /// Get a component of a specific type
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>Component if found, null otherwise</returns>
        public T GetComponent<T>() where T : UActorComponent
        {
            return Components.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Get a component by class name
        /// </summary>
        /// <param name="componentType">Component class name</param>
        /// <returns>Component if found, null otherwise</returns>
        public UActorComponent GetComponentByClass(string componentType)
        {
            return Components.FirstOrDefault(comp => comp.IsA(componentType));
        }

        /// <summary>
        /// Add a component to this actor
        /// </summary>
        /// <param name="component">Component to add</param>
        public void AddComponent(UActorComponent component)
        {
            if (component == null)
                return;

            component.Owner = this;
            Components.Add(component);
        }

        /// <summary>
        /// Remove a component from this actor
        /// </summary>
        /// <param name="component">Component to remove</param>
        /// <returns>True if component was removed</returns>
        public bool RemoveComponent(UActorComponent component)
        {
            if (Components.Remove(component))
            {
                if (component != null)
                    component.Owner = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if this actor has a specific tag
        /// </summary>
        /// <param name="tag">Tag to check for</param>
        /// <returns>True if actor has the tag</returns>
        public bool HasTag(string tag)
        {
            return Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Add a tag to this actor
        /// </summary>
        /// <param name="tag">Tag to add</param>
        public void AddTag(string tag)
        {
            if (!HasTag(tag))
                Tags.Add(tag);
        }

        /// <summary>
        /// Remove a tag from this actor
        /// </summary>
        /// <param name="tag">Tag to remove</param>
        /// <returns>True if tag was removed</returns>
        public bool RemoveTag(string tag)
        {
            return Tags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        /// <summary>
        /// Calculate distance to another actor
        /// </summary>
        /// <param name="otherActor">Actor to measure distance to</param>
        /// <returns>Distance in world units</returns>
        public float DistanceTo(UActor otherActor)
        {
            if (otherActor == null)
                return float.MaxValue;

            return Vector3.Distance(GetActorLocation(), otherActor.GetActorLocation());
        }

        /// <summary>
        /// Check if this actor is within a certain distance of a position
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="distance">Maximum distance</param>
        /// <returns>True if within distance</returns>
        public bool IsWithinDistance(Vector3 position, float distance)
        {
            return Vector3.Distance(GetActorLocation(), position) <= distance;
        }

        /// <summary>
        /// Virtual method called when actor is spawned
        /// </summary>
        public virtual void BeginPlay()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Virtual method called every tick
        /// </summary>
        /// <param name="deltaTime">Time since last tick</param>
        public virtual void Tick(float deltaTime)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Virtual method called when actor is destroyed
        /// </summary>
        public virtual void EndPlay()
        {
            // Override in derived classes
        }

        public override string ToString()
        {
            var location = GetActorLocation();
            return $"{GetSimpleClassName()}: {Name ?? "Unnamed"} at {location}";
        }
    }

    /// <summary>
    /// Base class for actor components
    /// </summary>
    public class UActorComponent : UObject
    {
        /// <summary>
        /// The actor that owns this component
        /// </summary>
        public UActor Owner { get; set; }

        /// <summary>
        /// Whether this component is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether this component should tick every frame
        /// </summary>
        public bool CanTick { get; set; } = false;

        /// <summary>
        /// Virtual method called when component is created
        /// </summary>
        public virtual void BeginPlay()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Virtual method called every tick (if CanTick is true)
        /// </summary>
        /// <param name="deltaTime">Time since last tick</param>
        public virtual void TickComponent(float deltaTime)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Virtual method called when component is destroyed
        /// </summary>
        public virtual void EndPlay()
        {
            // Override in derived classes
        }

        public override string ToString()
        {
            return $"{GetSimpleClassName()}: {Name ?? "Unnamed"} (Owner: {Owner?.Name ?? "None"})";
        }
    }

    /// <summary>
    /// Component for static mesh rendering
    /// </summary>
    public class UStaticMeshComponent : UActorComponent
    {
        /// <summary>
        /// The static mesh asset to render
        /// </summary>
        public string StaticMesh { get; set; }

        /// <summary>
        /// Materials applied to this mesh
        /// </summary>
        public List<string> Materials { get; set; }

        /// <summary>
        /// Relative transform from the actor
        /// </summary>
        public Transform RelativeTransform { get; set; }

        public UStaticMeshComponent()
        {
            Materials = new List<string>();
            RelativeTransform = new Transform();
        }
    }

    /// <summary>
    /// Actor replication information for networking
    /// </summary>
    public class ActorReplicationInfo
    {
        /// <summary>
        /// Whether this actor should be replicated over network
        /// </summary>
        public bool bReplicates { get; set; } = false;

        /// <summary>
        /// Whether movement should be replicated
        /// </summary>
        public bool bReplicateMovement { get; set; } = false;

        /// <summary>
        /// Network relevance distance
        /// </summary>
        public float NetCullDistanceSquared { get; set; } = 225000000.0f; // 15000^2

        /// <summary>
        /// Network update frequency
        /// </summary>
        public float NetUpdateFrequency { get; set; } = 100.0f;

        /// <summary>
        /// Whether this actor is network relevant to a specific player
        /// </summary>
        /// <param name="playerLocation">Player's location</param>
        /// <param name="actorLocation">This actor's location</param>
        /// <returns>True if relevant</returns>
        public bool IsNetworkRelevant(Vector3 playerLocation, Vector3 actorLocation)
        {
            if (!bReplicates)
                return false;

            var distanceSquared = Vector3.Distance(playerLocation, actorLocation);
            distanceSquared *= distanceSquared;

            return distanceSquared <= NetCullDistanceSquared;
        }
    }

    /// <summary>
    /// Specialized actor for player spawn points
    /// </summary>
    public class PlayerStart : UActor
    {
        /// <summary>
        /// Player number this spawn is designed for (0 = any player)
        /// </summary>
        public int PlayerStartTag { get; set; } = 0;

        /// <summary>
        /// Whether this spawn point is currently enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Last time this spawn point was used
        /// </summary>
        public DateTime LastUsedTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Minimum time between uses of this spawn point
        /// </summary>
        public TimeSpan CooldownTime { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Check if this spawn point is available for use
        /// </summary>
        /// <returns>True if available</returns>
        public bool IsAvailable()
        {
            return IsEnabled && 
                   IsActive && 
                   DateTime.UtcNow - LastUsedTime >= CooldownTime;
        }

        /// <summary>
        /// Mark this spawn point as used
        /// </summary>
        public void MarkAsUsed()
        {
            LastUsedTime = DateTime.UtcNow;
        }
    }
} 