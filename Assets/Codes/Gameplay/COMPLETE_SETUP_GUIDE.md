# 🎮 DEVOUR GAMEPLAY SYSTEM - COMPLETE SETUP GUIDE

**Hướng dẫn chi tiết 100% để setup toàn bộ hệ thống Devour 2D Gameplay**

---

## 📁 PHẦN 1: CẤU TRÚC THƯ MỤC & FILE LOCATIONS

### 1.1 Vị Trí Tất Cả Files

```
Assets/
├── Codes/
│   ├── Gameplay/                          ← TẤT CẢ FILES GAMEPLAY
│   │   ├── CharacterRole.cs               ✅ Role definitions (Hunt1, Hunt2, Survival1-4)
│   │   ├── StatusEffect.cs                ✅ Status effects system (7 types)
│   │   ├── AbilitySystem.cs               ✅ Base ability framework
│   │   ├── NetworkCharacterController.cs  ✅ Main character controller
│   │   ├── GameplayStateManager.cs        ✅ Game state, bonfire, win conditions
│   │   ├── WoodAndBonfireSystem.cs        ✅ Wood spawning, bonfire, gates
│   │   ├── InputHandling.cs               ✅ Input processing
│   │   ├── GameplayNetworkIntegration.cs  ✅ Network integration helpers
│   │   ├── GameplayConstants.cs           ✅ Game constants & config
│   │   ├── Hunt1Abilities.cs              ✅ Root Hunter abilities (E/R/F)
│   │   ├── Hunt2Abilities.cs              ✅ Eyes Hunter abilities (E/R/F)
│   │   ├── SurvivalAbilities.cs           ✅ 4 Survivors × 2 abilities each
│   │   ├── GameplayUI.cs                  ✅ Gameplay UI managers
│   │   ├── GameEndUIManager.cs            ✅ Game end result UI
│   │   ├── DevourGameplayQuickSetup.cs    ✅ ONE-CLICK auto setup script
│   │   ├── AutoSetupWizard.cs             ✅ EditorWindow setup wizard
│   │   ├── GameSetupWizard.cs             ✅ Runtime setup helper
│   │   ├── GameplayConstants.cs           ✅ Global constants
│   │   ├── COMPLETE_SETUP_GUIDE.md        ← YOU ARE HERE
│   │   ├── GAME_END_UI_SETUP.md           📖 Game End UI setup guide
│   │   ├── AUTO_SETUP_GUIDE.md            📖 Quick setup guide
│   │   ├── 00_START_HERE.md               📖 Start here guide
│   │   └── (other docs)
│   │
│   └── Multiplayer/                       ← EXISTING NETWORK CODE
│       ├── GameStartController.cs         ✅ Character selection RPC
│       ├── FusionNetworkManager.cs        ✅ Network manager (partial class)
│       ├── LobbyPlayerController.cs
│       ├── LobbySpawner.cs
│       ├── MultiplayerCharacter.cs
│       └── (other multiplayer files)
│
├── Prefabs/
│   ├── Characters/
│   │   ├── Hunt1_Character.prefab         ← TẠOCÓ SẴN prefab cho Root Master
│   │   ├── Hunt2_Character.prefab         ← CREATE: Prefab cho Eyes Hunter
│   │   ├── Survival1_Character.prefab     ← CREATE: Prefab cho Marksman
│   │   ├── Survival2_Character.prefab     ← CREATE: Prefab cho Boombox
│   │   ├── Survival3_Character.prefab     ← CREATE: Prefab cho Lumberjack
│   │   └── Survival4_Character.prefab     ← CREATE: Prefab cho Support
│   │
│   ├── UI/
│   │   ├── GameplayCanvas.prefab          ← UI canvas cho gameplay
│   │   └── GameEndUI.prefab               ← UI cho end game (✅ NEW)
│   │
│   └── Environment/
│       ├── Wood.prefab                    ← Wood collectible
│       ├── Bonfire.prefab                 ← Bonfire network object
│       └── ExitGate.prefab                ← Escape gate
│
├── Scenes/
│   ├── 0_MainMenu.unity                   ← Existing
│   ├── 1_Lobby.unity                      ← Existing
│   └── 2_GameplayLevel.unity              ← CREATE: Main gameplay scene
│
├── Settings/
│   └── CharacterDatabase.asset            ← CREATE: Character config database
│
└── Resources/
    └── Prefabs/
        ├── Characters/
        │   ├── Hunt1_Character.prefab
        │   ├── Hunt2_Character.prefab
        │   ├── Survival1_Character.prefab
        │   ├── Survival2_Character.prefab
        │   ├── Survival3_Character.prefab
        │   └── Survival4_Character.prefab
        │
        ├── Environment/
        │   ├── Wood.prefab
        │   ├── Bonfire.prefab
        │   └── ExitGate.prefab
        │
        └── UI/
            ├── GameplayCanvas.prefab
            └── GameEndUI.prefab
```

