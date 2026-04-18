# 📋 Danh Sách Script Gameplay Scene

## 🕐 1. COUNTDOWN SYSTEM - Đếm Ngược

1. **RaceManager.cs** `Gameplay/`
2. **RaceUI.cs** `Gameplay/`
3. **FinishLineDetector.cs** `Gameplay/`

---

## 📷 2. CAMERA SYSTEM - Hệ Thống Camera

1. **MultiCameraManager.cs** `Multiplayer/`
2. **CameraFollowTarget.cs** `Multiplayer/`
3. **CameraFollow.cs** `Multiplayer/`
4. **RacingCarSpawner.cs** `Multiplayer/` (liên quan - register camera)

---

## 💬 3. CHAT SYSTEM - Hệ Thống Chat

1. **GameChatManager.cs** `Multiplayer/`
2. **ChatNetworkHandler.cs** `Multiplayer/` (trong GameChatManager.cs)
3. **ChatMessageUI.cs** `Multiplayer/` (trong GameChatManager.cs)
4. **FusionNetworkManager.cs** `Multiplayer/` (cung cấp player name)

---

## 📍 Tóm Tắt Thư Mục

```
Gameplay/
├── RaceManager.cs              (Countdown)
├── RaceUI.cs                   (Countdown UI)
└── FinishLineDetector.cs       (Countdown trigger)

Multiplayer/
├── MultiCameraManager.cs       (Camera)
├── CameraFollowTarget.cs       (Camera follow)
├── CameraFollow.cs             (Fixed camera)
├── GameChatManager.cs          (Chat)
└── RacingCarSpawner.cs         (Camera register)
```
