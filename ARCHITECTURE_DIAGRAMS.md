# System Architecture Diagrams

## 1. NETWORK ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────┐
│                    PHOTON FUSION CLOUD                      │
│                  (Photon Backend Servers)                   │
└────────────┬─────────────────────────────────┬──────────────┘
             │                                 │
    ┌────────▼─────────────────┐      ┌───────▼────────────────┐
    │   HOST/SERVER PLAYER     │      │   CLIENT PLAYER(S)     │
    │                          │      │                        │
    │ ┌──────────────────────┐ │      │ ┌──────────────────────┐
    │ │  NetworkRunner       │ │      │ │  NetworkRunner       │
    │ │  (GameMode.Host)     │ │      │ │  (GameMode.Client)   │
    │ └──────────────────────┘ │      │ └──────────────────────┘
    │                          │      │                        │
    │ ┌──────────────────────┐ │      │ ┌──────────────────────┐
    │ │  RaceManager         │ │      │ │  RaceManager         │
    │ │  (HasStateAuth)      │ │      │ │  (Remote View)       │
    │ │  ├─ RaceStarted ✓    │ │      │ │  ├─ RaceStarted ✓    │
    │ │  ├─ RaceFinished ✓   │ │      │ │  ├─ RaceFinished ✓   │
    │ │  └─ Events ✓         │ │      │ │  └─ Events ✓         │
    │ └──────────────────────┘ │      │ └──────────────────────┘
    │                          │      │                        │
    │ ┌──────────────────────┐ │      │ ┌──────────────────────┐
    │ │  CarController[0]    │ │      │ │  CarController[0]    │
    │ │  (HasInputAuth)      │ │      │ │  (Local Authority)   │
    │ │  ├─ Position ✓       │ │      │ │  ├─ Position ✓       │
    │ │  ├─ Velocity ✓       │ │      │ │  ├─ Velocity ✓       │
    │ │  └─ Speed Mult ✓     │ │      │ │  └─ Speed Mult ✓     │
    │ └──────────────────────┘ │      │ └──────────────────────┘
    │                          │      │                        │
    │ ┌──────────────────────┐ │      │ ┌──────────────────────┐
    │ │  CarController[1-3]  │ │      │ │  CarController[1-3]  │
    │ │  (Remote Authority)  │ │      │ │  (Remote Authority)  │
    │ │  ├─ Position (sync)  │ │      │ │  ├─ Position (sync)  │
    │ │  └─ Velocity (sync)  │ │      │ │  └─ Velocity (sync)  │
    │ └──────────────────────┘ │      │ └──────────────────────┘
    │                          │      │                        │
    └──────────────────────────┘      └────────────────────────┘