---

## 🚀 PHẦN 2: QUICK SETUP (RECOMMENDED - 2 PHÚT)

### 2.1 Cách 1: ONE-CLICK Auto Setup (EASIEST)

**Bước 1:** Mở Unity Editor
```
File → Open Scene → Assets/Scenes/2_GameplayLevel.unity
```

**Bước 2:** Click menu item
```
Editor Menu → Devour → Quick Setup → All-in-One Setup
```

**Xong!** Hệ thống sẽ tự động:
- ✅ Tạo CharacterDatabase.asset
- ✅ Tạo 6 character prefabs
- ✅ Tạo 6 environment prefabs (Wood, Bonfire, Gate)
- ✅ Thêm tất cả managers vào scene
- ✅ Thiết lập UI canvas
- ✅ Lưu scene

### 2.2 Cách 2: STEP-BY-STEP Setup (MANUAL CONTROL)

Nếu bạn muốn control từng bước:

```
Devour → Quick Setup → 1. Create Database & Prefabs
↓
Devour → Quick Setup → 2. Create Character Prefabs
↓
Devour → Quick Setup → 3. Add Managers to Scene
↓
Devour → Quick Setup → 4. Setup UI & Save
```

---

## 📋 PHẦN 3: MANUAL SETUP (Chi Tiết Từng Bước)

Nếu auto setup không theo ý, làm theo hướng dẫn này:

### 3.1 Tạo Scene Gameplay

```
1. Right-click vào Assets/Scenes/
2. Create → Scene → "2_GameplayLevel"
3. Mở scene này
4. Save (Ctrl+S)
```

### 3.2 Tạo CharacterDatabase

```
1. Right-click Assets/Settings/
2. Create → Character Database
3. Đặt tên: "CharacterDatabase"
4. Select > Inspector → Configure 8 characters:
   - Hunt1: Root Master
   - Hunt2: Eyes Hunter
   - Survival1: Marksman
   - Survival2: Boombox
   - Survival3: Lumberjack
   - Survival4: Support
```

### 3.3 Tạo Character Prefabs

**VỚI ROOT MASTER (Hunt1):**
```
1. Tạo GameObject mới → "Hunt1_Character"
2. Add Components:
   - NetworkCharacterController.cs
   - StatusEffectManager.cs
   - AbilityManager.cs
   - Hunt1Abilities.cs
   - Rigidbody2D (Dynamic, Gravity Scale = 0)
   - BoxCollider2D (2x2)
   - Sprite Renderer (add sprite)
3. Drag GameObject → Assets/Prefabs/Characters/
4. Delete GameObject khỏi scene
```

**LẶP LẠI cho Hunt2, Survival1, Survival2, Survival3, Survival4:**
- Mỗi character cần hỗ trợ class-specific abilities
- Ví dụ: Hunt2Abilities.cs cho Eyes Hunter, SurvivalAbilities.cs cho survivors

### 3.4 Tạo Environment Prefabs

**Wood Prefab:**
```
1. GameObject → "Wood"
2. Add Components:
   - NetworkObject
   - WoodPickup.cs (or simple NetworkBehaviour)
   - SpriteRenderer
   - CircleCollider2D (isTrigger = true)
3. Save → Assets/Resources/Prefabs/Environment/Wood.prefab
```

**Bonfire Prefab:**
```
1. GameObject → "Bonfire"
2. Add Components:
   - NetworkObject
   - NetworkTransform
   - BonfireLogic.cs
   - SpriteRenderer
   - CircleCollider2D
3. Save → Assets/Resources/Prefabs/Environment/Bonfire.prefab
```

