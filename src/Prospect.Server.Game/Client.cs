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

internal class Client
{
    private const float TickRate = (1000.0f / 60.0f) / 1000.0f;
    private const float PositionUpdateRate = 1.0f / 20.0f; // 20 updates per second
    private readonly PeriodicTimer ClientTick = new PeriodicTimer(TimeSpan.FromSeconds(TickRate));
    private readonly PeriodicTimer PositionUpdateTick = new PeriodicTimer(TimeSpan.FromSeconds(PositionUpdateRate));
    
    private UNetConnection? UnitConn = null;
    private bool IsConnected = false;
    private bool IsHandshakeComplete = false;
    private bool IsLoggedIn = false;
    private bool IsJoined = false;
    private int PlayerId = -1;
    private readonly ILogger<Client> _logger;

    // Player state
    private Vector3 CurrentPosition = Vector3.Zero;
    private Vector3 TargetPosition = Vector3.Zero;
    private float CurrentRotation;
    private readonly Dictionary<int, PlayerState> OtherPlayers = new();

    public Client(ILogger<Client> logger)
    {
        _logger = logger;
    }

    public async Task<UIpNetDriver> Connect(string ipAddr, int port, FUrl worldUrl)
    {
        try
        {
            _logger.LogInformation("Connecting to {IpAddress}:{Port}", ipAddr, port);

            var connection = new UIpNetDriver(System.Net.IPAddress.Parse(ipAddr), port, false);
            await using (var world = new ProspectWorld())
                connection.InitConnect(world, new FUrl { Host = System.Net.IPAddress.Parse(ipAddr), Port = port });
            UnitConn = connection.ServerConnection;
            connection.ServerConnection.Handler?.BeginHandshaking(SendInitialJoin);

            // Start update loops
            _ = StartPositionUpdateLoop();

            while (await ClientTick.WaitForNextTickAsync())
            {
                if (UnitConn != null && connection != null)
                {
                    connection.TickDispatch(TickRate);
                    connection.PostTickDispatch();

                    connection.TickFlush(TickRate);
                    connection.PostTickFlush();
                }
            }

            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to server");
            throw;
        }
    }

    private async Task StartPositionUpdateLoop()
    {
        while (await PositionUpdateTick.WaitForNextTickAsync())
        {
            if (IsConnected && IsHandshakeComplete && IsLoggedIn && IsJoined && UnitConn != null)
            {
                SendPositionUpdate();
            }
        }
    }

    private void SendPositionUpdate()
    {
        try
        {
            if (UnitConn == null) return;
            var channel = UnitConn.CreateChannelByName(new Unreal.Core.Names.FName(Unreal.Core.Names.EName.Control), Unreal.Net.Channels.EChannelCreateFlags.None, 0);
            var bunch = new FOutBunch(channel, false);
            
            // Write message type
            bunch.WriteByte((byte)NetworkMessageType.Movement);
            
            // Write movement data
            bunch.WriteInt32(PlayerId);
            bunch.WriteFloat(CurrentPosition.X);
            bunch.WriteFloat(CurrentPosition.Y);
            bunch.WriteFloat(CurrentPosition.Z);
            bunch.WriteFloat(CurrentRotation);

            UnitConn.SendRawBunch(bunch, false);
            UnitConn.FlushNet();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending position update");
        }
    }

    public void UpdateMovement(Vector3 newPosition, float newRotation)
    {
        TargetPosition = newPosition;
        CurrentRotation = newRotation;
    }

    public void SendInitialJoin()
    {
        try
        {
            if (UnitConn == null) return;
            var channel = UnitConn.CreateChannelByName(new Unreal.Core.Names.FName(Unreal.Core.Names.EName.Control), Unreal.Net.Channels.EChannelCreateFlags.None, 0);
            var BunchSequence = ++UnitConn.OutReliable[0];
            var ControlChanBunch = new FOutBunch(channel, false);
            
            // Set up basic bunch properties
            ControlChanBunch.Time = 0.0;
            ControlChanBunch.ReceivedAck = false;
            ControlChanBunch.PacketId = 0;
            ControlChanBunch.ChIndex = 0;
            ControlChanBunch.ChName = new Unreal.Core.Names.FName(Unreal.Core.Names.EName.Control);
            ControlChanBunch.bReliable = true;
            ControlChanBunch.ChSequence = BunchSequence;
            ControlChanBunch.bOpen = true;

            if (UnitConn.Channels[0] != null && UnitConn.Channels[0].OpenPacketId.First == 0)
            {
                UnitConn.Channels[0].OpenPacketId = new FPacketIdRange(BunchSequence, BunchSequence);
            }

            // Send NMT_Hello
            ControlChanBunch.SetAllowResize(true);
            ControlChanBunch.WriteByte((byte)NetworkMessageType.Hello);
            ControlChanBunch.WriteByte(0); // IsLittleEndian
            ControlChanBunch.WriteInt32(0); // NetworkVersion

            UnitConn.SendRawBunch(ControlChanBunch, false);
            UnitConn.FlushNet();

            IsConnected = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending initial join");
        }
    }

    public void HandleJoinResponse(NMT_JoinResponse response)
    {
        if (response.Success)
        {
            PlayerId = response.PlayerId;
            IsJoined = true;
            _logger.LogInformation("Successfully joined game with PlayerId {PlayerId}", PlayerId);
        }
        else
        {
            _logger.LogError("Failed to join game: {Message}", response.Message);
        }
    }

    public void HandleMovementUpdate(NMT_Movement update)
    {
        if (OtherPlayers.TryGetValue(update.PlayerId, out var player))
        {
            player.Position = update.Position;
            player.Rotation = update.Rotation;
                }
                else
                {
            OtherPlayers[update.PlayerId] = new PlayerState
            {
                PlayerId = update.PlayerId,
                Position = update.Position,
                Rotation = update.Rotation
            };
        }
    }
}
