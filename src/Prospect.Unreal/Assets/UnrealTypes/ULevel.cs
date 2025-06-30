using System;
using System.Collections.Generic;
using System.Linq;

namespace Prospect.Unreal.Assets.UnrealTypes
{
    /// <summary>
    /// Represents an Unreal Engine Level/Map
    /// Contains actors, streaming levels, and world data
    /// </summary>
    public class ULevel : UObject
    {
        /// <summary>
        /// All actors present in this level
        /// </summary>
        public List<UActor> Actors { get; set; }

        /// <summary>
        /// List of streaming levels that should be loaded with this level
        /// </summary>
        public List<string> StreamingLevels { get; set; }

        /// <summary>
        /// World bounds for this level
        /// </summary>
        public WorldBounds Bounds { get; set; }

        /// <summary>
        /// Level-specific settings and metadata
        /// </summary>
        public LevelSettings Settings { get; set; }

        public ULevel()
        {
            Actors = new List<UActor>();
            StreamingLevels = new List<string>();
            Settings = new LevelSettings();
        }

        /// <summary>
        /// Find all actors of a specific type
        /// </summary>
        /// <param name="actorType">Type name to search for (e.g., "PlayerStart")</param>
        /// <returns>List of matching actors</returns>
        public List<UActor> FindActorsByType(string actorType)
        {
            return Actors.Where(actor => actor.IsA(actorType)).ToList();
        }

        /// <summary>
        /// Find an actor by name
        /// </summary>
        /// <param name="actorName">Name of the actor to find</param>
        /// <returns>Actor if found, null otherwise</returns>
        public UActor FindActorByName(string actorName)
        {
            return Actors.FirstOrDefault(actor => 
                actor.Name?.Equals(actorName, StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// Find all PlayerStart actors in the level
        /// </summary>
        /// <returns>List of PlayerStart actors</returns>
        public List<UActor> GetPlayerStartActors()
        {
            return FindActorsByType("PlayerStart");
        }

        /// <summary>
        /// Find actors within a specific radius of a position
        /// </summary>
        /// <param name="position">Center position</param>
        /// <param name="radius">Search radius</param>
        /// <returns>List of actors within radius</returns>
        public List<UActor> FindActorsInRadius(Vector3 position, float radius)
        {
            return Actors.Where(actor =>
            {
                if (actor.Transform?.Translation == null)
                    return false;

                var distance = Vector3.Distance(position, actor.Transform.Translation);
                return distance <= radius;
            }).ToList();
        }

        /// <summary>
        /// Add an actor to the level
        /// </summary>
        /// <param name="actor">Actor to add</param>
        public void AddActor(UActor actor)
        {
            if (actor == null)
                return;

            Actors.Add(actor);
        }

        /// <summary>
        /// Remove an actor from the level
        /// </summary>
        /// <param name="actor">Actor to remove</param>
        /// <returns>True if actor was removed</returns>
        public bool RemoveActor(UActor actor)
        {
            return Actors.Remove(actor);
        }

        /// <summary>
        /// Get statistics about this level
        /// </summary>
        /// <returns>Level statistics</returns>
        public LevelStats GetStats()
        {
            var stats = new LevelStats
            {
                TotalActors = Actors.Count,
                StreamingLevelCount = StreamingLevels.Count
            };

            // Count actors by type
            var actorTypeCounts = new Dictionary<string, int>();
            foreach (var actor in Actors)
            {
                var typeName = actor.GetSimpleClassName();
                actorTypeCounts[typeName] = actorTypeCounts.GetValueOrDefault(typeName, 0) + 1;
            }
            stats.ActorTypeCounts = actorTypeCounts;

            // Find bounds
            if (Actors.Any(a => a.Transform != null))
            {
                var positions = Actors
                    .Where(a => a.Transform != null)
                    .Select(a => a.Transform.Translation)
                    .ToList();

                if (positions.Any())
                {
                    stats.MinBounds = new Vector3(
                        positions.Min(p => p.X),
                        positions.Min(p => p.Y),
                        positions.Min(p => p.Z)
                    );
                    stats.MaxBounds = new Vector3(
                        positions.Max(p => p.X),
                        positions.Max(p => p.Y),
                        positions.Max(p => p.Z)
                    );
                }
            }

            return stats;
        }

        public override string ToString()
        {
            return $"Level: {Name} ({Actors.Count} actors, {StreamingLevels.Count} streaming levels)";
        }
    }

    /// <summary>
    /// World bounds information
    /// </summary>
    public class WorldBounds
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }
        public Vector3 Center => new Vector3(
            (Min.X + Max.X) / 2,
            (Min.Y + Max.Y) / 2,
            (Min.Z + Max.Z) / 2
        );
        public Vector3 Size => Max - Min;

        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }
    }

    /// <summary>
    /// Level-specific settings
    /// </summary>
    public class LevelSettings
    {
        public string GameMode { get; set; }
        public int MaxPlayers { get; set; } = 20;
        public bool EnableStreamingLevels { get; set; } = true;
        public float StreamingDistance { get; set; } = 10000.0f;
        public Dictionary<string, object> CustomSettings { get; set; }

        public LevelSettings()
        {
            CustomSettings = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Level statistics
    /// </summary>
    public class LevelStats
    {
        public int TotalActors { get; set; }
        public int StreamingLevelCount { get; set; }
        public Dictionary<string, int> ActorTypeCounts { get; set; }
        public Vector3 MinBounds { get; set; }
        public Vector3 MaxBounds { get; set; }

        public LevelStats()
        {
            ActorTypeCounts = new Dictionary<string, int>();
        }

        public override string ToString()
        {
            var bounds = $"Bounds: {MinBounds} to {MaxBounds}";
            var actorSummary = string.Join(", ", 
                ActorTypeCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            
            return $"Level Stats - Actors: {TotalActors}, Streaming: {StreamingLevelCount}, {bounds}, Types: {actorSummary}";
        }
    }
} 