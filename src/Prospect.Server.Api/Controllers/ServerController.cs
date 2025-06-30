using Microsoft.AspNetCore.Mvc;
using Prospect.Server.Api.Services.ServerManagement;

namespace Prospect.Server.Api.Controllers;

[Route("Server")]
[ApiController]
public class ServerController : ControllerBase
{
    private readonly ILogger<ServerController> _logger;
    private readonly ServerManagementService _serverManagementService;

    public ServerController(ILogger<ServerController> logger, ServerManagementService serverManagementService)
    {
        _logger = logger;
        _serverManagementService = serverManagementService;
    }

    [HttpGet("Status")]
    public IActionResult GetServerStatus()
    {
        var servers = _serverManagementService.GetAllServers();
        
        return Ok(new
        {
            ActiveServers = servers.Count(s => s.State == ServerState.Active),
            TotalServers = servers.Count(),
            Servers = servers.Select(s => new
            {
                s.ServerId,
                s.SessionId,
                s.MapName,
                s.Host,
                s.Port,
                State = s.State.ToString(),
                s.CreatedAt,
                s.StartedAt,
                s.LastHeartbeat,
                ConnectedPlayers = s.ConnectedPlayers.Count,
                ConnectionString = s.GetConnectionString(),
                IsHealthy = s.IsHealthy
            })
        });
    }

    [HttpPost("Shutdown/{serverId}")]
    public async Task<IActionResult> ShutdownServer(string serverId)
    {
        var server = _serverManagementService.GetServer(serverId);
        if (server == null)
        {
            return NotFound($"Server {serverId} not found");
        }

        await _serverManagementService.ShutdownServerAsync(serverId);
        
        _logger.LogInformation("[ADMIN] Manual shutdown requested for server {ServerId}", serverId);
        
        return Ok(new { Message = $"Server {serverId} shutdown initiated" });
    }

    [HttpPost("Create")]
    public async Task<IActionResult> CreateServer([FromBody] CreateServerRequest request)
    {
        try
        {
            var server = await _serverManagementService.GetOrCreateServerAsync(request.SessionId, request.MapName);
            
            return Ok(new
            {
                server.ServerId,
                server.SessionId,
                server.MapName,
                server.Host,
                server.Port,
                State = server.State.ToString(),
                ConnectionString = server.GetConnectionString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN] Failed to create server for session {SessionId}", request.SessionId);
            return BadRequest(new { Error = ex.Message });
        }
    }
}

public class CreateServerRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
} 