# Hệ Thống Gameplay Devour 2D - Hướng Dẫn Triển Khai

## 📋 Tổng Quan

Hệ thống này cung cấp một gameplay hoàn chỉnh cho trò chơi Devour 2D với:
- **8 nhân vật**: 2 Hunter + 4 Survivor (mỗi Survivor có 2 phiên bản)
- **Hệ thống Ability**: E, R, F skills cho mỗi nhân vật
- **Hệ thống Status Effect**: Slowness, Stun, Swiftness, True Sight, Blindness, Captain Black
- **Hệ thống Mục Tiêu**: Nhặt gỗ, đốt lửa trại, thoát chạy
- **Win Conditions**: Hunter tiêu diệt toàn bộ hoặc Survivor thoát chạy

## 🏗️ Kiến Trúc Hệ Thống

### 1. **Character System**
- `CharacterRole.cs`: Định nghĩa vai trò (Hunter/Survivor) và nhân vật
- `CharacterDatabase`: ScriptableObject chứa cấu hình tất cả 8 nhân vật

### 2. **Core Character Controller**
- `NetworkCharacterController.cs`: Base controller cho tất cả characters với:
  - Health management
  - Status effect tracking
  - Ability execution
  - Input handling

### 3. **Status Effects & Abilities**
- `StatusEffect.cs`: Hệ thống debuff/buff (Slowness, Stun, etc.)
- `AbilitySystem.cs`: Framework cho abilities (E, R, F)
- `Hunt1Abilities.cs` / `Hunt2Abilities.cs`: Abilities của các Hunter
- `SurvivalAbilities.cs`: Abilities của 4 Survivor

### 4. **Game State Management**
- `GameplayStateManager.cs`: Quản lý trạng thái game (phase, win condition)
- `WoodAndBonfireSystem.cs`: Hệ thống gỗ và lửa trại
- `GameStartController.cs`: Phân bổ role (Hunter/Survivor) cho player

### 5. **UI System**
- `GameplayUI.cs`: Health, status effects, objective UI
- Character selection UI
- Game end UI

## 📁 File Structure

```
Assets/Codes/Gameplay/
├── CharacterRole.cs
├── StatusEffect.cs
├── AbilitySystem.cs
├── NetworkCharacterController.cs
├── Hunt1Abilities.cs
├── Hunt2Abilities.cs
├── SurvivalAbilities.cs
├── GameplayStateManager.cs
├── WoodAndBonfireSystem.cs
├── GameStartController.cs
├── InputHandling.cs
├── GameplayUI.cs
└── GameSetupWizard.cs
```

## 🚀 Các Bước Triển Khai

### Bước 1: Setup Character Database

```csharp
// Chạy từ Unity Editor menu: Devour/Setup/Create Character Database
// Hoặc tạo thủ công ScriptableObject từ CharacterDatabase class
```

Đặt file tại: `Assets/Resources/CharacterDatabase.asset`

### Bước 2: Tạo Character Prefabs

Tạo 6 prefabs cho mỗi nhân vật trong `Assets/Resources/Prefabs/Characters/`:

**Cấu trúc Prefab:**
```
Hunt1_Character (Prefab)
├── Rigidbody2D (Dynamic, GravityScale=0)
├── SpriteRenderer
├── Animator
├── NetworkTransform
├── NetworkCharacterController
├── AbilityManager
│   ├── Hunt1AbilityE (script)
│   ├── Hunt1AbilityR (script)
│   └── Hunt1AbilityF (script)
├── StatusEffectManager
└── Hunt1Passive (script)
```

Thực hiện tương tự cho các nhân vật khác.

### Bước 3: Tích Hợp với Network Manager

Thêm các component này vào scene:

1. **GameplayStateManager**: Singleton quản lý trạng thái game
2. **GameStartController**: Quản lý bắt đầu game
3. **CharacterSpawner**: Network spawner cho characters
4. **WoodSystem**: Quản lý gỗ
5. **GameplayUIManager**: Quản lý UI

### Bước 4: Kết Nối với Existing Network

```csharp
// Trong FusionNetworkManager
public override void OnPlayerJoined(PlayerRef player)
{
    base.OnPlayerJoined(player);
    
    // Game sẽ tự động xử lý character selection
    // GameStartController sẽ phân bổ role khi tất cả ready
}
```

