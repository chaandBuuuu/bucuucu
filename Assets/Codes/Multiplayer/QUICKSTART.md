# Quick Start Guide - Hệ Thống Multiplayer 4 Người

🎮 **Hệ thống multiplayer kinh dí 4 người** - Từng bước cài đặt nhanh chóng

## 🚀 Cài Đặt Nhanh (5 Phút)

### 1️⃣ Cài Đặt Photon 2

```
Asset Store > Search "PUN 2" > Import > Setup > Paste AppId
```

### 2️⃣ Tạo 2 Scene

| Scene | Tên File | Mục Đích |
|-------|----------|---------|
| Lobby | `LobbyScene.unity` | Đăng nhập, chọn nhân vật |
| Game | `GameScene.unity` | Chơi game chính |

### 3️⃣ Tạo Character Prefab

```
1. Tạo GameObject (Sprite, Rigidbody2D, PlayerInput)
2. Thêm PhotonView + MultiplayerCharacter
3. Drag vào Assets/Prefabs/MultiplayerCharacter.prefab
```

### 4️⃣ Setup LobbyScene

```
Canvas
├── PhotonNetworkManager (script)
├── GameLobbyUI (script)
└── Game Start Controller (script)
```

### 5️⃣ Setup GameScene

```
Empty GameObject "GameSetup"
├── PhotonView
├── PlayerSpawner (script)
├── GameManager (script)
└── Camera with CameraFollow (script)
```

## ⚙️ Cài Đặt Quan Trọng

### PhotonNetworkManager Inspector

```
■ Game Version: "1.0"
■ Max Players Per Room: 4
■ Auto Connect: ✓
```

### MultiplayerCharacter Inspector

```
■ Move Speed: 5
■ Acceleration: 15
■ Deceleration: 20
■ Face Movement Direction: ✓
```

### PlayerSpawner Inspector

```
■ Spawn Points: [(−5,0), (5,0), (−5,5), (5,5)]
■ Player Prefab: "Prefabs/MultiplayerCharacter"
```

## 🎯 Flow Gameplay

```
1. Player 1 nhấn CONNECT
   ↓
2. Player 1 nhấn HOST (tạo room)
   ↓
3. Player 2,3,4 nhấn JOIN RANDOM
   ↓
4. Tất cả chọn CHARACTER → nhấn READY
   ↓
5. Khi tất cả 4 người sẵn sàng → Tự động vào GameScene
   ↓
6. Game bắt đầu
```

## 🔍 Kiểm Tra

### Debug Console Logs

```csharp
// Nếu thấy các log này là OK:
[NetworkManager] Kết nối thành công tới Photon!
[LobbyUI] Vào room thành công!
[GameStartController] Tất cả 4 người đã sẵn sàng!
[PlayerSpawner] Spawned Player_1 at (-5, 0, 0)
[GameManager] Game bắt đầu!
```

### Kiểm Tra Multiplayer

```
1. Chạy trong Unity Editor (Player 1 - Host)
2. Build & Run (Player 2 - Client)
3. Cả 2 phải thấy nhau di chuyển realtime
```

## ❌ Các Lỗi Phổ Biến

| Lỗi | Nguyên Nhân | Giải Pháp |
|-----|------------|----------|
| "Chưa kết nối" | Photon AppId sai | Kiểm tra Project Settings > Photon |
| "Không join được room" | Room đã đủ 4 người | Chọn room khác |
| "Player không xuất hiện" | Prefab path sai | Kiểm tra `Assets/Prefabs/MultiplayerCharacter.prefab` |
| "Movement bị lag" | Network rate thấp | Tăng SendRate = 60 |
| "Game không bắt đầu" | Chưa đủ 4 người sẵn sàng | Chờ tất cả nhấn Ready |

## 📊 Performance Tips

```csharp
// Trong PhotonNetworkManager.Start()

// Tối ưu network
PhotonNetwork.SendRate = 60;           // 60 msg/sec
PhotonNetwork.SerializationRate = 60;  // 60 updates/sec

// Giảm bandwidth - chỉ sync cần thiết
// Vị trí, rotation, animation (không tất cả frame)
```

## 🎮 Controls

```
WASD       → Di chuyển
Arrow Keys → Di chuyển (alternative)
P          → Tạm dừng/tiếp tục
ESC        → Quay lại lobby
```

## 📱 Build & Deploy

### Android
```bash
File > Build Settings
- Select Android
- Build APK
- Chạy trên 2+ điện thoại
```

### Windows PC
```bash
File > Build Settings
- Select Windows Standalone
- Build EXE
- Chạy nhiều instance
```

### WebGL
```bash
File > Build Settings
- Select WebGL
- Build → mở trên trình duyệt khác nhau
```

## 💡 Customization

### Thay Đổi Số Lượng Player

Trong `PhotonNetworkManager.cs`:
```csharp
maxPlayersPerRoom = 6; // 6 người thay vì 4
```

### Thêm Nhân Vật Mới

Trong `MultiplayerConfig`:
```csharp
characterNames = new[] { "Hacker", "Hunter", "Priest", "Scientist", "Warrior" };
```

### Tùy Chỉnh Spawn Points

Trong `PlayerSpawner.cs`:
```csharp
spawnPoints = new Vector3[]
{
    new Vector3(-10, -10, 0),
    new Vector3(10, -10, 0),
    new Vector3(-10, 10, 0),
    new Vector3(10, 10, 0)
};
```

## 📞 Support

- 🔗 Photon Docs: https://doc.photonengine.com/en-us/pun2/
- 📚 Unity Docs: https://docs.unity3d.com/
- 🎓 Video Tutorials: YouTube "Photon PUN2 Tutorial"

---

**✅ Nếu tất cả các bước hoàn thành, bạn đã có sẵn game multiplayer hoạt động!**
