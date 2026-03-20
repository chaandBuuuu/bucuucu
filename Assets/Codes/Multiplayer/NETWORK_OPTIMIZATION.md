# Network Optimization Guide - Tối Ưu Hóa Hiệu Năng Multiplayer

## 🎯 Mục Tiêu

Đảm bảo game chạy mượt mà (60 FPS) mà không bị lag hoặc delay lớn

## 📊 Network Optimization Strategies

### 1. Photon Settings Optimization

```csharp
// Trong PhotonNetworkManager.Start()
public void OptimizeNetworkSettings()
{
    // Tối ưu tốc độ gửi dữ liệu
    PhotonNetwork.SendRate = 60;              // 60 messages per second
    PhotonNetwork.SerializationRate = 60;     // 60 serializations per second
    
    // Tối ưu connection
    var customSettings = PhotonNetwork.PhotonServerSettings;
    customSettings.AppSettings.FixedRegion = "us"; // Chọn region gần nhất
}
```

### 2. Data Serialization Optimization

#### ❌ Không Tối Ưu (Gửi mỗi frame)
```csharp
public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
{
    // Gửi TẤT CẢ mỗi frame → Quá nhiều data
    if (stream.IsWriting)
    {
        stream.SendNext(transform.position);    // 12 bytes
        stream.SendNext(transform.rotation);    // 16 bytes
        stream.SendNext(moveInput);             // 8 bytes
        stream.SendNext(health);                // 4 bytes
        stream.SendNext(animationState);        // 4 bytes
        // Tổng: 44 bytes x 60 FPS = 2640 bytes/sec per player
        // 4 players = 10,560 bytes/sec (102 kbps) - QUÁ NHIỀU!
    }
}
```

#### ✅ Tối Ưu (Chỉ gửi khi thay đổi)
```csharp
private Vector3 lastSyncPosition;
private float lastSyncRotation;
private Vector2 lastSyncInput;

public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
{
    if (stream.IsWriting)
    {
        // Chỉ gửi khi dữ liệu thay đổi
        bool posChanged = Vector3.Distance(transform.position, lastSyncPosition) > 0.1f;
        bool rotChanged = Mathf.Abs(transform.eulerAngles.z - lastSyncRotation) > 1f;
        bool inputChanged = moveInput != lastSyncInput;
        
        stream.SendNext(posChanged);
        if (posChanged)
        {
            stream.SendNext(transform.position);
            lastSyncPosition = transform.position;
        }
        
        stream.SendNext(rotChanged);
        if (rotChanged)
        {
            stream.SendNext(transform.eulerAngles.z);
            lastSyncRotation = transform.eulerAngles.z;
        }
        
        stream.SendNext(inputChanged);
        if (inputChanged)
        {
            stream.SendNext(moveInput);
            lastSyncInput = moveInput;
        }
        
        // Tổng: ~8 bytes/sync (khi có thay đổi)
        // Nếu sync 10x/sec = 80 bytes/sec per player
        // 4 players = 320 bytes/sec (2.5 kbps) - GỤN NHẤT!
    }
    else
    {
        bool posChanged = stream.ReceiveNext<bool>();
        if (posChanged)
            networkPosition = stream.ReceiveNext<Vector3>();
            
        bool rotChanged = stream.ReceiveNext<bool>();
        if (rotChanged)
            networkRotation = stream.ReceiveNext<float>();
            
        bool inputChanged = stream.ReceiveNext<bool>();
        if (inputChanged)
            moveInput = stream.ReceiveNext<Vector2>();
    }
}
```

### 3. Compression Strategies

```csharp
// Nén dữ liệu trước khi gửi
public class CompressedVector3
{
    public static byte[] Compress(Vector3 v)
    {
        // Chuyển float sang short (bớt precision, giảm dung lượng)
        byte[] data = new byte[6];
        System.BitConverter.GetBytes((short)(v.x * 100)).CopyTo(data, 0);
        System.BitConverter.GetBytes((short)(v.y * 100)).CopyTo(data, 2);
        System.BitConverter.GetBytes((short)(v.z * 100)).CopyTo(data, 4);
        return data; // 6 bytes thay vì 12 bytes
    }
    
    public static Vector3 Decompress(byte[] data)
    {
        return new Vector3(
            System.BitConverter.ToInt16(data, 0) / 100f,
            System.BitConverter.ToInt16(data, 2) / 100f,
            System.BitConverter.ToInt16(data, 4) / 100f
        );
    }
}
```

### 4. Update Frequency Optimization

```csharp
// Không phải mỗi frame mới gửi dữ liệu
private float networkUpdateTimer = 0f;
private float networkUpdateInterval = 0.1f; // Gửi mỗi 0.1 giây

private void Update()
{
    networkUpdateTimer += Time.deltaTime;
    
    if (networkUpdateTimer >= networkUpdateInterval)
    {
        networkUpdateTimer = 0f;
        // Tính toán dữ liệu để gửi
        // Không càng sớm = càng ít dữ liệu
    }
}
```

