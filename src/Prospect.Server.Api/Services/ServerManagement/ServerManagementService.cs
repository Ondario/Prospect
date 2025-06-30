using System.Collections.Concurrent;
using System.Diagnostics;

namespace Prospect.Server.Api.Services.ServerManagement;

public class ServerManagementService
{
    private readonly ILogger<ServerManagementService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, ServerInstance> _servers = new();
    private readonly Timer _healthCheckTimer;
    
    public ServerManagementService(ILogger<ServerManagementService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Start health check timer - check every 30 seconds
        _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
    
    public async Task<ServerInstance> GetOrCreateServerAsync(string sessionId, string mapName)
    {
        // Check if server already exists for this session
        var existingServer = _servers.Values.FirstOrDefault(s => s.SessionId == sessionId);
        if (existingServer != null && existingServer.IsHealthy)
        {
            _logger.LogInformation("[SERVER] Reusing existing server {ServerId} for session {SessionId}", 
                existingServer.ServerId, sessionId);
            return existingServer;
        }
        
        // Create new server instance
        var serverId = $"server-{sessionId}-{DateTime.UtcNow.Ticks}";
        var serverPort = await GetAvailablePortAsync();
        
        var serverInstance = new ServerInstance
        {
            ServerId = serverId,
            SessionId = sessionId,
            MapName = mapName,
            Host = _configuration["DedicatedServerHost"] ?? "127.0.0.1",
            Port = serverPort,
            State = ServerState.Starting
        };
        
        _servers[serverId] = serverInstance;
        
        // Start the server process
        await StartServerProcessAsync(serverInstance);
        
        return serverInstance;
    }
    
    private async Task StartServerProcessAsync(ServerInstance server)
    {
        try
        {
            var serverExecutablePath = _configuration["ServerExecutablePath"] ?? 
                Path.Combine(Directory.GetCurrentDirectory(), "../Prospect.Server.Game/Prospect.Server.Game.exe");
            
            if (!File.Exists(serverExecutablePath))
            {
                _logger.LogError("[SERVER] Server executable not found at: {Path}", serverExecutablePath);
                server.State = ServerState.Error;
                return;
            }
            
            var startInfo = new ProcessStartInfo
            {
                FileName = serverExecutablePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            // Set environment variables for the server
            startInfo.EnvironmentVariables["SERVER_PORT"] = server.Port.ToString();
            startInfo.EnvironmentVariables["DEFAULT_MAP"] = server.MapName;
            startInfo.EnvironmentVariables["SESSION_ID"] = server.SessionId;
            startInfo.EnvironmentVariables["SERVER_ID"] = server.ServerId;
            
            _logger.LogInformation("[SERVER] Starting server process: {ExecutablePath} with port {Port} and map {MapName}", 
                serverExecutablePath, server.Port, server.MapName);
            
            var process = Process.Start(startInfo);
            if (process != null)
            {
                server.ServerProcess = process;
                server.ProcessId = process.Id;
                server.StartedAt = DateTime.UtcNow;
                server.LastHeartbeat = DateTime.UtcNow;
                
                // Monitor process output
                _ = Task.Run(() => MonitorServerOutput(server, process));
                
                // Wait a moment for server to initialize
                await Task.Delay(2000);
                
                if (!process.HasExited)
                {
                    server.State = ServerState.Active;
                    _logger.LogInformation("[SERVER] Server {ServerId} started successfully on port {Port}", 
                        server.ServerId, server.Port);
                }
                else
                {
                    server.State = ServerState.Error;
                    _logger.LogError("[SERVER] Server {ServerId} exited immediately", server.ServerId);
                }
            }
            else
            {
                server.State = ServerState.Error;
                _logger.LogError("[SERVER] Failed to start server process for {ServerId}", server.ServerId);
            }
        }
        catch (Exception ex)
        {
            server.State = ServerState.Error;
            _logger.LogError(ex, "[SERVER] Exception starting server {ServerId}", server.ServerId);
        }
    }
    
    private async Task MonitorServerOutput(ServerInstance server, Process process)
    {
        try
        {
            while (!process.HasExited)
            {
                var output = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrEmpty(output))
                {
                    _logger.LogDebug("[SERVER-{ServerId}] {Output}", server.ServerId, output);
                    
                    // Update heartbeat when we see output
                    server.LastHeartbeat = DateTime.UtcNow;
                }
            }
            
            _logger.LogWarning("[SERVER] Server {ServerId} process exited with code {ExitCode}", 
                server.ServerId, process.ExitCode);
            
            server.State = ServerState.Terminated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SERVER] Error monitoring server {ServerId} output", server.ServerId);
        }
    }
    
    private async Task<int> GetAvailablePortAsync()
    {
        var basePort = int.Parse(_configuration["DedicatedServerPort"] ?? "7777");
        
        // Simple port allocation - start from base port and increment
        for (int i = 0; i < 100; i++)
        {
            var testPort = basePort + i;
            if (!_servers.Values.Any(s => s.Port == testPort))
            {
                return testPort;
            }
        }
        
        throw new InvalidOperationException("No available ports for server allocation");
    }
    
    public async Task ShutdownServerAsync(string serverId)
    {
        if (_servers.TryGetValue(serverId, out var server))
        {
            _logger.LogInformation("[SERVER] Shutting down server {ServerId}", serverId);
            
            server.State = ServerState.Stopping;
            
            try
            {
                if (server.ServerProcess != null && !server.ServerProcess.HasExited)
                {
                    server.ServerProcess.Kill();
                    await server.ServerProcess.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SERVER] Error shutting down server {ServerId}", serverId);
            }
            
            server.State = ServerState.Terminated;
            _servers.TryRemove(serverId, out _);
        }
    }
    
    private void PerformHealthCheck(object? state)
    {
        var currentTime = DateTime.UtcNow;
        var unhealthyServers = _servers.Values
            .Where(s => !s.IsHealthy || (s.ServerProcess?.HasExited == true))
            .ToList();
            
        foreach (var server in unhealthyServers)
        {
            _logger.LogWarning("[SERVER] Server {ServerId} is unhealthy, cleaning up", server.ServerId);
            _ = Task.Run(() => ShutdownServerAsync(server.ServerId));
        }
    }
    
    public IEnumerable<ServerInstance> GetAllServers() => _servers.Values.ToList();
    
    public ServerInstance? GetServer(string serverId) => _servers.TryGetValue(serverId, out var server) ? server : null;
} 