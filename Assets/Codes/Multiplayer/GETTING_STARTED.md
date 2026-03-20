# 🎬 Getting Started - Bắt Đầu Ngay

Hướng dẫn 10 bước để có hệ thống multiplayer hoạt động trong 1 tiếng.

## ⏱️ Thời Gian: ~1 Tiếng

## 📖 Bước 1: Cài PUN2 (10 phút)

1. Mở **Asset Store** trong Unity
2. Tìm **"PUN 2"** (Photon Cloud)
3. Nhấn **Import** vào project
4. Chọn **"All"** → **Import**

## 🔑 Bước 2: Setup Photon AppId (5 phút)

1. Vào https://www.photonengine.com
2. Sign up miễn phí
3. Tạo **New Application**
4. Copy **AppId**
5. Trong Unity: **Edit > Project Settings > Photon**
6. Dán AppId vào ô **AppId**

## 📁 Bước 3: Tạo 2 Scene (5 phút)

### Scene 1: LobbyScene
```
File > New Scene > Rename "LobbyScene" > Save
```

### Scene 2: GameScene
```
File > New Scene > Rename "GameScene" > Save
```

## 🎮 Bước 4: Tạo Character Prefab (10 phút)

### Tạo GameObject
```
Right Click > Create Empty
Rename "MultiplayerCharacter"
```

### Thêm Components
```
1️⃣ SpriteRenderer
2️⃣ Rigidbody2D (Gravity Scale = 0)
3️⃣ PlayerInput (From Input System)
4️⃣ Animator (Optional - có animation)
5️⃣ PhotonView ← QUAN TRỌNG!
6️⃣ MultiplayerCharacter script
```

### Cấu Hình PhotonView
```
Ownership: "Takeover"
Observed Components: [MultiplayerCharacter]
```

### Lưu Prefab
```
Drag vào Assets/Prefabs/MultiplayerCharacter.prefab
```

## 🏠 Bước 5: Setup LobbyScene (15 phút)

### Tạo Canvas
```
Right Click > UI > Canvas > Rename "LobbyCanvas"
```

### Thêm UI Components

Trong Canvas, tạo:
```
Text: "Login"
InputField: "Nickname" → tên id: "NicknameInput"
Button: "Connect" → id: "ConnectButton"
Button: "Host" → id: "HostButton"
Button: "Join Random" → id: "JoinRandomButton"
InputField: "Room Name" → id: "RoomNameInput"
Button: "Join" → id: "JoinButton"
4x Button: "Character 1-4" → id: "CharacterButton1-4"
Text: Selected Character → id: "SelectedCharacterText"
Button: "Ready" → id: "ReadyButton"
Text: Room Info → id: "RoomInfoText"
Text: Status → id: "StatusText"
```

### Tạo Network Manager
```
Right Click > Create Empty > "NetworkSetup"
Add Component > PhotonNetworkManager
Gán UI references vào Inspector
```

### Tạo UI Manager
```
Right Click > Create Empty > "LobbyUIManager"
Add Component > GameLobbyUI
Gán tất cả UI elements vào Inspector
```

## 🎯 Bước 6: Setup GameScene (15 phút)

### Tạo Game Setup
```
Right Click > Create Empty > "GameSetup"
Add: PhotonView, PlayerSpawner, GameManager, GameStartController
```

### Cấu Hình PhotonView
```
View ID: 1
Ownership: Scene
```

### Cấu Hình PlayerSpawner
```
Player Prefab Name: "Prefabs/MultiplayerCharacter"
Spawn Points:
  [0]: (-5, 0, 0)
  [1]: (5, 0, 0)
  [2]: (-5, 5, 0)
  [3]: (5, 5, 0)
```

### Tạo Game Canvas
```
Right Click > UI > Canvas
Add:
  - Text: "Players: 4/4" → id: "PlayerCountText"
  - Text: "Game Status" → id: "GameStatusText"
  - Text: "Ready Status" → id: "ReadyStatusText"

Gán vào GameManager
```

### Cấu Hình Camera
```
Select Main Camera
Add Component > CameraFollow
```

## ⚙️ Bước 7: Build Settings (5 phút)

```
File > Build Settings
Drag + Drop:
  - LobbyScene → Index 0
  - GameScene → Index 1

Platform: Windows (hoặc Android/WebGL)
Press "Build" hoặc "Build and Run"
```

## 🧪 Bước 8: Test Solo Player (5 phút)