**ExitGate Prefab:**
```
1. GameObject → "ExitGate"
2. Add Components:
   - NetworkObject
   - GateLogic.cs
   - SpriteRenderer
   - BoxCollider2D
3. Save → Assets/Resources/Prefabs/Environment/ExitGate.prefab
```

### 3.5 Tạo UI Canvas

```
1. GameObject → UI → Canvas
2. Add Component:
   - GameplayUI.cs
   - GameplayUIManager.cs
3. Tạo children:
   - HealthBar
   - AbilityButtons (E, R, F)
   - StatusEffectDisplay
   - GameEndScreen
   - WoodCounter
   - BonfireCounter
```

### 3.5b Tạo Game End UI (NEW)

🎯 **Chi tiết đầy đủ xem:** [GAME_END_UI_SETUP.md](GAME_END_UI_SETUP.md)

**Quick Setup:**
```
1. Mở scene: Assets/Scenes/2_GameplayLevel.unity

2. Tạo GameObject → "GameEndPanel"
   - Add Component: Canvas (Screen Space - Overlay)
   - Add Component: CanvasGroup (for fade animation)

3. Tạo UI Elements (trong GameEndPanel):
   ├── Background (Image, Black with 0.8 alpha)
   ├── ContentContainer (Panel, VerticalLayoutGroup)
   │   ├── ResultText (TextMeshPro - "HUNTERS WIN!")
   │   ├── WinnerText (TextMeshPro - "🔥 HUNTERS VICTORY 🔥")
   │   ├── StatsContainer (Panel, VerticalLayoutGroup)
   │   │   ├── GameDurationText
   │   │   ├── HunterStatsText
   │   │   └── SurvivorStatsText
   │   └── ButtonContainer (HorizontalLayoutGroup)
   │       ├── BackToLobbyButton
   │       ├── MainMenuButton
   │       └── RestartButton

4. Add Component: GameEndUIManager
   └── Assign all UI elements trong Inspector

5. Drag GameEndPanel → Assets/Prefab/GameEndUI.prefab
   Xóa khỏi scene
```

### 3.6 Thêm Managers vào Scene

**Tạo GameObject mới cho mỗi manager:**

```
GameObject → "GameplayStateManager"
├── Add: NetworkObject
├── Add: GameplayStateManager.cs
└── Add: WoodAndBonfireSystem.cs

GameObject → "GameplayNetworkManager"
├── Add: GameplayNetworkManager.cs
└── Assign References (networkRunner, gameplayStateManager, etc.)

GameObject → "Spawners" (Empty parent)
├── Child: "SpawnPoint1" (position = 0, 0)
├── Child: "SpawnPoint2" (position = 5, 5)
├── Child: "SpawnPoint3" (position = -5, 5)
└── Child: "SpawnPoint4" (position = 0, -5)
```

---

## 🔧 PHẦN 4: COMPONENT SETUP DETAILS

### 4.1 NetworkCharacterController Inspector Settings

```
📍 Character Config
   └─ Character ID: [Hunt1/Hunt2/Survival1-4]

📍 Health
   └─ Max Health: 100

📍 Movement
   └─ Base Speed: 5

📍 References
   └─ Rigidbody2D: [auto-assigned]
   └─ StatusEffectManager: [auto-assigned]
   └─ AbilityManager: [auto-assigned]
```

### 4.2 GameplayStateManager Inspector Settings

```
📍 Game Config
   ├─ Game Duration: 300s (5 minutes)
   ├─ Wood Total: 10
   ├─ Bonfires Required: 3
   └─ Victory Conditions:
       ├─ Hunters Win: Kill all survivors
       └─ Survivors Win: Light all bonfires + escape

📍 References
   ├─ Network Runner: [assign from scene]
   └─ Game Canvas: [assign from scene]
```

### 4.3 AbilityManager Inspector Settings

```
📍 Ability Config
   ├─ E Ability: [populated automatically]
   ├─ R Ability: [populated automatically]
   └─ F Ability: [populated automatically]

📍 Cooldowns
   ├─ E Cooldown: 3-5s
   ├─ R Cooldown: 5-8s
   └─ F Cooldown: 8-12s
```

