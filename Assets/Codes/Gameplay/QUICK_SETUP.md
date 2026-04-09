# ⚡ Quick Setup Checklist - Devour 2D Gameplay

## 🎯 Tất Cả Các Bước Setup (15-30 phút)

### ✅ Phase 1: Core System Setup (5 min)

- [ ] Copy toàn bộ files từ `Assets/Codes/Gameplay/` vào project
- [ ] Verify imports không có errors
- [ ] Kiểm tra `CharacterRole.cs` compile successfully

### ✅ Phase 2: Create Prefabs (10 min)

Tạo 6 prefabs (hoặc sử dụng template):

#### Hunt #1 Prefab:
```
GameObject: Hunt1_Character
  ├── Rigidbody2D
  │   └── Body Type: Dynamic
  │   └── Gravity Scale: 0
  │   └── Constraints: Freeze Rotation Z
  ├── CircleCollider2D (radius: 0.5)
  ├── SpriteRenderer
  ├── Animator (với animation clips)
  ├── NetworkObject
  ├── NetworkTransform
  ├── NetworkCharacterController
  │   └── Character ID: Hunt1
  │   └── Base Speed: 4.5
  │   └── Max Health: 100
  ├── AbilityManager
  ├── StatusEffectManager
  ├── Hunt1AbilityE (script)
  ├── Hunt1AbilityR (script)
  ├── Hunt1AbilityF (script)
  └── Hunt1Passive (script)
```

**Repeat tương tự cho các nhân vật khác** (Hunt2, Survival1-4)

### ✅ Phase 3: Scene Setup (5 min)

1. **Tạo scene gameplay mới** (hoặc modify existing scene)

2. **Add các objects:**
   ```
   Hierarchy:
   ├── GameplayStateManager (empty GO + script)
   ├── GameStartController (empty GO + script)
   ├── CharacterSpawner (empty GO + script)
   ├── WoodSystem (empty GO + script)
   ├── Canvas (UI Root)
   │   ├── GameplayUIManager (canvas + script)
   │   ├── GameEndUIManager (canvas + script)
   │   └── ... UI Elements ...
   └── Spawners (empty GO with child transform for each spawn point)
       ├── Spawn_0
       ├── Spawn_1
       ├── Spawn_2
       └── Spawn_3
   ```

3. **Kết nối References:**
   - GameplayStateManager → Bonfire Spawns array
   - GameStartController → Spawner Positions array
   - CharacterSpawner → Thêm vào scene

### ✅ Phase 4: Integration (5 min)

1. **Modify FusionNetworkManager:**
```csharp
public class FusionNetworkManager : MonoBehaviour
{
    private void Awake()
    {
        // ... existing code ...
        InitializeGameplaySystem();  // ADD THIS
    }
    
    private void InitializeGameplaySystem()  // ADD THIS METHOD
    {
        var gameplayNetManager = FindObjectOfType<GameplayNetworkManager>();
        if (gameplayNetManager == null)
            Debug.LogError("GameplayNetworkManager not in scene!");
    }
    
    public override void OnPlayerJoined(PlayerRef player)
    {
        // ... existing code ...
        FindObjectOfType<GameplayNetworkManager>()
            ?.OnPlayerEnteredLobby(player);  // ADD THIS
    }
}
```

2. **Create CharacterDatabase Asset:**
   - Right-click in Assets → Create → Devour/Setup → Create Character Database
   - File tạo tại: `Assets/Resources/CharacterDatabase.asset`

3. **Verify InputHandler:**
   - Đảm bảo Input System bây giờ có hotkeys: E, R, F
   - Hoặc modify `InputHandler.cs` nếu cần

### ✅ Phase 5: Testing (5 min)

1. **Test startup:**
   ```csharp
   // Console log check:
   [CharacterRole] Character database initialized
   [GameplayStateManager] Initialized with 4 bonfires
   [GameplayNetworkManager] Initialized
   ```

2. **Test character spawn:**
   - Join game with 4 players
   - Kiểm tra character selection work
   - Verify 1 hunter + 3 survivors spawn

