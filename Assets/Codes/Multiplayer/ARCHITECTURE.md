# 🏛️ Architecture & Design Patterns

Tài liệu về cấu trúc kiến trúc, design patterns, và best practices cho hệ thống multiplayer.

## 🏗️ Kiến Trúc Hệ Thống

### High-Level Architecture

```
┌─────────────────────────────────────────┐
│        Photon Cloud Server              │
│     (Room Management, Messaging)        │
└────────────┬────┬────┬────────────────┘
             │    │    │
        ┌────▼────▼────▼────┐
        │ Local Client Area │    (Each Player/Device)
        ├─────────────────────┤
        │ ┌─────────────────┐ │
        │ │ Network Manager │ │
        │ │  (Photon PUN2)  │ │
        │ └────────┬────────┘ │
        │          │         │
        │ ┌────────▼───────────────────────┐
        │ │ Game State Manager              │
        │ │ ├─ Lobby State                  │
        │ │ ├─ Character Select State       │
        │ │ ├─ Game State                   │
        │ │ └─ Game Over State              │
        │ └────────┬────────────────────────┘
        │          │
        │ ┌────────▼──────────────────────────┐
        │ │ Multiplayer Components            │
        │ │ ├─ MultiplayerCharacter           │
        │ │ ├─ PlayerSpawner                  │
        │ │ ├─ CameraFollow                   │
        │ │ └─ GameLobbyUI                    │
        │ └──────────────────────────────────┘
        └─────────────────────────────────────┘
```

### Layer Architecture

```
┌─────────────────────────────────────┐
│     UI Layer (GameLobbyUI)          │
│  ├─ Login Panel                     │
│  ├─ Lobby Panel                     │
│  └─ Character Select Panel          │
└──────────────────┬──────────────────┘
                   │
┌──────────────────▼──────────────────┐
│  Network Layer (Photon)             │
│  ├─ Connection Management           │
│  ├─ Room Management                 │
│  └─ Data Serialization              │
└──────────────────┬──────────────────┘
                   │
┌──────────────────▼──────────────────┐
│  Game Logic Layer                   │
│  ├─ GameManager                     │
│  ├─ GameStartController             │
│  └─ Character Control               │
└──────────────────┬──────────────────┘
                   │
┌──────────────────▼──────────────────┐
│  Entity Layer (MonoBehaviour)       │
│  ├─ MultiplayerCharacter            │
│  ├─ CameraFollow                    │
│  └─ Enemy/NPC (không có sẵn)       │
└─────────────────────────────────────┘
```

## 🎨 Design Patterns Used

### 1. **Singleton Pattern**

```csharp
// PhotonNetworkManager.Instance
public class PhotonNetworkManager : MonoBehaviourPunCallbacks
{
    public static PhotonNetworkManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

// Sử dụng:
PhotonNetworkManager.Instance.CreateRoom();
```

**Tại sao?**
- Chỉ cần 1 instance network manager
- Dễ access từ bất cứ đâu
- Đạo diễn DontDestroyOnLoad

### 2. **Observer Pattern (Photon Callbacks)**

```csharp
public class GameLobbyUI : MonoBehaviourPunCallbacks
{
    public override void OnConnectedToPhoton()
    {
        // React khi connected
    }
    
    public override void OnJoinedRoom()
    {
        // React khi join room
    }
    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // React khi player property thay đổi
    }
}
```

**Tại sao?**
- Decoupled từ network code
- Tự động callback khi event xảy ra
- Easy event handling

### 3. **Factory Pattern (Prefab Instantiation)**

```csharp
// PlayerSpawner.cs
public void RPC_SpawnCharacter()
{
    GameObject playerObj = PhotonNetwork.Instantiate(
        playerPrefabName,  // Factory path
        spawnPoint,
        Quaternion.identity
    );
    // Photon handles instantiation on all clients
}
```

**Tại sao?**
- Tạo object qua Photon (sync tất cả client)
- Avoid manual instantiation
- Consistent object creation

### 4. **Command Pattern (RPC)**

```csharp
// GameStartController.cs
[PunRPC]
private void RPC_StartGame()
{
    // Execute on all clients simultaneously
    StartGameScene();
}

// Call from master client:
photonView.RPC(nameof(RPC_StartGame), RpcTarget.AllBuffered);
```

**Tại sao?**
- Encapsulate actions thành commands
- Execute on multiple clients
- Buffered = stores last execution result

### 5. **Strategy Pattern (Character Selection)**

```csharp
// MultiplayerCharacter.cs
private void SetCharacterVisuals(int index)
{
    // Different strategy cho mỗi character
    switch(index)
    {
        case 0: SetupHacker(); break;
        case 1: SetupGhostHunter(); break;
        case 2: SetupPriest(); break;
        case 3: SetupScientist(); break;
    }
}

private void SetupHacker()
{
    spriteRenderer.color = new Color(0.8f, 0.3f, 0.3f);
    // Load Hacker-specific components
}
```

**Tại sao?**
- Tách logic setup per character
- Easy add new characters
- Flexible behavior

### 6. **Observer Pattern (IPunObservable)**

```csharp
public class MultiplayerCharacter : MonoBehaviourPun, IPunObservable
{
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            // Owner sends data
        }
        else
        {
            networkPosition = (Vector2)stream.ReceiveNext();
            // Non-owner receives data
        }
    }
}
```

**Tại sao?**
- Automatic sync di mỗi serialization
- Photon handles scheduling
- Easy bandwidth optimization

## 📊 State Machine

