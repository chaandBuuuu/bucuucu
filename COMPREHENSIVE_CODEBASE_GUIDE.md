# Comprehensive Codebase Understanding Guide
## Multiplayer Racing Game - Architecture & Systems Overview

---

## 📋 Table of Contents
1. [Overview](#overview)
2. [File Structure](#file-structure)
3. [Gameplay Systems](#gameplay-systems)
4. [Multiplayer & Networking](#multiplayer--networking)
5. [Key Systems](#key-systems)
6. [Data Flow](#data-flow)
7. [Game Flow](#game-flow)
8. [Constants & Configuration](#constants--configuration)
9. [Architecture Diagram](#architecture-diagram)
10. [Important Notes](#important-notes)

---

## 🎮 Overview

**Game Type:** Multiplayer Top-Down Racing Game  
**Platform:** Unity 2D with Photon Fusion Networking  
**Max Players:** 4  
**Win Condition:** First player to cross finish line  
**Key Features:** Drifting, Powerups, Online Multiplayer, Session Discovery

---

## 📁 File Structure

### Complete Script List (47 files)

```
Assets/Codes/
│
├── Gameplay/  (17 files)
│   ├── Core Racing
│   │   ├── CarController.cs               - Vehicle physics, input, movement
│   │   ├── RaceManager.cs                 - Race state, timing, winner logic
│   │   ├── RaceUI.cs                      - HUD display (speed, lap, timer)
│   │   ├── FinishLineDetector.cs          - Lap/race detection
│   │   └── CarPrefabList.cs               - Car selection storage
│   │
│   ├── Powerup System
│   │   ├── PowerupInventory.cs            - Player powerup management
│   │   ├── PowerupPickup.cs               - Ground pickups on track
│   │   ├── BulletProjectile.cs            - Gun projectile (networked)
│   │   └── TrapObject.cs                  - Trap mechanics
│   │
│   ├── UI & Support
│   │   ├── MiniMapManager.cs              - Mini-map rendering
│   │   ├── GameEndChatManager.cs          - Post-race chat
│   │   ├── GameEndChatMessageUI.cs        - Chat message display
│   │   ├── GameEndVoteHandler.cs          - Vote handling
│   │   ├── GameInputLocker.cs             - Input disable/enable
│   │   ├── RacingGameAutoSetup.cs         - Auto-scene setup
│   │   ├── RacingConstants.cs             - ALL constants (source of truth)
│   │   └── RACING_SETUP.md                - Documentation
│   │
│
├── Multiplayer/  (19 files)
│   ├── Network Core
│   │   ├── FusionNetworkManager.cs        - Main network manager (Singleton)
│   │   ├── FusionCallbacksBase.cs         - Fusion callback base
│   │   ├── NetworkInputData.cs            - Input struct (networked)
│   │   ├── MultiplayerConfig.cs           - Network constants
│   │   └── InputHandler.cs                - Reads input → fires OnInput callback
│   │
│   ├── Lobby System
│   │   ├── GameLobbyUI.cs                 - Lobby menu UI
│   │   ├── GameStartController.cs         - Host "Start" button
│   │   ├── RoomListUI.cs                  - Room listing display
│   │   ├── PlayerNameInput.cs             - Player name entry
│   │   ├── LobbyCharacterSelectUI.cs      - Car selection UI
│   │   ├── LobbySpawner.cs                - Spawns lobby avatars
│   │   └── LobbyPlayerController.cs       - Lobby character display
│   │
│   ├── Session Discovery
│   │   ├── SessionDiscoveryManager.cs     - Room finder (Photon Lobby)
│   │   └── SessionDiscoveryAutoSetup.cs   - Auto-setup for discovery
│   │
│   ├── Racing - Spawning & Camera
│   │   ├── PlayerSpawner.cs               - Spawns cars in racing scene
│   │   ├── RacingCarSpawner.cs            - Alternative car spawner
│   │   ├── MultiplayerCharacter.cs        - Lobby player character (old system)
│   │   ├── CameraFollow.cs                - Camera follows local player
│   │   └── MultiCameraManager.cs          - Multi-player camera management
│   │
│   ├── Player Data
│   │   ├── PlayerData.cs                  - Player info (name, score, index)
│   │   └── PlayerList.cs                  - Tracks all players
│   │
│   ├── Chat System
│   │   ├── ChatNetworkHandler.cs          - Network chat handler
│   │   ├── ChatMessageUI.cs               - Chat message display
│   │   ├── GameChatManager.cs             - Game chat manager
│   │   └── ChatCameraAutoSetup.cs         - Chat camera setup
│   │
│   └── Game Management
│       └── GameManager.cs                 - General game manager
│
├── Audio/  (1 file)
│   └── AudioManager.cs                    - Centralized audio management
│
└── Root Files (Deprecated - Old System)
    ├── Move.cs                            - OLD movement control
    ├── InventorySystem.cs                 - OLD inventory
    ├── InventoryUI.cs                     - OLD inventory UI
    └── ItemPickup.cs                      - OLD item pickup
```

---

## 🏎️ GAMEPLAY SYSTEMS

### 1. **CarController.cs** - Vehicle Physics & Control

**Purpose:** Handles all car movement, physics, and input processing for a single vehicle

**Key Properties (Networked):**
```csharp
[Networked] bool IsDrifting           // Shift key active?
[Networked] int LapsCompleted         // Current lap count
[Networked] bool IsFinished           // Race finished for this car?
[Networked] private float SpeedMultiplier // From powerups (1.0 or 1.5x)
```

**Key Methods:**
- `FixedUpdateNetwork()` - Main physics loop per frame
- `HandleMovement(input)` - Process WASD → acceleration, friction, drift
- `HandlePowerup(input)` - Process Q key (use powerup)
- `PickupPowerup(type)` - Receive powerup pickup
- `ApplySpeedBoost(duration)` - Speed multiplier
- `RPC_ApplySlow(amount, duration)` - Networked slow effect
- `CreatePlayerNameplate()` - Display player name above car

**Movement Physics:**
- **WASD Input:** Move in X/Y direction, applies acceleration
- **Friction:** 0.95x per frame (0.92x when drifting)
- **Max Speed:** 15 units/s
- **Rotation:** 180°/s base, 1.5× when drifting
- **Drift:** Shift key enables → visual effect + rotation boost

**Authority:**
- **HasInputAuthority:** Can move the car locally, applies rigidbody velocity
- **HasStateAuthority:** Server, processes collision and physics
- **Remote Players:** Kinematic rigidbody, controlled by NetworkTransform

---

### 2. **RaceManager.cs** - Race State & Logic

**Purpose:** Manages race lifecycle, timing, countdown, winner calculation

**Key Properties (Networked):**
```csharp
[Networked] bool RaceStarted           // Race begun?
[Networked] bool RaceFinished          // Race ended?
[Networked] float RaceTimer            // Total race elapsed time
[Networked] int CountdownCounter       // Pre-race: 3, 2, 1, 0
[Networked] float FinishCountdown      // Post-finish: 10s countdown for other players
```

**Key Methods:**
- `FixedUpdateNetwork()` - Updates race state each frame
- `RegisterFinishCrossing(car)` - Called when car crosses finish line
- `CalculateFinalRankings()` - Calculates 1st, 2nd, 3rd, 4th place
- `StartCountdown()` - Begins 3-2-1 pre-race countdown
- `RPC_RestartRace()` - Restart race RPC
- `RPC_BackToLobby()` - Return to lobby RPC

**Events (Fire on all clients via OnChangedRender):**
- `OnRaceStart` - Race has started
- `OnPlayerFinish` - Individual player crossed finish
- `OnFinalRankings` - Final standings calculated
- `OnRaceEnd` - Race officially ended

**Ranking Logic:**
1. Players ranked by finish time (first to cross = position 1)
2. If not finished by countdown end, ranked by distance from finish line
3. Sends `OnFinalRankings` event with list of `(CarController, Position, FinishTime, Distance)`

**State Flow:**
```
Idle → CountdownCounter: 3,2,1 → RaceStarted: true → 
       (players race) → First player crosses → FinishCountdown: 10s → 
       Countdown ends → RaceFinished: true → Events fire
```

---

### 3. **FinishLineDetector.cs** - Lap Detection

**Purpose:** Detects when cars cross the finish line

**Key Methods:**
- `OnTriggerEnter2D()` - Collision detection
- `SetRaceManager(rm)` - Set reference to RaceManager
- `RegisterFinishCrossing(car)` - Call RaceManager when car crosses

**Protection Mechanisms:**
- **Cooldown per car:** 2 seconds between laps (prevents double-counting)
- **Server-only logic:** Only host processes crossing detection
- **Caching:** Caches NetworkRunner reference for performance

---

### 4. **RaceUI.cs** - HUD Display

**Purpose:** Shows real-time race information on screen

**Displays:**
- Current lap / total laps
- Current speed (units/s)
- Race timer
- Countdown (3-2-1 before start)
- Powerup status

**Updates from:**
- `CarController.FixedUpdateNetwork()` → Speed, lap count
- `RaceManager.OnRaceStart` → Show countdown
- `RaceManager.OnFinalRankings` → Final results

---

### 5. **PowerupSystem** - Collectible Items

#### **PowerupInventory.cs** - Player Powerup Management

**Enum Types:**
```csharp
public enum PowerupType
{
    Shield,     // Absorb 1 hit, 3s duration
    Gun,        // Shoot nearby car, slow 50% for 3s
    SpeedBoost, // 1.5× speed for 5s
    Trap        // Place slowing trap, 60% slow for 3s
}
```

**Key Methods:**
- `AddPowerup(type)` - Receive powerup
- `UseCurrent()` - Activate powerup
- `ActivateShield()` - Enable shield (green color)
- `FireGun()` - Find nearest car ahead, spawn bullet projectile
- `ActivateSpeedBoost()` - Apply 1.5x speed multiplier
- `PlaceTrap()` - Spawn trap at current location

**Events:**
- `OnPowerupAcquired(type)` - Received powerup
- `OnPowerupUsed(type)` - Used powerup
- `OnPowerupEmpty` - Inventory now empty

**Shield Mechanics:**
- Green color tint while active
- Resets to original color when expired
- One-hit protection (automatic on pickup)

**Gun Mechanics:**
- Finds nearest car ahead (in front of player)
- Spawns `BulletProjectile` via `Runner.Spawn()` (networked)
- Bullet homes toward target, slows on hit

**Speed Boost:**
- Sets `CarController.SpeedMultiplier = 1.5`
- Decays over 5 seconds

**Trap:**
- Spawned at car position
- Slows cars that touch it (60% for 3s)
- Lasts 15 seconds total

---

#### **PowerupPickup.cs** - Ground Pickups

**Purpose:** Collectible items scattered on track

**On Touch:**
- Calls `carController.PickupPowerup(type)`
- Hides pickup sprite
- Respawns after 10 seconds

---

#### **BulletProjectile.cs** - Gun Projectile

**Properties:**
- Speed: 20 units/s
- Lifetime: 5 seconds (auto-despawn)
- Target tracking: Homes toward target if set, otherwise flies straight

**Mechanics:**
- Spawned networked (`Runner.Spawn()`)
- Server-authority physics
- On hit: Calls `RPC_ApplySlow(0.5, 3.0)` on target

**Hit Logic:**
- If has target: Hit only that car
- If no target: Hit any car touched
- Only hit once (sets `_hasHit` flag)

---

#### **TrapObject.cs** - Trap Mechanics

**Purpose:** Placed on track, slows players

**On Touch:**
- Slows hit car 60% for 3 seconds
- Uses `RPC_ApplySlow()` for networking
- Self-destructs after 15 seconds

---

### 6. **RacingConstants.cs** - Global Configuration

```csharp
// Car Physics
CAR_ACCELERATION = 8f
CAR_MAX_SPEED = 15f
CAR_FRICTION = 0.95f
CAR_DRIFT_FRICTION = 0.92f
CAR_ROTATION_SPEED = 180f
CAR_DRIFT_ROTATION_MULTIPLIER = 1.5f

// Race
RACE_LAPS_TO_WIN = 1              // First to cross = win
MAX_PLAYERS = 4

// Powerups
SHIELD_DURATION = 3f
TRAP_SLOW_AMOUNT = 0.6f           // 60% reduction
TRAP_SLOW_DURATION = 3f
GUN_SLOW_AMOUNT = 0.5f            // 50% reduction
GUN_SLOW_DURATION = 3f
SPEED_BOOST_DURATION = 5f
SPEED_BOOST_MULTIPLIER = 1.5f
POWERUP_RESPAWN_TIME = 10f

// Bullets
BULLET_SPEED = 20f
BULLET_LIFETIME = 5f
```

---

## 🌐 MULTIPLAYER & NETWORKING

### 1. **FusionNetworkManager.cs** - Network Hub (Singleton)

**Purpose:** Central manager for all networking operations

**Singleton Pattern:**
```csharp
public static FusionNetworkManager Instance { get; private set; }
```

**Key Responsibilities:**

**Player Name Management:**
```csharp
SetPlayerName(string name)              // Associate name with local player
GetPlayerName(PlayerRef player)         // Retrieve player's name
SetStoredPlayerName(string name)        // Store name before joining
GetStoredPlayerName()                   // Retrieve stored name
```

**Car Selection:**
```csharp
RegisterPlayerCarChoice(PlayerRef, int) // Store which car player chose
GetPlayerCarChoice(PlayerRef)           // Retrieve car choice
RPC_RegisterCarChoice(PlayerRef, int)   // Network RPC to broadcast
```

**Session Management:**
```csharp
CreateSession(string sessionName)       // Host creates room
JoinSession(string sessionName)         // Client joins room
GetAvailableSessions()                  // Return list of rooms
```

**Key Properties:**
```csharp
public NetworkRunner Runner { get; private set; }    // Network runner reference
private Dictionary<PlayerRef, string> _playerNames   // Name mapping
private Dictionary<PlayerRef, int> _playerCarChoices // Car choice mapping
private List<SessionInfo> _availableSessions         // Rooms from lobby
```

**Events:**
- `OnConnectedEvent` - Joined session
- `OnDisconnectedEvent(reason)` - Left session
- `OnJoinedSessionEvent` - Successfully in session
- `OnJoinFailedEvent(reason)` - Join failed
- `OnSessionListUpdatedEvent(sessions)` - Room list refreshed

**Scene Configuration:**
```csharp
[SerializeField] private int lobbySceneIndex = 1;    // Lobby scene
[SerializeField] private int racingSceneIndex = 2;   // Racing scene
```

**Internal Flow:**
1. Player sets name via `SetStoredPlayerName()`
2. Calls `CreateSession()` or `JoinSession()`
3. Session starts, `Runner` becomes active
4. `FusionNetworkManager.Instance` is now available to other scripts

---

### 2. **NetworkInputData.cs** - Input Synchronization

**Purpose:** Structure sent to server every frame with player input

```csharp
public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDirection;    // WASD input (-1 to 1 on each axis)
    public bool IsDrifting;          // Shift key pressed?
    public bool UsePowerup;          // Q key pressed?
    
    // Legacy (for compatibility)
    public Vector2 Direction;
    public bool IsPausing;
    public bool PressE;
    public bool PressR;
    public bool PressF;
}
```

**Sent by:** `InputHandler.OnInput()` callback (Fusion fires this automatically)

**Used by:** `CarController.GetInput()` in FixedUpdateNetwork

---

### 3. **InputHandler.cs** - Input Collection

**Purpose:** Reads keyboard input and sends via Fusion's input system

**Key Methods:**
- `Update()` - Reads keyboard (WASD, Shift, Q, E, R, F)
- `OnInput()` - Fusion callback to send input to server
- `OnSceneLoaded()` - Re-register after scene transition (CRITICAL FIX)

**Scene Transition Fix:**
```
Problem: After lobby → racing transition, Runner still exists but callbacks are lost
Solution: OnSceneLoaded unregisters and re-registers with current Runner
Result: Input continues to work across scene loads
```

**Input Mapping:**
```
Keyboard          → NetworkInputData
WASD              → MoveDirection
Shift (left/right) → IsDrifting
Q                 → UsePowerup
E, R, F           → Legacy buttons
```

**Registration Pattern:**
```csharp
private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    TryUnregister();              // Remove old callbacks
    StartCoroutine(RegisterWhenReady());  // Re-register with current Runner
}
```

---

### 4. **SessionDiscoveryManager.cs** - Room Discovery (Photon Lobby)

**Purpose:** Finds available rooms so players can join without hosting

**Key Methods:**
```csharp
async Task StartDiscovery()   // Connect to Photon Lobby, receive room list
void StopDiscovery()          // Disconnect from discovery
```

**Flow:**
1. Create discovery runner (separate from main runner)
2. Call `JoinSessionLobby(SessionLobby.ClientServer)` 
3. Fusion automatically sends room updates via `OnSessionListUpdated()`
4. Update UI with available rooms
5. Player clicks room to join

**Important:** Discovery runner is SEPARATE from gameplay runner

**Events:**
- `OnSessionListUpdatedEvent` - Room list changed
- `OnDiscoveryConnected` - Connected to lobby
- `OnDiscoveryFailed(reason)` - Connection failed

---

### 5. **Player Spawning Flow**

#### **LobbySpawner.cs** - Lobby Scene

**Spawns** `MultiplayerCharacter` prefabs (old system) for each player in lobby

**Triggers:**
- `OnPlayerJoined()` callback from Fusion
- Late spawning for players who joined before spawner was ready

**Setup:**
- 4 spawn points configured in inspector
- Only server spawns
- Assigns input authority to joining player

#### **PlayerSpawner.cs** - Racing Scene

**Spawns** racing car prefabs when scene loads

**Key Methods:**
```csharp
OnPlayerJoined(runner, player)   // Callback when player joins racing scene
SpawnPlayer(runner, player)      // Actually spawn the car at spawn point
```

**Spawn Points:**
```csharp
Vector3[] spawnPoints = new Vector3[4]
{
    new Vector3(-5, 0, 0),
    new Vector3(5, 0, 0),
    new Vector3(-5, 5, 0),
    new Vector3(5, 5, 0)
}
```

**Authority Setup:**
- Player who spawned car = InputAuthority (controls movement)
- Server = StateAuthority (confirms/validates)
- Remote players = Kinematic rigidbody (interpolated via NetworkTransform)

---

### 6. **Lobby UI System**

#### **GameLobbyUI.cs** - Main Menu

**Responsibilities:**
1. Player enters name in text field
2. Host button to create room
3. Shows room list from SessionDiscoveryManager
4. Auto-hides menu when joining session

**Flow:**
```csharp
Player enters name → OnNameInputSubmitted()
    → SetStoredPlayerName() in FusionNetworkManager
    
Host clicks "Create Room" → OnHostClicked()
    → CreateSession(roomName)
    
Player sees room list (from SessionDiscoveryManager)
    → Click room → RoomListUI.OnRoomClicked()
    → JoinSession(sessionName)
    
Join succeeds → OnJoinedSession()
    → Hide GameLobbyUI canvas
```

#### **LobbyCharacterSelectUI.cs** - Car Selection

**When Player Enters Lobby:**
1. Shows 4 car buttons (Hacker, Ghost Hunter, Priest, Scientist)
2. Player clicks car
3. Sends `RPC_RegisterCarChoice(player, carIndex)`
4. Auto-hides selection panel
5. Shows "Waiting for Host..." panel (if client)

**Server (Host):**
- Can see "Start" button
- Clicks to load racing scene

#### **RoomListUI.cs** - Room Display

**Gets** room list from `SessionDiscoveryManager.OnSessionListUpdatedEvent`

**For Each Room:**
- Shows room name, player count, max capacity
- "Join" button to enter

---

## 🔑 KEY SYSTEMS

### 1. **Authority & Ownership**

**Networking Model:**
```
Host/Server (State Authority)
├── Validates all game logic
├── Owns RaceManager
├── Owns RaceStarted/Finished state
└── Despawns bullets/traps

Player (Input Authority)
├── Moves their own car
├── Picks up powerups locally
└── Sends car position to others via CarController
```

**Synchronization:**
- CarController sends position/velocity via `[Networked]` properties
- NetworkTransform syncs to remote players
- RPCs broadcast special events (ApplySlow, RPC_RegisterCarChoice)

### 2. **State vs Input Authority**

| Aspect | State Authority | Input Authority |
|--------|-----------------|-----------------|
| Who | Server/Host | Player of that object |
| Owns Logic | Yes (Race state) | No |
| Can Read Input | Yes (via GetInput) | Yes (provides input) |
| Owns Physics | Yes (validation) | Yes (local movement) |
| Sets Networked Properties | Yes | Sometimes (needs approval) |

### 3. **RPC Communication**

**Example - Gun Hit:**
```csharp
// BulletProjectile.cs (Server/Authority)
car.RPC_ApplySlow(GUN_SLOW_AMOUNT, GUN_SLOW_DURATION);

// CarController.cs (All players execute)
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_ApplySlow(float amount, float duration)
{
    // All players apply slow locally
    ApplySlowEffect(amount, duration);
}
```

**RPCs in Use:**
- `RPC_ApplySlow()` - Slow effect (gun, trap)
- `RPC_RegisterCarChoice()` - Broadcast car choice
- `RPC_RestartRace()` - Restart the race
- `RPC_BackToLobby()` - Return to lobby

---

## 📊 DATA FLOW

### Game Startup Flow
```
Player Launches Game
    ↓
Scene 1 (Lobby) Loads
    ↓
[PlayerNameInput] Ready
    ↓
Player Types Name
    ↓
Host Click "Create Room" / Client Sees Rooms (SessionDiscoveryManager)
    ↓
FusionNetworkManager.CreateSession()/JoinSession()
    ↓
NetworkRunner Starts → OnConnected fires
    ↓
LobbySpawner spawns MultiplayerCharacter per player
    ↓
GameLobbyUI hides
    ↓
[LobbyCharacterSelectUI] Shows Car Selection
    ↓
Player clicks car → RPC_RegisterCarChoice()
    ↓
Host sees "Start" button → Host Clicks Start
    ↓
GameStartController.OnStartRaceClicked()
    ↓
Runner.LoadScene(racingSceneIndex) → Load Scene 2
```

### During Race
```
Scene 2 (Racing) Loads
    ↓
PlayerSpawner spawns cars at spawn points
    ↓
RaceManager starts 3-2-1 countdown
    ↓
RaceStarted = true (all players see via OnChangedRender)
    ↓
Each Frame:
    Player presses WASD/Shift/Q
        ↓
    InputHandler reads input → OnInput() fires
        ↓
    Sends NetworkInputData to server
        ↓
    CarController.GetInput() receives data
        ↓
    CarController.FixedUpdateNetwork() processes movement
        ↓
    CarController syncs velocity to other players
    ↓
Player touches powerup:
    PowerupPickup.OnTriggerEnter2D()
        ↓
    CarController.PickupPowerup(type)
        ↓
    PowerupInventory.AddPowerup(type)
    ↓
Player presses Q:
    PowerupInventory.UseCurrent()
        ↓
    Executes powerup (shield, gun, boost, trap)
        ↓
    If gun: FireGun() → Runner.Spawn(bullet)
    ↓
First player crosses finish line:
    FinishLineDetector.OnTriggerEnter2D()
        ↓
    RaceManager.RegisterFinishCrossing(car)
        ↓
    RaceManager.FinishCountdown = 10s
    ↓
10 seconds pass, other players finish/timeout:
    RaceManager.CalculateFinalRankings()
        ↓
    OnFinalRankings fires (all clients)
        ↓
    Show rankings
```

### Input Processing Each Frame
```
InputHandler.Update()
    ↓ Reads keyboard
    
CarController.FixedUpdateNetwork()
    ↓
GetInput(out NetworkInputData input)  [Fusion callback]
    ↓ Gets input from InputHandler.OnInput()
    
HandleMovement(input)
    ├─ Process MoveDirection
    ├─ Apply acceleration/friction
    ├─ Check drift state
    └─ Update rotation
    
HandlePowerup(input)
    └─ If UsePowerup: PowerupInventory.UseCurrent()
    
Update Networked Properties
    ├─ Set IsDrifting
    └─ Update position/velocity
    
Remote Players' Render()
    ├─ NetworkTransform interpolates position
    └─ Animation updates
```

---

## 🎯 GAME FLOW - Detailed Walkthrough

### **Scene 1: Lobby (Scene Index = 1)**

#### Stage 1: Connection
1. Player launches game → Scene 1 loads
2. `GameLobbyUI` appears with name input field
3. Player enters name (e.g., "Alice")
4. `SessionDiscoveryManager` starts → connects to Photon Lobby
5. Room list updates via `OnSessionListUpdatedEvent`

#### Stage 2: Host or Join
**If Creating Room (Host):**
- Host enters name
- Clicks "Create Room"
- `FusionNetworkManager.CreateSession()` creates new room
- `NetworkRunner` starts as GameMode.Host
- Scene 1 stays, awaits players

**If Joining Room (Client):**
- Player sees available rooms from SessionDiscoveryManager
- Clicks room to join
- `FusionNetworkManager.JoinSession()` joins as GameMode.Client
- `NetworkRunner` connects to host's session

#### Stage 3: In Lobby
1. `LobbySpawner` spawns `MultiplayerCharacter` per player
   - Host at spawn point 0
   - 1st client at point 1
   - 2nd client at point 2
   - 3rd client at point 3
2. Players see each other's avatars
3. `LobbyCharacterSelectUI` shows car selection (4 buttons)
4. Each player clicks their car choice
5. `RPC_RegisterCarChoice()` broadcasts choice to all
6. Selection panels auto-hide after 0.5s
7. Clients see "Waiting for Host..." panel

#### Stage 4: Host Starts Race
1. Host clicks "Start Game" button (only host sees)
2. `GameStartController.OnStartRaceClicked()`
3. `Runner.LoadScene(SceneRef.FromIndex(2))` → Load racing scene
4. All players transition together

---

### **Scene 2: Racing (Scene Index = 2)**

#### Stage 1: Scene Load & Spawn
1. Scene 2 loads on all players
2. `PlayerSpawner` spawns car prefabs
3. Each player gets their car at designated spawn point
4. `CarController.Spawned()` initializes physics
5. `CreatePlayerNameplate()` displays name above each car
6. All players ready

#### Stage 2: Pre-Race Countdown
1. `RaceManager.StartCountdown()` called (host-only)
2. `CountdownCounter = 3`
3. All clients see counter via `OnChangedRender`
4. `RaceUI` displays "3... 2... 1..."
5. When counter reaches 0: `RaceStarted = true`
6. `OnRaceStart` event fires on all clients

#### Stage 3: Racing (Active Race)
**Each Frame:**
1. Players hold WASD keys
2. `InputHandler.Update()` reads keyboard
3. `CarController.GetInput()` receives input via Fusion
4. `HandleMovement()` calculates new position/velocity
5. `_rb.linearVelocity = _localVelocity` applies movement
6. `NetworkTransform` syncs position to remote players
7. `RaceUI` updates speed/lap display

**Powerup Collection:**
1. Player drives into `PowerupPickup` trigger
2. `CarController.PickupPowerup()` called
3. `PowerupInventory.AddPowerup()` stores powerup
4. `OnPowerupAcquired` event fires
5. UI updates to show powerup icon

**Powerup Usage:**
1. Player presses Q
2. `PowerupInventory.UseCurrent()` executes powerup
3. **Shield:** Changes car color to green, lasts 3s
4. **Gun:** `FireGun()` → finds nearest car ahead → `Runner.Spawn(bullet)`
   - Bullet travels at 20 units/s
   - On hit: `RPC_ApplySlow(0.5, 3.0)` → car slows to 50% for 3s
5. **Speed Boost:** `ApplySpeedBoost()` → 1.5× speed for 5s
6. **Trap:** `PlaceTrap()` → spawns trap, slows car that touches it (60% for 3s)

#### Stage 4: Finish Line
1. First player crosses finish line
2. `FinishLineDetector.OnTriggerEnter2D()` detects
3. Calls `RaceManager.RegisterFinishCrossing(car)`
4. `RaceManager`:
   - Records finish time for car
   - Calculates distance from finish
   - Sets `FinishCountdown = 10f`
   - Fires `OnPlayerFinish` event
5. UI shows "Player ABC finished!"
6. Other 3 players have 10 seconds to finish

#### Stage 5: Race End
1. 10 second countdown expires OR all players finish
2. `RaceManager.RaceFinished = true`
3. `OnFinalRankings` fires with list:
   ```
   [(Car1, Position: 1, Time: 45.2s, Dist: 0),
    (Car2, Position: 2, Time: 48.5s, Dist: -2.1),
    (Car3, Position: 3, Time: 50.0s, Dist: -5.0),
    (Car4, Position: 4, Time: 10.0s, Dist: -100.0)]
   ```
4. Results UI shows final standings
5. Options to return to lobby or quit

---

## ⚙️ IMPORTANT NETWORKING CONSIDERATIONS

### 1. **Photon Fusion Callbacks**

**OnInput (Every Frame on Input Authority)**
```csharp
public void OnInput(NetworkRunner runner, NetworkInput input)
{
    var data = new NetworkInputData();
    // Read keyboard
    data.MoveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), ...);
    data.IsDrifting = Input.GetKey(KeyCode.LeftShift);
    data.UsePowerup = Input.GetKeyDown(KeyCode.Q);
    
    input.Set(data);
}
```

**OnChangedRender (Networked Property Changed)**
```csharp
[Networked, OnChangedRender(nameof(OnRaceStartedChanged))]
private bool _raceStartedTrigger { get; set; }

private void OnRaceStartedChanged()
{
    // Fires on ALL clients when property changes
    OnRaceStart?.Invoke();
}
```

### 2. **RPC Best Practices in This Project**

```csharp
// Server broadcasts to all
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_ApplySlow(float amount, float duration) { ... }

// Any client can call, only host executes
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void RPC_RestartRace() { ... }
```

### 3. **DontDestroyOnLoad Management**

These persist across scenes:
- `FusionNetworkManager` - Network runner
- `InputHandler` - Input system
- `SessionDiscoveryManager` - Discovery runner
- Any player prefabs with IsPlayer=true

### 4. **Scene Transitions**

```
Lobby Scene (1) loaded
    ↓
Runner exists, callbacks registered
    ↓
Load Racing Scene (2)
    ↓
Runner still exists (DontDestroyOnLoad)
    ↓
Callbacks might be lost!
    ↓
FIX: InputHandler.OnSceneLoaded() re-registers
```

### 5. **Authority Checks**

Always check before modifying state:
```csharp
// Only server processes race logic
if (!HasStateAuthority) return;

// Only player can move own car
if (!HasInputAuthority) return;

// Remote players use kinematic rigidbody
if (!HasInputAuthority)
    rb.isKinematic = true;
```

---

## 🎬 Detailed Architecture Components

### **Singleton Patterns Used**

1. **FusionNetworkManager** - Network hub, accessible everywhere
2. **RaceManager** - Race state, accessible to UI/controllers
3. **SessionDiscoveryManager** - Room finder (optional)
4. **AudioManager** - Audio playback (not fully integrated)

### **Component Hierarchies**

```
Scene (Lobby or Racing)
├── Canvas (UI)
│   ├── GameLobbyUI (Lobby only)
│   ├── LobbyCharacterSelectUI (Lobby only)
│   ├── RaceUI (Racing only)
│   └── GameEndChatManager (Racing)
│
├── NetworkRunner (DontDestroyOnLoad)
│
├── RaceManager (Racing only)
│   └── FinishLineDetector (child)
│
├── MainCamera
│   ├── CameraFollow (Racing)
│   └── ChatCameraAutoSetup (Racing)
│
└── SpawnedNetworkObjects
    ├── MultiplayerCharacter× 4 (Lobby)
    └── CarController× 4 (Racing)
        ├── PowerupInventory (child)
        └── Nameplate (child)
```

---

## 📝 CURRENT STATUS

### ✅ Completed Systems
- Racing mechanics (car physics, drift, acceleration)
- Network synchronization (Fusion)
- Multiplayer lobby with car selection
- Player spawning and authority setup
- Powerup system (Shield, Gun, Speed, Trap)
- Race manager and winner calculation
- Input handling with scene persistence
- Session discovery (finding rooms)
- Finish line detection with lap cooldown
- Chat system (post-game)

### ⏳ In Progress
- Game End UI / Results screen (basic framework exists)
- Audio system integration (AudioManager exists but not wired)

### ❌ Not Implemented
- Main Menu (Scene 0)
- Persistent player profiles
- Matchmaking/rating system
- Replays
- Custom map support

---

## 🔧 CONFIGURATION & TWEAKING

### To Adjust Game Feel
Edit `RacingConstants.cs`:

```csharp
// Make game faster/slower
CAR_MAX_SPEED = 15f;              // Higher = faster

// Make turning more responsive
CAR_ROTATION_SPEED = 180f;        // Higher = faster turn
CAR_DRIFT_ROTATION_MULTIPLIER = 1.5f;  // Drift boost

// Adjust friction/momentum
CAR_FRICTION = 0.95f;             // Higher = more slippery
CAR_DRIFT_FRICTION = 0.92f;       // Drift is more slippery

// Powerup balance
SHIELD_DURATION = 3f;             // Longer = more protection
SPEED_BOOST_MULTIPLIER = 1.5f;    // Higher = stronger boost
GUN_SLOW_AMOUNT = 0.5f;           // Higher = more slowing
```

### To Add Players
Change in Inspector:
- `FusionNetworkManager.maxPlayers = 8` (up to 8)
- Add spawn points in `PlayerSpawner` and `LobbySpawner`

### To Change Scene Indices
```csharp
// In FusionNetworkManager
[SerializeField] private int lobbySceneIndex = 1;
[SerializeField] private int racingSceneIndex = 2;
```

---

## 🎨 VISUALS & NAMEPLATES

Each car shows a floating nameplate:
```
    [Player Name]
     ___________
    |           |
    | Car Model |  ← Visual sprite
    |___________|
```

Created in `CarController.CreatePlayerNameplate()`:
- TextMeshPro text
- Positioned below car
- Black outline for readability
- Updates with player name from `FusionNetworkManager`

---

## 📌 KEY TAKEAWAYS

1. **Host Authority:** Server validates all critical game state (race start/end, powerups)
2. **Client Prediction:** Players move locally, then sync to others
3. **DontDestroyOnLoad:** Network objects persist across scenes
4. **Scene Transitions:** Re-register input callbacks after loading
5. **Networked Properties:** Automatically sync between clients
6. **RPCs:** Broadcast special events (hits, restarts)
7. **Authority Checks:** Always verify HasInputAuthority/HasStateAuthority
8. **Powerup Balance:** All constants in one place (RacingConstants.cs)

---

## 🐛 Common Issues & Fixes

| Issue | Cause | Fix |
|-------|-------|-----|
| Car won't move | Input not registered after scene load | InputHandler.OnSceneLoaded() handles it |
| Double lap count | Multiple trigger entries | LAP_COOLDOWN = 2s prevents it |
| Powerup doesn't appear | Local instantiate instead of networked | Use Runner.Spawn() (networked) |
| Remote player teleporting | Conflicting velocity updates | Check authority: only HasInputAuthority moves locally |
| Race doesn't start | RaceStarted on server but clients don't see | OnChangedRender notifies all clients |

---

## 📚 File Cross-References

**Player Movement Pipeline:**
1. `InputHandler.cs` → reads input
2. `CarController.cs` → receives input via GetInput()
3. `CarController.FixedUpdateNetwork()` → applies movement
4. `NetworkTransform` (component) → syncs to others

**Powerup Pipeline:**
1. `PowerupPickup.cs` → trigger detection
2. `CarController.PickupPowerup()` → store powerup
3. `PowerupInventory.cs` → manage inventory
4. `PowerupInventory.UseCurrent()` → execute
5. `BulletProjectile.cs` / `TrapObject.cs` → spawned objects
6. `RPC_ApplySlow()` → broadcast effect

**Race Win Pipeline:**
1. `FinishLineDetector.cs` → detect crossing
2. `RaceManager.RegisterFinishCrossing()` → record finish
3. `RaceManager.CalculateFinalRankings()` → calculate rankings
4. `OnFinalRankings` event → notify UI
5. Results displayed to players

---

**Document Generated:** April 2026  
**Last Updated:** Comprehensive analysis of 47 C# scripts  
**Status:** Complete & Ready for Development
