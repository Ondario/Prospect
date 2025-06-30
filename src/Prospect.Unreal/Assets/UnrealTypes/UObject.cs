using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Prospect.Unreal.Assets.UnrealTypes
{
    /// <summary>
    /// Base class for all Unreal Engine objects
    /// Represents the fundamental UObject type from Unreal Engine
    /// </summary>
    public class UObject
    {
        /// <summary>
        /// The name of this object
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The Unreal Engine class of this object (e.g., "UScriptClass'Actor'")
        /// </summary>
        public string Class { get; set; }

        /// <summary>
        /// The asset path this object was loaded from
        /// </summary>
        public string AssetPath { get; set; }

        /// <summary>
        /// Object flags from Unreal Engine
        /// </summary>
        public string Flags { get; set; }

        /// <summary>
        /// Properties of this object, stored as dynamic JSON values
        /// </summary>
        public Dictionary<string, JToken> Properties { get; set; }

        /// <summary>
        /// Child objects owned by this object
        /// </summary>
        public List<UObject> Children { get; set; }

        public UObject()
        {
            Properties = new Dictionary<string, JToken>();
            Children = new List<UObject>();
        }

        /// <summary>
        /// Get a property value as a specific type
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="defaultValue">Default value if property doesn't exist</param>
        /// <returns>Property value or default</returns>
        public T GetProperty<T>(string propertyName, T defaultValue = default(T))
        {
            if (Properties.TryGetValue(propertyName, out var token))
            {
                try
                {
                    return token.ToObject<T>();
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Set a property value
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="value">Value to set</param>
        public void SetProperty(string propertyName, object value)
        {
            Properties[propertyName] = JToken.FromObject(value);
        }

        /// <summary>
        /// Check if this object has a specific property
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>True if property exists</returns>
        public bool HasProperty(string propertyName)
        {
            return Properties.ContainsKey(propertyName);
        }

        /// <summary>
        /// Get the simple class name without Unreal Engine prefixes
        /// </summary>
        /// <returns>Clean class name</returns>
        public string GetSimpleClassName()
        {
            if (string.IsNullOrEmpty(Class))
                return "Unknown";

            // Remove UScriptClass' prefix and trailing '
            var className = Class;
            if (className.StartsWith("UScriptClass'") && className.EndsWith("'"))
            {
                className = className.Substring(13, className.Length - 14);
            }
            else if (className.StartsWith("BlueprintGeneratedClass'") && className.EndsWith("'"))
            {
                className = className.Substring(25, className.Length - 26);
                // Extract just the class name from the path
                var lastSlash = className.LastIndexOf('/');
                if (lastSlash >= 0)
                {
                    className = className.Substring(lastSlash + 1);
                }
                // Remove _C suffix common in blueprints
                if (className.EndsWith("_C"))
                {
                    className = className.Substring(0, className.Length - 2);
                }
            }

            return className;
        }

        /// <summary>
        /// Check if this object is of a specific class type
        /// </summary>
        /// <param name="className">Class name to check (e.g., "Actor", "PlayerStart")</param>
        /// <returns>True if object is of the specified class</returns>
        public bool IsA(string className)
        {
            var simpleClass = GetSimpleClassName();
            return simpleClass.Equals(className, StringComparison.OrdinalIgnoreCase) ||
                   simpleClass.Contains(className);
        }

        public override string ToString()
        {
            return $"{GetSimpleClassName()}: {Name ?? "Unnamed"} ({AssetPath})";
        }
    }

    /// <summary>
    /// Represents a 3D vector
    /// </summary>
    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 Zero => new Vector3(0, 0, 0);
        public static Vector3 One => new Vector3(1, 1, 1);

        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3 Normalized
        {
            get
            {
                var mag = Magnitude;
                return mag > 0 ? new Vector3(X / mag, Y / mag, Z / mag) : Zero;
            }
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) =>
            new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3 operator -(Vector3 a, Vector3 b) =>
            new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3 operator *(Vector3 a, float scalar) =>
            new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);

        public static float Distance(Vector3 a, Vector3 b) => (a - b).Magnitude;

        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
    }

    /// <summary>
    /// Represents a quaternion rotation
    /// </summary>
    public struct Quaternion
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Quaternion Identity => new Quaternion(0, 0, 0, 1);

        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2}, {W:F2})";
    }

    /// <summary>
    /// Represents a 3D transformation (position, rotation, scale)
    /// </summary>
    public class Transform
    {
        public Vector3 Translation { get; set; } = Vector3.Zero;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; } = Vector3.One;

        public Transform() { }

        public Transform(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
        }

        public override string ToString() =>
            $"Pos: {Translation}, Rot: {Rotation}, Scale: {Scale}";
    }

    /// <summary>
    /// Generic DataTable container
    /// </summary>
    /// <typeparam name="T">Type of row data</typeparam>
    public class DataTable<T> : UObject where T : class
    {
        public Dictionary<string, T> Rows { get; set; }

        public DataTable()
        {
            Rows = new Dictionary<string, T>();
        }

        /// <summary>
        /// Get a row by name
        /// </summary>
        /// <param name="rowName">Name of the row</param>
        /// <returns>Row data or null if not found</returns>
        public T GetRow(string rowName)
        {
            return Rows.TryGetValue(rowName, out var row) ? row : null;
        }

        /// <summary>
        /// Check if a row exists
        /// </summary>
        /// <param name="rowName">Name of the row</param>
        /// <returns>True if row exists</returns>
        public bool HasRow(string rowName)
        {
            return Rows.ContainsKey(rowName);
        }

        /// <summary>
        /// Get all row names
        /// </summary>
        /// <returns>Collection of row names</returns>
        public IEnumerable<string> GetRowNames()
        {
            return Rows.Keys;
        }

        /// <summary>
        /// Get all rows
        /// </summary>
        /// <returns>Collection of all row data</returns>
        public IEnumerable<T> GetAllRows()
        {
            return Rows.Values;
        }
    }
} 