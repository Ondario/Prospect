# TODO: Full Local Multiplayer Implementation Roadmap

## 1. Game Server Orchestration & Session Management
- [ ] Implement API logic to launch a new Prospect.Server.Game process per multiplayer session
    - [ ] Pass session ID, map, and port to the game server
    - [ ] Track running sessions and their addresses/ports
    - [ ] Clean up/shutdown game servers when sessions end
- [ ] Ensure API provides correct session info (address/port) to all clients in a squad
- [ ] Add robust error handling for session creation, server launch failures, and client assignment

## 2. Matchmaking & Squad Flow
- [ ] Complete squad matchmaking logic (ready-up, map selection, trigger session creation)
- [ ] Notify all squad members of session info via SignalR or API response
- [ ] Ensure clients can only join their assigned session

## 3. Networking & Actor Replication
- [ ] Implement full actor replication in NetDriver, NetConnection, UChannel, and AActor
    - [ ] Server-to-client actor state sync (spawn, update, destroy)
    - [ ] Actor channel management for multiple clients
    - [ ] Reliable/ordered packet flow for actor data
    - [ ] Support for reconnections and late joins
- [ ] Finish channel management for all channel types (control, actor, voice)
    - [ ] Channel open/close, state sync, error handling

## 4. Client-Server Handshake & Authentication
- [ ] Implement robust handshake: login, join, session validation
- [ ] Integrate authentication/session join with API and game server
- [ ] Handle edge cases: duplicate logins, invalid sessions, disconnects

## 5. Voice & Additional Multiplayer Features
- [ ] Implement voice channel logic (if required)
- [ ] Add support for squad deployment, world events, and other multiplayer features

## 6. Test Coverage & Documentation
- [ ] Expand test coverage for all multiplayer flows (session creation, join, actor replication)
- [ ] Add integration tests for multi-client scenarios
- [ ] Document end-to-end multiplayer flow and C++/C# integration (Loader/Agent)
- [ ] Update ProjectOverview.md and TODO.md after every major change

---

**Execution Priorities:**
1. Game server orchestration and session management
2. Matchmaking and squad flow
3. Networking and actor replication
4. Handshake/authentication
5. Voice/extra features
6. Tests and documentation

**All changes must be explicit, measurable, and documented. No undocumented hacks or shortcuts.**
