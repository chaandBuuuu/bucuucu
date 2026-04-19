# Quick Reference Guide - File Interactions & Dependencies

## 📌 QUICK FILE LOOKUP

### By Functionality

#### **MOVEMENT & PHYSICS**
- `CarController.cs` → Main car physics
- `InputHandler.cs` → Reads keyboard input
- `RacingConstants.cs` → Physics constants

#### **RACING & WINNING**
- `RaceManager.cs` → Race state & winner
- `FinishLineDetector.cs` → Lap detection
- `RaceUI.cs` → HUD display

#### **POWERUPS**
- `PowerupInventory.cs` → Powerup storage & use
- `PowerupPickup.cs` → Ground pickups
- `BulletProjectile.cs` → Gun projectile
- `TrapObject.cs` → Trap mechanics

#### **NETWORKING**
- `FusionNetworkManager.cs` → Network hub
- `NetworkInputData.cs` → Input struct
- `SessionDiscoveryManager.cs` → Room finder

#### **LOBBY**
- `GameLobbyUI.cs` → Main menu
- `LobbyCharacterSelectUI.cs` → Car selection
- `LobbySpawner.cs` → Spawn lobby avatars
- `PlayerNameInput.cs` → Name entry

#### **RACING SPAWN**
- `PlayerSpawner.cs` → Spawn cars
- `GameStartController.cs` → Start race

#### **CAMERA & DISPLAY**
- `CameraFollow.cs` → Camera tracking
- `MiniMapManager.cs` → Mini-map
- `MultiCameraManager.cs` → Multi-player camera

#### **CHAT & END GAME**
- `GameEndChatManager.cs` → Chat system
- `GameEndVoteHandler.cs` → Voting

#### **AUDIO**
- `AudioManager.cs` → Sound effects

---

## 🔄 FILE INTERACTION MATRIX

### CarController ↔ Related Files

```
CarController (Main racing car)
    │
    ├─ Reads Input From:
    │  └─ NetworkInputData (via GetInput callback)
    │     └─ InputHandler.OnInput() provides data
    │
    ├─ Interacts With:
    │  ├─ PowerupInventory (child component)
    │  │  ├─ PickupPowerup()
    │  │  └─ CarController.ApplySpeedBoost()
    │  │
    │  ├─ RaceManager (Singleton)
    │  │  ├─ Calls RegisterFinishCrossing()
    │  │  └─ Reads RaceStarted, IsFinished
    │  │
    │  ├─ BulletProjectile (collision)
    │  │  └─ RPC_ApplySlow()
    │  │
    │  ├─ TrapObject (collision)
    │  │  └─ RPC_ApplySlow()
    │  │
    │  ├─ PowerupPickup (collision)
    │  │  └─ PickupPowerup()
    │  │
    │  └─ FusionNetworkManager (for nameplate)
    │     └─ GetPlayerName()
    │
    ├─ Syncs To:
    │  └─ NetworkTransform (component)
    │     ├─ Position
    │     └─ Rotation
    │
    └─ Constants From:
       └─ RacingConstants.cs
          ├─ Acceleration, MaxSpeed, Friction
          └─ Rotation Speed, Drift Multiplier
```

### InputHandler ↔ Related Files

```
InputHandler
    │
    ├─ Reads From:
    │  └─ Keyboard Input (Unity Input system)
    │     ├─ WASD
    │     ├─ Shift
    │     ├─ Q, E, R, F
    │
    ├─ Registers With:
    │  └─ FusionNetworkManager.Runner
    │     ├─ Calls AddCallbacks(this)
    │     └─ Re-registers on OnSceneLoaded
    │
    ├─ Sends To:
    │  └─ CarController
    │     └─ Via Fusion OnInput() → GetInput()
    │
    └─ Listens To:
       └─ SceneManager.sceneLoaded event
          └─ Re-register on scene transition
```

### RaceManager ↔ Related Files

