# 🚀 AUTO SETUP - DEVOUR 2D GAMEPLAY

## Cách Sử Dụng Auto Setup Scripts

Có **2 cách** tự động setup toàn bộ hệ thống:

---

## 🎯 **Cách 1: Quick Setup (Recommended) - 4 bước**

Từ Unity Editor menu:

### **Bước 1**: Tạo Character Database
```
Devour/Quick Setup/1. Create Character Database
```
- ✅ Tạo `Assets/Resources/CharacterDatabase.asset`
- Chứa cấu hình 6 characters

### **Bước 2**: Tạo Character Prefabs
```
Devour/Quick Setup/2. Create Character Prefabs
```
- ✅ Tạo 6 prefabs trong `Assets/Resources/Prefabs/Characters/`
- Hunt1, Hunt2, Survival1-4

### **Bước 3**: Add Managers to Scene
```
Devour/Quick Setup/3. Add Managers to Scene
```
- ✅ Thêm GameplayStateManager, GameStartController, etc
- ✅ Tạo 4 spawn points

### **Bước 4**: Setup UI Canvas
```
Devour/Quick Setup/4. Setup UI Canvas
```
- ✅ Tạo Canvas + UI managers
- ✅ Ready to play! 🎮

---

## ⚡ **Cách 2: All-in-One Setup (Fastest) - 1 bước**

Từ Unity Editor menu:

```
Devour/Quick Setup/All-in-One Setup
```

**Tự động chạy tất cả 4 bước trên cùng lúc!**

---

## 📋 Điều Kiện Trước Khi Setup

1. ✅ Copy tất cả files từ `Assets/Codes/Gameplay/` vào project
2. ✅ Compile thành công (không có errors)
3. ✅ Có 1 scene sẵn sàng (hoặc tạo mới)

---

## 🔍 Kiểm Tra Sau Setup

### Bước 1: Kiểm Tra Assets
```
Assets/Resources/
├── CharacterDatabase.asset ✅
└── Prefabs/Characters/
    ├── Hunt1_Character.prefab
    ├── Hunt2_Character.prefab
    ├── Survival1_Character.prefab
    ├── Survival2_Character.prefab
    ├── Survival3_Character.prefab
    └── Survival4_Character.prefab
```

### Bước 2: Kiểm Tra Scene (Hierarchy)
```
Scene Objects:
├── GameplayStateManager ✅
├── GameStartController ✅
├── CharacterSpawner ✅
├── WoodSystem ✅
├── GameplayNetworkManager ✅
├── Spawners (folder)
│   ├── Spawn_0
│   ├── Spawn_1
│   ├── Spawn_2
│   └── Spawn_3
└── Canvas ✅
    ├── GameplayPanel
    └── GameEndPanel
```

### Bước 3: Kiểm Tra Console
```
[Setup] Creating CharacterDatabase...
✅ [Setup] CharacterDatabase created

[Setup] Creating character prefabs...
[Setup] Created prefab: Hunt1
[Setup] Created prefab: Hunt2
[Setup] Created prefab: Survival1
[Setup] Created prefab: Survival2
[Setup] Created prefab: Survival3
[Setup] Created prefab: Survival4
✅ [Setup] Character prefabs created

[Setup] Adding managers to scene...
✅ [Setup] Managers added to scene

[Setup] Setting up UI Canvas...
✅ [Setup] UI Canvas created
```

---

## 🎮 Bắt Đầu Chơi

1. **Open gameplay scene**
2. **Press Play** ▶️
3. **Network runs with 4 players** (hoặc dùng Network Simulator)
4. Mỗi player chọn character
5. Game tự động phân bổ: 1 Hunter + 3 Survivors
6. **LET'S GO!** 🚀

---

## ⚙️ Manual Config (Optional)

### Nếu cần adjust values:

**CharacterDatabase:**
- Game → GameplayStateManager → Adjust bonfire count, wood per bonfire

**Spawn Positions:**
Đổi position từ Spawners → Spawn_0 (Hunter), Spawn_1-3 (Survivors)

**Character Stats:**
Mở prefab → NetworkCharacterController inspector → Adjust maxHealth, baseSpeed

---

## 🆘 Nếu Có Lỗi

### Console Error: "PlayTimeStuckWarning"
- ✅ Normal warning, không ảnh hưởng gameplay

### Prefabs không load
- ❌ Kiểm tra path: `Assets/Resources/Prefabs/Characters/`
- Đảm bảo tên file đúng (Hunt1_Character, không có spaces)

### Network error sau setup
- ❌ Kiểm tra Scene có GameplayNetworkManager không
- ❌ Kiểm tra FusionNetworkManager đã initialize không

### UI không hiển thị
- ❌ Kiểm tra Canvas > RenderMode = ScreenSpaceOverlay
- ❌ Kiểm tra GameplayUIManager được Add vào Canvas

---

## 📞 Quick Reference

| Cần | Làm |
|-----|-----|
| Tạo Database | `Devour/Quick Setup/1. Create Character Database` |
| Tạo Prefabs | `Devour/Quick Setup/2. Create Character Prefabs` |
| Add Managers | `Devour/Quick Setup/3. Add Managers to Scene` |
| Setup UI | `Devour/Quick Setup/4. Setup UI Canvas` |
| Làm Hết 1 Lần | `Devour/Quick Setup/All-in-One Setup` |

---

## ✨ Tất Cả Xong!

- ✅ 8 characters ready
- ✅ Abilities configured
- ✅ Game state tracking
- ✅ UI setup
- ✅ Network ready

**Bây giờ bạn có thể:**
1. Add sprites/animations
2. Customize abilities
3. Balance stats
4. Add sound effects
5. Deploy game!

---

**Setup Time**: < 5 minutes  
**Difficulty**: ⭐ (Very Easy)  
**Status**: ✅ READY TO PLAY

Enjoy your Devour 2D game! 🎮
