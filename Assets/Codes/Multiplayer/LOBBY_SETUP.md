# 🎮 Lobby Setup - Di Chuyển + Vote + Chat

Hướng dẫn setup lobby mới với các tính năng:
- Di chuyển tự do trong lobby
- Vote bắt đầu game (dựa vào số người)
- Chat realtime

## 📋 Thay Đổi Flow

### Cũ
```
Login → Chọn Nhân Vật → Ready → Game Bắt đầu
```

### Mới
```
Login → Vào Lobby (di chuyển tự do) 
→ Chọn Nhân Vật (có thể giữ, chọn lại)
→ Vote Bắt Đầu (cần tất cả vote)
→ Game Bắt Đầu
```

## 🎬 Bước 1: Tạo Prefab - LobbyPlayer

### Tạo từ MultiplayerCharacter
```
1. Duplicate MultiplayerCharacter prefab
2. Rename → "LobbyPlayer"
3. Replace component:
   ❌ Remove: MultiplayerCharacter
   ✅ Add: LobbyPlayerController
   
4. Cấu hình LobbyPlayerController:
   - Move Speed: 5
   - Acceleration: 15
   - Deceleration: 20
   - Face Movement: ✓

5. Save vào Assets/Prefabs/LobbyPlayer.prefab
```

### Hoặc Tạo Mới từ Đầu
```
1. Create Empty → "LobbyPlayer"
2. Add:
   - SpriteRenderer
   - Rigidbody2D (Gravity = 0)
   - Collider (CircleCollider2D)
   - Animator (nếu có animation)
   - PlayerInput
   - PhotonView (quan trọng!)
   - LobbyPlayerController

3. Cấu hình PhotonView:
   - Ownership: Takeover
   - Observed: LobbyPlayerController

4. Save vào Assets/Prefabs/LobbyPlayer.prefab
```

## 🏠 Bước 2: Setup LobbyScene

### Scene Hierarchy

```
LobbyScene
├─ Canvas (z = 0)
│  ├─ LoginPanel (code cũ)
│  ├─ LobbyPanel
│  │  ├─ RoomInfo (Text)
│  │  ├─ PlayerListPanel (List content)
│  │  ├─ CharacterSelectPanel
│  │  │  ├─ 4x CharacterButton
│  │  │  └─ SelectedText
│  │  ├─ ChatPanel
│  │  │  ├─ ChatDisplay (Text)
│  │  │  ├─ ChatInput (InputField)
│  │  │  └─ SendButton (Button)
│  │  ├─ VotePanel
│  │  │  ├─ VoteStatus (Text)
│  │  │  ├─ VoteCount (Text)
│  │  │  └─ VoteButton (Button)
│  │  └─ StatusText (Text)
│
├─ GameSetup (Empty)
│  ├─ PhotonView (View ID = 1)
│  ├─ PhotonNetworkManager
│  ├─ LobbySpawner
│  ├─ VoteSystem
│  └─ ChatSystem (gắn vào LobbyManager)
│
├─ Camera
│  └─ CameraFollow

└─ Lighting (baked/realtime)
```

### Tạo Components

#### 1. VotePanel Setup
```
Inspector > VoteSystem:
- Vote Button: [VoteButton]
- Vote Status Text: [VoteStatus]
- Vote Count Text: [VoteCount]
- Min Players To Vote: 2
- Max Players: 4
```

#### 2. ChatPanel Setup
```
Inspector > ChatSystem:
- Chat Scroll: [ChatScroll]
- Chat Display Text: [ChatDisplay]
- Chat Input Field: [ChatInput]
- Send Button: [SendButton]
- Max Messages: 50
```

#### 3. LobbySpawner Setup
```
Inspector > LobbySpawner:
- Spawn Points: [(-5,0,0), (5,0,0), (-5,5,0), (5,5,0)]
- Lobby Player Prefab: "Prefabs/LobbyPlayer"
- Chat System: [ChatSystem object]
- Vote System: [VoteSystem object]
```

## 🎯 Bước 3: Sửa GameLobbyUI.cs

Thêm phần **Character Select** trong lobby (không login panel):

```csharp
// Sau khi login → vào LobbyPanel (có di chuyển)
// Ngoài Canvas có thể chọn character

private void ShowLobbyPanel()
{
    loginPanel.SetActive(false);
    lobbyPanel.SetActive(true);
    // Character selection buttons sẽ ở đây
}
```

## 🗳️ Bước 4: Vote Flow

```
1. Player 1,2,3 vào lobby
2. Tất cả chọn character
3. Tất cả vote bắt đầu game
4. Vote count = Chosen player count
   - 2 người chọn nhân vật → cần 2 vote
   - 3 người chọn nhân vật → cần 3 vote
   - 4 người chọn nhân vật → cần 4 vote
5. Khi vote == chosen → Game load
```

## 💬 Bước 5: Chat Usage

```csharp
// Player gõ tin nhắn trong ChatInput
// Bấm Enter hoặc nút Send
// Tất cả thấy: "[PlayerName]: message"

// System sẽ tự động thông báo:
// "[PlayerName] join lobby"
// "[PlayerName] left lobby"
```

## 🧪 Test Checklist

```
□ Login vào
□ Vào LobbyScene
□ Thấy player của mình di chuyển
□ WASD để di chuyển
□ Chọn character
□ Gõ chat message
□ Vote bắt đầu game
□ Tất cả 3-4 người vote
□ Game auto-load vào GameScene
```

## 🖼️ UI Layout Reference

### Chat Box
```
┌─────────────────┐
│ Player: Hello   │
│ Player2: Hi     │
│ Player3: Ready! │
│ System: X join  │
└─────────────────┘
[Input field_____] [Send]
```

### Vote Box
```
┌──────────────────┐
│ Voted: 2/3       │
│ You: Ready!      │
│ [Vote Button]    │
└──────────────────┘
```

### Lobby Info
```
Room: Room_1234
Players: 3/4
```

## 📊 Properties Sync

```csharp
// Player properties sau khi login
{
    "characterIndex": 0,        // Chọn nhân vật
    "characterName": "Hacker",
    "voted": true               // Vote bắt đầu
}

// Room properties
{
    "gameStarted": true         // Game đã bắt đầu
}
```

## 💡 Tips

1. **Character Selection Button:**
   - Gắn script để set selectedCharacter
   - Update UI khi người khác chọn

2. **Vote Button State:**
   - Disable nếu chưa chọn character
   - Disable nếu đã vote rồi

3. **Chat Persistence:**
   - Clear chat khi vào GameScene
   - Hiển thị join/leave messages

4. **Camera:**
   - Theo dõi player owner
   - Tối ưu view lobbyfragment

## ❓ FAQ

**Q: Làm sao để unvote?**
A: Tương tự - bấm lại nút thành "Unvote"

**Q: Nếu 1 người unvote?**
A: Vote count giảm, game không start

**Q: Chat lưu nơi đâu?**
A: Chỉ lưu trong memory, không lưu database

**Q: Character selection có giới hạn không?**
A: Không, 2 người có thể chọn cùng character

---

**Hoàn thành setup = Lobby hoàn chỉnh! 🎉**
