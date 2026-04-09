# 🏎️ Hướng Dẫn Setup Racing Game - Cách Tự Động & Cách Thủ Công

## Mục Lục
1. [Cách 1: Thiết Lập Tự Động (Auto Setup)](#cách-1-thiết-lập-tự-động)
2. [Cách 2: Thiết Lập Thủ Công (Manual Setup)](#cách-2-thiết-lập-thủ-công)
3. [Tiếp Theo](#tiếp-theo)

---

## Cách 1: Thiết Lập Tự Động

### Bước 1: Tạo Scene
- Tạo scene mới tên `Racing` trong thư mục `Assets/Scenes/`

### Bước 2: Tạo Setup GameObject
1. Tạo một **GameObject rỗng** (Ctrl+Shift+N) tên `SetupManager`
2. Gắn script `RacingGameAutoSetup` vào GameObject
3. Xem Inspector, script sẽ hiển thị các settings

### Bước 3: Chạy Auto Setup
**Trong Unity Editor:**
- Chọn `SetupManager` GameObject
- Ở Script panel, nhấn nút **"Setup Racing Scene"** (nếu có)
- Hoặc gọi từ code: `setupManagerScript.SetupRacingScene();`

**Kết Quả:**
- ✅ RaceManager được tạo
- ✅ FinishLine được tạo
- ✅ 4 SpawnPoints được tạo
- ✅ 4 Powerup Items được tạo
- ✅ RaceUI Canvas được tạo

### Ưu Điểm Cách Tự Động:
- Nhanh chóng (1-2 phút)
- Tất cả components tự động được gắn
- Không cần cấu hình thủ công
- Giảm lỗi nhân công

---

## Cách 2: Thiết Lập Thủ Công

### Bước 1: Tạo Scene
- Tạo scene mới tên `Racing` trong thư mục `Assets/Scenes/`
- Xóa Main Camera (hoặc giữ lại)

### Bước 2: Tạo RaceManager
1. **Tạo GameObject:** Right-click → Create Empty → Đặt tên `RaceManager`
2. **Gắn Script:** Thêm component `RaceManager`
3. **Cấu Hình:**
   - Laps To Win: `4`
   - Winner Panel: (để sau khi tạo UI)

### Bước 3: Tạo Finish Line
1. **Tạo GameObject:** Create Empty → Đặt tên `FinishLine`
2. **Thêm Components:**
   - `BoxCollider2D` (isTrigger = True)
   - `FinishLineDetector`
3. **Cấu Hình Collider:**
   - Size: `(3, 8)`
   - Center: `(0, 0)`
4. **Gắn RaceManager:**
   - Trong Inspector của `FinishLineDetector`, kéo `RaceManager` từ Hierarchy vào field `raceManager`

### Bước 4: Tạo Spawn Points
1. **Tạo Container:** Create Empty → Đặt tên `SpawnPoints`
2. **Tạo 4 Child Gamebjects:**
   ```
   SpawnPoints/
   ├── SpawnPoint_0  (Position: -5, 5, 0)
   ├── SpawnPoint_1  (Position: 5, 5, 0)
   ├── SpawnPoint_2  (Position: -5, -5, 0)
   └── SpawnPoint_3  (Position: 5, -5, 0)
   ```
3. **Gắn Spawner:**
   - Bên Container `SpawnPoints`, thêm component `RacingCarSpawner`
   - Script sẽ tự động tìm các SpawnPoint children

### Bước 5: Tạo Powerup Items
1. **Tạo Container:** Create Empty → Đặt tên `Powerups`
2. **Tạo 4 Powerup Objects:**
   ```
   Powerups/
   ├── Powerup_Shield     (Position: 10, 0, 0)
   ├── Powerup_Gun        (Position: -10, 0, 0)
   ├── Powerup_SpeedBoost (Position: 0, 10, 0)
   └── Powerup_Trap       (Position: 0, -10, 0)
   ```
3. **Mỗi Powerup cần:**
   - **Components:**
     - `CircleCollider2D` (Radius: 0.5, isTrigger: True)
     - `SpriteRenderer` (hoặc hình ảnh tạm)
     - `PowerupPickup`
   - **Màu sắc (SpriteRenderer):**
     - Shield: Cyan (0, 1, 1)
     - Gun: Red (1, 0, 0)
     - SpeedBoost: Yellow (1, 1, 0)
     - Trap: Magenta (1, 0, 1)

### Bước 6: Tạo Race UI Canvas
1. **Tạo Canvas:** Right-click → URI/Input Method → Canvas → Đặt tên `RaceUICanvas`
   - Render Mode: `Screen Space - Overlay`
   - Canvas Scaler: `Scale With Screen Size`

2. **Tạo Text Objects (TextMeshPro):**
   
   **LapCounter:**
   - Name: `LapCounter`
   - Position: Top-Left (20, -20)
   - Font Size: 36
   - Text: "Vòng: 1/4"
   - Color: White
   
   **Timer:**
   - Name: `Timer`
   - Position: Top-Center (0, -20)
   - Font Size: 36
   - Text: "Thời gian: 0:00"
   - Color: White
   - Alignment: Top Center
   
   **Speed Display:**
   - Name: `SpeedDisplay`
   - Position: Top-Right (20, -20)
   - Font Size: 32
   - Text: "Tốc độ: 0"
   - Color: White
   
   **PowerUP Display:**
   - Name: `PowerupDisplay`
   - Position: Bottom-Right (20, 20)
   - Font Size: 24
   - Text: "Powerup: Không có"
   - Color: White

### Ưu Điểm Cách Thủ Công:
- Hiểu rõ từng bước
- Kiểm soát tuyệt đối mỗi component
- Học tập cách game được cấu trúc
- Dễ debug nếu có lỗi

---

## Tiếp Theo

### 1️⃣ Tạo Car Prefab
- Tạo GameObject với Sprite 2D (hình ảnh chiếc xe)
- Gắn components:
  - `BoxCollider2D` (kinematic)
  - `Rigidbody2D` (Gravity Scale: 0, Constraints: Freeze Rotation Z)
  - `CarController`
  - `NetworkTransform` (Fusion networking)
  - `NetworkObject` (Fusion networking)
- Lưu thành Prefab trong `Assets/Prefab/Racing/CarPrefab.prefab`

### 2️⃣ Assign Car Prefab
- Ngành Scene, chọn `RacingCarSpawner`
- Kéo Car Prefab vào field `carPrefab` trong Inspector

### 3️⃣ Tạo Track Layout
- Vẽ đường đi bằng các sprites hoặc quạn lồi
- Tạo Walls với Colliders để ngăn chặn xe ra ngoài
- Đặt Powerup Items dọc theo đường đi

### 4️⃣ Kết Nối Lobby → Racing
- Trong `GameStartController`, cảnh `Racing` đã được set
- Khi Host nhấn "Bắt Đầu Đua", cảnh sẽ load

### 5️⃣ Test Game
- Nhấn Play
- 4 Player (hoặc 1 player chế độ offline)
- Kiểm tra:
  - ✅ Xe xuất hiện đúng vị trí
  - ✅ Di chuyển bằng WASD
  - ✅ Trôi (Shift)
  - ✅ Thu thập Powerup (Q)
  - ✅ Qua FinishLine → Đếm vòng

---

## Bảng So Sánh

| Tính Năng | Tự Động | Thủ Công |
|-----------|---------|---------|
| **Thời gian** | ~2 phút | ~15 phút |
| **Độ chính xác** | 100% | Phụ thuộc |
| **Hiểu biết** | Thấp | Cao |
| **Linh hoạt** | Không | Có |
| **Debug** | Khó | Dễ |

---

## 🐛 Debug Gợi Ý

**Nếu xe không xuất hiện:**
- Kiểm tra Car Prefab có gắn `NetworkObject` không?
- RaceManager có được set trong FinishLineDetector?
- SpawnPoints có được gắn đúng tên?

**Nếu FusionNetork lỗi:**
- Chắc chắn `FusionNetwork.Instance` đã được khởi tạo từ Lobby
- Car Prefab phải đảo tính "networkPrefab"

**Nếu UI không hiện:**
- Canvas Render Mode có phải "Screen Space - Overlay"?
- TextMeshPro objects có là children của Canvas?

---

## 📝 Ghi Chú
- Sau khi setup, bạn có thể chỉnh lại vị trí Spawn Points, Powerups theo ý muốn
- Nếu dùng Cách Tự Động, bạn vẫn có thể chỉnh lại các vị trí sau khi tạo
- Vẽ track đẹp giúp trải nghiệm game tốt hơn!

**Chúc bạn setup thành công! 🎉**
