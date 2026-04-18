# 🕐 Hệ Thống Đếm Ngược Game - Tổng Hợp

## **Tổng Quát**
Game có **2 hệ thống đếm ngược** riêng biệt:
1. **Pre-race Countdown** (3 → 2 → 1 → 0) - Trước khi bắt đầu đua
2. **Post-finish Countdown** (10 giây) - Sau khi người đầu tiên về đích

---

## **1️⃣ PRE-RACE COUNTDOWN (3,2,1,0)**

### 📌 Chịu Trách Nhiệm: `RaceManager.cs`
**File**: [Gameplay/RaceManager.cs](Gameplay/RaceManager.cs)

### 🎮 Cơ Chế Hoạt Động

#### **Networked Property** (mạng lưới tất cả player):
```csharp
[Networked] public int CountdownCounter { get; private set; } = -1;
```
- `-1` = Chưa bắt đầu đếm
- `3`, `2`, `1`, `0` = Giá trị đếm ngược

#### **Kích Hoạt Countdown**:
```csharp
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void RPC_StartRace()
{
    if (!HasStateAuthority) return;
    CountdownCounter = 3;      // Bắt đầu từ 3
    _countdownTimer = 0f;
    Debug.Log("[RaceManager] 🎬 Starting pre-race countdown!");
}
```

#### **Đếm Ngược trong FixedUpdateNetwork**:
```csharp
// Khi CountdownCounter >= 0, liên tục cập nhật
if (CountdownCounter >= 0)
{
    _countdownTimer += Runner.DeltaTime;
    int desiredCount = Mathf.Max(0, 3 - Mathf.FloorToInt(_countdownTimer));
    
    if (desiredCount != CountdownCounter)
    {
        CountdownCounter = desiredCount;  // Cập nhật: 3 → 2 → 1 → 0
        Debug.Log($"[RaceManager] Countdown: {CountdownCounter}");
    }

    if (_countdownTimer >= 3f)  // Sau 3 giây
    {
        CountdownCounter = -1;
        RaceStarted = true;     // ✅ BẮT ĐẦU ĐUA
        OnRaceStart?.Invoke();  // Fire event
    }
}
```

### ⚠️ VẤNĐỀ HIỆN CÓ
**RPC_StartRace() không được gọi từ bất cứ đâu trong codebase!**
- Phương thức được định nghĩa nhưng chưa có caller
- **Countdown pre-race chưa được tự động kích hoạt**

### 🔧 Nơi Cần Bổ Sung Để Kích Hoạt
Có thể gọi từ:
1. **RacingCarSpawner.cs** - Sau khi spawn xong tất cả xe (1-2 giây delay)
2. **GameStartController.cs** - Khi host bấm "Start Race"
3. **Tự động** - Sau 2-3 giây từ khi scene load xong

---

## **2️⃣ POST-FINISH COUNTDOWN (10 giây)**

### 📌 Chịu Trách Nhiệm: `RaceManager.cs`

### 🎮 Cơ Chế Hoạt Động

#### **Networked Property** (tất cả player):
```csharp
[Networked] public float FinishCountdown { get; private set; } = -1f;
```
- `-1` = Không countdown
- `10.0 → 9.9 → ... → 0.0` = Giá trị đếm ngược

#### **Kích Hoạt Tự Động**:
Khi **_PersonHandler đầu tiên qua đích_**:
```csharp
public void RegisterFinishCrossing(CarController car)
{
    // ...
    
    // Nếu đây là người finish đầu tiên → bắt đầu 10s countdown
    if (_firstFinisher == null)
    {
        _firstFinisher = car;
        FinishCountdown = finishCountdownDuration;  // Gán = 10f
        _finishCountdownTimer = 0f;
        Debug.Log($"[RaceManager] 🎉 First finisher: {car.name} - Starting 10s countdown!");
    }
}
```

#### **Đếm Ngược trong FixedUpdateNetwork**:
```csharp
if (FinishCountdown >= 0f)
{
    _finishCountdownTimer += Runner.DeltaTime;
    float desiredCountdown = Mathf.Max(-0.1f, 10f - _finishCountdownTimer);
    FinishCountdown = desiredCountdown;  // 10.0 → 9.9 → ... → 0.0

    if (_finishCountdownTimer >= 10f)  // Sau 10 giây
    {
        _finishCountdownTimer = 0f;
        FinishCountdown = -1f;
        FinishRace();  // ✅ KẾT THÚC ĐUA, TÍNH RANKING
        Debug.Log("[RaceManager] ⏱️ Finish countdown complete - Race ended!");
    }
}
```

### ✅ Hành Động Sau Countdown Hoàn Tất
```csharp
private void FinishRace()
{
    if (RaceFinished) return;
    RaceFinished = true;

    // Tính toán ranking (sắp xếp theo thời gian finish + khoảng cách)
    var rankings = CalculateFinalRankings();
    
    OnFinalRankings?.Invoke(rankings);  // Thông báo UI
    OnRaceEnd?.Invoke(winner);          // Thông báo người thắng
}
```

---

## **🎨 HIỂN THỊ COUNTDOWN - RaceUI.cs**

### 📌 Chịu Trách Nhiệm: `RaceUI.cs`
**File**: [Gameplay/RaceUI.cs](Gameplay/RaceUI.cs)

### 🎯 Pre-race Countdown Display
**CHƯA IMPLEMENT** - không tìm thấy code hiển thị countdown 3,2,1,0

