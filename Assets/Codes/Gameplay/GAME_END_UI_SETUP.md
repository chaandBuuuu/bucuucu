# 🎮 GAME END UI - SETUP (Simplified)

## 📋 Overview

Hiển thị kết quả game (Hunt chiến thắng vs Survivor thoát), thời gian chơi, và navigation buttons.

**Script:** `GameEndUIManager` (trong `GameplayUI.cs`)

---

## 🛠️ QUICK SETUP (5 mins)

### Bước 1: Chọn/Tạo Gameplay Scene

**Option A - Nếu đã có gameplay scene:**
- Mở scene: `Assets/Scenes/lobby.unity` hoặc scene gameplay của bạn

**Option B - Tạo scene mới:**
```
File → New Scene → Save as: Assets/Scenes/2_GameplayLevel.unity
```

### Bước 2: Tạo Game End UIPainel

1. **Hierarchy → Right-click → UI → Panel**
   - Rename: `GameEndPanel`

2. **Select `GameEndPanel` → Inspector:**
   - Add Component: `Canvas`
   - Canvas Settings:
     - Render Mode: `Screen Space - Overlay`
   
3. **Add Component: `CanvasGroup`** (cho fade animation)

### Bước 3: Tạo UI Elements

**Cấu trúc:**
```
GameEndPanel (Canvas + CanvasGroup)
├── Background (Image)
│   └── Color: Black (α=0.8)
├── ContentContainer (Panel + VerticalLayoutGroup)
│   ├── ResultText (TextMeshPro)
│   ├── WinnerText (TextMeshPro)
│   ├── GameDurationText (TextMeshPro)
│   ├── HunterStatsText (TextMeshPro)
│   ├── SurvivorStatsText (TextMeshPro)
│   └── ButtonContainer (HorizontalLayoutGroup)
│       ├── BackToLobbyButton
│       ├── MainMenuButton
│       └── RestartButton
```

#### 3.1 Background
```
Right-click GameEndPanel → UI → Image
├── Name: Background
├── Image → Color: Black, Alpha: 0.8
└── RectTransform:
    ├── Anchors: Stretch (full screen)
    ├── Left/Right/Top/Bottom: 0
```

#### 3.2 ContentContainer
```
Right-click GameEndPanel → UI → Panel
├── Name: ContentContainer
├── VerticalLayoutGroup:
│   ├── Spacing: 15
│   ├── Child Force Expand: Width ✓, Height ✗
│   └── Child Control Size: Width ✓, Height ✗
└── RectTransform:
    ├── Width: 900
    ├── Height: 700
    └── Center: (0, 0)
```

#### 3.3 Text Elements (Trong ContentContainer)

**ResultText:**
```
Right-click ContentContainer → UI → Text - TextMeshPro
├── Name: ResultText
├── Text: "HUNTERS WIN!"
├── Font Size: 60
├── Alignment: Center
├── Color: Red (255, 0, 0)
└── Layout: Preferred Height: 100
```

**WinnerText:**
```
Right-click ContentContainer → UI → Text - TextMeshPro
├── Name: WinnerText
├── Text: "🔥 HUNTERS VICTORY 🔥"
├── Font Size: 45
├── Color: Yellow (255, 255, 0)
└── Layout: Preferred Height: 80
```

**GameDurationText:**
```
Right-click ContentContainer → UI → Text - TextMeshPro
├── Name: GameDurationText
├── Text: "Game Duration: 5m 42s"
├── Font Size: 24
├── Color: White
└── Layout: Preferred Height: 40
```

**HunterStatsText:**
```
Right-click ContentContainer → UI → Text - TextMeshPro
├── Name: HunterStatsText
├── Text: "🔥 HUNTER STATS\n━━━━━━━\nDamage: 0\nKills: 0"
├── Font Size: 18
├── Color: Light Red (255, 100, 100)
└── Layout: Preferred Height: 80
```

**SurvivorStatsText:**
```
Right-click ContentContainer → UI → Text - TextMeshPro
├── Name: SurvivorStatsText
├── Text: "🌲 SURVIVOR STATS\n━━━━━━━\nWood: 0\nFires: 0"
├── Font Size: 18
├── Color: Light Green (100, 255, 100)
└── Layout: Preferred Height: 80
```

#### 3.4 Button Container
```
Right-click ContentContainer → UI → Panel
├── Name: ButtonContainer
├── HorizontalLayoutGroup:
│   ├── Spacing: 20
│   ├── Child Force Expand: Width ✓, Height ✗
│   └── Child Control Size: Width ✓, Height ✗
└── Layout: Preferred Height: 60
```

#### 3.5 Buttons (Trong ButtonContainer)

