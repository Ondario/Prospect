# MAP01 Dedicated Server Implementation Plan

## Executive Summary

With complete game assets now available in `Exports/`, we can implement a functional dedicated server for MAP01 (Bright Sands). This plan outlines the development phases, technical architecture, and implementation strategy to create a working server that can host the LOOP game mode.

## Project Status

✅ **COMPLETED**:
- Networking foundation (Prospect.Unreal framework)
- Backend API services (Prospect.Server.Api)
- Game asset extraction (Exports/ folder complete)
- Map analysis and configuration understanding

🚧 **IN PROGRESS**:
- Asset loading system implementation

❌ **MISSING**:
- Map loading and world creation
- Player spawning and movement
- Game mode implementation
- Actor replication system

## Phase 1: Asset Loading Foundation (Week 1-2)

### 1.1 JSON Asset Parser Development

**Objective**: Build robust C# system to parse Unreal Engine JSON assets

**Key Components**:

```csharp
// New files to create:
src/Prospect.Unreal/Assets/
├── AssetParser.cs              # Core JSON parsing engine
├── AssetReference.cs           # Asset reference resolution
├── UnrealTypes/
│   ├── UObject.cs             # Base Unreal object representation
│   ├── ULevel.cs              # Level/map container
│   ├── UActor.cs              # Actor base class
│   └── UStaticMeshComponent.cs # Mesh components
└── DataTables/
    ├── DataTableParser.cs      # Data table loading system
    ├── MapInfo.cs             # Map configuration data
    └── PlayerTuning.cs        # Player movement parameters
```

**Implementation Tasks**:
1. **JSON Schema Analysis**: Parse Unreal's JSON format structure
2. **Asset Reference System**: Resolve `ObjectName` and `ObjectPath` references between files
3. **Large File Handling**: Efficient streaming parser for 27MB+ files
4. **Type System**: C# representation of Unreal Engine types

**Success Criteria**:
- Parse `MP_Map01_P.json` without errors
- Load `MapsInfos_DT.json` and extract MAP01 configuration
- Resolve asset references between different JSON files

### 1.2 Data Table Integration

**Objective**: Load game configuration from extracted data tables

**Priority Data Tables**:
1. `MapsInfos_DT.json` - Map metadata and loading parameters
2. `GameModeTuning_DT.json` - Game mode configuration (LOOP mode)
3. `PlayerTuning_DT.json` - Player movement and mechanics
4. `PRO_Weapons.json` - Weapon definitions (for basic combat)

**Implementation**:
```csharp
// New service in Prospect.Server.Game:
public class GameDataService
{
    public MapInfo GetMapInfo(string mapId);
    public GameModeTuning GetGameMode(string modeId);
    public PlayerTuning GetPlayerTuning();
    public void LoadAllDataTables();
}
```

## Phase 2: MAP01 World Loading (Week 2-3)

### 2.1 Persistent Level Loading

**Objective**: Load and instantiate MAP01's main persistent level

**Key Files**:
- `Exports/Prospect/Content/Maps/MP/MAP01/MP_Map01_P.json` (27MB main level)
- Map configuration from `MapsInfos_DT.json`

**Implementation Strategy**:
```csharp
// Enhanced ProspectWorld.cs:
public class ProspectWorld : UWorld
{
    public async Task LoadMapAsync(string mapName)
    {
        // 1. Load persistent level data
        var levelData = await AssetParser.LoadLevelAsync($"Maps/MP/{mapName}/{mapName}_P.json");
        
        // 2. Initialize world bounds and streaming system
        InitializeWorldStreaming(levelData.StreamingLevels);
        
        // 3. Load essential actors (PlayerStart points, etc.)
        SpawnEssentialActors(levelData.Actors);
    }
}
```

**Streaming System**:
- Implement grid-based loading (A-J rows, 0-9 columns)
- Load geometry (`GEO/` files) and gameplay (`GP/` files) on-demand
- Memory management for large world data

### 2.2 Player Spawn System

**Objective**: Implement intelligent player spawning using MAP01's distance-based scoring

**Configuration** (from MapsInfos_DT.json):
- Cluster radius: 1500.0 units
- Cluster cooldown: 60.0 seconds
- Distance-based scoring rules (5k-40k range)

**Implementation**:
```csharp
public class PlayerSpawnManager
{
    public Vector3 FindSpawnLocation(List<Player> existingPlayers)
    {
        // 1. Get all PlayerStart actors from level
        var spawnPoints = GetPlayerStartActors();
        
        // 2. Score each spawn point based on distance rules
        var scoredSpawns = ScoreSpawnPoints(spawnPoints, existingPlayers);
        
        // 3. Select highest scoring spawn point
        return SelectBestSpawn(scoredSpawns);
    }
}
```

## Phase 3: Player System Implementation (Week 3-4)

### 3.1 Player Controller Integration

**Objective**: Convert Unreal Blueprint player logic to C# implementation

**Source Assets**:
- `PRO_PlayerController.json` (269KB of blueprint logic)
- `PRO_PlayerCharacter.json` (203KB of character mechanics)
- `PlayerTuning_DT.json` (movement parameters)

**Key Systems to Implement**:
1. **Movement System**: Walking, sprinting, crouching, sliding, climbing
2. **Input Processing**: Keyboard/mouse input handling
3. **Camera System**: First-person view management
4. **Interaction System**: Object interaction and UI integration

