# 🏎️ Lobby Scene Setup - Racing Game

## 📋 Overview

Cập nhật lobby scene để chọn xe racing + Ready button.

---

## 🛠️ Lobby Scene Structure

```
Hierarchy:
├── Canvas (Main UI)
│   ├── Panel_CarSelection (Panel)
│   │   ├── Title (TextMeshPro: "Chọn Loại Xe")
│   │   ├── CarButtons (HorizontalLayoutGroup)
│   │   │   ├── Button_Hacker
│   │   │   ├── Button_GhostHunter
│   │   │   ├── Button_Priest
│   │   │   └── Button_Scientist
│   │   ├── SelectedCarText (TextMeshPro: "Xe: ...")
│   │   └── ReadyButton (Button: "Sẵn Sàng")
│   └── StatusText (TextMeshPro: "Chọn loại xe...")
│
├── GameStartController (Empty)
│   └── Script: GameStartController.cs
│       ├── Status Text: Assigned
│       ├── Start Race Button: Assigned
│       └── Required Players: 4
│
└── NetworkRunner (existing)
```

---

## 📝 Setup Steps

### Step 1: Create Car Selection UI

**1.1 Background Panel**
```
Right-click Canvas → UI → Panel
├── Name: Panel_CarSelection
├── RectTransform: Anchor = Stretch (full screen)
├── Image:
│   ├── Color: Black (α=0.8)
│   └── Source Image: None (solid color)
```

**1.2 Title Text**
```
Right-click Panel_CarSelection → UI → Text - TextMeshPro
├── Name: Title
├── Text: "CHỌN LOẠI XE"
├── Font Size: 48
├── Alignment: Center
├── Color: White
└── Layout: Preferred Height: 100
```

**1.3 Car Buttons Container**
```
Right-click Panel_CarSelection → UI → Panel
├── Name: CarButtons
├── Add Component: HorizontalLayoutGroup
├── Spacing: 20
├── Child Force Expand: Width ✓, Height ✗
├── Child Control Size: Width ✓, Height ✗
└── Layout: Preferred Height: 150
```

**1.4 Create 4 Car Buttons (Repeat 4 times)**

Button #1 - Hacker:
```
Right-click CarButtons → UI → Button - TextMeshPro
├── Name: Button_Hacker
├── Image:
│   └── Color: Red (255, 0, 0)
├── Button Text: "Hacker"
├── Text Font Size: 24
├── Layout:
│   ├── Preferred Width: 150
│   └── Preferred Height: 100
└── Button > OnClick():
    ├── GameO: Assign LobbyCharacterSelectUI
    ├── Function: LobbyCharacterSelectUI.SelectCar(0)
```

Button #2 - Ghost Hunter:
```
... (same, but index = 1, Color: Green)
Text: "Ghost Hunter"
Function: SelectCar(1)
```

Button #3 - Priest:
```
... (same, but index = 2, Color: Yellow)
Text: "Priest"
Function: SelectCar(2)
```

Button #4 - Scientist:
```
... (same, but index = 3, Color: Cyan)
Text: "Scientist"
Function: SelectCar(3)
```

**1.5 Selected Car Text**
```
Right-click Panel_CarSelection → UI → Text - TextMeshPro
├── Name: SelectedCarText
├── Text: "Xe: ..."
├── Font Size: 32
├── Alignment: Center Bottom
├── Color: White
└── Layout: Preferred Height: 80
```

**1.6 Ready Button**
```
Right-click Panel_CarSelection → UI → Button - TextMeshPro
├── Name: ReadyButton
├── Text: "✅ SẴN SÀNG"
├── Text Font Size: 28
├── Button:
│   ├── Normal Color: Green
│   └── Highlighted Color: Light Green
├── Layout:
│   ├── Preferred Width: 300
│   └── Preferred Height: 80
└── Button > OnClick():
    ├── GameO: Assign LobbyCharacterSelectUI
    ├── Function: LobbyCharacterSelectUI.OnReadyClicked()
```

**1.7 Status Text (Bottom)**
```
Right-click Canvas → UI → Text - TextMeshPro
├── Name: StatusText
├── Text: "Chọn loại xe của bạn!"
├── Font Size: 24
├── Alignment: Bottom Center
├── Color: White
├── RectTransform:
│   ├── Anchors: Bottom Center
│   ├── Pos Y: 50
│   └── Height: 60
```

