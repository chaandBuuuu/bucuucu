# 🎯 System Architecture Diagram

## Overall Gameplay Flow

```
┌─────────────────────────────────────────────────────────────┐
│                     GAME INITIALIZATION                      │
│  FusionNetworkManager → GameplayNetworkManager              │
│        Player Join → Character Database Load               │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                   CHARACTER SELECTION                        │
│  LobbyCharacterSelectManager → Player picks character       │
│  - Hunt #1 / Hunt #2 (Hunters)                             │
│  - Survival #1-4 (Survivors)                               │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    ROLE ASSIGNMENT                          │
│  GameStartController → RPC_PlayerReadyWithCharacter()      │
│  Random 1 = Hunter, Rest = Survivors                       │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              CHARACTER SPAWNING & GAMEPLAY                   │
│  CharacterSpawner → Spawn 1 Hunter + 3 Survivors           │
│  GameplayStateManager → Phase = Playing                    │
└─────────────────────────────────────────────────────────────┘
                              ↓
                    ┌─────────┴─────────┐
                    ↓                   ↓
            ┌───────────────┐  ┌──────────────────┐
            │ HUNTER PHASE  │  │ SURVIVOR PHASE   │
            ├───────────────┤  ├──────────────────┤
            │ • Chase       │  │ • Hunt Wood      │
            │ • Attack      │  │ • Light Bonfires │
            │ • Kill        │  │ • Escape Gate    │
            └───────────────┘  └──────────────────┘
                    ↓                   ↓
                    └─────────┬─────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                   WIN CONDITION CHECK                        │
│  GameplayStateManager.CheckWinConditions()                 │
│  - If all survivors dead → HUNTER WINS                     │
│  - If all bonfires lit + ≥1 escaped → SURVIVORS WIN        │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                      GAME END SCREEN                        │
│  GameEndUIManager → Show results + Return to Lobby         │
└─────────────────────────────────────────────────────────────┘
```

---

## Character Controller Architecture

```
┌─────────────────────────────────────────────────────────────┐
│         NetworkCharacterController (Base)                   │
│  (Every character has this)                                 │
├─────────────────────────────────────────────────────────────┤
│ Properties:                                                 │
│  • CharacterID (Hunt1, Hunt2, Survival1-4)                 │
│  • CurrentHealth                                            │
│  • IsDead                                                   │
│  • IsHunter / IsSurvivor                                    │
│  • NetworkedVelocity                                        │
├─────────────────────────────────────────────────────────────┤
│ Components:                                                 │
│  └─ Rigidbody2D (Dynamic for input authority, Kinematic    │
│  └─ StatusEffectManager                                     │
│  └─ AbilityManager                                          │
│  └─ NetworkTransform                                        │
└─────────────────────────────────────────────────────────────┘
         ├─ Hunt1Passive + Hunt1AbilityE/R/F
         ├─ Hunt2Passive + Hunt2AbilityE/R/F
         ├─ Survival1Passive + Survival1AbilityE/R
         ├─ Survival2Passive + Survival2AbilityE/R
         ├─ Survival3Passive + Survival3AbilityE/R
         └─ Survival4Passive + Survival4AbilityE/R
```

---

## Status Effect System

```
┌──────────────────────────────────────────┐
│     StatusEffectManager (per character)   │
├──────────────────────────────────────────┤
│ Active Effects List:                      │
│  ┌──────────────────────────────────┐    │
│  │ [StatusEffect]                    │    │
│  │ • Type: Slowness                  │    │
│  │ • Duration: 5s                    │    │
│  │ • Magnitude: 0.3                  │    │
│  │ • ElapsedTime: 2.5s               │    │
│  └──────────────────────────────────┘    │
│  ┌──────────────────────────────────┐    │
│  │ [StatusEffect]                    │    │
│  │ • Type: Stun                      │    │
│  │ • Duration: 1.5s                  │    │
│  │ • Magnitude: 1                    │    │
│  │ • ElapsedTime: 0.5s               │    │
│  └──────────────────────────────────┘    │
│                                          │
│ Methods:                                  │
│  • AddEffect(type, duration, magnitude)  │
│  • RemoveEffect(type)                    │
│  • HasEffect(type)                       │
│  • GetEffectMagnitude(type)               │
│  • CalculateSpeedMultiplier()            │
│  • CalculateDamage()                     │
└──────────────────────────────────────────┘
         ↓
    Speed Calculation:
    baseSpeed × (1 - Slowness%) × (1 + Swiftness%)
    If Stun: 0% movement
```

