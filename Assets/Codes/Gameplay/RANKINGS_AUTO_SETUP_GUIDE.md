# 🏁 Rankings Auto Setup Guide

## ✅ Cách Sử Dụng

### **1️⃣ Thêm Scripts**

Scripts đã tạo:
- `RankingsAutoSetup.cs` - Tự động tạo UI
- `RankingItemUI.cs` - Display mỗi ranking item

(Có sẵn trong `Assets/Codes/Gameplay/`)

---

### **2️⃣ Setup Trong GamePlay Scene**

**A. Select GameEndChatManager GameObject:**

```
GameEndCanvas
└─ GameEndChatManager ← Select cái này
```

**B. Inspector → Rankings UI section:**

```
Rankings UI:
├─ Rankings Container: (để trống - auto setup sẽ tạo)
├─ Ranking Item Prefab: (để trống - auto setup sẽ tạo)
├─ Rankings Title: (để trống - auto setup sẽ tạo)
└─ ✓ Auto Setup Rankings: TRUE (bật)
```

---

### **3️⃣ Chạy Game**

**Khi GameEndChatManager Start():**
1. ✅ Auto detect rankingItemPrefab is NULL
2. ✅ Tự động add RankingsAutoSetup component
3. ✅ Tự động tạo toàn bộ UI hierarchy:
   - RankingsPanel
   - RankingsScrollView
   - Content (với VerticalLayoutGroup)
   - RankingItemPrefab

---

### **4️⃣ Xem Kết Quả**

**Khi race kết thúc:**
```
🏆 FINAL RANKINGS
─────────────────────
#1 Player1 - 23.45s
#2 Player2 - 25.12s
#3 Player3 - 26.78s
```

---

## 🎨 **Tùy Chỉnh Giao Diện**

### **Màu Sắc Hạng**

Mở `RankingItemUI.cs` → `Initialize()`:

```csharp
if (position == 1)
    positionText.color = new Color(1f, 0.84f, 0f);        // Gold ⭐
else if (position == 2)
    positionText.color = new Color(0.75f, 0.75f, 0.75f);  // Silver
else if (position == 3)
    positionText.color = new Color(0.8f, 0.5f, 0.2f);     // Bronze
```

### **Font Size**

`RankingsAutoSetup.cs` → `CreateRankingItemPrefab()`:

```csharp
posText.fontSize = 24;      // Position
nameText.fontSize = 22;     // Name
timeText.fontSize = 22;     // Time
```

### **Spacing & Layout**

`RankingsAutoSetup.cs` → `CreateRankingsPanel()`:

```csharp
vlg.spacing = 10;           // Khoảng cách các item
contentVLG.spacing = 5;     // Spacing trong Content
```

---

## 🐛 **Debug**

**Console sẽ show:**
```
[RankingsAutoSetup] ✅ RankingsPanel created
[RankingsAutoSetup] ✅ RankingItemPrefab created
[RankingsAutoSetup] ✅ References wired up
[RankingsAutoSetup] ✅ Setup complete!

[GameEndChatManager] ✅ Auto setup rankings completed
[GameEndChatManager] ✅ Added ranking: #1 Player1 - 23.45s
[GameEndChatManager] ✅ Added ranking: #2 Player2 - 25.12s
[GameEndChatManager] ✅ Added ranking: #3 Player3 - 26.78s
```

---

## ⚙️ **Manual Setup (Nếu Không Muốn Auto)**

**Nếu muốn setup thủ công:**

1. Set `Auto Setup Rankings: FALSE`
2. Tạo UI theo hướng dẫn trong `README.md`
3. Gán references trong inspector

---

**✅ Setup xong! Rankings sẽ hiện khi race kết thúc!** 🏆
