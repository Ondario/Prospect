# Prospect.Server.Game - Dedicated Game Server

A reverse-engineered dedicated server implementation for **The Cycle: Frontier**, built from the ground up using C# and custom Unreal Engine networking protocols.

## 🎯 Project Status

**Current State**: Networking foundation complete, asset integration required  
**Branch**: `Feature/Server-Development`  
**Phase**: Early development - testing network compatibility

### What Works
- ✅ **UDP Networking**: Complete UIpNetDriver implementation with packet handling
- ✅ **Connection Handshaking**: Challenge/response authentication system
- ✅ **Multi-map Support**: Station, BrightSands, CrescentFalls, TharisIsland
- ✅ **60 FPS Server Tick**: Real-time game loop with configurable settings
- ✅ **Environment Configuration**: Port, map, and game mode via environment variables

### Critical Missing Components
- ❌ **Asset Loading**: No integration with extracted game PAK files
- ❌ **Player Spawning**: Stubbed PlayerController implementation
- ❌ **Actor Replication**: No multiplayer object synchronization
- ❌ **Steam Networking**: May require USteamNetDriver for client compatibility

## 🏗️ Architecture

### Core Components
```
Prospect.Server.Game/
├── Program.cs          # Main server entry point, 60 FPS tick loop
├── ProspectWorld.cs    # Game world wrapper (extends UWorld)
└── Client.cs           # Test client for network validation
```

### Network Stack
```
UIpNetDriver (UDP Sockets)
    ↓
UNetConnection (Per-client state)
    ↓
UChannel (Control/Actor/Voice channels)
    ↓
Packet Processing (FInBunch/FOutBunch)
```

### Dependencies
- **Prospect.Unreal**: Core networking framework (~70% complete)
- **Prospect.Server.Api**: Separate PlayFab-compatible backend (complete)

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 Runtime
- The Cycle: Frontier client (for connectivity testing)
- Visual Studio 2022 or JetBrains Rider

### Running the Server
```bash
# Set environment variables (optional)
set SERVER_PORT=7777
set DEFAULT_MAP=Station
set GAME_MODE=/Script/Prospect/YGameMode_Station

# Run the server
dotnet run --project src/Prospect.Server.Game/Prospect.Server.Game.csproj
```

### Testing Client Connectivity
```bash
# In The Cycle: Frontier console
open 127.0.0.1:7777
```

**Expected Result**: Server logs should show incoming UDP packets and connection attempt.  
**If it fails**: Client likely requires USteamNetDriver implementation.

## 📋 Development Roadmap

### Phase 1: Asset Pipeline (1-2 weeks)
1. **Extract Game Assets**: Use FModel/UnrealPak to extract PAK files
   - Maps: `/Game/Maps/MP/Station/Station_P.umap`
   - Actors: `/Game/Blueprints/` class definitions
   - Game Modes: Actual implementation data

2. **Asset Loading System**: Build C# infrastructure
   ```csharp
   public class ProspectAssetLoader
   {
       public MapData LoadMap(string mapName);
       public SpawnPoint[] GetSpawnPoints(string mapName);
       public ActorBlueprint LoadActorClass(string className);
   }
   ```

### Phase 2: Network Compatibility (3-5 days)
1. **Test IP Connections**: Validate UIpNetDriver with real client
2. **USteamNetDriver** (if required): Implement Steam networking layer
3. **Protocol Validation**: Ensure packet format compatibility

### Phase 3: Core Actor System (2-3 weeks)
1. **PlayerController Implementation**: Replace stubbed spawning logic
2. **Actor Replication**: Implement multiplayer synchronization
3. **Basic Physics**: Collision detection and movement validation

## 🔧 Configuration

### Environment Variables
| Variable | Default | Description |
|----------|---------|-------------|
| `SERVER_PORT` | `7777` | UDP port for game connections |
| `DEFAULT_MAP` | `Station` | Starting map (Station/BrightSands/CrescentFalls/TharisIsland) |
| `GAME_MODE` | `/Script/Prospect/YGameMode_Station` | Unreal game mode path |

### Hardcoded Map Paths (To be replaced with asset loading)
```csharp
"/Game/Maps/MP/Station/Station_P"
"/Game/Maps/MP/BrightSands/BrightSands_P"  
"/Game/Maps/MP/CrescentFalls/CrescentFalls_P"
"/Game/Maps/MP/TharisIsland/TharisIsland_P"
```

## 🧪 Testing

### Network Testing
```bash
# Run test client (included)
dotnet run --project src/Prospect.Server.Game -- --test-client

# Monitor server logs for:
# - UDP packet reception
# - Connection handshake completion
# - PlayerController spawn attempts
```

### Debugging
- **Wireshark**: Monitor UDP traffic on port 7777
- **Server Logs**: Detailed connection and packet information
- **Client Console**: The Cycle: Frontier developer console commands

## 🎮 Client Compatibility

### Supported Connection Methods
- **Direct IP**: `open IP:PORT` (testing required)
- **Steam Networking**: May require USteamNetDriver implementation

### Known Issues
- **SteamNetDriver Dependency**: Client may only support Steam networking
- **Asset Dependencies**: Server cannot spawn real game objects without extracted assets
- **Authentication**: Steam integration required for production use

## 🔨 Contributing

### Before Making Changes
1. Read `ProjectOverview.md` for current project state
2. Update `ProjectOverview.md` after significant changes
3. Follow the phase-based development approach

### Key Development Areas
- **Asset Extraction**: Tools and conversion processes
- **Network Drivers**: Steam networking implementation  
- **Actor System**: Game object spawning and replication
- **Physics Integration**: Movement and collision systems

## 📚 Additional Resources

- **ProjectOverview.md**: Complete project status and architecture
- **Prospect.Unreal/**: Core networking framework documentation
- **Motion Tasks**: Detailed development task tracking
- **UE4/UE5 Networking**: Official Unreal Engine networking documentation

---

**⚠️ Legal Notice**: This is a reverse engineering project for educational purposes. Ensure compliance with applicable laws and the game's terms of service. 