---

## Ability System

```
┌────────────────────────────────────────────┐
│        AbilityManager (per character)       │
├────────────────────────────────────────────┤
│ Stored Abilities:                           │
│  • "AbilityE" → Hunt1AbilityE              │
│  • "AbilityR" → Hunt1AbilityR              │
│  • "AbilityF" → Hunt1AbilityF              │
│                                            │
│ Methods:                                   │
│  • TryExecuteAbility(name)                 │
│  • GetAbility(name)                        │
└────────────────────────────────────────────┘
         └─ Each ability inherits from Ability base class
            ├─ CanExecute()
            ├─ Execute()
            ├─ GetCooldownRemaining()
            └─ GetCooldownPercent()

Input Flow:
InputHandler → InputData → NetworkCharacterController
    ↓
if (input.PressE) → AbilityManager.TryExecuteAbility("AbilityE")
    ↓
if (CanExecute()) → Execute() → RPC_TakeDamage / RPC_AddStatusEffect
```

---

## Gameplay State Management

```
┌─────────────────────────────────────────────────────────┐
│     GameplayStateManager (Singleton)                     │
├─────────────────────────────────────────────────────────┤
│ Current State:                                           │
│  • Phase: Waiting → CharSelect → Playing → GameOver    │
│  • GameWinner: None / Hunter / Survivors                │
│  • GameTimer: 0.0s                                      │
│  • IsGameActive: true/false                             │
│                                                         │
│ Bonfires Array (4 items):                               │
│  ┌──────────────────────────────────────┐               │
│  │ Bonfire #0 (pos: 5,-5)               │               │
│  │ • woodCollected: 5                   │               │
│  │ • isLit: true                        │               │
│  └──────────────────────────────────────┘               │
│  ┌──────────────────────────────────────┐               │
│  │ Bonfire #1 (pos: 5,5)                │               │
│  │ • woodCollected: 3                   │               │
│  │ • isLit: false                       │               │
│  └──────────────────────────────────────┘               │
│  ... (bonfires 2-3)                                    │
│                                                         │
│ Hunters List: [PlayerRef → NetworkCharacterController]  │
│ Survivors List: [3 × PlayerRef → NetworkCharacterController]
│                                                         │
│ Win Condition Logic:                                    │
│  if (aliveSurvivors == 0)                              │
│      → EndGame(GameWinner.Hunter)                       │
│  if (allBonfiresPit && escapedSurvivors > 0)          │
│      → EndGame(GameWinner.Survivors)                    │
└─────────────────────────────────────────────────────────┘
```

---

## Wood & Bonfire System

