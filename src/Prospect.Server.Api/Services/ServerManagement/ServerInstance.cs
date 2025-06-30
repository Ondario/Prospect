using System.Diagnostics;

namespace Prospect.Server.Api.Services.ServerManagement;

public class ServerInstance
{
    public string ServerId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7777;
    public ServerState State { get; set; } = ServerState.Starting;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public List<string> ConnectedPlayers { get; set; } = new List<string>();
    public Process? ServerProcess { get; set; }
    public int ProcessId { get; set; }
    
    public string GetConnectionString() => $"{Host}:{Port}";
    
    public bool IsHealthy => 
        State == ServerState.Active && 
        LastHeartbeat.HasValue && 
        DateTime.UtcNow - LastHeartbeat.Value < TimeSpan.FromMinutes(2);
}

public enum ServerState
{
    Starting,
    Active,
    Stopping,
    Terminated,
    Error
} 