```
RaceManager (Singleton, State Authority)
    │
    ├─ Receives From:
    │  └─ FinishLineDetector
    │     └─ RegisterFinishCrossing(car)
    │
    ├─ Queries:
    │  └─ CarController (all in scene)
    │     ├─ Position
    │     ├─ IsFinished
    │     └─ LapsCompleted
    │
    ├─ Fires Events For:
    │  ├─ OnRaceStart → RaceUI, GameInputLocker
    │  ├─ OnPlayerFinish → UI, Chat
    │  ├─ OnFinalRankings → Results UI
    │  └─ OnRaceEnd → UI
    │
    ├─ Used By:
    │  ├─ RaceUI (reads state)
    │  ├─ CarController (reads RaceStarted, IsFinished)
    │  ├─ GameEndChatManager (listens to events)
    │  └─ RacingGameAutoSetup (initialization)
    │
    └─ Hosted By:
       └─ FusionNetworkManager (as Singleton)
          └─ Gets via RaceManager.Instance
```

### FusionNetworkManager ↔ Related Files

```
FusionNetworkManager (Singleton, DontDestroyOnLoad)
    │
    ├─ Provides To:
    │  ├─ InputHandler
    │  │  └─ Runner reference
    │  │
    │  ├─ PlayerSpawner
    │  │  └─ Runner, carPrefabList
    │  │
    │  ├─ LobbySpawner
    │  │  └─ Runner
    │  │
    │  ├─ CarController
    │  │  └─ GetPlayerName()
    │  │
    │  ├─ LobbyCharacterSelectUI
    │  │  └─ Runner, RPC_RegisterCarChoice()
    │  │
    │  └─ GameLobbyUI
    │     └─ SetStoredPlayerName()
    │
    ├─ Calls:
    │  ├─ SessionDiscoveryManager
    │  │  └─ StopDiscovery() on join
    │  │
    │  └─ Runner.Spawn() for networked objects
    │
    ├─ Events Fired:
    │  ├─ OnConnectedEvent
    │  ├─ OnDisconnectedEvent
    │  ├─ OnJoinedSessionEvent
    │  ├─ OnJoinFailedEvent
    │  └─ OnSessionListUpdatedEvent
    │
    └─ Manages:
       ├─ Player names (Dictionary)
       ├─ Car choices (Dictionary)
       ├─ Available sessions (List)
       └─ Scene indices (Lobby=1, Racing=2)
```

### GameLobbyUI ↔ Related Files

```
GameLobbyUI
    │
    ├─ Requires:
    │  ├─ FusionNetworkManager
    │  │  ├─ SetStoredPlayerName()
    │  │  ├─ CreateSession()
    │  │  └─ Events
    │  │
    │  └─ SessionDiscoveryManager
    │     └─ StartDiscovery()
    │        └─ Gets room list
    │
    ├─ Contains:
    │  ├─ PlayerNameInput (reference)
    │  │  └─ OnNameInputSubmitted()
    │  │
    │  ├─ RoomListUI (reference)
    │  │  └─ Displays sessions
    │  │
    │  └─ GameStartController (reference, host only)
    │     └─ OnStartRaceClicked()
    │
    └─ Hides On:
       └─ OnJoinedSessionEvent
          └─ canvasToHide.SetActive(false)
```

### PlayerSpawner ↔ Related Files

```
PlayerSpawner
    │
    ├─ Gets From:
    │  └─ FusionNetworkManager.Instance
    │     ├─ Runner
    │     └─ carPrefabList
    │
    ├─ Spawns:
    │  └─ CarController prefab via Runner.Spawn()
    │     ├─ Position: spawnPoints[index]
    │     ├─ InputAuthority: player
    │     └─ StateAuthority: server
    │
    ├─ Registers With:
    │  └─ NetworkRunner callbacks
    │     ├─ OnPlayerJoined()
    │     └─ OnPlayerLeft()
    │
    └─ Result:
       └─ CarController in scene
          ├─ Connects to PowerupInventory
          ├─ Creates Nameplate
          └─ Ready for racing
```

### PowerupInventory ↔ Related Files

```
PowerupInventory (child of CarController)
    │
    ├─ Spawns:
    │  ├─ BulletProjectile (gun)
    │  │  ├─ Via Runner.Spawn()
    │  │  └─ Sets target & direction
    │  │
    │  └─ TrapObject (trap)
    │     ├─ Via Runner.Spawn()
    │     └─ Sets position
    │
    ├─ Modifies:
    │  └─ CarController (parent)
    │     ├─ ApplySpeedBoost()
    │     └─ Color (for shield)
    │
    ├─ Uses Constants From:
    │  └─ RacingConstants.cs
    │     ├─ SHIELD_DURATION
    │     ├─ GUN_SLOW_AMOUNT/DURATION
    │     ├─ SPEED_BOOST_MULTIPLIER/DURATION
    │     └─ TRAP_SLOW_AMOUNT/DURATION
    │
    └─ Events:
       ├─ OnPowerupAcquired
       ├─ OnPowerupUsed
       └─ OnPowerupEmpty
```