**Implementation Strategy**:
```csharp
// Enhanced src/Prospect.Server.Game/PlayerController.cs:
public class ProspectPlayerController : UPlayerController
{
    public PlayerMovement MovementComponent { get; set; }
    public PlayerInteraction InteractionComponent { get; set; }
    
    public void ProcessPlayerInput(InputState input)
    {
        MovementComponent.UpdateMovement(input);
        InteractionComponent.CheckInteractions();
    }
}
```

### 3.2 Movement System

**Parameters** (from PlayerTuning_DT.json):
- Walk speed: 357.0 units/sec
- Sprint modifier: 1.91x
- Crouch speed: 150.0 units/sec
- Slide mechanics with velocity curves

**Physics Integration**:
- Collision detection with level geometry
- Movement validation server-side
- Smooth interpolation for network replication

## Phase 4: Game Mode Implementation (Week 4-5)

### 4.1 LOOP Game Mode

**Objective**: Implement the primary PvP game mode for MAP01

**Configuration** (from GameModeTuning_DT.json):
- Session management and player lifecycle
- Score sharing: disabled (0.0 multiplier)
- Heat map system: disabled for initial implementation
- Session timeout: 3.0 seconds

**Core Systems**:
1. **Match Lifecycle**: Pre-game, active match, post-game phases
2. **Player Management**: Join/leave handling, team assignment
3. **Objective System**: Basic quest and activity framework
4. **Session Management**: Connection timeout and cleanup

### 4.2 Basic World Interactions

**Essential Features**:
- Loot container interaction
- Environmental object interaction
- Basic item pickup system
- Player-to-player visibility and collision

## Phase 5: Network Protocol Validation (Week 5-6)

### 5.1 Client Connectivity Testing

**Testing Strategy**:
1. **Direct IP Connection**: Test `open IP:PORT` command with real client
2. **Packet Compatibility**: Validate network messages match client expectations
3. **Steam Integration**: If direct IP fails, implement USteamNetDriver

**Test Scenarios**:
- Client connection and handshaking
- Player spawning and movement replication
- Basic interaction with world objects
- Graceful disconnection handling

### 5.2 Performance Optimization

**Key Metrics**:
- Server tick rate: 60 FPS target
- Memory usage: < 2GB for MAP01
- Player capacity: 20+ players initially
- Network bandwidth: < 1MB/s per player

## Implementation Roadmap

### Week 1: Asset Foundation
- [ ] JSON asset parser implementation
- [ ] Data table loading system
- [ ] Basic asset reference resolution

### Week 2: World Loading  
- [ ] MAP01 persistent level loading
- [ ] Grid-based streaming system
- [ ] Player spawn point extraction

### Week 3: Player Systems
- [ ] Player controller implementation
- [ ] Movement system with physics
- [ ] Basic interaction framework

### Week 4: Game Mode
- [ ] LOOP mode implementation
- [ ] Session management
- [ ] Player lifecycle handling

### Week 5: Integration & Testing
- [ ] Client connectivity testing
- [ ] Network protocol validation
- [ ] Performance optimization

### Week 6: Polish & Deployment
- [ ] Bug fixes and stability
- [ ] Performance tuning
- [ ] Documentation and deployment

## Technical Architecture

### Asset Loading Pipeline
```
Exports/Prospect/Content/ → AssetParser → C# Objects → Game World
                     ↑
              DataTables/ → GameDataService → Configuration
```

### World Streaming System
```
MP_Map01_P.json (Persistent) → Core level data
        ↓
Grid System (A-J, 0-9) → On-demand loading
        ↓                        ↓
    GEO/ files              GP/ files
   (Geometry)             (Gameplay)
```

### Network Architecture
```
Client → UIpNetDriver → ProspectWorld → PlayerController → Game Logic
                           ↓
                    ActorReplication → Network Updates
```

## Risk Assessment

### High Risk
- **Client Compatibility**: Real client may not connect to custom server
- **Asset Complexity**: Unreal Blueprint → C# conversion challenges
- **Performance**: Large world data may cause memory/performance issues

### Medium Risk  
- **Network Protocol**: Custom networking may not match client expectations
- **Physics System**: Complex movement mechanics implementation
- **Streaming System**: Grid-based loading complexity

### Low Risk
- **Asset Parsing**: JSON format is well-documented and parseable
- **Basic Features**: Core networking framework already functional
- **Configuration**: Data tables provide clear parameter definitions

## Success Metrics

### Phase 1 Success
- [ ] Parse MAP01 level data successfully
- [ ] Load essential data tables
- [ ] Extract player spawn points

### Phase 2 Success  
- [ ] Load MAP01 world with basic geometry
- [ ] Players can spawn at appropriate locations
- [ ] Basic world streaming functional

### Phase 3 Success
- [ ] Players can connect and move in MAP01
- [ ] Movement feels accurate to original game
- [ ] Basic interactions working

### Final Success
- [ ] Real game client can connect via `open IP:PORT`
- [ ] 10+ players can play simultaneously on MAP01
- [ ] Basic LOOP game mode mechanics functional
- [ ] Server stable for extended play sessions

## Next Steps

1. **Immediate**: Begin Phase 1 implementation with JSON asset parser
2. **Week 1**: Set up development environment and start asset loading
3. **Week 2**: Focus on MAP01 level loading and player spawning
4. **Ongoing**: Regular testing with incremental client compatibility validation

This implementation plan provides a clear roadmap from our current networking foundation to a fully functional MAP01 dedicated server capable of hosting real players in the LOOP game mode. 