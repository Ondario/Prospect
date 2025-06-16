using Prospect.Unreal.Core;
using Prospect.Unreal.Net;
using Prospect.Unreal.Runtime;
using Prospect.Unreal.Net.Packets.Control;
using Prospect.Unreal.Net.Packets.Bunch;
using Serilog;
using System.Numerics;
using Prospect.Shared.Protocol;
using Microsoft.Extensions.Logging;

namespace Prospect.Server.Game;

public class GameServer
{
    private readonly ILogger<GameServer> _logger;
    private readonly Dictionary<int, PlayerState> _players = new();
    private readonly Dictionary<UNetConnection, int> _connectionToPlayerId = new();
    private int _nextPlayerId = 1;
    private readonly PeriodicTimer _serverTick = new PeriodicTimer(TimeSpan.FromSeconds(1.0f / 60.0f));

    public GameServer(ILogger<GameServer> logger)
    {
        _logger = logger;
    }

    public async Task Start(string ipAddress, int port)
    {
        try
        {
            _logger.LogInformation("Starting game server on {IpAddress}:{Port}", ipAddress, port);

            var world = new ProspectWorld();
            var netDriver = new UIpNetDriver(System.Net.IPAddress.Parse(ipAddress), port, true);
            
            // Initialize the connectionless handler
            netDriver.InitConnectionlessHandler();
            
            netDriver.InitListen(world);

            // Start server tick loop
            _ = StartServerTick(netDriver);

            // Keep the server running
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting game server");
            throw;
        }
    }

    private async Task StartServerTick(UIpNetDriver netDriver)
    {
        while (await _serverTick.WaitForNextTickAsync())
        {
            try
            {
                netDriver.TickDispatch(1.0f / 60.0f);
                netDriver.PostTickDispatch();

                netDriver.TickFlush(1.0f / 60.0f);
                netDriver.PostTickFlush();

                // Broadcast world state to all clients
                BroadcastWorldState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in server tick");
            }
        }
    }

    public void HandleClientConnection(UNetConnection connection)
    {
        _logger.LogInformation("New client connected");
        connection.Handler?.BeginHandshaking(() => HandleClientHandshake(connection));
    }

    private void HandleClientHandshake(UNetConnection connection)
    {
        try
        {
            // Assign a new player ID
            var playerId = _nextPlayerId++;
            _connectionToPlayerId[connection] = playerId;

            // Create initial player state
            var playerState = new PlayerState
            {
                PlayerId = playerId,
                Position = Vector3.Zero,
                Rotation = 0.0f
            };
            _players[playerId] = playerState;

            // Send join response
            SendJoinResponse(connection, playerId);

            // Notify other players about new player
            BroadcastPlayerJoined(playerId);

            _logger.LogInformation("Client {PlayerId} joined the game", playerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client handshake");
        }
    }

    private void SendJoinResponse(UNetConnection connection, int playerId)
    {
        try
        {
            var channel = connection.CreateChannelByName(new Unreal.Core.Names.FName(Unreal.Core.Names.EName.Control), Unreal.Net.Channels.EChannelCreateFlags.None, 0);
            var bunch = new FOutBunch(channel, false);

            // Write message type
            bunch.WriteByte((byte)NetworkMessageType.JoinResponse);

            // Write response data
            bunch.WriteBoolean(true); // Success
            bunch.WriteInt32(playerId);
            bunch.WriteString("Welcome to the game!");

            connection.SendRawBunch(bunch, false);
            connection.FlushNet();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending join response");
        }
    }

    private void BroadcastPlayerJoined(int playerId)
    {
        var player = _players[playerId];
        foreach (var connection in _connectionToPlayerId.Keys)
        {
            if (_connectionToPlayerId[connection] != playerId)
            {
                try
                {
                    var channel = connection.CreateChannelByName(new Unreal.Core.Names.FName(Unreal.Core.Names.EName.Control), Unreal.Net.Channels.EChannelCreateFlags.None, 0);
                    var bunch = new FOutBunch(channel, false);

                    // Write message type
                    bunch.WriteByte((byte)NetworkMessageType.StateUpdate);

                    // Write player state
                    bunch.WriteInt32(player.PlayerId);
                    bunch.WriteFloat(player.Position.X);
                    bunch.WriteFloat(player.Position.Y);
                    bunch.WriteFloat(player.Position.Z);
                    bunch.WriteFloat(player.Rotation);

                    connection.SendRawBunch(bunch, false);
                    connection.FlushNet();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting player joined");
                }
            }
        }
    }

    public void HandleMovementUpdate(UNetConnection connection, NMT_Movement update)
    {
        if (_connectionToPlayerId.TryGetValue(connection, out var playerId) && _players.TryGetValue(playerId, out var player))
        {
            // Update player state
            player.Position = update.Position;
            player.Rotation = update.Rotation;

            // Broadcast movement to other players
            foreach (var otherConnection in _connectionToPlayerId.Keys)
            {
                if (otherConnection != connection)
                {
                    try
                    {
                        var channel = otherConnection.CreateChannelByName(new Unreal.Core.Names.FName(Unreal.Core.Names.EName.Control), Unreal.Net.Channels.EChannelCreateFlags.None, 0);
                        var bunch = new FOutBunch(channel, false);

                        // Write message type
                        bunch.WriteByte((byte)NetworkMessageType.Movement);

                        // Write movement data
                        bunch.WriteInt32(playerId);
                        bunch.WriteFloat(update.Position.X);
                        bunch.WriteFloat(update.Position.Y);
                        bunch.WriteFloat(update.Position.Z);
                        bunch.WriteFloat(update.Rotation);

                        otherConnection.SendRawBunch(bunch, false);
                        otherConnection.FlushNet();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error broadcasting movement update");
                    }
                }
            }
        }
    }

    private void BroadcastWorldState()
    {
        var worldState = new WorldState
        {
            Players = _players
        };

        foreach (var connection in _connectionToPlayerId.Keys)
        {
            try
            {
                var channel = connection.CreateChannelByName(new Unreal.Core.Names.FName(Unreal.Core.Names.EName.Control), Unreal.Net.Channels.EChannelCreateFlags.None, 0);
                var bunch = new FOutBunch(channel, false);

                // Write message type
                bunch.WriteByte((byte)NetworkMessageType.StateUpdate);

                // Write world state
                bunch.WriteInt32(worldState.Players.Count);
                foreach (var player in worldState.Players)
                {
                    bunch.WriteInt32(player.Value.PlayerId);
                    bunch.WriteFloat(player.Value.Position.X);
                    bunch.WriteFloat(player.Value.Position.Y);
                    bunch.WriteFloat(player.Value.Position.Z);
                    bunch.WriteFloat(player.Value.Rotation);
                }

                connection.SendRawBunch(bunch, false);
                connection.FlushNet();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting world state");
            }
        }
    }

    public void HandleClientDisconnection(UNetConnection connection)
    {
        if (_connectionToPlayerId.TryGetValue(connection, out var playerId))
        {
            _players.Remove(playerId);
            _connectionToPlayerId.Remove(connection);
            _logger.LogInformation("Client {PlayerId} disconnected", playerId);
        }
    }
} 