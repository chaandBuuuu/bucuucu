# 📖 Hướng Dẫn Setup Multiplayer 4 Người (Đơn Giản)

4-player multiplayer horror game - **Chọn phòng → Chọn nhân vật → Di chuyển lobby → Chơi game**

**Thời gian**: 30-45 phút  
**Cấp độ**: Dễ  
**Updated**: 2026

---

## 🔄 Flow Đơn Giản

```
Start Game
    ↓
[LobbyScene]
    ├─ Chọn/Tạo Phòng
    ├─ Chọn Nhân Vật (4 lựa chọn)
    ├─ Di chuyển tự do (WASD)
    └─ Khi đủ 4 người → Tự động vào GameScene
         ↓
    [GameScene - Chơi Game]
```

---

## 🔑 Bước 1: Photon 2 Setup

### 1.1 Cấu Hình Photon

1. **Window > Photon 2 > Highlight Server Settings**
2. Tạo tài khoản miễn phí: https://www.photonengine.com
3. Tạo Application baru
4. Copy **AppId** → Paste vào Photon Settings

### 1.2 Photon Settings

```
[Photon Tab]
- Game Version: "1.0"
- Fixed Region: "us"

[Network Tab]
- SendRate: 60
- SerializationRate: 60
```

---

## 📁 Bước 2: Cấu Trúc Thư Mục

```
Assets/
├── Codes/Multiplayer/ (11 scripts - all ready)
├── Prefabs/
│   ├── LobbyPlayer.prefab
│   └── MultiplayerCharacter.prefab
├── Scenes/
│   ├── LobbyScene.unity
│   └── GameScene.unity
```

Scripts đã tạo: PhotonNetworkManager, GameLobbyUI, LobbySpawner, LobbyPlayerController, PlayerSpawner, MultiplayerCharacter, GameStartController, GameManager, CameraFollow, MultiplayerConfig

---

## 🎨 Bước 3: Setup LobbyScene

### 3.1 Tạo Scene