```

## 2. SCENE FLOW ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────┐
│                    GAME STARTUP                             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
        ┌────────────────────────────────────┐
        │    SCENE 0: Main Menu              │
        │    (Not Yet Implemented)           │
        └────────┬───────────────────────────┘
                 │
                 ▼
    ┌──────────────────────────────────────────┐
    │       SCENE 1: LOBBY                     │
    ├──────────────────────────────────────────┤
    │                                          │
    │  ┌─ PlayerNameInput                     │
    │  │  └─ Player enters name               │
    │  │     └─ SetStoredPlayerName()         │
    │  │                                      │
    │  ├─ SessionDiscoveryManager             │
    │  │  ├─ Connects to Photon Lobby        │
    │  │  └─ Gets list of available rooms   │
    │  │                                      │
    │  ├─ GameLobbyUI                        │
    │  │  ├─ "Create Room" button (Host)    │
    │  │  └─ Room list (Join client)        │
    │  │                                      │
    │  ├─ FusionNetworkManager               │
    │  │  ├─ CreateSession() or JoinSession()│
    │  │  └─ NetworkRunner starts            │
    │  │                                      │
    │  ├─ LobbySpawner                       │
    │  │  └─ Spawns MultiplayerCharacter×4  │
    │  │                                      │
    │  └─ LobbyCharacterSelectUI             │
    │     ├─ Player selects car              │
    │     └─ RPC_RegisterCarChoice()         │
    │                                          │
    └────────┬───────────────────────────────┘
             │
             │ Host clicks "Start"
             │ GameStartController.OnStartRaceClicked()
             │ Runner.LoadScene(2)
             │
             ▼
    ┌──────────────────────────────────────────┐
    │       SCENE 2: RACING                    │
    ├──────────────────────────────────────────┤
    │                                          │
    │  ┌─ PlayerSpawner                      │
    │  │  └─ Spawns CarController×4         │
    │  │     ├─ Authority setup             │
    │  │     └─ Nameplate created           │
    │  │                                      │
    │  ├─ RaceManager                        │
    │  │  ├─ StartCountdown() [3-2-1]      │
    │  │  └─ Race begins                    │
    │  │                                      │
    │  ├─ Race Loop (Each Frame)            │
    │  │  ├─ InputHandler reads WASD        │
    │  │  ├─ CarController moves            │
    │  │  ├─ Powerups collected            │
    │  │  └─ Positions synced              │
    │  │                                      │
    │  ├─ FinishLineDetector                │
    │  │  └─ First cross → FinishCountdown │
    │  │                                      │
    │  ├─ RaceManager (After 10s)           │
    │  │  ├─ CalculateFinalRankings()      │
    │  │  └─ OnFinalRankings event fires   │
    │  │                                      │
    │  └─ Show Results & Options           │
    │     ├─ Return to Lobby               │
    │     └─ Main Menu                     │
    │                                          │
    └──────────────────────────────────────────┘
```

## 3. GAMEPLAY LOOP - PER FRAME

```
┌────────────────────────────────────────────────────────────┐
│                  FIXED FRAME LOOP                          │
└──────────────────────┬─────────────────────────────────────┘
                       │
        ┌──────────────┴──────────────┐
        │                             │
        ▼                             ▼
    ┌─────────────┐         ┌──────────────────┐
    │ Input Phase │         │ Network Phase    │
    └──────┬──────┘         └─────────┬────────┘
           │                          │
           ▼                          ▼
    ┌─────────────────────┐   ┌────────────────────┐
    │ InputHandler        │   │ NetworkRunner      │
    │ .Update()           │   │ .FixedUpdateNetwork()
    │ - Read WASD         │   │ - Query input      │
    │ - Read Shift        │   │ - Update networked │
    │ - Read Q            │   │   properties       │
    │ - Store in fields   │   │                    │
    └─────────┬───────────┘   └─────────┬──────────┘
              │                         │
              │                         ▼
              │              ┌────────────────────┐
              │              │ OnInput() Callback │
              │              │ - Build input data │
              │              │ - Send to runner   │
              │              └──────────┬─────────┘
              │                         │
              └────────┬────────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │ CarController.FixedUpdateNetwork
        │ - GetInput(out data)         │
        │ - HandleMovement(data)       │
        │   ├─ Apply acceleration     │
        │   ├─ Apply friction         │
        │   ├─ Update rotation        │
        │   └─ Check drift            │
        │ - HandlePowerup(data)       │
        │   ├─ If Q pressed:          │
        │   │  PowerupInventory.UseCurrent()
        │   │  ├─ Spawn bullet/trap   │
        │   │  ├─ Apply boost/shield  │
        │   │  └─ Fire RPC            │
        │ - Update position/velocity  │
        └──────────┬───────────────────┘
                   │
                   ▼
        ┌──────────────────────────────┐
        │ NetworkTransform (Component) │
        │ - Sync position to others    │
        │ - Remote clients interpolate │
        └──────────┬───────────────────┘
                   │
                   ▼
        ┌──────────────────────────────┐
        │ Collision Detection          │
        ├──────────────────────────────┤
        │ PowerupPickup.OnTriggerEnter2D
        │ - Car touches item           │
        │ - PickupPowerup()            │
        │                              │
        │ FinishLineDetector.OnTrigger2D
        │ - Car crosses finish         │
        │ - RegisterFinishCrossing()   │
        │                              │
        │ BulletProjectile.OnTrigger2D │
        │ - Bullet hits car            │
        │ - RPC_ApplySlow()            │
        │                              │
        │ TrapObject.OnTriggerStay2D   │
        │ - Car touches trap           │
        │ - RPC_ApplySlow()            │
        └──────────────────────────────┘
```