### 5. Interest Management

```csharp
// Chỉ sync với player gần nhất (Distance-based)
public bool ShouldSynchronizeWith(MultiplayerCharacter otherPlayer)
{
    float distance = Vector3.Distance(transform.position, otherPlayer.transform.position);
    
    // Chỉ sync nếu trong khoảng cách
    if (distance > 50f)
        return false; // Quá xa, không cần sync
    
    if (distance > 30f)
        return networkUpdateInterval > 0.2f; // Cách xa hơn, update ít hơn
    
    return true; // Gần, sync bình thường
}
```

## 🚀 Performance Benchmarks

### Bandwidth Usage Comparison

| Phương Pháp | Dữ liệu/sec (1 player) | Tổng 4 players |
|------------|----------------------|----------------|
| ❌ Mỗi frame (60 FPS) | 2640 bytes | 102 kbps |
| ⚠️ 20 FPS | 880 bytes | 34 kbps |
| ✅ Change-based (10 updates/sec) | 80 bytes | 2.5 kbps |
| ✅✅ Compressed (10 updates/sec) | 40 bytes | 1.2 kbps |

### Latency Expectations

```
Latency < 50ms   : Rất tốt (LAN, gần server)
Latency 50-100ms : Tốt (local region)
Latency 100-200ms: Chấp nhận được
Latency > 200ms  : Khó chơi (quá xa)
```

## 🔧 Implementation Checklist

### PhotonNetworkManager Setup
```csharp
✓ SendRate = 60
✓ SerializationRate = 60
✓ FixedRegion = nearest server
✓ Cache enabled
```

### MultiplayerCharacter Optimization
```csharp
✓ Chỉ gửi dữ liệu khi thay đổi
✓ Sử dụng networkUpdateInterval
✓ Chỉ sync vị trí/rotation/animation
✓ Disable input xử lý cho non-owner
```

### Network Bandwidth Saving
```csharp
✓ Giảm precision (short thay vì float)
✓ Interest management (không sync quá xa)
✓ Update interval (không mỗi frame)
✓ RPC caching chỉ khi cần
```

## 🎯 Troubleshooting

### "Lag/Delay Cao"
```csharp
// 1. Kiểm tra SendRate + SerializationRate
Debug.Log(PhotonNetwork.SendRate); // Nên là 60

// 2. Kiểm tra bandwidth sử dụng
// Dùng Network Monitor trong Photon Client

// 3. Kiểm tra region
// Chọn region gần nhất người chơi
PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "us";
```

### "Packet Loss"
```csharp
// Photon tự động xử lý retransmission
// Nhưng tối thiểu hóa packet để giảm loss
// Gửi ít data hơn = ít chance loss
```

### "Desync (Character ở vị trí khác nhau)"
```csharp
// Sử dụng PhotonMessageInfo.timestamp
public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
{
    // Tính latency
    double latency = PhotonNetwork.GetPing() / 1000.0;
    
    // Extrapolate position dựa trên velocity + latency
    Vector3 predictedPos = networkPosition + (currentVelocity * (float)latency);
    
    // Lerp tới predicted position
    transform.position = Vector3.Lerp(transform.position, predictedPos, lerpSpeed);
}
```

## 📈 Profiling Tools

### Photon Client Monitor
```
Thêm component: PhotonClientPhotonLobbyStatsGui
→ Hiển thị bandwidth, message count, latency
```

### Unity Performance Profiler
```
Window > Analysis > Profiler
→ Network Messages (send/receive)
→ Memory usage
→ GC Allocs (garbage collection)
```

## 🎓 Best Practices

1. **Reliable Channels chỉ cho dữ liệu quan trọng**
   - Unreliable: Position, rotation, animation
   - Reliable: Game events, player actions

2. **Batch Updates**
   ```csharp
   // Gộp nhiều update vào một lần gửi
   ```

3. **Reduce Instantiate Calls**
   - Dùng pooling cho bullets, effects
   - PhotonNetwork.Instantiate chỉ khi cần

4. **Client-Side Prediction**
   - Không chờ server reply cho movement
   - Predict ngay, correct sau

5. **Server Authority cho Game Logic**
   - Server quyết định health, ammo, game state
   - Client chỉ send input

## 📝 Checklist Optimization Cuối Cùng

- [ ] SendRate, SerializationRate = 60
- [ ] Dữ liệu sync chỉ khi thay đổi
- [ ] networkUpdateInterval = 0.1 giây (10 updates/sec)
- [ ] Không sync quá 30m/vị trí
- [ ] Dùng uint/ushort thay vì float khi có thể
- [ ] RPC chỉ khi cần (không mỗi frame)
- [ ] PhotonView Observed chỉ script này
- [ ] Test trên connection thực (không localhost)

---

**Với các tối ưu hóa này, game sẽ chạy mượt mà dưới 100ms latency! 🚀**