```
┌─────────────────────────────────────────┐
│        Wood Distribution (20 total)      │
├─────────────────────────────────────────┤
│ WoodSystem:                              │
│  • Spawns 20 wood pieces around map     │
│  • Each piece has circular collider     │
│  • On trigger → Survivor picks up       │
│                 (sent to inventory)     │
│                                         │
└─────────────────────────────────────────┘
         ↓
┌─────────────────────────────────────────┐
│    Bonfire Collection System             │
├─────────────────────────────────────────┤
│ Bonfire Prefab (×4):                    │
│  [Bonfire] → Collider (2m range)        │
│      • woodRequired: 5                  │
│      • OnInteract: AddWood()            │
│                                         │
│ Survivor drops wood → isLit = true      │
│ When isLit = true for all 4             │
│     → ExitGate.OpenGate()               │
│     → All survivors can escape          │
│                                         │
└─────────────────────────────────────────┘
         ↓
┌─────────────────────────────────────────┐
│      Exit Gate (Escape)                 │
├─────────────────────────────────────────┤
│ ExitGate:                               │
│  • Starts: IsOpen = false               │
│  • On all bonfires lit:                 │
│      → IsOpen = true                    │
│      → Survivors can pass through       │
│  • Each survivor that reaches gate:     │
│      → Marked as "Escaped"              │
│      → If ≥1 escaped:                   │
│          SURVIVORS WIN                  │
│                                         │
└─────────────────────────────────────────┘
```

---

## Network RPC Flow

```
Local Player Action:
  Input.GetKeyDown(KeyCode.E)
  ↓
InputHandler.GetNetworkInput() → NetworkInputData
  ↓
FusionRunner.GetInput() → INetworkInput
  ↓
Network Send: E button pressed
  ↓
NetworkCharacterController.Spawned() (All clients)
  ↓
HandleAbilities(input)
  ↓
if (input.PressE)
  ├─ AbilityManager.TryExecuteAbility("AbilityE")
  └─ CanExecute() check (local)
      ├─ YES → Execute() (local)
      │   └─ RPC_TakeDamage(target, amount)
      │       └─ [Rpc(RpcTarget.StateAuthority)]
      │           └─ Auto-routes to state authority
      │               └─ Apply damage on server
      │                   └─ Replicated to all clients
      │
      └─ NO → Show cooldown UI
```

---

## Team Assignment Logic

```
ON GAME START:
1. Collect 4 PlayerRefs (all connected players)
   └─ playerList = [P1, P2, P3, P4]

2. Random select 1 as Hunter
   └─ hunterIndex = Random.Range(0, 4)
   └─ hunter = playerList[hunterIndex]

3. Remaining 3 = Survivors
   └─ survivors = [rest of playerList]

4. Role Assignment:
   ├─ Hunter → CharacterID.Hunt1 (or Hunt2 options)
   └─ Survivors → Can pick Survival1-4

5. Spawn Position:
   ├─ Hunter → spawnerPositions[0]
   ├─ Survivor1 → spawnerPositions[1]
   ├─ Survivor2 → spawnerPositions[2]
   └─ Survivor3 → spawnerPositions[3]
```

---

## Input Handling

```
Input Sources:
├─ WASD/Arrow Keys → MoveDirection (Vector2)
├─ E Key → PressE (bool)
├─ R Key → PressR (bool)
├─ F Key → PressF (bool)
└─ ESC Key → IsPausing (bool)

Each frame:
  InputHandler.GetNetworkInput()
      ↓
  NetworkInputData struct filled
      ↓
  Sent via Fusion network
      ↓
  All NetworkCharacterControllers receive
      ↓
  Local: HandleMovement(input.MoveDirection)
  Local: HandleAbilities(input.PressE/R/F)
```

---

## Memory & Performance

```
Per Character Memory:
├─ NetworkCharacterController: ~500 bytes
├─ StatusEffectManager (list of effects): ~200 bytes
├─ AbilityManager (ability refs): ~300 bytes
└─ Rigidbody2D physics: ~400 bytes
   = ~1.4 KB per character

4 Characters × 1.4 KB = 5.6 KB (negligible)

Network Messages:
├─ Input per frame: 12 bytes
├─ RPC damage: 20 bytes
├─ RPC status effect: 16 bytes
├─ Position sync: 16 bytes (handled by NetworkTransform)
└─ Total overhead: ~30-50 bytes/frame/player
   = ~3-5 KB/second at 60 FPS per player
```

---

**Last Updated**: 2025-04-07 | Version: 1.0