## 4. POWERUP SYSTEM FLOW

```
POWERUP COLLECTION
    │
    ├─ Player drives over PowerupPickup
    │
    ├─ PowerupPickup.OnTriggerEnter2D()
    │  ├─ Get CarController
    │  └─ Call PickupPowerup(type)
    │
    └─ CarController.PickupPowerup(type)
       │
       └─ PowerupInventory.AddPowerup(type)
          │
          ├─ Store _currentPowerup
          └─ Fire OnPowerupAcquired event

POWERUP USAGE
    │
    ├─ Player presses Q
    │
    ├─ InputHandler detects Q
    │  └─ Sets data.UsePowerup = true
    │
    ├─ CarController.HandlePowerup()
    │  └─ PowerupInventory.UseCurrent()
    │     │
    │     ├─ if Shield:
    │     │  ├─ _hasShield = true
    │     │  ├─ _shieldTime = 3s
    │     │  └─ Color car green
    │     │
    │     ├─ if Gun:
    │     │  ├─ Find nearest car ahead
    │     │  ├─ Runner.Spawn(bullet)
    │     │  │  ├─ Set target
    │     │  │  └─ Set direction
    │     │  └─ Bullet travels & hits
    │     │     └─ RPC_ApplySlow(0.5, 3.0)
    │     │
    │     ├─ if SpeedBoost:
    │     │  ├─ SpeedMultiplier = 1.5
    │     │  ├─ Duration timer = 5s
    │     │  └─ Decrement each frame
    │     │
    │     └─ if Trap:
    │        ├─ Runner.Spawn(trap)
    │        │  └─ Set position
    │        └─ Trap detects hits
    │           └─ RPC_ApplySlow(0.6, 3.0)

SLOW EFFECT (Gun/Trap)
    │
    ├─ Hit car receives RPC_ApplySlow(amount, duration)
    │
    ├─ CarController.ApplySlow()
    │  ├─ _slowMultiplier = (1 - amount)
    │  ├─ _slowTimer = duration
    │  └─ Speed *= multiplier
    │
    └─ Each frame while slowed:
       ├─ _slowTimer -= deltaTime
       └─ When timer ≤ 0:
          └─ Reset to normal speed
```

## 5. RACE STATE MACHINE

```
┌─────────────────────────────────────────────────────────────┐
│                    RACE STATE MACHINE                       │
└─────────────────────────────────────────────────────────────┘

                        IDLE
                         │
                         │ Scene 2 loads
                         │ RaceManager spawned
                         │
                         ▼
                    PRE-COUNTDOWN
                    CountdownCounter = -1
                    RaceStarted = false
                    RaceFinished = false
                         │
                         │ GameStartController → Host ready
                         │ RaceManager.StartCountdown()
                         │
                         ▼
                    COUNTDOWN PHASE
                    CountdownCounter: 3 → 2 → 1 → 0
                         │
                         │ Each frame:
                         │ _countdownTimer += DeltaTime
                         │ Update CountdownCounter
                         │
                         │ When _countdownTimer >= 3.0
                         │
                         ▼
                    RACING PHASE
                    RaceStarted = true
                    RaceTimer increases
                    Players can move
                         │
                         │ Each frame:
                         │ RaceTimer += DeltaTime
                         │ InputHandler sends input
                         │ CarController processes movement
                         │
                         │ UNTIL: First car crosses finish line
                         │
                         ▼
                    FINISH DETECTED PHASE
                    FinishCountdown = 10.0
                         │
                         │ First finisher: OnPlayerFinish event
                         │
                         │ Each frame:
                         │ FinishCountdown -= DeltaTime
                         │
                         │ Other players can still finish
                         │ Last one crosses OR countdown expires
                         │
                         ▼
                    RACE END PHASE
                    RaceFinished = true
                    CalculateFinalRankings()
                    OnFinalRankings event fires
                         │
                         │ All clients see rankings
                         │ UI shows results
                         │
                         ▼
                    IDLE (Ready for next race)
                    Return to Lobby OR
                    Rematch OR
                    Exit to Menu
```