### SessionDiscoveryManager ↔ Related Files

```
SessionDiscoveryManager (Singleton, DontDestroyOnLoad)
    │
    ├─ Creates:
    │  └─ Separate NetworkRunner (for discovery only)
    │     └─ JoinSessionLobby(SessionLobby.ClientServer)
    │
    ├─ Called By:
    │  ├─ GameLobbyUI
    │  │  └─ StartDiscovery()
    │  │
    │  └─ FusionNetworkManager
    │     └─ StopDiscovery() before join/host
    │
    ├─ Fires Events:
    │  ├─ OnSessionListUpdatedEvent
    │  │  └─ Listened by RoomListUI
    │  │
    │  ├─ OnDiscoveryConnected
    │  └─ OnDiscoveryFailed
    │
    └─ Provides:
       └─ GetDiscoveredSessions()
          └─ List of SessionInfo
             ├─ Room name
             ├─ Player count
             └─ Max players
```

### RoomListUI ↔ Related Files

```
RoomListUI
    │
    ├─ Listens To:
    │  └─ SessionDiscoveryManager
    │     └─ OnSessionListUpdatedEvent
    │
    ├─ For Each Room:
    │  ├─ Display SessionInfo
    │  │  ├─ Name
    │  │  ├─ Players / Max
    │  │  └─ Ping
    │  │
    │  └─ "Join" Button
    │     └─ Calls FusionNetworkManager.JoinSession()
    │
    └─ Updates Display:
       └─ When room list changes
          ├─ Add new rooms
          ├─ Remove full rooms
          └─ Sort by players
```

---

## 🔀 DATA FLOW PATHS

### Path 1: Player Movement
```
Keyboard Input
    ↓
InputHandler.Update()
    ↓
InputHandler.OnInput() callback
    ↓
NetworkInputData created
    ↓
CarController.GetInput()
    ↓
CarController.HandleMovement()
    ↓
_rb.linearVelocity updated
    ↓
NetworkTransform syncs
    ↓
Remote players interpolate
```

### Path 2: Powerup Collection
```
Player drives to PowerupPickup
    ↓
PowerupPickup.OnTriggerEnter2D()
    ↓
CarController.PickupPowerup(type)
    ↓
PowerupInventory.AddPowerup(type)
    ↓
OnPowerupAcquired event
    ↓
UI updates icon
    ↓
Player presses Q
    ↓
InputHandler sends UsePowerup=true
    ↓
PowerupInventory.UseCurrent()
    ↓
Execute powerup (shield/gun/boost/trap)
    ↓
On hit: RPC_ApplySlow() to all
```

### Path 3: Race Start to Finish
```
Host clicks "Start"
    ↓
GameStartController loads Scene 2
    ↓
PlayerSpawner spawns cars
    ↓
RaceManager.StartCountdown()
    ↓
CountdownCounter: 3 → 2 → 1 → 0
    ↓
OnRaceStartedChanged() fires
    ↓
RaceStarted = true
    ↓
Players can move & collect powerups
    ↓
Player crosses FinishLineDetector
    ↓
RegisterFinishCrossing(car)
    ↓
FinishCountdown = 10s
    ↓
OnPlayerFinish event
    ↓
10s countdown
    ↓
CalculateFinalRankings()
    ↓
OnFinalRankings event
    ↓
Show results
```

---

## 🎯 COMMON TASKS & REQUIRED FILES

### To Add A New Powerup Type

**Files to Modify:**
1. `RacingConstants.cs` - Add duration/multiplier constants
2. `PowerupInventory.cs` - Add case in UseCurrent(), implement activation
3. `PowerupPickup.cs` - Already generic, just place in scene
4. Create new prefab or class if needed

**Data Flow:**
```
New Type → PowerupInventory.AddPowerup()
        → PowerupInventory.UseCurrent()
        → [New Type Case]
        → Modify CarController state
```

### To Modify Car Physics

