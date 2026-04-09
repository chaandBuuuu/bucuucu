# 🏎️ MARIO KART RACING GAME - NEW GAMEPLAY SYSTEM

## 🎮 Overview

Thay thế toàn bộ Hunt vs Survivor system bằng Mario Kart Racing 2D.

**Game Mechanics:**
- 4 players tham gia cuộc đua
- Win condition: Hoàn thành 4 laps
- Powerups trên đường: Shield, Gun, Speed Boost, Trap
- Controls: WASD để chạy, Shift để drift, Q để dùng powerup
- Physics: Có cơ chế "đà" (momentum/inertia)

---

## 📁 New Files Created

**Core Racing System:**
- ✅ `CarController.cs` - Xe + physics + lap tracking
- ✅ `RaceManager.cs` - Quản lý trạng thái race, lap, kết thắc
- ✅ `FinishLineDetector.cs` - Detect khi xe qua finish line

**Powerup System:**
- ✅ `PowerupInventory.cs` - Quản lý powerups của xe
- ✅ `PowerupPickup.cs` - Item powerup trên đường
- ✅ `BulletProjectile.cs` - Đạn từ gun
- ✅ `TrapObject.cs` - Cái bẫy làm chậm

**UI:**
- ✅ `RaceUI.cs` - Hiển thị lap, timer, speed, powerup

**Input:**
- ✅ Updated `NetworkInputData.cs` - Thêm drift + powerup input
- ✅ Updated `InputHandler.cs` - WASD + Shift + Q

---

## 🎯 Game Mechanics Detail

### 1. Movement & Drift (CarController.cs)

```
WASD:        Điều khiển hướng + tăng tốc
Shift:       Kích hoạt drift (xoay cua nhanh hơn + rẻ tệp hơn)
Friction:    95% mỗi frame (khi không ấn, chậm dần)
MaxSpeed:    15 units/second
```

**Physics:**
- Momentum-based movement
- Acceleration builds up speed
- Friction naturally slows down
- Drift tăng rotation speed nhưng giảm top speed

### 2. Lap & Race Finish

```
FinishLine → Trigger
           → Register lap (CarController.RPC_CompleteLap())
           → RaceManager.RegisterLapCompletion()
           → If lap == 4 → Winner!
```

### 3. Powerups

**Shield (3 seconds):**
- Tạo khiên bảo vệ
- Chặn tất cả damage/slow
- Visual: Xe tô màu xanh lá

**Gun:**
- Tìm xe phía trước gần nhất
- Bắn đạn hướng tới xe đó
- Hit → làm chậm target 50% trong 3 giây
- Hotkey: Q

**Speed Boost:**
- Tăng max speed trong 5 giây
- Tự động reset

**Trap:**
- Đặt bẫy tại vị trí hiện tại
- Bất kỳ xe nào qua bị làm chậm 60% trong 3 giây
- Trap tồn tại ~15 giây rồi tự hủy

---

## 🛠️ How to Setup Scene

### Step 1: Create Racing Track

```
Scene: Assets/Scenes/RacingTrack.unity (hoặc tên khác)

Hierarchy:
├── Track (Sprite - hình đường đua)
├── FinishLine (Trigger BoxCollider2D)
│   └── Script: FinishLineDetector
├── RaceManager (Empty)
│   └── Script: RaceManager
├── PowerupSpawns (Empty parent)
│   ├── Spawn1-4 (PowerupPickup)
│   │   └── Script: PowerupPickup
│   │   └── PowerupType: [Shield/Gun/Speed/Trap]
├── CarSpawns (Empty parent)
│   ├── Spawn1-4 (Empty)
│   └── (Store spawn positions)
├── Canvas (UI)
│   └── Script: RaceUI
│   └── Children: LapText, TimerText, SpeedText, PowerupText
└── NetworkRunner (Photon)
```

### Step 2: Create Car Prefab

