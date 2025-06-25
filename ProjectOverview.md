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
**Status**: Basic framework implemented, requires substantial expansion

**Current Implementation**:
- **Program.cs**: Main entry point with 60 FPS tick loop
- **ProspectWorld.cs**: Minimal world wrapper (7 lines)
- **Client.cs**: Incomplete client connection implementation with handshake logic

**Key Features**:
- Configurable server port, map selection, and game mode
- Basic UDP networking setup
- Minimal world initialization

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
1. **Actor System**: No actor spawning, replication, or management
2. **PlayerController**: Stub implementation - no actual player logic
3. **GameMode Integration**: Minimal game mode support
4. **Physics/Movement**: No physics simulation or movement handling
5. **World State**: No persistent world state or entity management
6. **Security**: Missing encryption, authentication validation
7. **Error Handling**: Incomplete error recovery and validation

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

## Next Steps Analysis
The server framework provides a solid foundation but requires significant development in actor management, game logic, and security before it can handle real game sessions.

Last Updated: 2024-12-27 