3. **Test basic abilities:**
   - Press E → Ability execute
   - Check cooldowns work
   - Verify RPC calls execute

---

## 🔧 Configuration

### Tạo Character Database (Manual):
```csharp
// Assets/Resources/CharacterDatabase.asset

Characters:
  - ID: Hunt1, Name: "Root Hunter", Role: Hunter, Speed: 4.5, Health: 100
  - ID: Hunt2, Name: "Eyes Hunter", Role: Hunter, Speed: 5, Health: 100
  - ID: Survival1, Name: "Marksman", Role: Survivor, Speed: 5.5, Health: 80
  - ID: Survival2, Name: "Boombox", Role: Survivor, Speed: 5.2, Health: 85
  - ID: Survival3, Name: "Lumberjack", Role: Survivor, Speed: 5, Health: 90
  - ID: Survival4, Name: "Support", Role: Survivor, Speed: 5, Health: 75
```

### Bonfire Positions (Đặt trong scene):
```
Spawn_0 (0, 0)      - Hunter spawn
Spawn_1 (5, 5)      - Survivor 1 spawn
Spawn_2 (-5, 5)     - Survivor 2 spawn
Spawn_3 (0, -5)     - Survivor 3 spawn

Bonfires: (5, -5), (5, 5), (-5, 5), (-5, -5)
Exit Gate: (0, 10)
```

---

## 🐛 Common Issues & Fixes

| Issue | Cause | Fix |
|-------|-------|-----|
| Characters not spawn | Missing prefabs | Create prefabs in `Assets/Resources/Prefabs/Characters/` |
| Abilities don't execute | Input not detected | Check E, R, F input in InputHandler |
| Status effects not working | Missing StatusEffectManager | Add component to character prefab |
| Network errors | Missing NetworkObject | Add NetworkObject component to prefabs |
| Game won't start | GameplayStateManager missing | Add GO with script to scene |
| Characters freeze | Rigidbody kinematic wrong | Check RB settings based on authority |

---

## 📊 File Checklist

```
Assets/Codes/Gameplay/ (16 files required)
├── ✅ CharacterRole.cs
├── ✅ StatusEffect.cs
├── ✅ AbilitySystem.cs
├── ✅ NetworkCharacterController.cs
├── ✅ Hunt1Abilities.cs
├── ✅ Hunt2Abilities.cs
├── ✅ SurvivalAbilities.cs
├── ✅ GameplayStateManager.cs
├── ✅ WoodAndBonfireSystem.cs
├── ✅ GameStartController.cs
├── ✅ InputHandling.cs
├── ✅ GameplayUI.cs
├── ✅ GameplayNetworkIntegration.cs
├── ✅ GameplayConstants.cs
├── ✅ GameSetupWizard.cs
├── ✅ IMPLEMENTATION_GUIDE_VN.md
└── ✅ ABILITY_REFERENCE.md

Assets/Resources/
├── CharacterDatabase.asset
└── Prefabs/Characters/
    ├── Hunt1_Character.prefab
    ├── Hunt2_Character.prefab
    ├── Survival1_Character.prefab
    ├── Survival2_Character.prefab
    ├── Survival3_Character.prefab
    └── Survival4_Character.prefab
```

---

## 🚀 Run Your First Game

```
1. Open Gameplay scene
2. Run in Play mode with Network Simulator (4 players)
3. Each player selects character
4. Game assigns roles automatically
5. Control with:
   - WASD: Move
   - E: E-ability
   - R: R-ability
   - F: F-ability (hunters only)
   - ESC: Pause
```

---

## 📞 Support References

- **Photon Fusion Docs**: https://doc.photonengine.com/fusion/current/overview
- **Unity NetworkBehaviour**: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Networking.NetworkBehaviour.html
- **Physics 2D**: https://docs.unity3d.com/2022.3/Documentation/Manual/Physics2DReference.html

---

**Version**: 1.0 | **Last Updated**: 2025-04-07
**Estimated Setup Time**: 20-30 minutes | **Difficulty**: Intermediate