## 6. AUTHORITY FLOW - WHO CONTROLS WHAT

```
┌─────────────────────────────────────────────────────────────┐
│                     AUTHORITY STRUCTURE                     │
└─────────────────────────────────────────────────────────────┘

STATE AUTHORITY (Server/Host)
    │
    ├─ Owns RaceManager
    │  ├─ Sets RaceStarted
    │  ├─ Sets RaceFinished
    │  ├─ Writes RaceTimer
    │  └─ Fires final rankings
    │
    ├─ Owns FinishLineDetector
    │  ├─ Validates lap crossing
    │  └─ Records finish time
    │
    ├─ Owns Bullet Projectiles
    │  ├─ Moves bullets
    │  ├─ Detects hits
    │  └─ Broadcasts ApplySlow RPC
    │
    └─ Owns Trap Objects
       ├─ Detects collisions
       └─ Broadcasts ApplySlow RPC

INPUT AUTHORITY (Each Player)
    │
    ├─ Owns Input for Own Car
    │  ├─ Reads keyboard
    │  ├─ Sends MoveDirection
    │  ├─ Sends IsDrifting
    │  └─ Sends UsePowerup
    │
    ├─ CarController with InputAuthority
    │  ├─ Applies local velocity
    │  ├─ Calculates movement
    │  └─ Sends position to network
    │
    └─ PowerupInventory for Own Car
       ├─ Stores powerup
       ├─ Executes powerup
       └─ Spawns gun/trap

REMOTE PLAYERS (No Authority)
    │
    ├─ Cannot move car locally
    │
    ├─ Receive position via NetworkTransform
    │
    └─ Interpolate movement visually


EXAMPLE: Player Fires Gun

Host authority: Can fire (HasInputAuthority for own car)
    │
    ├─ Player presses Q
    │ └─ InputHandler.Update() → _usePowerup = true
    │
    ├─ CarController.GetInput() receives input
    │ └─ data.UsePowerup = true
    │
    ├─ CarController.HandlePowerup()
    │ └─ PowerupInventory.FireGun()
    │
    ├─ Runner.Spawn(bullet) at player position
    │ └─ Bullet created on all clients
    │
    ├─ Bullet moves (Authority = Host)
    │ └─ Host broadcasts position
    │
    ├─ Bullet hits another car
    │ └─ Host calls RPC_ApplySlow()
    │    └─ All clients apply slow effect
    │
    └─ Result: Gun works on all machines

Remote player trying to move someone else's car:
    │
    ├─ Remote player reads input
    │ └─ No effect (not InputAuthority)
    │
    ├─ CarController.HasInputAuthority = false
    │ └─ Rigidbody set to kinematic
    │
    ├─ NetworkTransform controls position
    │ └─ Interpolates from host's updates
    │
    └─ Result: Can't puppet other cars
```

## 7. MULTIPLAYER CHARACTER SPAWN HIERARCHY

```
SCENE 1 (LOBBY)
    │
    ├─ LobbySpawner.OnPlayerJoined()
    │  │
    │  └─ FOR EACH PLAYER:
    │     │
    │     └─ Runner.Spawn(lobbyPlayerPrefab, position, quaternion)
    │        │
    │        ├─ Creates NetworkObject
    │        │
    │        └─ MultiplayerCharacter (OLD SYSTEM)
    │           ├─ Renderer: Sprite (character visual)
    │           ├─ Animator: Walk animation
    │           ├─ Name above character
    │           └─ Input Authority = Player who spawned it
    │
    └─ Result: 1-4 lobby characters visible to all


SCENE 2 (RACING)
    │
    ├─ PlayerSpawner.OnPlayerJoined()
    │  │
    │  └─ FOR EACH PLAYER:
    │     │
    │     └─ Runner.Spawn(carPrefab, spawnPoint, quaternion)
    │        │
    │        ├─ Creates NetworkObject
    │        │
    │        └─ CarController (RACING CAR)
    │           ├─ Rigidbody2D: Physics simulation
    │           ├─ SpriteRenderer: Car visual
    │           ├─ PowerupInventory (child script)
    │           ├─ Nameplate (child GameObject)
    │           │  └─ TextMeshPro (player name)
    │           │
    │           ├─ If HasInputAuthority (this player's car):
    │           │  ├─ Rigidbody Type: Dynamic
    │           │  ├─ Can read input
    │           │  ├─ Applies local velocity
    │           │  └─ Syncs position to network
    │           │
    │           └─ If NOT HasInputAuthority (other player's car):
    │              ├─ Rigidbody Type: Kinematic
    │              ├─ NetworkTransform interpolates
    │              └─ No local input processing
    │
    └─ Result: 1-4 racing cars, only local car moves
```