1. **File > New Scene** → Save as `LobbyScene` trong `Assets/Scenes/`
2. **Create > 2D Object > Sprite (Square)** → Rename `Background`
   - Scale: (20, 20, 1)
   - Color: Dark (e.g., #1a1a2e)
   - Sort Order: -10
3. **Create > 2D Object > Box Collider 2D** (x4) → Tạo 4 bức tường để giới hạn chuyển động

### 3.2 Tạo Canvas

**Create > UI > Canvas** → Rename `LobbyCanvas`

Canvas Scaler:
- UI Scale Mode: Scale with Screen Size
- Reference Resolution: 1920x1080

### 3.3 Tạo RoomPanel (Chọn Phòng)

```
LobbyCanvas
└── RoomPanel (Panel - Image White)
    ├── Title (Text: "Chọn Phòng")
    ├── RoomInput (InputField)
    │   └── Placeholder: "Nhập tên phòng..."
    ├── HostButton (Button)
    │   └── Text: "Tạo Phòng"
    │   └── Color: Green
    └── JoinButton (Button)
        └── Text: "Vào Phòng"
        └── Color: Blue
```

**Setup**:
- Attach **GameLobbyUI** to `LobbyCanvas`
- Assign trong Inspector:
  - `RoomPanel`: RoomPanel (GameObject)
  - `RoomInput`: RoomInput (InputField)
  - `HostButton`: HostButton (Button)
  - `JoinButton`: JoinButton (Button)

### 3.4 Tạo CharacterSelectPanel

```
LobbyCanvas
└── CharacterSelectPanel (Panel)
    ├── Title (Text: "Chọn Nhân Vật")
    ├── Character1Button (Red, Text: "Hacker")
    ├── Character2Button (Green, Text: "Ghost Hunter")
    ├── Character3Button (Yellow, Text: "Priest")
    ├── Character4Button (Blue, Text: "Scientist")
    └── SelectedCharText (Text: "Chọn: ...")
```

**Button Setup**:
Each button: **On Click** → GameLobbyUI.OnCharacterSelected(0/1/2/3)

**Script Assign**:
- All 4 character buttons
- `SelectedCharText`: SelectedCharText (Text)

### 3.5 Tạo StatusText

```
LobbyCanvas
└── StatusText (Text)
    └── Text: "Đang chờ..."
    └── Font Size: 32
```

Assign vào GameLobbyUI: `StatusText`

### 3.6 Tạo PhotonNetworkManager (Singleton)

Empty GameObject: `PhotonNetworkManager`
- Add **PhotonNetworkManager** script
- Setup: **DontDestroyOnLoad** enabled
- Game Version: "1.0"
- Max Players: 4

### 3.7 Tạo LobbyManager

Empty GameObject: `LobbyManager`
- Add **PhotonView** (View ID: 1)
- Add **LobbySpawner**
- Add **GameStartController**

**LobbySpawner Config**:
- Spawn Points:
  ```
  [0]: (-5, 0, 0)
  [1]: (5, 0, 0)
  [2]: (-5, 5, 0)
  [3]: (5, 5, 0)
  ```

---

## 🎭 Bước 4: Tạo LobbyPlayer Prefab

### 4.1 Tạo GameObject

```
LobbyPlayer (GameObject)
├── SpriteRenderer
│   └── Color: White (square)
├── Rigidbody2D
│   └── Body Type: Dynamic
│   └── Gravity Scale: 0
│   └── Freeze Rotation Z: True
├── BoxCollider2D
├── PhotonView
│   └── View ID: (auto)
│   └── Ownership: Takeover
├── LobbyPlayerController
│   └── Move Speed: 5
│   └── Acceleration: 15
│   └── Deceleration: 20
└── Text (for name display)
```

### 4.2 Lưu Prefab

Drag `LobbyPlayer` vào **Assets/Prefabs/**
Rename: `LobbyPlayer`

---

## ⚔️ Bước 5: Tạo MultiplayerCharacter Prefab

### 5.1 Tạo GameObject

```
MultiplayerCharacter (GameObject)
├── SpriteRenderer (White square)
├── Rigidbody2D (Dynamic, Gravity 0)
├── BoxCollider2D
├── PhotonView (Takeover)
├── MultiplayerCharacter
│   └── Move Speed: 5
├── PlayerInput (InputSystem)
└── Animator (optional)
```

### 5.2 Lưu Prefab

Drag vào **Assets/Prefabs/**
Rename: `MultiplayerCharacter`

---

## 🎮 Bước 6: Setup GameScene

### 6.1 Tạo Scene

**File > New Scene** → Save as `GameScene`

Background + Colliders (giống LobbyScene)

### 6.2 Tạo GameSetup

```
GameSetup (Empty)
├── PhotonView (View ID: 1)
├── PlayerSpawner
│   └── Spawn Points: [(-5,0,0), (5,0,0), (-5,5,0), (5,5,0)]
│   └── Prefab Name: "Prefabs/MultiplayerCharacter"
├── GameManager
└── GameStartController
```

### 6.3 Game UI (Optional)

```
Canvas
├── PlayerCountText (Text: "Người: 4/4")
└── StatusText (Text: "Playing...")
```

Assign vào GameManager nếu cần.

---

## 🎯 Bước 7: Build Settings

1. **File > Build Settings** (Ctrl+Shift+B)
2. **Add Open Scenes**:
   - Index 0: `LobbyScene`
   - Index 1: `GameScene`

---

## 🧪 Bước 8: Test

### Solo (1 Player)
- Play
- Create room name
- Click "Tạo Phòng"
- Select character
- Move with WASD
- Console check: logs show "Room created", "Player joined", etc.

### 2 Players
- Editor: Create room, select char 1, move
- Build: Join room, select char 2, move
- Verify: movement sync, character different colors

### 4 Players
- All 4 join room
- Each select different character (1, 2, 3, 4)
- All move freely in lobby
- After all 4 ready (or auto delay): Game starts
- GameScene loads, all 4 spawn at different positions
- Verify: No lag, movement smooth

---

## 🐛 Troubleshooting

| Issue | Fix |
|-------|-----|
| "Not connected to Photon" | Check AppId, internet connection |
| "Prefab not found" | Path: `Assets/Prefabs/MultiplayerCharacter.prefab` |
| "Movement lag" | Check SendRate=60, SerializationRate=60 |
| "Character position wrong" | Check spawn points in PlayerSpawner |
| "Can't select character" | Button On Click not linked to GameLobbyUI |

---

## 📚 Script Quick Reference

| Script | Purpose |
|--------|---------|
| **PhotonNetworkManager** | Room connection & management |
| **GameLobbyUI** | UI buttons: room, character select |
| **LobbySpawner** | Spawn LobbyPlayer at positions |
| **LobbyPlayerController** | WASD movement + network sync |
| **PlayerSpawner** | Spawn game characters at spawn points |
| **MultiplayerCharacter** | Game character controller |
| **GameStartController** | Auto-start when 4 players ready |
| **CameraFollow** | Smooth camera |
| **GameManager** | Game state management |

---

## ✅ Checklist

- [ ] Photon AppId set
- [ ] LobbyScene created (room panel + char panel)
- [ ] LobbyPlayer prefab created
- [ ] MultiplayerCharacter prefab created
- [ ] GameScene created with GameSetup
- [ ] Build Settings: LobbyScene (0), GameScene (1)
- [ ] Solo test passed
- [ ] 2-player test passed
- [ ] 4-player test passed

---

**Status**: Ready to test!

Simple. Effective. Works.


---

## 📦 Yêu Cầu Trước Khi Bắt Đầu

- Unity 2022 LTS trở lên
- Photon 2 PUN2 (Free plan)
- Input System (New)
- Ít nhất 2 test devices/instances (hoặc Build + Editor)

---

## 🔄 System Flow Diagram

```
                    ┌─────────────────┐
                    │   LobbyScene    │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
          ┌────────┐    ┌────────┐    ┌────────┐
          │1.ROOM  │    │2.CHAR  │    │3.MOVE  │
          │SELECT  │───→│SELECT  │───→│LOBBY   │
          └────────┘    └────────┘    │(WASD)  │
                                      └────────┘
                                           │
                                           ▼ (4 players ready)
                                    ┌──────────────┐
                                    │  GameScene   │
                                    │   (Auto)     │
                                    └──────────────┘

UI Panels (All on same Canvas):
├─ RoomPanel ───────────→ Show at start
├─ CharacterSelectPanel ─→ Show after join room
└─ StatusText ──────────→ Shows connection status
```

---

## 🔑 Bước 1: Photon 2 Setup

### 1.1 Cài Đặt PUN2

1. Mở **Window > TextMesh Pro > Import TMP Essential Resources** (nếu chưa)
2. Vào **Window > Photon 2 > Highlight Server Settings**
3. Tạo tài khoản miễn phí tại https://www.photonengine.com
4. Tạo Application baru
5. Copy **AppId**
6. Paste vào Photon Settings

### 1.2 Cấu Hình Photon Settings

Vào **Assets > PhotonServerSettings** (hoặc **Window > Photon > Highlight Server Settings**):

```
[Photon Tab]
- Game Version: "1.0"
- Fixed Region: "us" (hoặc region gần nhất)

[Network Tab]
- SerializationRate: 60
- SendRate: 60
- PhotonNetwork.TimeoutDisconnect: 20000ms
```

---

## 📁 Bước 2: Cấu Trúc Thư Mục & Scripts

### 2.1 Cấu Trúc Assets

```
Assets/
├── Codes/Multiplayer/
│   ├── PhotonNetworkManager.cs ✅
│   ├── GameLobbyUI.cs ✅
│   ├── GameStartController.cs ✅
│   ├── MultiplayerCharacter.cs ✅
│   ├── PlayerSpawner.cs ✅
│   ├── CameraFollow.cs ✅
│   ├── GameManager.cs ✅
│   ├── LobbyPlayerController.cs ✅
│   ├── LobbySpawner.cs ✅
│   └── MultiplayerConfig.cs ✅
├── Prefabs/
│   ├── MultiplayerCharacter.prefab [Game Scene]
│   └── LobbyPlayer.prefab [Lobby Scene]
├── Scenes/
│   ├── LobbyScene.unity
│   └── GameScene.unity
└── UI/
    └── (Canvas UI built in-scene)
```

All scripts **already created** - verify in Assets/Codes/Multiplayer/ folder.

---

## 🎨 Bước 3: Cấu Hình LobbyScene

### 3.1 Tạo Scene Mới

1. **File > New Scene** → Create empty scene
2. **Ctrl+Shift+S** (Save Scene As)
3. Tên: `LobbyScene`, lưu trong `Assets/Scenes/`
4. **Scene > New Scenes > Scene** để add second scene nếu cần

### 3.2 Setup Lobby Game World

1. **Create > 2D Object > Sprite (Square)** → Rename `Background`
   - Scale: (20, 20, 1) để tạo nền
   - Color: tuỳ chọn (e.g., #1a1a2e)
   - Sort Order: -10

2. **Create > 2D Object > Box Collider 2D** (cho các bức tường)
   - Tạo 4 walls (trên/dưới/trái/phải) để giới hạn di chuyển
   - Make sure colliders blocking player movement

### 3.3 Tạo Canvas (Chứa tất cả UI Panels)

1. **Create > UI > Canvas** → Rename `LobbyCanvas`
2. Canvas Scaler Settings:
   - **UI Scale Mode**: Scale with Screen Size
   - **Reference Resolution**: 1920x1080
   - **Screen Match Mode**: Expand

### 3.4 Tạo RoomPanel (Chọn Phòng)

**Hierarchy**:
```
LobbyCanvas
└── RoomPanel (Panel, Image - Light Gray)
    ├── Title (Text: "Chọn Phòng")
    ├── RoomInput (InputField)
    │   └── Placeholder: "Tên phòng..."
    ├── HostButton (Button - Green)
    │   └── Text: "🏠 Tạo Phòng"
    ├── JoinButton (Button - Blue)
    │   └── Text: "🚪 Vào Phòng"
    └── StatusText (Text)
        └── Text: "Chờ kết nối..."
```

**Script Setup** (GameLobbyUI):
- Attach to `LobbyCanvas`
- Gán `RoomPanel`: RoomPanel GameObject
- Gán `RoomInput`: RoomInput InputField
- Gán `HostButton`: HostButton Button
- Gán `JoinButton`: JoinButton Button
- Gán `StatusText`: StatusText (Text)

### 3.5 Tạo CharacterSelectPanel

**Hierarchy**:
```
LobbyCanvas
└── CharacterSelectPanel (Panel, Image - Medium Gray)
    ├── Title (Text: "Chọn Nhân Vật")
    ├── Character1Button (Button)
    │   ├── Image/Color: Red (#FF4444)
    │   └── Text: "🧠 Hacker"
    ├── Character2Button (Button)
    │   ├── Image/Color: Green (#44FF44)
    │   └── Text: "👻 Ghost Hunter"
    ├── Character3Button (Button)
    │   ├── Image/Color: Yellow (#FFFF44)
    │   └── Text: "✝️ Priest"
    ├── Character4Button (Button)
    │   ├── Image/Color: Blue (#4444FF)
    │   └── Text: "🔬 Scientist"
    └── SelectedCharText (Text)
        └── Text: "Chọn: [Undefined]"
```

**Button Setup**:
- **On Click** → Add Listener → GameLobbyUI.OnCharacterSelected(0/1/2/3)

**IMPORTANT - Allow WASD Input with UI**:
- Select **RoomPanel** and **CharacterSelectPanel**
- **Graphic** component: **Raycast Target** = **FALSE**
- Allows WASD movement even with UI visible

**Script Setup** (GameLobbyUI):
- Assign all 4 character buttons
- Assign `SelectedCharText`

### 3.6 Tạo LobbyManager (Auto-Start Manager)

**Hierarchy**:
```
LobbyScene
└── LobbyManager (Empty GameObject)
    ├── PhotonView (Component)
    ├── LobbySpawner (Component)
    └── GameStartController (Component)
```

**Setup LobbyManager**:

1. **PhotonView**:
   - **View ID**: 1
   - **Ownership**: Takeover
   - **Observed**: Leave empty

2. **LobbySpawner**:
   - Create 4 empty GameObjects as spawn points:
     - `SpawnPoint_1` at (-5, 0, 0)
     - `SpawnPoint_2` at (5, 0, 0)
     - `SpawnPoint_3` at (-5, 5, 0)
     - `SpawnPoint_4` at (5, 5, 0)
   - In Inspector, set **Spawn Points Size: 4**
   - Drag each SpawnPoint into the array

3. **GameStartController**:
   - No special setup (auto-detects 4 players ready)
   - Optional: Assign `StatusText` for UI feedback

### 3.7 Cấu Hình PhotonNetworkManager (Singleton)

**Create new GameObject or use prefab:**

```
DontDestroyOnLoad
└── PhotonNetworkManager (Empty GameObject)
    └── PhotonNetworkManager (Script Component)
```

**Setup in Inspector**:
- **Game Version**: "1.0"
- **Max Players Per Room**: 4
- **Auto Connect**: FALSE (manual connection on start)

Script has `DontDestroyOnLoad(gameObject);` in Awake

---

## 🎭 Bước 4: Tạo LobbyPlayer Prefab

### 4.1 Chuẩn Bị Character GameObject

1. Tạo GameObject mới: `LobbyPlayer`
2. Thêm Components:
   - **SpriteRenderer** (Character visual - tạm dùng hình vuông)
   - **Rigidbody2D**
   - **BoxCollider2D**
   - **Animator** (nếu có animation)
   - **PhotonView**
   - **LobbyPlayerController** (script)
   - **Text** (for player name display)

### 4.2 Cấu Hình Rigidbody2D

```
Body Type: Dynamic
Gravity Scale: 0
Velocity: Freezed (X, Y)
Constraints:
  ✓ Freeze Rotation Z
  ✓ Collision Enabled
```

### 4.3 Cấu Hình PhotonView

```
View ID: Auto Allocate (0 initially)
Ownership: Takeover
Observed Components:
  - LobbyPlayerController (class sẽ implement IPunObservable)
```

### 4.4 Cấu Hình LobbyPlayerController (Script)

Attach component **LobbyPlayerController**, settings:

```
Move Speed: 5.0
Acceleration: 15.0
Deceleration: 20.0
Animation Speed Multiplier: 1.0
Name Display Offset: (0, 1, 0)
```

### 4.5 Lưu thành Prefab

1. Drag `LobbyPlayer` GameObject vào **Assets/Prefabs/**
2. Rename thành `LobbyPlayer`
3. **Ctrl+Alt+P** (Prefab > Apply) hoặc drag vào folder lại lần nữa

---

## ⚔️ Bước 5: Tạo MultiplayerCharacter Prefab (Game Scene)

### 5.1 Chuẩn Bị Character

1. Tạo GameObject mới: `MultiplayerCharacter`
2. Thêm Components:
   - **SpriteRenderer**
   - **Rigidbody2D** (Dynamic, Gravity=0)
   - **BoxCollider2D**
   - **PlayerInput** (từ InputSystem)
   - **Animator** (nếu có)
   - **PhotonView** (Ownership: Takeover)
   - **MultiplayerCharacter** (script chủ)

### 5.2 Cấu Hình MultiplayerCharacter (Script)

```
Move Speed: 5.0
Acceleration: 15.0
Deceleration: 20.0
Animation Speed Multiplier: 1.0
Face Movement Direction: True
```

### 5.3 Cấu Hình PhotonView

```
View ID: (Auto allocate, 0 initially)
Ownership: Takeover
Observed Components:
  - MultiplayerCharacter
```

### 5.4 Lưu thành Prefab

1. Drag `MultiplayerCharacter` vào **Assets/Prefabs/**
2. Rename thành `MultiplayerCharacter`

---

## 🎮 Bước 6: Tạo GameScene

### 6.1 Tạo Scene Mới

1. **File > New Scene**
2. Save tên: `GameScene`, path: `Assets/Scenes/`

### 6.2 Setup Game World

- Tạo map/tileset/background (tương tự LobbyScene)
- Thêm colliders cho bức tường/chướng ngại vật
- Size nên lớn hơn để di chuyển/chase enemies

### 6.3 Tạo GameSetup Manager (Empty)

**Hierarchy**:
```
GameScene
└── GameSetup (Empty GameObject)
    ├── PhotonView (View ID: 1)
    ├── PlayerSpawner (Script)
    ├── GameManager (Script)
    ├── GameStartController (Script)
    └── Canvas (cho game UI)
```

### 6.4 Cấu Hình PlayerSpawner

**Script Component Settings**:

```
Spawn Points (Array[4]):
  [0]: (-5, 0, 0)
  [1]: (5, 0, 0)
  [2]: (-5, 5, 0)
  [3]: (5, 5, 0)

Player Prefab Name: "Prefabs/MultiplayerCharacter"
```

### 6.5 Cấu Hình GameManager

1. Attach **GameManager** script
2. Settings:
   ```
   Network Send Rate: 60
   Network Serialization Rate: 60
   ```
3. **Create GameCanvas** (để display game stats):
   ```
   Canvas
   ├── PlayerCountText (Text: "Người chơi: 4/4")
   ├── GameStatusText (Text: "Playing...")
   └── PausePanel (Inactive initially)
   ```
4. Gán Canvas references vào GameManager

### 6.6 Cấu Hình GameStartController

- Attach **GameStartController** script
- Không cần settings (auto-run)

---

## 🎯 Bước 7: Build Settings & Scene Setup

### 7.1 Cấu Hình Build Settings

1. **File > Build Settings** (Ctrl+Shift+B)
2. **Add Scenes**:
   - Drag `LobbyScene` vào → Index 0
   - Drag `GameScene` vào → Index 1
   - **Ensure LobbyScene is first** (loads at startup)

### 7.2 Project Settings - Input System

1. **Edit > Project Settings > Input System Package**
   - Ensure **Supported Devices**: Keyboard, Gamepad, etc.
   - Create **Input Actions** (hoặc sử dụng default nếu có)

### 7.3 Script Compilation Check

- **Ctrl+Shift+Alt+O** (Refresh compilation)
- Kiểm tra **Console** không có errors
- Tất cả scripts phải green ✓

---

## 🧪 Bước 8: Testing Checklist (CRITICAL)

### 8.1 Solo Test (1 Player)

1. **Play trong Editor**
2. **Kiểm tra**:
   - ✓ RoomPanel hiển thị
   - ✓ Nhập room name, click Host
   - ✓ CharacterSelectPanel hiển thị, 4 buttons clickable
   - ✓ Click character → SelectedCharText cập nhật
   - ✓ WASD movement works
   - ✓ Console logs show "Room created", "Character selected", etc.

### 8.2 2-Player Test (Editor + Build)

1. **Unity Editor**: Host, SelectChar, Move
2. **Build & Run**: Join room, Select different char, Move
3. **Kiểm tra**:
   - ✓ Both players visible in LobbyScene
   - ✓ WASD movement works for both
   - ✓ Both can move independently (networking smooth)
   - ✓ Both names show above characters
   - ✓ After both select character → Auto-start (no countdown needed)
   - ✓ Both clients load GameScene automatically
   - ✓ Multiplayer characters spawn at correct positions

### 8.3 4-Player Test (Full)

1. **4 instances** (Editor + 3 Builds, hoặc 4 Builds)
2. **Kiểm tra**:
   - ✓ All 4 spawn at different locations
   - ✓ All names visible + colors assigned
   - ✓ Movement syncs for all players
   - ✓ After all 4 select character → Auto-start
   - ✓ All 4 enter GameScene as MultiplayerCharacters
   - ✓ All 4 can move independently in game
   - ✓ Camera follows owner
   - ✓ No lag/stuttering (bandwidth ~2.5KB per player)

### 8.4 Critical Bug Checks

- ❌ **Player movement stuttering?** (check SendRate/SerializationRate)
- ❌ **Spawn points wrong?** (verify Transform array in LobbySpawner)
- ❌ **Network timeout sau 5 phút → still connected?** (check timeout settings)
- ❌ **Memory leak: Play → Quit → Play again → memory increase?** (cleanup scenes)

---

## 🐛 Debug & Troubleshooting

### Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| "Chưa kết nối tới Photon" | AppId wrong hoặc network offline | Check Photon Settings, test internet |
| "RoomPanel không hiển thị" | Canvas not setup properly | Create Canvas, attach GameLobbyUI |
| "Character movement lag" | SendRate/SerializationRate thấp | Increase to 60 both |
| "WASD input bị chặn bởi UI" | Raycast Target chưa tắt | Set Panel Raycast Target = FALSE |
| "Prefab not found error" | Prefab path wrong | Đảm bảo `Assets/Prefabs/MultiplayerCharacter.prefab` |
| "RPC not received" | PhotonView View ID duplicate | Each object cần unique ID |
| "Character không spawn tại đúng vị trí" | Spawn points not assigned | Check Transform array in LobbySpawner |

### Console Logs để Monitor

```csharp
// Expected logs khi running:
[PhotonNetworkManager] Connected to Photon
[PhotonNetworkManager] Created room: [RoomName]
[LobbySpawner] Spawning LobbyPlayer for Player [PID]
[GameStartController] All 4 players ready! Loading GameScene...
[PlayerSpawner] Spawned character [0] at position (-5, 0, 0)
[GameManager] Game started! Players: 4
```

---

## 📊 Network Optimization Settings

### Đã Cấu Hình:

```csharp
// PhotonNetwork settings:
PhotonNetwork.SendRate = 60;           // 60 updates/sec
PhotonNetwork.SerializationRate = 60;  // 60 serializations/sec
OnPhotonSerializeView() method optimize bandwidth

// Bandwidth per player: ~2.5 kB/s
// Max 20 players simultaneously (Free Photon Plan)
```

### Bandwidth Breakdown:
- **Position Sync** (~1KB/s per player)
- **Rotation Sync** (~0.5KB/s)
- **Animation** (~0.5KB/s)

**Total**: ~2 KB/s × 4 players = 8 KB/s network usage (very efficient)

---

## 📚 Script Reference Summary

| Script | Location | Purpose |
|--------|----------|---------|
| **PhotonNetworkManager** | Assets/Codes/Multiplayer | Photon connection, rooms, player properties |
| **GameLobbyUI** | Assets/Codes/Multiplayer | Room selection & character select UI |
| **LobbySpawner** | Assets/Codes/Multiplayer | Spawns LobbyPlayer prefabs at spawn points |
| **LobbyPlayerController** | Assets/Codes/Multiplayer | WASD movement in lobby + network sync |
| **PlayerSpawner** | Assets/Codes/Multiplayer | Instantiates MultiplayerCharacters in GameScene |
| **MultiplayerCharacter** | Assets/Codes/Multiplayer | Game character controller & movement sync |
| **GameManager** | Assets/Codes/Multiplayer | Game state management |
| **GameStartController** | Assets/Codes/Multiplayer | Auto-start when 4 players ready |
| **CameraFollow** | Assets/Codes/Multiplayer | Smooth camera following |
| **MultiplayerConfig** | Assets/Codes/Multiplayer | Optional - centralized config |

---

## 📞 Hỗ Trợ & Resources

- **Photon Docs**: https://doc.photonengine.com/pun/v2/
- **Unity Docs**: https://docs.unity3d.com
- **Input System**: https://docs.unity3d.com/Packages/com.unity.inputsystem@latest

---

## ✅ Completion Checklist

- [ ] Photon AppId configured
- [ ] LobbyScene created with RoomPanel & CharacterSelectPanel
- [ ] LobbyPlayer prefab created & saved
- [ ] MultiplayerCharacter prefab created & saved
- [ ] GameScene created with GameSetup & spawn points
- [ ] Build Settings has both scenes (LobbyScene=0, GameScene=1)
- [ ] Solo test passed (room select → character select → movement)
- [ ] 2-player test passed (movement sync & auto-start)
- [ ] 4-player test passed (all players auto-start game)
- [ ] No compilation errors
- [ ] Console shows expected logs

---

**Status**: ✅ Ready for multiplayer testing!

Simplified Version (Room Select → Character Select → Auto-Start)  
Last Updated: 2026
