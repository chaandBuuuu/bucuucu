# Executive Summary - Codebase Architecture

## 🎮 What Is This Game?

A **multiplayer 2D top-down racing game** for 1-4 players with:
- Real-time networking via **Photon Fusion**
- Physics-based car movement (WASD + Shift drift)
- Collectible powerups (Shield, Gun, Speed Boost, Trap)
- Session discovery (find & join rooms)
- Post-race chat and voting

**Win Condition:** First player to cross the finish line wins

---

## 🏗️ Architecture at a Glance

### Network Layer
```
┌─ Photon Fusion ─┐
│  All players    │
│  synced in      │
│  real-time via  │
│  NetworkRunner  │
└─────────────────┘
```

### Game Layers
```
PRESENTATION LAYER (UI, Audio)
        ↓
GAMEPLAY LAYER (Car physics, Race logic, Powerups)
        ↓
NETWORK LAYER (Fusion, Input, Sync)
        ↓
STORAGE LAYER (Constants, Player data)
```

### Key Patterns Used

| Pattern | Where | Purpose |
|---------|-------|---------|
| Singleton | FusionNetworkManager, RaceManager | Single instance, global access |
| NetworkBehaviour | CarController, RaceManager, PowerupInventory | Networked objects |
| Event System | RaceManager, PowerupInventory | Decouple systems |
| Authority Model | HasInputAuthority, HasStateAuthority | Control who does what |
| Callback Registration | InputHandler, PlayerSpawner | Respond to Fusion events |

---

## 📊 47 Scripts Summary

### By Responsibility

**Core Gameplay** (5 scripts)
- Movement: `CarController.cs`
- Racing: `RaceManager.cs`, `FinishLineDetector.cs`, `RaceUI.cs`
- Setup: `RacingGameAutoSetup.cs`

**Powerup System** (4 scripts)
- Inventory: `PowerupInventory.cs`
- Pickups: `PowerupPickup.cs`
- Projectiles: `BulletProjectile.cs`
- Obstacles: `TrapObject.cs`

**Networking** (5 scripts)
- Hub: `FusionNetworkManager.cs`
- Input: `NetworkInputData.cs`, `InputHandler.cs`
- Discovery: `SessionDiscoveryManager.cs`
- Config: `MultiplayerConfig.cs`, `FusionCallbacksBase.cs`

**Lobby System** (7 scripts)
- UI: `GameLobbyUI.cs`, `RoomListUI.cs`, `LobbyCharacterSelectUI.cs`
- Players: `LobbySpawner.cs`, `LobbyPlayerController.cs`, `MultiplayerCharacter.cs`
- Input: `PlayerNameInput.cs`

**Racing Spawning** (3 scripts)
- Spawn: `PlayerSpawner.cs`, `RacingCarSpawner.cs`
- Control: `GameStartController.cs`

**Camera & Display** (3 scripts)
- Camera: `CameraFollow.cs`, `MultiCameraManager.cs`
- Mini-map: `MiniMapManager.cs`

**Chat & End Game** (3 scripts)
- Chat: `GameEndChatManager.cs`, `GameEndChatMessageUI.cs`, `ChatNetworkHandler.cs`
- Voting: `GameEndVoteHandler.cs`

**Support Systems** (4 scripts)
- Input Locking: `GameInputLocker.cs`
- Car Storage: `CarPrefabList.cs`
- Player Data: `PlayerData.cs`, `PlayerList.cs`

**Player Management** (2 scripts)
- Character: `MultiplayerCharacter.cs`
- Game: `GameManager.cs`

**Audio** (1 script)
- Audio: `AudioManager.cs`

**Constants** (1 script)
- Configuration: `RacingConstants.cs`

**Deprecated** (4 scripts)
- Old system: `Move.cs`, `InventorySystem.cs`, `InventoryUI.cs`, `ItemPickup.cs`

---

## 🔄 Data Flows

### Simplest Flow: Player Movement

```
User presses W
    ↓
InputHandler.Update() sees key
    ↓
OnInput() callback sends to server
    ↓
CarController.GetInput() receives
    ↓
HandleMovement() updates position
    ↓
NetworkTransform syncs to others
    ↓
Remote players see car move
```

### Complex Flow: Powerup Gun

