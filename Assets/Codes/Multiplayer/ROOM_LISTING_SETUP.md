# Room Listing UI Setup Guide

## 📋 Tóm Tắt Thay Đổi

**Hệ thống mới:**
1. ✅ Client menu hiến danh sách phòng (room) được tạo bởi host
2. ✅ Client can nhấp vào phòng để chọn
3. ✅ Click "Join" để vào phòng đã chọn
4. ✅ Đổi input từ "tên server" → "tên người chơi"

---

## 🎯 Files Được Tạo/Cập Nhật

| File | Thay Đổi | Mục Đích |
|------|---------|---------|
| **RoomListUI.cs** | NEW | Hiển thị danh sách phòng + quản lý chọn |
| **GameLobbyUI.cs** | UPDATED | Thêm player name input, integrate room list |
| **FusionNetworkManager.cs** | UPDATED | Track available sessions |
| **PlayerNameInput.cs** | UNCHANGED | Giữ lại cho lobby scene |

---

## 🎨 UI Hierarchy (Canvas Structure)

```
MainMenuCanvas
├─ Panel_MainMenu
│  ├─ PlayerNamePanel
│  │  ├─ Label "Player Name"
│  │  ├─ InputField (Name Input)
│  │  └─ Text_Error (for validation)
│  │
│  ├─ RoomListPanel
│  │  ├─ Button_Refresh
│  │  ├─ ScrollView
│  │  │  └─ Content
│  │  │     ├─ RoomItem (prefab instance 1)
│  │  │     ├─ RoomItem (prefab instance 2)
│  │  │     └─ ...
│  │  └─ Text_Status
│  │
│  └─ ButtonPanel
│     ├─ Button_Host
│     ├─ Button_Join
│     └─ Text_Status
```

---

## 📝 Bước Thiết Lập Chi Tiết

### **Step 1: Chuẩn Bị Prefab RoomItem**

Tạo prefab `RoomItem` với cấu trúc:

```
RoomItem (Panel)
├─ SelectButton (Button)
│  ├─ Image (background)
│  └─ LayoutGroup
│     ├─ Text_RoomName (TMP_Text)
│     └─ Text_PlayerCount (TMP_Text)
└─ (Optional) Gradient/Border image
```

**RoomItem Script Assignment:**
- Attach `RoomItemUI` component (auto-added if missing)
- Assign fields:
  - `sessionNameText` → Text_RoomName
  - `playerCountText` → Text_PlayerCount
  - `selectButton` → SelectButton
  - `backgroundImage` → Panel Image

### **Step 2: Tạo MainMenuCanvas**

Tạo Canvas cho Main Menu:

```csharp
Name: MainMenuCanvas
Render Mode: Screen Space - Overlay
Plane Distance: 100
```

### **Step 3: Thêm GameLobbyUI Script**

Tạo GameObject `MenuController` trong canvas:

```
MenuController
├─ Script: GameLobbyUI
├─ Inspector Assignments:
│  ├─ playerNameInput → InputField (Name)
│  ├─ playerNameError → Text (Error message)
│  ├─ roomListUI → RoomListUI (from Step 5)
│  ├─ hostButton → Button (Host)
│  ├─ joinButton → Button (Join)
│  ├─ refreshButton → Button (Refresh)
│  ├─ statusText → Text (Status)
│  └─ canvasToHide → Canvas (để ẩn menu khi vào lobby)
```

### **Step 4: Thiết Lập Player Name Input**

Tạo Panel cho nhập tên:

```
PlayerNamePanel
├─ Text "Enter Your Name"
├─ InputField (TMP_InputField)
│  └─ Placeholder text: "Your name..."
└─ Text_Error (TMP_Text, initially inactive)
```

**Gán vào GameLobbyUI:**
- `playerNameInput` → InputField
- `playerNameError` → Text_Error

### **Step 5: Thiết Lập RoomListUI**

Tạo GameObject `RoomListController`:

