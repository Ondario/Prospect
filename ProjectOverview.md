# Prospect Game Server - Project Overview

## Current State
This project is a reverse engineering effort to create a dedicated server for the Unreal Engine-based game "Prospect". The codebase contains a substantial networking framework reimplementation and several related components.

## Directory Structure

```
Prospect/
├── src/
│   ├── Prospect.Server.Game/          # Main game server executable
│   ├── Prospect.Server.Api/           # Web API server (PlayFab-like backend)
│   ├── Prospect.Unreal/               # Core Unreal Engine networking reimplementation
│   ├── Prospect.Unreal.Generator/     # Source generators for networking messages
│   ├── Prospect.Unreal.Tests/         # Unit tests for Unreal framework
│   ├── Prospect.Steam/                # Steam integration utilities
│   └── Prospect.sln                   # Solution file
├── LoaderPack/                        # Client-side mod loader and scripts
├── modfiles/                          # Game UI modification files
└── utils/                             # Utility scripts (SSL generation)
```

## Core Components

### 1. Prospect.Server.Game (Game Server)
**Status**: Networking foundation complete, requires asset system and actor implementation

**Current Implementation**:
- **Program.cs**: Main entry point with 60 FPS tick loop and environment-based configuration
- **ProspectWorld.cs**: Minimal world wrapper extending UWorld (7 lines)
- **Client.cs**: Test client implementation with UIpNetDriver connectivity

**Key Features**:
- Configurable server (port 7777, multiple map support: Station, BrightSands, CrescentFalls, TharisIsland)
- UIpNetDriver-based UDP networking with full packet handling pipeline
- Connection handshaking, challenge/response authentication
- Environment variable configuration (SERVER_PORT, DEFAULT_MAP, GAME_MODE)
- Real-time packet processing and network tick management

**Critical Missing Components**:
- **Asset Loading System**: No integration with extracted game assets (.uasset files)
- **Map Data Loading**: Hardcoded map paths need actual level geometry and spawn points
- **Actor Spawning**: PlayerController creation stubbed, no actual game object instantiation
- **Physics Integration**: No collision detection or movement validation
- **Steam Networking**: Client compatibility requires USteamNetDriver or IP connection testing

**Network Compatibility Issues**:
- Current UIpNetDriver may not support Steam-based clients
- Requires testing with `open IP:PORT` commands for direct client connection
- SteamNetDriver implementation needed for full client compatibility

### 2. Prospect.Unreal (Networking Framework)
**Status**: Comprehensive networking layer implementation - ~70% complete

**Core Classes**:
- **UWorld**: Game world management with actor initialization and network notification handling
- **UIpNetDriver**: UDP-based network driver with connection management
- **UNetConnection**: Connection state management and packet processing
- **UChannel**: Channel-based communication (Control, Voice, Actor channels)

**Network Protocol Support**:
- Control message handling (Hello, Welcome, Challenge, Login, Join, etc.)
- Reliable/unreliable packet transmission
- Channel-based communication
- Packet acknowledgment and retry logic
- Stateless connection handshaking

### 3. Prospect.Server.Api (Backend Services)
**Status**: Complete PlayFab-compatible backend implementation

**Features**:
- Authentication (Steam, Entity-based)
- CloudScript function execution
- Database integration (MongoDB)
- Squad/friend management
- SignalR real-time communication
- Store/inventory management

## Multiplayer Architecture

### Connection Flow
1. Client connects via UDP to game server
2. Stateless handshake challenge/response
3. Hello message exchange with version verification
4. Login authentication (currently stubbed)
5. Welcome message with level/gamemode information
6. Join message to spawn PlayerController
7. Actor replication begins

### Current Limitations

**Critical Missing Components**:
1. **Asset Loading System**: No extraction/loading of game PAK files (maps, actors, blueprints)
2. **Actor System**: No actor spawning, replication, or management beyond stubs
3. **PlayerController**: Stub implementation - no actual player logic or spawning
4. **GameMode Integration**: Hardcoded paths with no actual game mode implementation
5. **Physics/Movement**: No physics simulation or movement handling
6. **World State**: No persistent world state or entity management
7. **Steam Network Driver**: Client compatibility requires USteamNetDriver implementation
8. **Security**: Missing encryption, authentication validation
9. **Error Handling**: Incomplete error recovery and validation

**Networking Gaps**:
- Actor channel implementation incomplete
- No actor replication
- Missing reliable message retry logic
- Incomplete packet validation
- No bandwidth management

## Technical Debt

1. **Hardcoded Values**: Map names, game modes, network versions
2. **TODO Comments**: 50+ unimplemented features marked
3. **Exception Handling**: Many `NotImplementedException` calls
4. **Memory Management**: Potential memory leaks in connection handling
5. **Testing**: Minimal test coverage for networking components

## Dependencies

**External Packages**:
- Serilog (logging)
- MongoDB.Driver (database)
- Microsoft.AspNetCore (web API)
- SignalR (real-time communication)

**Internal Dependencies**:
- Prospect.Unreal → Core networking primitives
- Prospect.Server.Game → Prospect.Unreal
- Prospect.Server.Api → Independent backend services

## Recent Activity
- Branch: MultiplayerFixes01
- Focus: Basic server framework and networking foundation
- Status: Clean working tree, no pending commits

## Immediate Development Priorities

### Phase 1: Asset Pipeline (1-2 weeks)
1. **PAK File Extraction**: Extract maps, actors, and blueprints from The Cycle: Frontier
2. **Asset Loading System**: Build C# asset loader for converted game data
3. **Map Data Integration**: Replace hardcoded paths with real spawn points and level geometry

### Phase 2: Network Driver Testing (3-5 days)
1. **Client Connectivity Testing**: Test `open IP:PORT` command with live client
2. **USteamNetDriver Implementation**: If IP connections fail, implement Steam networking layer
3. **Network Protocol Validation**: Ensure packet compatibility with real client

### Phase 3: Core Actor System (2-3 weeks)
1. **PlayerController Implementation**: Real player spawning and basic movement
2. **Actor Replication**: Network synchronization of game objects
3. **Basic Physics Integration**: Collision detection and movement validation

## Next Steps Analysis
The networking foundation is solid and ready for testing. **Primary blocker** is asset integration - server cannot spawn real game objects without extracted game data. Network driver compatibility testing is critical before major development investment.

Last Updated: 2024-12-27 