### Step 2: Add LobbyCharacterSelectUI Script

```
Select: Panel_CarSelection
Inspector → Add Component: LobbyCharacterSelectUI
Assign:
├── Car Buttons[0]: Button_Hacker
├── Car Buttons[1]: Button_GhostHunter
├── Car Buttons[2]: Button_Priest
├── Car Buttons[3]: Button_Scientist
├── Selected Car Text: SelectedCarText
├── Ready Button: ReadyButton
└── Status Text: StatusText (from Canvas)
```

### Step 3: Add GameStartController

```
Hierarchy: Right-click Canvas → Create Empty
├── Name: GameStartController
├── Add Component: GameStartController.cs
Assign:
├── Status Text: StatusText
├── Start Race Button: (Create button or assign existing if needed)
├── Required Players: 4
└── Check Interval: 1.0
```

### Step 4: Create Start Race Button (Host Only)

```
Right-click Canvas → UI → Button - TextMeshPro
├── Name: StartRaceButton
├── Text: "🏁 BẮT ĐẦU RACE"
├── Text Font Size: 28
├── Button:
│   ├── Normal Color: Blue
│   └── Highlighted Color: Light Blue
├── RectTransform:
│   ├── Anchors: Top Right
│   ├── Pos X: -150
│   ├── Pos Y: -50
│   ├── Width: 300
│   ├── Height: 80
└── Button > OnClick():
    ├── Function: GameStartController.OnStartRaceClicked()
```

Assign StartRaceButton to GameStartController:
```
Select: GameStartController (in hierarchy)
Inspector:
└──, Game Start Controller → Start Race Button: Drag StartRaceButton
```

---

## 🎮 Gameplay Flow

```
1. Player enters Lobby
   ↓
2. LobbyCharacterSelectUI shows 4 car buttons
   ↓
3. Player clicks a car button (SelectCar)
   ↓
4. Car name + color displayed
   ↓
5. Player clicks READY button
   ↓
6. Buttons disabled, status shows "✅ Sẵn sàng"
   ↓
7. GameStartController counts ready players
   ↓
8. When all 4 ready, Host clicks START RACE
   ↓
9. LoadScene(2) → Racing scene
```

---

## 🔧 Debugging

**Problem: Ready button doesn't disable**
- Check: LobbyCharacterSelectUI has readyButton assigned

**Problem: Start Race button appears for all players**
- Check: GameStartController.Spawned() checks HasStateAuthority

**Problem: Cars don't spawn in racing scene**
- Check: RacingCarSpawner has carPrefab assigned
- Check: carPrefab has NetworkObject component

**Problem: Wrong spawn positions**
- Check: RacingCarSpawner.spawnPoints array (adjust if needed)

---

## 📋 Checklist

- [ ] Panel_CarSelection created (full screen, black overlay)
- [ ] Title text created ("CHỌN LOẠI XE")
- [ ] CarButtons container created (HorizontalLayoutGroup)
- [ ] 4 car buttons created + colored (Red, Green, Yellow, Cyan)
- [ ] SelectedCarText created
- [ ] ReadyButton created + green
- [ ] StatusText created (bottom of screen)
- [ ] LobbyCharacterSelectUI script assigned to Panel_CarSelection
- [ ] All buttons assigned to LobbyCharacterSelectUI
- [ ] GameStartController GameObject created
- [ ] GameStartController script assigned
- [ ] StatusText + StartRaceButton assigned to GameStartController
- [ ] StartRaceButton created in top-right
- [ ] RacingCarSpawner in racing scene (or lobby as prefab)
- [ ] Car prefab assigned to RacingCarSpawner
- [ ] Test: Player can select car → status updates
- [ ] Test: Player clicks Ready → buttons disable
- [ ] Test: All 4 players ready → Start Race enabled
- [ ] Test: Click Start Race → Load racing scene ✓

---

## 📁 Files Updated

- ✅ [LobbyCharacterSelectUI.cs](LobbyCharacterSelectUI.cs) - Car selection + Ready
- ✅ [GameStartController.cs](GameStartController.cs) - Start race management
- ✅ [RacingCarSpawner.cs](RacingCarSpawner.cs) - Spawn cars in racing scene

---

**Status: Ready to setup in Unity editor!**