### Test 1: Start
```
1. Bấm Play (Unity Editor)
2. Nhập name, bấm Connect
3. Bấm Host (tạo room)
4. Chọn character, bấm Ready
5. Xem console log (mong muốn: "Waiting for X players")

Kết quả: Game KHÔNG start (chỉ 1 người, cần 4)
✓ Đúng!
```

## 🎮 Bước 9: Test 4 Players (20 phút)

### Setup
```
1 Editor + 3 Builds (hoặc 4 Builds)
```

### Run Script
```bash
# Terminal
cd "C:\Program Files\Unity\Hub\Editor\2022.3.0f1\Editor"
Unity -projectPath "D:\unity\bucuucu" -buildWindows64Player "D:\builds\build4.exe" -quit -batchmode
```

Hoặc Manual:
```
1. File > Build Settings > Build & Run (Player 2)
2. Repeat (Player 3)
3. Repeat (Player 4)
```

### Test Flow
```
Player 1 (Editor):
  ✓ Connect
  ✓ Host Room
  ✓ Select Character
  ✓ Ready

Player 2 (Build):
  ✓ Connect
  ✓ Join Random
  ✓ Select Character
  ✓ Ready

Player 3 (Build):
  ✓ Connect
  ✓ Join Random
  ✓ Select Character
  ✓ Ready

Player 4 (Build):
  ✓ Connect
  ✓ Join Random
  ✓ Select Character
  ✓ Ready

Kết quả: Scene auto-change → GameScene
✓ All 4 players visible
✓ Can move (WASD)
✓ See others move realtime
```

## ✅ Bước 10: Verify Success (5 phút)

Nếu thấy các dấu hiệu này trong Console:

```
[NetworkManager] Kết nối thành công tới Photon!
[NetworkManager] Tạo Room: Room_XXXX
[LobbyUI] Vào room thành công!
[GameStartController] Chọn nhân vật: Hacker
[GameStartController] Tất cả 4 người đã sẵn sàng!
[GameStartController] RPC: Bắt đầu game!
[PlayerSpawner] Spawned Player1 at (-5, 0, 0)
[PlayerSpawner] Spawned Player2 at (5, 0, 0)
[PlayerSpawner] Spawned Player3 at (-5, 5, 0)
[PlayerSpawner] Spawned Player4 at (5, 5, 0)
[GameManager] Game bắt đầu!
```

**Nếu thấy 11 log này → ✅ THÀNH CÔNG!**

---

## 🎮 Tiếp Theo: Develop Game

Bây giờ bạn có hệ thống multiplayer hoạt động. Tiếp theo:

1. **Tạo Enemy/NPC** (không multiplayer)
2. **Thêm Gameplay** (hộp cơ khí, ghiệc, sợ hãi)
3. **Tối ưu hóa** (graphics, performance)
4. **Deploy** (publish trên Steam, AppStore, etc)

---

## 🆘 Lỗi Phổ Biến & Fix

### ❌ "Photon not connected"
```
Fix: Kiểm tra AppId trong Project Settings > Photon
```

### ❌ "Prefab not found"
```
Fix: Kiểm tra path "Prefabs/MultiplayerCharacter"
     (phải là thư mục d:\unity\bucuucu\Assets\Prefabs\)
```

### ❌ "Game không auto-start"
```
Fix: Kiểm tra 4 người đã sẵn sàng chưa
     (bấm Ready của mỗi người)
```

### ❌ "Player không xuất hiện"
```
Fix: PhotonView trong GameScene "GameSetup" phải có View ID = 1
     PhotonView trong Prefab phải có Observer = MultiplayerCharacter
```

### ❌ "Lag/Delay cao"
```
Fix: PhotonNetworkManager.cs
     SendRate = 60
     SerializationRate = 60
```

---

## 📚 Tiếp Theo: Tìm Hiểu Thêm

Sau khi hoàn thành, đọc:

1. **SETUP_GUIDE.md** - Chi tiết toàn bộ quá trình
2. **ARCHITECTURE.md** - Design patterns & optimization
3. **NETWORK_OPTIMIZATION.md** - Giảm lag, tối ưu bandwidth

---

## 🎉 Chúc Mừng!

**Bạn đã tạo game multiplayer 4 người hoàn chỉnh! 🎮**

Tiếp theo là:
- Thêm gameplay logic
- Tối ưu visual
- Deploy lên các platform

Hãy enjoy! 🚀