```
Player has gun powerup
    ↓
Player presses Q
    ↓
PowerupInventory.UseCurrent()
    ↓
PowerupInventory.FireGun()
    ↓
Runner.Spawn(bullet) on server
    ↓
Bullet moves toward target
    ↓
Bullet hits car
    ↓
RPC_ApplySlow() broadcast to all
    ↓
All clients slow that car 50% for 3s
```

### Race Flow: Start to Finish

```
Scene 2 loads
    ↓
PlayerSpawner spawns 4 cars
    ↓
RaceManager.StartCountdown()
    ↓
3... 2... 1...
    ↓
RaceStarted = true (all see via OnChangedRender)
    ↓
Players race (10-60 seconds typically)
    ↓
First player crosses finish line
    ↓
FinishCountdown = 10s for others
    ↓
RaceFinished = true
    ↓
Show final rankings
    ↓
Return to lobby or menu
```

---

## 🎯 Authority & Responsibility

### Server/Host Owns

✅ **Race state** - When does race start/end?  
✅ **Winner calculation** - Who finished when?  
✅ **Powerup spawns** - Bullets, traps appear on server  
✅ **Collision validation** - Did car really cross finish?  
✅ **Effect broadcasts** - Send slow/shield effects via RPC  

### Player Owns (Local Authority)

✅ **Movement input** - Read WASD keyboard  
✅ **Local velocity** - Apply acceleration/friction locally  
✅ **Powerup use** - Can use own powerup  
✅ **Position sync** - Send position to network  

### What Can't They Do

❌ Player can't move another player's car  
❌ Player can't declare themselves winner  
❌ Player can't modify other players' powerups  
❌ Client can't change race state  

---

## 📁 Directory Organization

```
Assets/Codes/
├── Gameplay/           ← Car physics, race logic
├── Multiplayer/        ← Networking, lobby
├── Audio/              ← Sound system
└── Root (Deprecated)   ← Old inventory system
```

**Most Important Files to Know:**
1. `RacingConstants.cs` - All game balance in ONE place
2. `FusionNetworkManager.cs` - How networking works
3. `CarController.cs` - How cars move
4. `RaceManager.cs` - How races are managed
5. `InputHandler.cs` - How input flows to network

---

## 🎮 Game Progression

### User Perspective

```
1. Launch Game
   ↓
2. Enter Lobby
   ├─ Type player name
   ├─ Create room or join existing
   └─ See other players
   ↓
3. Car Selection
   ├─ Choose from 4 cars
   └─ Wait for host to start
   ↓
4. Racing
   ├─ 3-2-1 countdown
   ├─ Drive to finish line
   ├─ Collect powerups
   ├─ First to cross wins
   └─ Watch others finish (10s)
   ↓
5. Results
   ├─ See final rankings
   ├─ Chat with players
   └─ Return to lobby
```

### Behind the Scenes

```
SCENE 1: Lobby
├─ FusionNetworkManager.Runner active
├─ InputHandler registered
├─ SessionDiscoveryManager finding rooms
├─ LobbySpawner creating avatars
└─ GameLobbyUI showing menu

↓ Host clicks "Start"

SCENE 2: Racing
├─ FusionNetworkManager.Runner still active
├─ InputHandler re-registered (OnSceneLoaded fix)
├─ PlayerSpawner creating cars
├─ RaceManager counting down
├─ CarController moving cars
└─ RaceUI showing speed/timer
```

---

## 🔧 Configuration Points

### Game Balance (Edit RacingConstants.cs)

```
Car Speed: CAR_MAX_SPEED = 15                    // Increase for faster
Acceleration: CAR_ACCELERATION = 8               // Increase for snappier
Turning: CAR_ROTATION_SPEED = 180               // Increase for responsive
Drifting: CAR_DRIFT_ROTATION_MULTIPLIER = 1.5  // Increase for better drift

Shield Duration: SHIELD_DURATION = 3s           // Longer = more protection
Gun Slow: GUN_SLOW_AMOUNT = 0.5 (50%)          // Increase for more slow
Speed Boost: SPEED_BOOST_MULTIPLIER = 1.5 (50%) // Increase for stronger
```

### Network Settings (Edit FusionNetworkManager.cs)

```
Max Players: maxPlayers = 4                      // Up to 8
Lobby Scene: lobbySceneIndex = 1                // Keep consistent
Racing Scene: racingSceneIndex = 2              // Keep consistent
```

---

## 🐛 Common Problems & Solutions