**BackToLobbyButton:**
```
Right-click ButtonContainer → UI → Button - TextMeshPro
├── Name: BackToLobbyButton
├── Text: "Back to Lobby"
├── Text Font Size: 28
├── Color: Light Blue
├── Layout: Preferred Width: 280
└── Button:
    └── OnClick:
        ├── GameObject: GameEndPanel
        ├── Function: GameEndUIManager.OnBackToLobbyClicked()
```

**MainMenuButton:**
```
Right-click ButtonContainer → UI → Button - TextMeshPro
├── Name: MainMenuButton
├── Text: "Main Menu"
├── Text Font Size: 28
├── Layout: Preferred Width: 280
└── Button:
    └── OnClick:
        ├── GameObject: GameEndPanel
        ├── Function: GameEndUIManager.OnMainMenuClicked()
```

**RestartButton:**
```
Right-click ButtonContainer → UI → Button - TextMeshPro
├── Name: RestartButton
├── Text: "Restart"
├── Text Font Size: 28
├── Layout: Preferred Width: 280
└── Button:
    └── OnClick:
        ├── GameObject: GameEndPanel
        ├── Function: GameEndUIManager.OnRestartClicked()
```

### Bước 4: Thêm GameEndUIManager Script

1. **Select: `GameEndPanel`**
2. **Inspector → Add Component: `GameEndUIManager`**
3. **Drag các UI elements vào fields:**
   - **gameEndCanvas**: Drag `GameEndPanel`
   - **canvasGroup**: Tự động (sẽ auto-find)
   - **resultText**: Drag `ResultText`
   - **winnerText**: Drag `WinnerText`
   - **gameDurationText**: Drag `GameDurationText`
   - **hunterStatsText**: Drag `HunterStatsText`
   - **survivorStatsText**: Drag `SurvivorStatsText`
   - **backToLobbyButton**: Drag `BackToLobbyButton`
   - **mainMenuButton**: Drag `MainMenuButton`
   - **restartButton**: Drag `RestartButton`
   - **fadeInDuration**: 0.5 (default)

### Bước 5: Disable Canvas Initially

```
Select GameEndPanel → Inspector
Canvas:
└── ☐ Enabled (UNCHECK - will be enabled when game ends)
```

### Bước 6: Verify GameplayStateManager

Tìm `GameplayStateManager` object trong scene → Inspector → Verify:
- ✅ có script `GameplayStateManager.cs` attached
- ✅ có event `OnGameEnd` defined

### Bước 7: Save Scene

```
Ctrl+S → Chọn vị trí: Assets/Scenes/2_GameplayLevel.unity (hoặc tên scene của bạn)
```

---

## ✅ Testing

1. **Play game**
2. **Chờ game end condition trigger:**
   - Hunter kills all survivors → "HUNTERS WIN!"
   - Survivors light all bonfires + escape → "SURVIVORS WIN!"
3. **Verify:**
   - ✅ UI fade in (0.5s)
   - ✅ Result text hiển thị đúng
   - ✅ Buttons hoạt động
   - ✅ Click "Back to Lobby" → Load lobby scene

---

## 🔧 Customization

### Thay đổi animation duration
```
GameEndPanel → GameEndUIManager
→ Fade In Duration: [giá trị in seconds]
```

### Thay đổi button text/colors
- Select button → Inspector → Button component:
  - Normal Color (unselected)
  - Highlighted Color (hovered)
  - Pressed Color (clicked)

### Thêm sound effect
Trong `GameEndUIManager` - method `DisplayGameResults()`:
```csharp
// Thêm dòng này:
// AudioManager.Instance.PlaySFX("game_end_victory", 1f);
```

---

## 📋 Checklist

- [ ] GameEndPanel created với Canvas & CanvasGroup
- [ ] Background image tạo (Black overlay)
- [ ] Text elements tạo (Result, Winner, Duration, Stats)
- [ ] Buttons tạo (Back, Menu, Restart)
- [ ] GameEndUIManager script attached
- [ ] Tất cả fields trong inspector assigned
- [ ] Canvas disabled initially
- [ ] Scene saved
- [ ] Tested game end trigger

---

## 🐛 Troubleshooting

**Q: UI không hiển thị khi game end**
- A: Check GameEndUIManager.cs component, verify `gameEndCanvas` assigned

**Q: Buttons không click được**
- A: Verify Canvas trên parent → Render Mode: Screen Space - Overlay
- A: Check Button component có `OnClick` listener

**Q: Game kết thúc nhưng không trigger UI**
- A: Verify `GameplayStateManager.OnGameEnd` event được invoke
- A: Check console cho logs: "[GameplayStateManager] Game ended! Winner:"

---

## 📁 Files

- **Script**: `Assets/Codes/Gameplay/GameplayUI.cs` → `GameEndUIManager` class
- **Setup**: Này file
- **Scenes**: `Assets/Scenes/2_GameplayLevel.unity` (hoặc scene của bạn)
