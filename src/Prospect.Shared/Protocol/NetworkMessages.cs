using System.Numerics;

namespace Prospect.Shared.Protocol;

public enum NetworkMessageType
{
    Hello,
    HelloResponse,
    JoinRequest,
    JoinResponse,
    LoginRequest,
    LoginResponse,
    SpawnRequest,
    SpawnResponse,
    Movement,
    StateUpdate,
    ActorReplication,
    Error
}

public class PlayerState
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class WorldState
{
    public Dictionary<int, PlayerState> Players { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
    public List<WorldEvent> Events { get; set; } = new();
    public int StateVersion { get; set; }
}

public class WorldEvent
{
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public float Timestamp { get; set; }
}

public class NMT_JoinResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PlayerId { get; set; }
}

public class NMT_SpawnResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ActorId { get; set; }
    public Vector3 Position { get; set; }
}

public class NMT_Movement
{
    public int PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
}

public class NMT_StateUpdate
{
    public int PlayerId { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
} 