| Problem | Cause | Fix |
|---------|-------|-----|
| Car won't move after joining | Input callbacks lost after scene load | Already fixed: `InputHandler.OnSceneLoaded()` |
| Powerup doesn't appear | Used `Instantiate()` instead of networked | Use `Runner.Spawn()` in code |
| Lag/teleporting cars | Multiple velocity updates conflict | Check authority: only HasInputAuthority moves locally |
| Race doesn't start on clients | Event not firing on clients | Use `OnChangedRender` to trigger on all clients |
| Can't see other players | NetworkTransform not configured | Make sure component attached to car |
| Shooting own car | No target check in bullet logic | Already fixed: null target = hit any car |

---

## ✅ What Works (Completed)

- ✅ Player movement (WASD + momentum)
- ✅ Drift mechanic (Shift key)
- ✅ Multiplayer sync (Photon Fusion)
- ✅ Powerup system (Shield, Gun, Speed, Trap)
- ✅ Race timing and winner
- ✅ Lobby with car selection
- ✅ Session discovery (find rooms)
- ✅ Player name display
- ✅ Post-race chat
- ✅ Input persistence across scenes

## ⏳ What's In Progress

- ⏳ Game End UI (Results screen - basic framework exists)
- ⏳ Audio integration (AudioManager exists but not wired)

## ❌ What's Not Implemented

- ❌ Main Menu (Scene 0)
- ❌ Player profiles / progression
- ❌ Matchmaking system
- ❌ Replays
- ❌ Custom maps

---

## 🚀 How to Add a Feature

### To Add a New Powerup

1. Add constant to `RacingConstants.cs`
2. Add case to `PowerupInventory.UseCurrent()`
3. Implement activation method
4. Place pickup in scene

### To Modify Car Speed

1. Change `RacingConstants.CAR_ACCELERATION` or `CAR_MAX_SPEED`
2. Restart game
3. All players automatically get new speed

### To Add a UI Element

1. Add to Canvas in scene
2. Subscribe to RaceManager events
3. Update display in event handler

### To Debug Multiplayer Issue

1. Check `FusionNetworkManager.Instance != null`
2. Check `InputHandler` is registered
3. Check console for Fusion logs
4. Verify authority (HasInputAuthority/HasStateAuthority)
5. Ensure prefab is registered with Fusion

---

## 📚 Key Classes at a Glance

### CarController
```
PURPOSE: Control individual car
KEY METHODS: HandleMovement(), ApplySpeedBoost(), RPC_ApplySlow()
PROPERTIES: Position, Velocity, IsDrifting, IsFinished
AUTHORITY: Input = Player, State = Server
```

### RaceManager
```
PURPOSE: Manage race state and timing
KEY METHODS: RegisterFinishCrossing(), CalculateFinalRankings()
PROPERTIES: RaceStarted, RaceFinished, RaceTimer
AUTHORITY: State only (Server)
EVENTS: OnRaceStart, OnPlayerFinish, OnFinalRankings, OnRaceEnd
```

### FusionNetworkManager
```
PURPOSE: Network hub and player data
KEY METHODS: CreateSession(), JoinSession(), SetPlayerName()
PROPERTIES: Runner (NetworkRunner instance)
PATTERN: Singleton
SCOPE: DontDestroyOnLoad
```

### InputHandler
```
PURPOSE: Collect keyboard input and send to network
KEY METHODS: OnInput() (Fusion callback)
INPUT: WASD, Shift, Q, E, R, F
OUTPUT: NetworkInputData sent to CarController
FIX: Re-registers on scene load
```

### PowerupInventory
```
PURPOSE: Manage player's powerup
KEY METHODS: AddPowerup(), UseCurrent()
TYPES: Shield, Gun, SpeedBoost, Trap
EVENTS: OnPowerupAcquired, OnPowerupUsed, OnPowerupEmpty
```

### SessionDiscoveryManager
```
PURPOSE: Find available rooms
KEY METHODS: StartDiscovery(), StopDiscovery()
USES: Separate NetworkRunner for discovery
SCOPE: DontDestroyOnLoad
EVENTS: OnSessionListUpdatedEvent
```

---

## 🎯 Recommended First Changes

If you're new to this codebase and want to make changes:

1. **Tweak game balance** (easy)
   - Edit RacingConstants.cs values
   - Test in play mode