```
Prefab: Assets/Resources/Prefabs/Car.prefab

Structure:
Car (GameObject)
├── Sprite Renderer (show car sprite)
├── Rigidbody2D (Dynamic, GravityScale=0)
├── BoxCollider2D (collision)
├── NetworkObject (for Fusion)
├── NetworkTransform (sync position)
├── CarController.cs
│   ├── Max Speed: 15
│   ├── Acceleration: 8
│   └── Rotation Speed: 180
├── PowerupInventory.cs
└── (Optional) SpriteAnimator for wheels
```

### Step 3: Spawn Cars

```
Use LobbySpawner or custom spawner:
- Max 4 players
- Each gets Car prefab spawned at Spawn point
- Assign CarController to player input
```

### Step 4: Setup Powerup Pickups 

```
Place PowerupPickup around track:
- 4 Powerup items
- Mix of types: 1 Shield, 1 Gun, 1 Speed, 1 Trap
- Configure PowerupType on each
- Respawn time: 10 seconds
```

### Step 5: Connect RaceManager

```
RaceManager (in scene):
├── Lap To Win: 4
├── Finish Line: Assign FinishLine object
└── Checkpoints: (optional checkpoint array)
```

---

## 🎮 Player Controls

```
W/A/S/D      Move forward/left/back/right
Shift        Hold to drift (faster turn, slower acceleration)
Q            Use current powerup
P            Pause (if implemented)
```

---

## 📊 Game Flow

```
1. 4 players join lobby
   ↓
2. Load RacingTrack scene
   ↓
3. Spawn cars at spawn points
   ↓
4. RaceManager.RPC_StartRace()
   ↓
5. Players drive & collect powerups
   ↓
6. Complete laps → Register with FinishLineDetector
   ↓
7. First to 4 laps wins!
   ↓
8. RaceUI shows winner
   ↓
9. Return to lobby or restart
```

---

## 🔧 Key Files to Modify/Check

**GameplayStateManager.cs** (Optional - can keep for other logic)
- Now mostly unused for racing
- Can be repurposed or left as-is

**InputHandler.cs** ✅ UPDATED
- Now captures WASD + Shift + Q
- Sends NetworkInputData to CarController

**NetworkCharacterController.cs** (Legacy - not used in racing)
- Keep as-is for now
- Can remove later

**FusionNetworkManager.cs**
- Should spawn Car prefab instead of character prefab
- Update spawn logic

---

## 💡 Advanced Features (Optional)

**Can add later:**
- Lap split times
- Position ranking display
- Sound effects for powerups
- Better drift feel (angle correction)
- Respawn after falling off track
- Shortcuts/secret routes
- Leaderboard

---

## 🐛 Testing Checklist

- [ ] Car moves with WASD
- [ ] Drift works with Shift (turns faster)
- [ ] Can pick up powerups
- [ ] Q uses powerup
- [ ] Shield blocks attacks / creates visual
- [ ] Gun finds & hits nearest car
- [ ] Speed boost works
- [ ] Trap slows down cars
- [ ] Lap counter increments
- [ ] Finish line detect triggers
- [ ] First to 4 laps wins
- [ ] End screen shows winner

---

## 📁 File Reference

```
Assets/Codes/Gameplay/
├── CarController.cs         ← Main vehicle logic
├── RaceManager.cs           ← Race state & lap tracking
├── PowerupInventory.cs      ← Powerup management
├── BulletProjectile.cs      ← Gun projectile
├── TrapObject.cs            ← Trap slowdown
├── PowerupPickup.cs         ← Powerup item
├── FinishLineDetector.cs    ← Lap detection
├── RaceUI.cs                ← UI display
└── RACING_SETUP.md          ← This file

Assets/Codes/Multiplayer/
├── InputHandler.cs          ← Input polling (UPDATED)
└── NetworkInputData.cs      ← Input struct (UPDATED)

Assets/Resources/Prefabs/
├── Car.prefab               ← Vehicle prefab (CREATE)
├── Bullet.prefab            ← Bullet (CREATE)
└── Trap.prefab              ← Trap (CREATE)
```

---

**Status:** ✅ Racing game system ready for scene setup!

Next: Create racing track scene and test!