```
RoomListController
├─ Script: RoomListUI
├─ Assignments:
│  ├─ roomListContainer → ScrollView/Content
│  ├─ roomItemPrefab → RoomItem prefab (từ Step 1)
│  ├─ refreshButton → Button (Refresh)
│  ├─ joinButton → Button (Join)  ← Same as in GameLobbyUI!
│  └─ statusText → Text (Room list status)
```

### **Step 6: Thiết Lập Button Layout**

```
ButtonPanel
├─ Button "🏠 Host" → OnHostClicked()
├─ Button "🚪 Join" → OnJoinClicked()
├─ Button "🔄 Refresh" → OnRefreshClicked()
└─ Text "Status: Ready"
```

---

## 🔄 Luồng Hoạt Động

```
1. Client nhìn thấy UI:
   ├─ Input field (nhập tên)
   ├─ Danh sách phòng (empty nếu chưa có phòng)
   └─ Buttons (Host, Join, Refresh)

2. Khi Host tạo phòng:
   ├─ Nhập tên người chơi → Click "Host"
   ├─ Server tạo session
   ├─ Session xuất hiện trong Fusion
   └─ Clients thấy phòng mới qua OnSessionListUpdated()

3. Khi Clients join:
   ├─ Click "Refresh" → Cập nhật danh sách phòng
   ├─ Nhập tên → Click phòng → Click "Join"
   ├─ Server kết nối client
   └─ Chuyển sang Lobby scene
```

---

## 🧪 Test Checklist

- [ ] Nhập tên → Click Host → Tạo session thành công
- [ ] Session xuất hiện trong danh sách (có thể phải refresh)
- [ ] Click vào phòng → Color thay đổi (select state)
- [ ] Click Join → Join vào phòng thành công
- [ ] Validation: Empty name → Error message
- [ ] Validation: Name < 2 chars → Error message
- [ ] Status text cập nhật đúng
- [ ] Buttons disable/enable đúng

---

## 🎮 Integration with Existing Code

### PlayerNameInput (Unchanged)
- Giữ lại cho Lobby Scene
- Vẫn dùng cho character select

### FusionCallbacksBase
- `OnSessionListUpdated()` được gọi tự động bởi Fusion
- RoomListUI subscribe vào events từ FusionNetworkManager

### Lobby Scene Flow
```
Menu (GameLobbyUI) → Host/Join → OnJoinedSession()
                                        ↓
                    Hide Menu Canvas
                    Load Lobby Scene
                    PlayerNameInputUI (ask name again)
                                        ↓
                    CharacterSelectUI (choose car)
                                        ↓
                    GameStartController (ready?)
                                        ↓
                    Racing Scene
```

---

## ⚙️ Configuration Values

**In GameLobbyUI:**
```csharp
playerNameInput.maxLength = 16  // Max player name length
playerNameInput.characterLimit = 16
```

**In RoomListUI:**
```csharp
maxRoomsToDisplay = 10  // Show max 10 rooms in list
```

**In FusionNetworkManager:**
```csharp
maxPlayers = 4  // Max players per session
lobbySceneIndex = 1
racingSceneIndex = 2
```

---

## 🐛 Troubleshooting

| Vấn Đề | Nguyên Nhân | Giải Pháp |
|--------|-----------|----------|
| Danh sách phòng trống | Chưa có phòng | Host tạo phòng trước |
| Join button không click được | Chưa select phòng | Click phòng trước |
| Tên người chơi không lưu | Quên SetStoredPlayerName() | Check integration |
| Scene không chuyển | canvasToHide not assigned | Assign Canvas để ẩn |

---

## 📱 Scene Assignment

**Menu Scene:**
- Canvas: MainMenuCanvas (with GameLobbyUI + RoomListUI)
- Anything else: Optional

**Lobby Scene:**
- Canvas: Existing lobby UI
- PlayerNameInputUI: For second name input
- CharacterSelectUI: For car selection

---

## ✅ Implementation Complete

Tất cả files đã được cập nhật, không có compilation errors.

**Sẵn sàng:**
1. Tạo UI structure theo hướng dẫn
2. Gán script + prefabs
3. Test flow
4. Submit UI setup