### ✅ Post-finish Countdown Display
```csharp
private void UpdateCountdownUI()
{
    if (_raceManager == null || !_raceManager.IsSpawned)
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        return;
    }

    float finishCountdown = _raceManager.FinishCountdown;
    if (finishCountdown >= 0f)
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = $"⏳ Finish in {finishCountdown:F1}s";
        countdownText.color = finishCountdown < 5f ? Color.yellow : Color.white;
    }
    else
    {
        countdownText.gameObject.SetActive(false);
    }
}
```

**Hiển Thị**:
- Text: "⏳ Finish in 9.8s" (cập nhật mỗi frame)
- Màu: WHITE (10s ~ 5s), YELLOW (< 5s)

---

## **📊 FLOW COUNTDOWN TOÀN BỘ**

```
┌─────────────────────────────────────────────────────────┐
│  Scene Load (Racing)                                     │
├─────────────────────────────────────────────────────────┤
│  ↓                                                       │
│  ✅ RaceManager.Spawned()                               │
│     - CountdownCounter = -1 (chưa countdown)            │
│     - RaceStarted = false                               │
│                                                          │
│  ↓ [MISSING] RPC_StartRace() được gọi?                 │
│             (chưa tìm thấy caller)                      │
│                                                          │
│  ⏳ PRE-RACE COUNTDOWN (3,2,1,0)                        │
│     - 3 giây từ CountdownCounter=3 đến RaceStarted=true│
│     - [⚠️ Hiển thị: CHƯA IMPLEMENT trong UI]           │
│                                                          │
│  ↓                                                       │
│  🏁 RaceStarted = true                                  │
│     - Xe bắt đầu di chuyển                              │
│     - Đọc input từ player                               │
│     - RaceTimer bắt đầu tăng                            │
│                                                          │
│  ↓ [Player Di Chuyển]                                  │
│                                                          │
│  ↓ Xe 1 qua FinishLine                                 │
│     RegisterFinishCrossing() gọi                        │
│                                                          │
│  ⏱️ POST-FINISH COUNTDOWN (10 giây)                     │
│     - FinishCountdown = 10.0 → 9.9 → ... → 0.0         │
│     - ✅ Hiển thị: "⏳ Finish in 9.8s" (vàng < 5s)     │
│     - Xe khác tiếp tục đua                              │
│                                                          │
│  ↓ 10 giây kết thúc                                     │
│     FinishRace() gọi                                    │
│                                                          │
│  🏆 EXIT - Tính Ranking & Hiển Thị Kết Quả             │
└─────────────────────────────────────────────────────────┘
```

---

## **🔑 KHI NÀO COUNTDOWN XẢY RA**

| Loại Countdown | Kích Hoạt Bởi | Khi Nào |
|---|---|---|
| **Pre-race (3,2,1,0)** | ❓ `RPC_StartRace()` (CHƯA CALL) | Cần setup kích hoạt |
| **Post-finish (10s)** | ✅ `RegisterFinishCrossing()` AUTO | Khi player đầu qua đích |

---

## **🐛 BUG / THIẾU SÓT**

### ❌ Pre-race Countdown Chưa Được Auto-Trigger
- Phương thức `RPC_StartRace()` tồn tại nhưng **không được gọi từ bất cứ đâu**
- Cần thêm logic để gọi nó:
  - **Option 1**: Auto-trigger 2 giây sau khi tất cả xe spawn xong
  - **Option 2**: Host bấm button → gọi RPC_StartRace()
  - **Option 3**: Tự động sau scene load + 2-3 giây delay

### ⚠️ Pre-race Countdown Chưa Có UI Display
- Không tìm thấy code hiển thị "3", "2", "1", "0" trên màn hình
- Post-finish countdown CÓ hiển thị (code `UpdateCountdownUI()` trong RaceUI.cs)

---

## **💡 KHUYẾN NGHỊ SỬA LÝ**

1. **Bổ sung Pre-race Countdown Trigger**:
   - Thêm vào `RacingCarSpawner` sau khi spawn xong xe:
     ```csharp
     StartCoroutine(StartCountdownDelayed(2f));
     ```
   - Hoặc thêm button "Start Race" trong UI

2. **Thêm Pre-race Countdown Display**:
   - Thêm `countdownText` vào `RaceUI.cs`
   - Hiển thị "3", "2", "1", "0" như post-finish

3. **Test Thứ Tự**:
   ```
   Scene Load → 2s delay → Countdown 3,2,1,0 → Race Starts
   → Race Running → Player finish → Countdown 10,9,8... → Race End
   ```

---

## **📁 Liên Quan Files**

- [Gameplay/RaceManager.cs](Gameplay/RaceManager.cs) - Logic countdown
- [Gameplay/RaceUI.cs](Gameplay/RaceUI.cs) - Hiển thị countdown
- [Gameplay/FinishLineDetector.cs](Gameplay/FinishLineDetector.cs) - Phát hiện finish
- [Multiplayer/RacingCarSpawner.cs](Multiplayer/RacingCarSpawner.cs) - Spawn xe
- [Multiplayer/GameStartController.cs](Multiplayer/GameStartController.cs) - Host start button

---

**Cập nhật**: April 18, 2026
**Status**: Post-finish countdown ✅ HOẠT ĐỘNG | Pre-race countdown ⚠️ CHƯA KÍCH HOẠT