2. **Add UI text** (medium)
   - Modify RaceUI.cs
   - Add text display elements
   - Subscribe to RaceManager events

3. **Add sound effects** (medium)
   - Use AudioManager.PlaySound()
   - Wire into powerup usage

4. **Add a new powerup** (medium)
   - Add constant, add case, implement method

5. **Fix a bug** (medium)
   - Reproduce issue
   - Check authority and networking
   - Add logs to trace flow

6. **Add main menu** (hard)
   - Create Scene 0
   - Add transition logic
   - Wire into game flow

---

## 📊 Quick Stats

| Metric | Value |
|--------|-------|
| Total Scripts | 47 |
| Active Scripts | 43 |
| Deprecated Scripts | 4 |
| Scenes | 2 (+ 1 planned) |
| Max Players | 4 |
| Networking | Photon Fusion |
| Game Type | Multiplayer Racing |
| Lines of Code | ~10,000+ |
| State Management | NetworkBehaviour + Networked properties |

---

## 🔗 File Relationships Simplified

```
FusionNetworkManager (hub)
    ├─ Provides Runner to: InputHandler, PlayerSpawner, LobbySpawner
    ├─ Stores: Player names, car choices, sessions
    └─ Events: OnConnected, OnDisconnected, OnSessionListUpdated

RaceManager (game logic)
    ├─ Receives: RegisterFinishCrossing() from FinishLineDetector
    ├─ Queried by: CarController, RaceUI
    └─ Fires: OnRaceStart, OnPlayerFinish, OnFinalRankings, OnRaceEnd

CarController (player car)
    ├─ Uses: NetworkInputData (input)
    ├─ Contains: PowerupInventory (child)
    ├─ Syncs: Position/velocity via NetworkTransform
    └─ Calls: RaceManager.RegisterFinishCrossing()

InputHandler (keyboard)
    ├─ Registers: With NetworkRunner callback
    ├─ Reads: Keyboard (WASD, Shift, Q, etc.)
    ├─ Sends: NetworkInputData via OnInput()
    └─ Listens: OnSceneLoaded to re-register

SessionDiscoveryManager (room finder)
    ├─ Uses: Separate discovery NetworkRunner
    ├─ Provides: List of available rooms
    └─ Listens: Photon Lobby for room updates

GameLobbyUI (menu)
    ├─ Uses: FusionNetworkManager, SessionDiscoveryManager
    ├─ Shows: Room list, name input
    └─ Calls: CreateSession() or JoinSession()
```

---

## 🎓 Learning Path

If you want to understand this codebase deeply:

1. **Start with constants** (5 min)
   - Read `RacingConstants.cs`
   - Understand game balance

2. **Read the simple parts** (15 min)
   - `NetworkInputData.cs` - What input is sent
   - `PowerupType` enum - Available powerups

3. **Understand movement** (20 min)
   - `CarController.cs` - How cars move
   - `InputHandler.cs` - How input gets to cars

4. **Understand networking** (30 min)
   - `FusionNetworkManager.cs` - How connection works
   - `NetworkBehaviour` pattern - How things sync

5. **Understand game flow** (30 min)
   - `RaceManager.cs` - Race timing and winning
   - Scene flow: Lobby → Racing

6. **Understand powerups** (20 min)
   - `PowerupInventory.cs` - How powerups work
   - `BulletProjectile.cs` - How networking RPCs work

7. **Understand lobbies** (25 min)
   - `GameLobbyUI.cs` - Lobby interface
   - `SessionDiscoveryManager.cs` - Room finding

**Total: ~2 hours** to understand the entire system

---

## 🎯 Next Steps for Developers

### If Fixing Bugs
1. Check console for errors
2. Add Debug.Log() statements
3. Verify authority (HasInputAuthority, HasStateAuthority)
4. Check Networking section of this guide

### If Adding Features
1. Consult RacingConstants.cs for values
2. Check if feature needs to network (use [Networked])
3. Subscribe to RaceManager events if UI-related
4. Test in multiplayer if networking involved

### If Optimizing
1. Review CarController.FixedUpdateNetwork() - runs every frame
2. Check for unnecessary instantiations (use Runner.Spawn())
3. Profile in-game with Profiler
4. Consider object pooling for bullets/traps

---

**This guide provides everything needed to understand and work with the codebase effectively.**  
**For specific questions, refer to the detailed guides in other documentation files.**