```
┌──────────────┐
│   Offline    │
└────┬─────────┘
     │ Connect
     ▼
┌─────────────────┐
│   Connecting    │
└────┬────────────┘
     │
     ▼
┌─────────────────┐
│   Lobby         │
├─────────────────┤
│ Host/Join Room  │
└────┬────────────┘
     │
     ▼
┌──────────────────────┐
│ Character Select     │
├──────────────────────┤
│ Select Character     │
│ Wait for 4 Players   │
│ Ready → Game Starts  │
└────┬─────────────────┘
     │
     ▼
┌──────────────────┐
│  In Game         │
├──────────────────┤
│ Play Game        │
│ Pause/Resume     │
└────┬─────────────┘
     │
     ▼
┌──────────────────┐
│  Game Over       │
├──────────────────┤
│ Return to Lobby  │
└────┬─────────────┘
     │
     ▼
   Start
```

## 🔄 Data Flow

### Character Movement Sync Flow

```
Player 1 Input (Local)
        │
        ▼
┌──────────────────────────┐
│ TopDownController        │
│ Handle Movement Physics  │
└────────────┬─────────────┘
             │
             ▼
┌──────────────────────────┐
│ MultiplayerCharacter     │
│ OnPhotonSerializeView()  │ (every 0.1s)
│ Send Position/Rotation   │
└────────────┬─────────────┘
             │
             ▼
┌──────────────────────────┐
│ Photon Cloud             │
│ Serialize to Byte Array  │
│ Send to Other Clients    │
└────────────┬─────────────┘
             │
    ┌────────┴────────┐
    │                 │
    ▼                 ▼
Player 2 Client   Player 3 Client
    │                 │
    ▼                 ▼
Receive Data        Receive Data
Update Position     Update Position
Smooth Lerp           Smooth Lerp
Render             Render
```

## 🎯 Component Responsibilities

### PhotonNetworkManager
- Khởi tạo kết nối Photon
- Tạo/Join room
- Quản lý player properties
- Broadcast events

### GameLobbyUI
- Render login panel
- Render lobby panel
- Render character select panel
- Respond to user input

### GameStartController
- Kiểm tra tất cả 4 người sẵn sàng
- Broadcast RPC StartGame
- Load level đồng bộ

### MultiplayerCharacter
- Xử lý input (chỉ owner)
- Update physics (chỉ owner)
- Serialize vị trí/rotation/animation
- Smooth network movement (non-owner)

### PlayerSpawner
- Spawn character prefab trên tất cả client
- Gắn camera vào owner
- Sync spawn points

### CameraFollow
- Follow target player
- Constrain within bounds
- Smooth camera movement

### GameManager
- Quản lý game state
- Track alive players
- Handle pause/resume
- End game logic

## 🔐 Security Considerations

### 1. **Authorization**
```csharp
// Chỉ owner authorize movement
if (!photonView.IsMine)
{
    DisableLocalInput();
    return;
}
```

### 2. **RPC Validation**
```csharp
// Validate RPC data
[PunRPC]
private void RPC_PlayerDamaged(int damage)
{
    if (!photonView.IsMasterClient)
        return; // Chỉ accept từ master
    
    if (damage < 0 || damage > 100)
        return; // Validate range
}
```

### 3. **Server Authority**
```csharp
// Tất cả game logic critical phải qua master
if (!PhotonNetwork.IsMasterClient)
    return;

// Xử lý game logic ở server
```

## 🚀 Optimization Techniques

### 1. **Object Pooling**
```csharp
// Thay vì tạo/xóa bullets liên tục
public class BulletPool : MonoBehaviour
{
    private Queue<Bullet> availableBullets = new();
    
    public Bullet GetBullet()
    {
        return availableBullets.Count > 0 
            ? availableBullets.Dequeue() 
            : Instantiate(bulletPrefab);
    }
    
    public void ReturnBullet(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
        availableBullets.Enqueue(bullet);
    }
}
```

### 2. **Network Culling**
```csharp
// Chỉ sync nếu trong khoảng cách
private bool ShouldSync()
{
    if (Vector3.Distance(transform.position, Camera.main.transform.position) > 50f)
        return false;
    return true;
}
```

### 3. **Message Batching**
```csharp
// Gộp nhiều update vào 1 message
private List<PlayerAction> pendingActions = new();

private void QueueAction(PlayerAction action)
{
    pendingActions.Add(action);
    if (pendingActions.Count >= 10)
        SendBatch();
}
```

## 📈 Scalability

### Từ 4 lên 8 Người

```csharp
// 1. Thay maxPlayersPerRoom
maxPlayersPerRoom = 8;

// 2. Tăng spawn points
spawnPoints = new Vector3[8] { ... };

// 3. Optimize network (8 player = 8x bandwidth)
PhotonNetwork.SendRate = 30; // Giảm từ 60
PhotonNetwork.SerializationRate = 30;

// 4. Implement interest management
bool ShouldSynchronizeWith(Player player)
{
    float distance = Vector3.Distance(...);
    return distance < MAX_SYNC_DISTANCE;
}
```

## 🔑 Key Takeaways

1. **Use Photon Callbacks** - Event-driven architecture
2. **Owner-Only Input** - Non-owner không xử lý input
3. **Master Client Authority** - Game logic chạy trên master
4. **Network Optimization First** - Bandwidth là resource hạn chế
5. **Smooth Client-Side Prediction** - Không chờ server reply
6. **Centralized Configuration** - MultiplayerConfig.asset
7. **Proper State Management** - Clear game states
8. **Testing on Real Network** - Không test trên localhost

---

**Kiến trúc này tỏ ra scalable, maintainable, và performant! 🏛️**
