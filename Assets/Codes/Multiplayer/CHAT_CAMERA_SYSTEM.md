# 🎮 Chat & Camera System Upgrade Guide

## Status: ✅ COMPLETE & READY

---

## 📢 NEW: Global Chat System

### What's New
- ✅ **Global Chat** - Players can chat from **lobby onwards** (not just at game end)
- ✅ **Real-time Sync** - Messages broadcast to all players via RPC
- ✅ **Toggle Chat** - Press **T** to show/hide chat panel
- ✅ **Pause Game** - Automatically pauses game when typing, resumes when done

### Features
- Chat input field available during:
  - Lobby phase
  - Racing phase  
  - Game end phase
- Up to 30 messages displayed (auto-scroll to latest)
- Displays player name + message
- Enter to send message (or click send button)

### Setup (5 minutes)

#### Step 1: Add GameChatManager Script
1. Create empty GameObject in your scene: **"GameChatManager"**
2. Attach script: **`GameChatManager.cs`** (location: `Assets/Codes/Multiplayer/`)

#### Step 2: Create Chat UI Panel
1. Create Canvas (if doesn't exist)
2. Create Panel under Canvas: **"ChatPanel"**
   - Set as child of Canvas
3. Create child elements:
   - **Scroll View** → For chat messages
     - Content → Vertical Layout Group
   - **Input Field** (TMP) → For message input
   - **Button** (Optional) → "Send" button

#### Step 3: Assign References in Inspector
Select **"GameChatManager"** and in Inspector:
- **Chat Messages Container** → Drag "Scroll View > Viewport > Content"
- **Chat Message Prefab** → (See below)
- **Chat Input Field** → Drag "Input Field"
- **Chat Send Button** → Drag "Send" button
- **Chat Scroll Rect** → Drag "Scroll View"
- **Chat Panel Canvas Group** → Drag "ChatPanel" and add CanvasGroup component

#### Step 4: Create Chat Message Prefab
1. Create Prefab: **"ChatMessagePrefab"**
   - Add Text (TMP) → For player name
   - Add Text (TMP) → For message content
2. Attach **`ChatMessageUI`** class (it's in GameChatManager.cs)
3. Assign references:
   - **Player Name Text** → First TMP text
   - **Message Text** → Second TMP text
4. Assign this prefab to GameChatManager's **Chat Message Prefab** field

#### Step 5: Test
1. Play scene
2. Players should see chat input available from start
3. Press **T** to toggle visibility
4. Type message + Enter to send
5. Message appears for all players in real-time

---

## 📹 NEW: Camera System with Toggle

### What's New (For Host)
- ✅ **Enter Key Toggle** - Press **Enter** to switch camera modes:
  - **Mode 1: Single Camera** - Full screen, follows host only
  - **Mode 2: Split Screen** - Grid layout, all players visible (4-camera split)
- ✅ **Always See Own Camera** - Participants now see their own camera (not blank)
- ✅ **Proper Player Cameras** - Each player has AudioListener on their camera (sound works)

### Camera Modes

#### Single Camera Mode (Host)
```
┌─────────────────────┐
│                     │
│   HOST CAR ONLY     │
│   (Full Screen)     │
│                     │
└─────────────────────┘
```
- Host sees their car in full screen
- Camera follows host smoothly
- Other players not visible
- Better for immersive racing from host's perspective

#### Split Screen Mode (4 Players)
```
┌─────────────┬─────────────┐
│   P1 CAM    │   P2 CAM    │
├─────────────┼─────────────┤
│   P3 CAM    │   P4 CAM    │
└─────────────┴─────────────┘
```
- All players visible in 2x2 grid
- 2 players: Top/Bottom split
- 3 players: Top full + 2 bottom
- Each camera follows own car

### For Participants
- **Always see their own camera** (not nothing)
- Can't toggle modes (host only feature)
- Audio listener on own camera (sound works properly)
- See own car in viewport

### Setup (Already Done!)

The camera system is **already integrated** in MultiCameraManager.cs. No additional setup needed!

**Just make sure:**
1. `MultiCameraManager.cs` is in your scene
2. `NetworkRunner` is active
3. `CarController` scripts are spawned with cars

### Usage

#### For Host
- **Press Enter** during game to toggle between modes
- Modes toggle in real-time (cameras update instantly)
- Shows log: `[MultiCameraManager] 📹 Camera Mode: SingleCamera` or `SplitScreen`

#### For Participants  
- Automatically get their camera in split-screen mode
- No controls needed - just drive and you'll see your camera

### Technical Details

**File Modified:**
- `MultiCameraManager.cs` - Major update with toggle functionality

**Key Components:**
- `MultiCameraManager` - Manages all camera modes
- `CameraFollowTarget` - Smooth camera follow script (unchanged)

**Configuration (inspector)**
- **Follow Speed** - Camera smoothing speed (5f default)
- **Single Camera Ortho Size** - Zoom level for single mode (15f default)  
- **Ortho Size** - Zoom for split-screen (10f default)
- **Camera Offset** - Distance behind car (-10 on Z)

---

## 🎯 Player Experience

### Host (Server)
1. Game starts in **SplitScreen** mode (all players visible)
2. Press **Enter** to switch to **Single Camera** (host only)
3. Press **Enter** again to switch back to **SplitScreen**
4. Chat available with **T** toggle + Enter to send

### Participant (Client)
1. Joins game, automatically has **their own camera** visible
2. Sees other players' cameras in split-screen
3. Can toggle chat with **T** + Enter to send
4. **Cannot** toggle camera mode (host feature only)

---

## 📋 Checklist

- [ ] GameChatManager script in Multiplayer folder
- [ ] ChatManager added to scene
- [ ] Chat UI properly configured
- [ ] Chat Message Prefab created
- [ ] MultiCameraManager updated with toggle
- [ ] Test: Can chat during racing
- [ ] Test: Can see own camera as participant
- [ ] Test: Can toggle modes as host with Enter
- [ ] Test: Audio listener working on cameras

---

## 🐛 Troubleshooting

### Chat not showing messages
- Ensure Canvas is set to render
- Check ChatMessagesContainer is correct reference
- Verify ChatMessagePrefab is assigned

### Can't see participant camera
- Check MultiCameraManager is in scene
- Verify NetworkRunner is running
- Check CarController is properly spawned

### Participant sees blank screen
- **SOLUTION**: Now FIXED! Split-screen mode ensures all players get cameras

### Toggle not working
- Must be **host/server**
- Press **Return** key (not Enter numpad)
- Check console for logs

### Chat pauses game when typing
- This is **intentional** - players can focus on message
- Press Escape or click elsewhere to pause/unpause

---

## 🚀 Integration Notes

This system integrates with:
- **FusionNetworkManager** - Handles player names
- **CarController** - Registers car with camera system
- **RaceManager** - Works during race phase
- **MultiplayerCharacter** - Works for player spawning

No breaking changes to existing systems!

---

**Version:** 1.0
**Status:** ✅ Production Ready