### 4.4 StatusEffectManager Inspector Settings

```
📍 Visual Settings
   └─ UI Indicator Prefab: [auto-created]

📍 Effect Configs
   ├─ Slowness: Speed × 0.5
   ├─ Stun: 2 seconds
   ├─ Swiftness: Speed × 1.5
   ├─ TrueSight: Vision +100%
   └─ (etc)
```

---

## 🔗 PHẦN 5: NETWORK SETUP

### 5.1 FusionNetworkManager Configuration

```
📍 GameObject: "NetworkRunner" (in Lobby scene)
   └─ Component: FusionNetworkManager
       ├─ Runner Prefab: [Fusion Runner prefab]
       ├─ Max Players: 4
       └─ Auto Connect: true
```

### 5.2 Scene Loading Setup

```
Build Settings → Scenes In Build
├─ Scene 0: Assets/Scenes/0_MainMenu.unity
├─ Scene 1: Assets/Scenes/1_Lobby.unity
└─ Scene 2: Assets/Scenes/2_GameplayLevel.unity  ← GAMEPLAY
```

### 5.3 Network Properties Sync

```
Tất cả characters có [Networked] properties:
├─ CurrentHealth
├─ IsDead
├─ NetworkedVelocity (cho remote players)
└─ StatusEffects list

Tất cả RPC calls dùng:
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
```

---

## 🎯 PHẦN 6: TESTING CHECKLIST

### 6.1 Pre-Game Tests

```
☐ Tất cả scripts compile không lỗi
☐ Character Database có 8 entries
☐ 6 character prefabs tồn tại
☐ Scene 2_GameplayLevel load được
☐ UI Canvas render bình thường
```

### 6.2 Runtime Tests (Single Player)

```
☐ Character spawn tại SpawnPoint1
☐ Có thể move (WASD hoặc Arrow Keys)
☐ E/R/F abilities execute (test movement, delays)
☐ Status effects apply & disappear
☐ Health bar updates
```

### 6.3 Network Tests (Multiplayer)

```
☐ Connect 4 players vào same session
☐ 1 player assigned Hunt1 (Hunter)
☐ 3 players assigned Survival roles
☐ Hunter có thể hit survivors → damage applies
☐ Survivors có thể collect wood → counter updates
☐ Bonfires light up khi đủ wood
☐ Game ends khi Hunter kills all OR Survivors escape
```

### 6.4 Performance Tests

```
☐ FPS stable ở 60+ (trong gameplay)
☐ Network latency < 100ms acceptable
☐ No memory leaks sau 5 phút gameplay
☐ RPC calls deliver reliably
```

---

## 📊 PHẦN 7: CONFIGURATION VALUES

### 7.1 Character Stats

```
HUNTERS:
├─ Hunt1 (Root Master): HP=100, Speed=4, Damage=20
└─ Hunt2 (Eyes Hunter): HP=80, Speed=5, Damage=15

SURVIVORS:
├─ Survival1 (Marksman): HP=80, Speed=5, Damage=10
├─ Survival2 (Boombox): HP=100, Speed=3, Damage=5
├─ Survival3 (Lumberjack): HP=120, Speed=4, Damage=8
└─ Survival4 (Support): HP=70, Speed=6, Damage=0
```

### 7.2 Game Mechanics

```
WOOD SYSTEM:
├─ Total Wood: 10 pieces
├─ Wood Respawn Time: 30s
└─ Collection: 3 wood per bonfire

BONFIRES:
├─ Total Bonfires: 3
├─ Wood Required Per Bonfire: 3
└─ Light Time: 5s per bonfire

WIN CONDITIONS:
├─ Hunter Win: Kill all 3 survivors
├─ Survivor Win: Light all bonfires + reach exit
└─ Time Limit: 300s (5 minutes)
```

### 7.3 Ability Cooldowns

```
E ABILITIES: 3-5s cooldown
R ABILITIES: 5-8s cooldown
F ABILITIES: 8-12s cooldown

STATUS EFFECTS:
├─ Slowness: 3s
├─ Stun: 2s
├─ Swiftness: 5s
├─ TrueSight: 5s
└─ Burn: 5s damage over time
```

---