**Files to Edit:**
1. `RacingConstants.cs` - Change CAR_* constants
2. `CarController.cs` - HandleMovement() reads from constants

**Changes Affect:**
- All cars immediately (synced across network)
- Drift behavior
- Max speed
- Acceleration

### To Add UI to Race

**Files to Update:**
1. `RaceUI.cs` - Add new UI display
2. `RaceManager.cs` - Add event if needed
3. Subscribe to RaceManager events for updates

**Event Sources:**
- `RaceManager.OnRaceStart`
- `RaceManager.OnPlayerFinish`
- `RaceManager.OnFinalRankings`
- `RaceManager.OnRaceEnd`

### To Debug Networking

**Check These Files:**
1. `FusionNetworkManager.cs` - Is runner initialized?
2. `InputHandler.cs` - Is input being registered?
3. `CarController.cs` - Does GetInput() receive data?
4. `NetworkInputData.cs` - Is structure correct?
5. Console for Fusion logs

**Common Issues:**
- Input not received: InputHandler didn't register
- Car doesn't move: HasInputAuthority check failed
- Powerup doesn't appear: Used Instantiate instead of Runner.Spawn()
- Remote car teleporting: Competing velocity updates

---

## 📦 SCRIPT INITIALIZATION ORDER

### Scene 1 (Lobby)

1. **FusionNetworkManager** - Auto-loads if missing
2. **InputHandler** - DontDestroyOnLoad, registers with runner
3. **SessionDiscoveryManager** - Created by GameLobbyUI
4. **GameLobbyUI** - Canvas setup
5. **LobbyCharacterSelectUI** - Car selection UI
6. **LobbySpawner** - Waits for runner, spawns characters
7. **PlayerNameInput** - Input field setup

### Scene 2 (Racing)

1. **FusionNetworkManager** - Already exists
2. **InputHandler** - Already exists, re-registers
3. **RaceManager** - NetworkSpawned by host
4. **FinishLineDetector** - Sets up in scene
5. **PlayerSpawner** - Spawns cars
6. **CarController** - Spawned per player
7. **PowerupInventory** - Created as child of CarController
8. **RaceUI** - Canvas setup
9. **GameEndChatManager** - Chat setup

---

## 🚀 KEY PROPERTIES TO KNOW

### NetworkInputData
```csharp
public Vector2 MoveDirection;  // WASD (-1 to 1)
public bool IsDrifting;        // Shift pressed?
public bool UsePowerup;        // Q pressed?
```

### CarController (Networked)
```csharp
[Networked] bool IsDrifting
[Networked] int LapsCompleted
[Networked] bool IsFinished
[Networked] float SpeedMultiplier  // 1.0 or 1.5
```

### RaceManager (Networked)
```csharp
[Networked] bool RaceStarted
[Networked] bool RaceFinished
[Networked] float RaceTimer
[Networked] int CountdownCounter    // 3, 2, 1, 0, -1
[Networked] float FinishCountdown   // 10s to 0
```

---

## 💾 WHERE TO SAVE THINGS

### DontDestroyOnLoad (Persist Across Scenes)
- `FusionNetworkManager` - Always
- `InputHandler` - Always
- `SessionDiscoveryManager` - Until stopped
- Any NetworkRunner - Always

### Scene-Specific
- `RaceManager` - Lobby: No, Racing: Yes
- `PlayerSpawner` - Lobby: No, Racing: Yes
- `CarController` - Lobby: No, Racing: Yes
- `LobbySpawner` - Lobby: Yes, Racing: No

---

## 🔍 DEBUGGING CHECKLIST

When something doesn't work:

- [ ] Is FusionNetworkManager.Instance != null?
- [ ] Is NetworkRunner.IsRunning?
- [ ] Is GetInput() receiving data?
- [ ] Is HasInputAuthority correct?
- [ ] Is HasStateAuthority correct?
- [ ] Are Networked properties using [Networked]?
- [ ] Are RPCs using correct RpcSources/RpcTargets?
- [ ] Is prefab marked as Spawnable in Fusion?
- [ ] Did object spawn with Runner.Spawn()?
- [ ] Are NetworkTransforms set up?
- [ ] Is authority assigned correctly?
- [ ] Check console for Fusion error messages

---

**Document Version:** 1.0  
**Last Updated:** April 2026  
**Scope:** 47 Scripts, 2 Scenes, Photon Fusion Networking