## 💡 Hướng Dẫn Sử Dụng Key Features

### Thêm Status Effect

```csharp
var controller = GetComponent<NetworkCharacterController>();
controller.RPC_AddStatusEffect(StatusEffectType.Slowness, 3f, 0.5f);
// 3 giây slowness với magnitude 0.5 (50% speed reduction)
```

### Gây Damage

```csharp
controller.RPC_TakeDamage(25f);
// Damage sẽ tính toán Captain Black reduction tự động
```

### Execute Ability

```csharp
var abilityManager = GetComponent<AbilityManager>();
abilityManager.TryExecuteAbility("AbilityE");
```

### Kiểm Tra Status Effect

```csharp
var statusManager = controller.GetStatusEffectManager();
if (statusManager.HasEffect(StatusEffectType.Stun))
{
    // Nhân vật bị stun
}
```

## 🎮 Gameplay Flow

```
1. Lobby
   ↓
2. Character Selection (Mỗi player chọn nhân vật)
   ↓
3. GameStartController phân bổ:
   - 1 người làm Hunter
   - 3 người làm Survivor
   ↓
4. Spawn characters tại spawn points
   ↓
5. Gameplay:
   - HUNTER: Tìm và tiêu diệt toàn bộ survivor
   - SURVIVOR: Nhặt gỗ, đốt 4 lửa trại, trốn thoát
   ↓
6. Win Condition:
   - HUNTER WIN: Tất cả survivor chết
   - SURVIVOR WIN: Tất cả lửa trại đốt + ≥1 người trốn thoát
   ↓
7. Show Game End UI + Trở về Lobby
```

## ⚙️ Configuration Tweaking

### Health Values
```csharp
// NetworkCharacterController.cs
[SerializeField] private float maxHealth = 100f;
```

### Ability Cooldowns & Magnitude
```csharp
// Trong mỗi Ability script:
[SerializeField] private float cooldown = 5f;
[SerializeField] private float damageAmount = 25f;
```

### Status Effect Duration & Magnitude
```csharp
// Thêm effect
controller.RPC_AddStatusEffect(StatusEffectType.Slowness, 5f, 0.3f);
//                                                         duration  magnitude
```

### Bonfire Requirements
```csharp
// GameplayStateManager.cs
[SerializeField] private int totalBonfires = 4;
[SerializeField] private int woodPerBonfire = 5;
```

## 🔧 Troubleshooting

### Character từ không spawn
- Kiểm tra prefab path trong `CharacterSpawner.cs`
- Đảm bảo prefabs nằm trong `Assets/Resources/Prefabs/Characters/`

### Status effects không apply
- Kiểm tra server authority: `RPC_AddStatusEffect` phải chạy trên state authority
- Verify `StatusEffectManager` đã được add vào character

### Abilities không thực thi
- Kiểm tra cooldown: `GetCooldownRemaining() > 0`
- Verify input đã được gọi: `InputHandler.GetNetworkInput()`

### Win condition chưa trigger
- Kiểm tra toàn bộ survivor đã dead hoặc không
- Verify `GameplayStateManager.CheckWinConditions()` được gọi

## 📊 Performance Optimization

- **RPC Calls**: Sử dụng networking hiệu quả cho damage/effects
- **Physics**: Rigidbody2D set thành Kinematic cho remote players
- **UI Updates**: Update tách riêng từ network FixedUpdateNetwork
- **Ability Cooldown**: Tracked locally, không cần network sync

## 🎯 Tiếp Theo

1. **Tạo Visual Effects**: Particles/animations cho abilities
2. **Audio System**: SFX cho abilities, status effects
3. **Balance Testing**: Adjust damage, cooldowns dựa trên playtest
4. **Advanced Features**:
   - Terrain interaction (walls, obstacles)
   - Camera shake, screen effects
   - Leaderboard, stats tracking
   - Cosmetics/skins

---

**Lưu ý**: Hệ thống này sử dụng Photon Fusion 2 cho networking. Tất cả `[Networked]` properties và `[Rpc]` calls phải tuân theo Fusion architecture.
