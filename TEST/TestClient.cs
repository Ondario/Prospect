using Microsoft.Extensions.Logging;
using Prospect.Server.Game; // For ProspectWorld
using Prospect.Unreal.Core;
using Prospect.Unreal.Net;
using Prospect.Unreal.Runtime;
using Prospect.Shared.Protocol;
using Prospect.Unreal.Net.Packets.Bunch; // For FOutBunch
using System.Net;
using System.Security.Cryptography;
using Prospect.Unreal.Serialization;

namespace Prospect.TestClient;

public class TestClient
{
    private readonly ILogger<TestClient> _logger;
    private UIpNetDriver? _netDriver;
    private UNetConnection? _connection;
    private bool _isConnected;
    private int _playerId = -1;
    private byte[] _challengeCookie = new byte[20];
    private double _challengeTimestamp;
    private byte _challengeSecretId;

    public TestClient(ILogger<TestClient> logger)
    {
        _logger = logger;
    }

    public async Task Connect(string ipAddress, int port)
    {
        try
        {
            _logger.LogInformation("Connecting to {IpAddress}:{Port}", ipAddress, port);

            // Create network driver
            _netDriver = new UIpNetDriver(System.Net.IPAddress.Parse(ipAddress), port, false);
            
            // Initialize connection
            await using (var world = new ProspectWorld())
            {
                _netDriver.InitConnect(world, new FUrl { Host = System.Net.IPAddress.Parse(ipAddress), Port = port });
            }

            _connection = _netDriver.ServerConnection;
            if (_connection?.Handler != null)
            {
                // Add our custom handler component
                var handler = _connection.Handler;
                var statelessComponent = handler.AddHandler<StatelessConnectHandlerComponent>();
                statelessComponent.SetActive(true);

                // Start the handshake
                handler.BeginHandshaking(() => {
                    _logger.LogInformation("Handshake callback invoked");
                    SendInitialJoin();
                });
            }

            // Start update loop
            while (!_isConnected)
            {
                if (_netDriver != null)
                {
                    _netDriver.TickDispatch(1.0f / 60.0f);
                    _netDriver.PostTickDispatch();
                    _netDriver.TickFlush(1.0f / 60.0f);
                    _netDriver.PostTickFlush();
                }

                if (_connection != null)
                {
                    _connection.Tick(1.0f / 60.0f);
                    _connection.Handler?.Tick(1.0f / 60.0f);
                    _logger.LogInformation("Connection state: {State}", _connection.State);
                }

                await Task.Delay(16); // ~60 FPS
            }

            _logger.LogInformation("Connected to server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to server");
            throw;
        }
    }

    private void SendInitialJoin()
    {
        try
        {
            if (_connection == null) return;

            // Wait for the control channel to be open before sending the initial join message
            var controlChannel = _connection.Channels[0];
            if (controlChannel == null || !controlChannel.OpenAcked)
            {
                _logger.LogWarning("Control channel not open yet. Retrying...");
                Task.Delay(100).ContinueWith(_ => SendInitialJoin());
                return;
            }

            var bunch = new FOutBunch(controlChannel, false);
            
            // Write message type
            bunch.WriteByte((byte)NetworkMessageType.Hello);
            bunch.WriteByte(0); // IsLittleEndian
            bunch.WriteInt32(0); // NetworkVersion

            _connection.SendRawBunch(bunch, false);
            _connection.FlushNet();

            _isConnected = true;
            _logger.LogInformation("Sent initial join message");
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
            _playerId = response.PlayerId;
            _logger.LogInformation("Successfully joined game with PlayerId {PlayerId}", _playerId);
        }
        else
        {
            _logger.LogError("Failed to join game: {Message}", response.Message);
        }
    }
} 