## 🐛 PHẦN 8: TROUBLESHOOTING

### Problem 1: "CharacterDatabase not found"
```
Solution:
1. Assets/Settings/ → Create → Character Database
2. Configure 8 characters
3. DevourGameplayQuickSetup → Create Database
```

### Problem 2: "Prefabs spawn at wrong position"
```
Solution:
1. Check spawner positions (should be in Spawners GameObject)
2. Verify SpawnCharacter() in GameStartController
3. Check Resources/Prefabs/ path matches code
```

### Problem 3: "RPC calls not working"
```
Solution:
1. Ensure [NetworkObject] on all network gameobjects
2. Check [Rpc(RpcSources.All, RpcTargets.StateAuthority)] syntax
3. Verify HasStateAuthority before applying effects
4. Check network connection status
```

### Problem 4: "Status effects not applying"
```
Solution:
1. Verify StatusEffectManager component exists
2. Check effect type enum matches
3. Ensure duration > 0
4. Check speed multiplier calculation
```

### Problem 5: "UI not updating"
```
Solution:
1. Verify GameplayUI component assigned to canvas
2. Check OnHealthChanged event is being called
3. Ensure UI references point to correct TextMesh Pro components
4. Check EventSystem is in scene
```

---

## 📚 PHẦN 9: FILE QUICK REFERENCE

### Core Scripts
```
CharacterRole.cs                   → 8 character role definitions
StatusEffect.cs (150 lines)        → 7 status effect types
AbilitySystem.cs (90 lines)        → Base ability framework
NetworkCharacterController.cs       → Main player controller
GameplayStateManager.cs            → Game state + bonfire system
```

### Ability Scripts
```
Hunt1Abilities.cs      → VinePull, FlowerBloom, Dash, RootTrails
Hunt2Abilities.cs      → ConeVision, LightFlash, NarrowBeam, LightOrbs
SurvivalAbilities.cs   → 4 survivors × 2 abilities each = 8 total
```

### System Scripts
```
WoodAndBonfireSystem.cs       → Wood spawning, bonfire lighting
GameplayNetworkIntegration.cs → Network helpers & player lookup
InputHandling.cs              → Input processing
```

### Setup Scripts
```
DevourGameplayQuickSetup.cs   → ONE-CLICK auto setup (RECOMMENDED)
AutoSetupWizard.cs            → Step-by-step wizard
GameSetupWizard.cs            → Runtime setup helper
```

---

## ✅ FINAL CHECKLIST BEFORE PLAYING

```
BEFORE RUNNING GAME:
☐ All scripts compile (0 errors)
☐ CharacterDatabase.asset created
☐ 6 character prefabs in Resources/Prefabs/Characters/
☐ Scene 2_GameplayLevel properly configured
☐ UI Canvas has all required children
☐ Network Runner prefab assigned
☐ Spawner positions set (4 positions)
☐ Game duration set (300s)
☐ Wood count set (10)
☐ Bonfire count set (3)

BEFORE MULTIPLAYER:
☐ FusionNetworkManager setup in lobby
☐ Scene indices correct in BuildSettings
☐ Character selection UI works
☐ RPC_PlayerReadyWithCharacter() callable from GameStartController
☐ NetworkRunner active before loading gameplay scene
```

---

## 🎮 HOW TO START PLAYING

```
1. Load Assets/Scenes/1_Lobby.unity
2. Start game in editor → Click "Connect"
3. Select character (Hunt1 for Hunter, Survival1-4 for Survivors)
4. Click "Ready"
5. Game loads Assets/Scenes/2_GameplayLevel.unity
6. 1 Hunter vs 3 Survivors battle!
```

---

## 📞 SUPPORT COMMANDS

```
Devour → Quick Setup → All-in-One Setup    → Auto setup everything
Devour → Quick Setup → 1. Create Database   → Create CharacterDatabase only
Devour → Quick Setup → 2. Create Prefabs    → Create all 6 prefabs
Devour → Quick Setup → 3. Add Managers      → Add managers to scene
Devour → Quick Setup → 4. Setup UI          → Setup UI + save
```

---

**Generated: April 9, 2026**
**Version: 1.0 - Complete Setup System**
**Status: ✅ Ready for Production**