## 8. MESSAGE FLOW - GUN HIT EXAMPLE

```
┌──────────────────────────────────────────────────────────────┐
│           GUN HIT MESSAGE FLOW (SIMPLIFIED)                 │
└──────────────────────────────────────────────────────────────┘

PLAYER A (Host/Client) SHOOTS PLAYER B (Client)

    Player A presses Q
         │
         ▼
    InputHandler detects Q
         │
         ├─ Q pressed this frame
         └─ _usePowerup = true
         │
         ▼
    InputHandler.OnInput() callback (Host)
         │
         └─ input.Set(new NetworkInputData { UsePowerup = true, ... })
         │
         ▼
    CarController[A].GetInput(out NetworkInputData)
         │
         └─ Receives input data
         │
         ▼
    CarController[A].HandlePowerup(input)
         │
         ├─ if (input.UsePowerup)
         │  └─ PowerupInventory.UseCurrent()
         │
         ▼
    PowerupInventory[A].FireGun()
         │
         ├─ Find nearest car ahead
         │  └─ CarController[B] is ahead
         │
         ├─ Runner.Spawn(bulletPrefab, posA, rotA)
         │  └─ Creates bullet on HOST
         │
         ▼
    BulletProjectile created
         │
         ├─ HasStateAuthority = Host
         ├─ _targetCar = CarController[B]
         └─ _moveDirection = A → B
         │
         ▼
    Fusion sends OnSessionListUpdated (to all clients)
         │
         ├─ All clients create local copy of bullet
         │
         ▼
    BulletProjectile.FixedUpdateNetwork() (Host)
         │
         ├─ Calculate direction to target [B]
         ├─ Move bullet at 20 units/s
         └─ Host updates bullet position
         │
         ▼
    Fusion syncs bullet position (to all clients)
         │
         ├─ All clients see bullet moving
         │
         ▼
    Bullet touches CarController[B] (detected on Host)
         │
         ├─ OnTriggerEnter2D fires
         │
         ▼
    BulletProjectile.OnTriggerEnter2D()
         │
         ├─ Check: collision is CarController
         ├─ Check: _targetCar == collision.car
         ├─ Set _hasHit = true (no more hits)
         │
         ▼
    Host calls: car.RPC_ApplySlow(0.5f, 3.0f)
         │
         ├─ RPC targets: All clients
         │ └─ [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
         │
         ▼
    Fusion broadcasts RPC to all clients
         │
         ├─ Client B: CarController[B].RPC_ApplySlow()
         │  ├─ _slowMultiplier = 0.5f (50% slow)
         │  ├─ _slowTimer = 3.0s
         │  └─ Speed reduced by 50%
         │
         ├─ Client A: CarController[B].RPC_ApplySlow()
         │  └─ Same reduction (shows remotely)
         │
         └─ Host: CarController[B].RPC_ApplySlow()
            └─ Same reduction
         │
         ▼
    BulletProjectile despawns (after lifetime or Server.Despawn)
         │
         └─ Removed from all clients

RESULT: Player B is slowed 50% for 3 seconds on all machines
```

---

These diagrams provide visual understanding of:
1. How the network synchronizes between host and clients
2. The progression through lobby and racing scenes
3. The per-frame update loop
4. Powerup mechanics and flow
5. Race state changes
6. Authority structure and who controls what
7. Character spawning in each scene
8. A concrete example of network message flow

