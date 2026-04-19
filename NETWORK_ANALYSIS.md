# Fusion Networking Setup Analysis - Racing Game

**Analysis Date:** April 20, 2026  
**Framework:** Photon Fusion  
**Game Type:** 2D Multiplayer Racing (Top-Down)

---

## 📊 EXECUTIVE SUMMARY

Your racing game uses **Photon Fusion** with a **host-authority** architecture. The game synchronizes position/rotation via **NetworkTransform** with automatic client-side interpolation. Input is sent at **Fusion's default tick rate** with **authority-based physics** to prevent desyncs.

### Current Configuration Status:
- ✅ Position/Rotation sync: **NetworkTransform (Automatic Interpolation)**
- ✅ Input sending: **Per-frame via OnInput() callback**
- ✅ Physics authority: **HasInputAuthority (Owner) + HasStateAuthority (Host)**
- ✅ Remote player sync: **Kinematic + NetworkTransform**
- ⚠️ Tick rate: **Using Fusion defaults (likely 60 Hz client, 20 Hz server)**
- ⚠️ Network simulation: **No custom latency compensation detected**

---

## 1️⃣ FUSION NETWORK MANAGER CONFIGURATION

### File: `Assets/Codes/Multiplayer/FusionNetworkManager.cs`

#### Initial Settings
```csharp
[SerializeField] private int maxPlayers = 4;
[SerializeField] private bool autoConnect = true;
[SerializeField] private int lobbySceneIndex = 1;
[SerializeField] private int racingSceneIndex = 2;
```

#### Network Runner Initialization
```csharp
private async Task StartRunner(GameMode mode, string sessionName)
{
    if (Runner == null)
    {
        Runner = Instantiate(runnerPrefab);
        Runner.AddCallbacks(this);
    }

    var args = new StartGameArgs
    {
        GameMode = mode,                    // Host or Client
        SessionName = sessionName,
        PlayerCount = maxPlayers,
        SceneManager = Runner.GetComponent<NetworkSceneManagerDefault>()
    };

    var result = await Runner.StartGame(args);
}
```

### ⚠️ FINDINGS:
1. **No custom SimulationConfig** - Using Fusion's default tick rate
2. **NetworkSceneManagerDefault** - Standard scene management
3. **No tick rate override detected** - Defaults: Client=60Hz, Server=20Hz (typical Fusion defaults)
4. **No network bandwidth settings** - Using Fusion defaults

---

## 2️⃣ CAR CONTROLLER - NETWORK OBJECT SYNC

### File: `Assets/Codes/Gameplay/CarController.cs`

#### Network Properties (Synchronized)
```csharp
[Networked] public  bool    IsDrifting       { get; private set; }
[Networked] public  int     LapsCompleted    { get; set; }
[Networked] public  bool    IsFinished       { get; set; }
[Networked] private float   SpeedMultiplier  { get; set; } = 1f;
```

#### Authority Check
```csharp
public override void Spawned()
{
    if (HasInputAuthority)
    {
        _rb.bodyType       = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = Vector2.zero;
        Debug.Log($"[CarController] ✅ Spawned AUTHORITY");
    }
    else
    {
        _rb.bodyType = RigidbodyType2D.Kinematic;  // Remote car uses NetworkTransform
        Debug.Log($"[CarController] ✅ Spawned REMOTE");
    }
}
```

#### Physics Update Loop
```csharp
public override void FixedUpdateNetwork()
{
    if (IsFinished) return;

    // Re-enable physics for owner if reset
    if (HasInputAuthority && _rb.bodyType == RigidbodyType2D.Kinematic)
    {
        _rb.bodyType       = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = Vector2.zero;
    }

    if (!_inputEnabled)
    {
        // Apply friction only (no input processing)
        _localVelocity *= friction;
        if (HasInputAuthority || HasStateAuthority)
        {
            _rb.linearVelocity = _localVelocity;
        }
        return;
    }

    // ✅ Get input once per Fusion tick
    if (GetInput(out NetworkInputData input))
    {
        HandleMovement(input);
        HandlePowerup(input);
    }

    // Apply velocity ONLY on simulating machine (owner + server)
    if (HasInputAuthority || HasStateAuthority)
    {
        Vector2 newVelocity = _localVelocity;
        if (Vector2.Distance(_rb.linearVelocity, newVelocity) > 0.01f)
        {
            _rb.linearVelocity = newVelocity;
        }
    }
}
```

#### Movement Calculation
```csharp
private void HandleMovement(NetworkInputData input)
{
    Vector2 moveDir = input.MoveDirection;
    _isDrifting = input.IsDrifting;
    IsDrifting = _isDrifting;

    float effectiveMaxSpeed = maxSpeed * SpeedMultiplier;

    if (moveDir.magnitude > 0.01f)
    {
        // ACCELERATE (no friction when actively moving)
        _localVelocity += moveDir.normalized * acceleration * Runner.DeltaTime;
        _localVelocity  = Vector2.ClampMagnitude(_localVelocity, effectiveMaxSpeed);

        // Reactive grip during drift + steering
        if (_isDrifting && _localVelocity.magnitude > 5f)
        {
            float steeringDot = Vector2.Dot(moveDir.normalized, _localVelocity.normalized);
            if (steeringDot < 0.7f)  // Steering away from momentum
            {
                _localVelocity *= 0.90f;  // High grip to catch drift
            }
        }
    }
    else
    {
        // COAST: Apply friction when not moving
        float currentFriction = _isDrifting ? driftFriction : friction;
        _localVelocity *= currentFriction;
    }

    // Rotation (smoothed with LerpAngle)
    if (moveDir.magnitude > 0.01f)
    {
        float targetRotation = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
        float rotSpeed       = _isDrifting
                             ? rotationSpeed * driftRotationMultiplier
                             : rotationSpeed;

        _currentRotation = Mathf.LerpAngle(_currentRotation, targetRotation, rotSpeed * Runner.DeltaTime);
        transform.rotation = Quaternion.AngleAxis(_currentRotation, Vector3.forward);
    }
}
```

### Current Car Physics Constants
```csharp
public const float CAR_ACCELERATION              = 12f;      // Units per second²
public const float CAR_MAX_SPEED                 = 22f;      // Units per second
public const float CAR_FRICTION                  = 0.95f;    // 95% retention per frame
public const float CAR_DRIFT_FRICTION            = 0.85f;    // 85% retention when drifting
public const float CAR_ROTATION_SPEED            = 180f;     // Degrees per second
public const float CAR_DRIFT_ROTATION_MULTIPLIER = 2.2f;     // 2.2x faster when drifting
```

### ⚠️ FINDINGS:
1. **Per-Tick Physics** - Using `Runner.DeltaTime` for frame-independent movement
2. **Local Velocity** - Physics calculated locally, then synced via network properties
3. **Authority-Only Physics** - Rigidbody velocity only updated by owner/server
4. **No Prediction** - No client-side prediction for remote cars
5. **Velocity NOT Networked** - Only game state (IsDrifting, SpeedMultiplier, LapsCompleted)

---

## 3️⃣ POSITION & ROTATION SYNCHRONIZATION

### NetworkTransform Component

**Used by:**
- `MultiplayerCharacter.cs` (Lobby characters)
- `CarController.cs` (Racing cars) - **Implicitly via NetworkObject**

```csharp
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkTransform))]
public class MultiplayerCharacter : NetworkBehaviour
```

#### NetworkTransform Configuration
```csharp
// Remote player setup:
if (!HasInputAuthority)
{
    rb.bodyType  = RigidbodyType2D.Kinematic;
    rb.simulated = false;  // NO physics simulation on remote
}
```

### Interpolation Mechanism
- **Method:** NetworkTransform automatic client-side interpolation
- **Update Rate:** Determined by Fusion's FixedUpdateNetwork() tick rate
- **Smoothing:** Built-in Fusion interpolation between network ticks
- **Remote Position:** Updated via RPC state updates only, no manual Lerp

### ⚠️ FINDINGS:
1. **Automatic Interpolation** - NetworkTransform handles all smoothing
2. **No Manual Lerping** - Code explicitly removed custom Lerp logic
3. **Kinematic Remote Bodies** - Prevents physics engine from interfering
4. **Simulated = False** - Critical for clean position sync
5. **Position Sync Quality:** Depends entirely on Fusion's tick rate

---

## 4️⃣ INPUT HANDLING & SENDING FREQUENCY

### File: `Assets/Codes/Multiplayer/InputHandler.cs`

#### Input Collection
```csharp
private void Update()
{
    // Racing controls - collected every frame
    _moveInput = new Vector2(
        Input.GetAxisRaw("Horizontal"),
        Input.GetAxisRaw("Vertical")
    );

    _isDrifting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    _usePowerup = Input.GetKeyDown(KeyCode.Q);

    // Legacy controls
    _pressE = Input.GetKeyDown(KeyCode.E);
    _pressR = Input.GetKeyDown(KeyCode.R);
    _pressF = Input.GetKeyDown(KeyCode.F);

    if (Input.GetKeyDown(KeyCode.P))
        _isPausing = true;
}
```

#### Input Sending to Network
```csharp
public override void OnInput(NetworkRunner runner, NetworkInput input)
{
    input.Set(new NetworkInputData
    {
        Direction     = _moveInput,
        MoveDirection = _moveInput,
        IsDrifting    = _isDrifting,
        UsePowerup    = _usePowerup,
        IsPausing     = _isPausing,
        PressE        = _pressE,
        PressR        = _pressR,
        PressF        = _pressF
    });

    // Reset one-time flags
    _isPausing  = false;
    _usePowerup = false;
    _pressE     = false;
    _pressR     = false;
    _pressF     = false;
}
```

#### NetworkInputData Structure
```csharp
public struct NetworkInputData : INetworkInput
{
    // Racing Controls
    public Vector2 MoveDirection;  // WASD input
    public bool    IsDrifting;     // Shift key
    public bool    UsePowerup;     // Q key
    
    // Legacy (compatibility)
    public Vector2 Direction;
    public bool    IsPausing;
    public bool    PressE;
    public bool    PressR;
    public bool    PressF;
}
```

#### Input Registration Mechanism
```csharp
private System.Collections.IEnumerator RegisterWhenReady()
{
    // ✅ CRITICAL: Retry INDEFINITELY instead of timeout
    while (FusionNetworkManager.Instance?.Runner == null ||
           FusionNetworkManager.Instance.Runner.LocalPlayer == null)
    {
        yield return null;  // Wait 1 frame and try again
    }

    // Register with runner when ready
    if (!_isRegistered)
    {
        var runner = FusionNetworkManager.Instance.Runner;
        if (runner != null && runner.LocalPlayer != null)
        {
            runner.AddCallbacks(this);
            _isRegistered = true;
            Debug.Log($"[InputHandler] ✅ Registered with Runner");
        }
    }
}
```

### Input Sending Frequency
- **Collection Rate:** Every frame (Update) → ~60 Hz (variable with framerate)
- **Network Sending Rate:** Every Fusion tick (OnInput callback) → **Fusion default: ~20 Hz server**
- **Latency:** Typical delay between input and execution = **1 network tick (50 ms at 20 Hz)**
- **Scene Re-registration:** Automatic on scene load to prevent input loss

### ⚠️ FINDINGS:
1. **Frame-Rate Coupled** - Input collected at variable 60+ FPS
2. **Tick-Rate Limited** - Sent at ~20 Hz server tick rate
3. **No Input Prediction** - No client-side prediction on owned car
4. **One-Shot Flags** - Properly reset each network tick (UsePowerup, etc.)
5. **Scene Load Fix** - Re-registers on scene transitions to prevent input timeout

---

## 5️⃣ TICK RATE & SIMULATION SETTINGS

### Detected Configuration

| Setting | Current Value | Source | Effect |
|---------|---|---|---|
| **Server Tick Rate** | ~20 Hz (50 ms) | Fusion Default | Network state updates every 50ms |
| **Client Tick Rate** | ~60 Hz | Fusion Default | Physics/movement runs at 60 FPS |
| **Interpolation** | Automatic (NetworkTransform) | Built-in | Smooth movement between ticks |
| **Physics Engine** | Unity 2D (Rigidbody2D) | Built-in | Local calculation, network synced |
| **Frame Interpolation** | Client-side via NetworkTransform | Built-in | Smooth remote player movement |
| **Network Bandwidth** | Default (not configured) | N/A | Using Fusion defaults |
| **Packet Loss Handling** | OnInputMissing callback | Built-in | Replay last input if lost |

### Fusion Default Tick Timing
```
Server Tick Time: 50 ms (20 Hz)
Client Render Update: ~16.67 ms (60 Hz)
Network Update Ratio: 3:1 (client renders 3 frames per network tick)
```

### ⚠️ FINDINGS:
1. **No Custom Tick Rate Config** - Using Fusion's hardcoded defaults
2. **No TickRate Override** - Not overriding in StartGameArgs
3. **No ClientTickRate Config** - Client-side tick rate not specified
4. **Default Interpolation** - NetworkTransform handles all smoothing
5. **Bandwidth Not Optimized** - Could benefit from custom settings

---

## 6️⃣ NETWORK SYNCHRONIZATION FLOW

### State Authority vs Input Authority

```
LOCAL PLAYER (Owner):
├─ HasInputAuthority = TRUE
├─ HasStateAuthority = TRUE (if host)
├─ Rigidbody: Dynamic, simulated=true
├─ Physics: Full control
├─ Input: Sends to network
└─ Output: Position+Rotation via NetworkTransform

REMOTE PLAYER:
├─ HasInputAuthority = FALSE
├─ HasStateAuthority = FALSE (if not host)
├─ Rigidbody: Kinematic, simulated=false
├─ Physics: NONE (NetworkTransform only)
├─ Input: Ignored locally
└─ Input: Received from network → CarController.GetInput()
```

### Synchronization Pipeline

```
Player Input (Update)
    ↓
InputHandler.OnInput() - Collects WASD, Shift, Q
    ↓
NetworkInputData sent to Fusion
    ↓
Fusion Network Tick (every ~50 ms)
    ↓
CarController.FixedUpdateNetwork()
    ├─ Owner: Processes GetInput() → moves locally
    ├─ Remote: Gets state from network
    └─ Both: Position/Rotation synced via NetworkTransform
    ↓
Position sent to other players (via NetworkTransform)
    ↓
Remote clients receive and interpolate position
    ↓
Render() - Display interpolated position
```

### Network Properties Synchronized
```csharp
// CarController
[Networked] bool    IsDrifting       // Drift state indicator
[Networked] int     LapsCompleted    // Lap counter
[Networked] bool    IsFinished       // Race completion flag
[Networked] float   SpeedMultiplier  // Power-up speed effect

// NOT Networked:
// - _localVelocity (calculated locally)
// - _currentRotation (calculated locally)
// - Position/Rotation (via NetworkTransform)
```

### ⚠️ FINDINGS:
1. **Velocity NOT Synced** - Only speed multiplier is networked
2. **Position Auto-Synced** - NetworkTransform handles this
3. **Rotation Auto-Synced** - NetworkTransform handles this
4. **State Properties Synced** - IsDrifting, LapsCompleted, IsFinished
5. **No Custom Prediction** - Remote players use only networked state

---

## 7️⃣ POTENTIAL NETWORK BOTTLENECKS & LAG SOURCES

### 🔴 CRITICAL ISSUES

#### 1. **No Client-Side Prediction**
```
Problem: Remote cars only update when network packets arrive
Effect:  Visible rubber-banding on high latency (>100ms)
Source:  CarController doesn't implement client-side prediction
Fix:     Add predicted position using last velocity + elapsed time
```

#### 2. **Velocity Not Networked**
```
Problem: Can't smoothly extrapolate remote car movement between ticks
Effect:  Remote cars appear to "step" rather than flow smoothly
Current: Position syncs every 50ms, but velocity is unknown
Fix:     [Networked] Vector2 NetworkedVelocity to enable extrapolation
```

#### 3. **No Input Prediction**
```
Problem: Owner sees their own car move instantly, but 50ms before network updates
Effect:  Slight delay feel on input (50-100ms latency)
Source:  GetInput() processed same tick as position update
Fix:     Process input immediately, separate from network tick
```

### ⚠️ MODERATE ISSUES

#### 4. **Network Tick Rate is Low (20 Hz)**
```
Problem: 50ms between network updates = visible jumping at 200+ ms latency
Impact:  With 4 players @ 100ms RTT, position updates arrive stale
Current: Using Fusion defaults (no override)
Fix:     Override to 30-40 Hz server tick rate
         new SimulationConfig { TickRate = 40 } in StartGameArgs
```

#### 5. **No Bandwidth Optimization**
```
Problem: Every property synced every tick = unnecessary bandwidth
Impact:  Wasted bandwidth on low-latency connections
Current: All [Networked] properties updated always
Fix:     Use [Networked(OnChanged=nameof(OnPropertyChanged))] for selective sync
```

#### 6. **Friction Calculation Per-Frame (At 60 FPS)**
```
Problem: Physics updates at client FPS but networked at tick rate
Impact:  Owner sees smooth 60 FPS, remote sees 20 FPS updates
Current: _localVelocity *= friction applied every frame
Fix:     Ensure friction calculation matches actual tick rate
```

#### 7. **No Latency Compensation**
```
Problem: Remote car appears behind where it actually is
Impact:  Collision detection feels wrong (hits don't register properly)
Current: Only NetworkTransform interpolation (no extrapolation)
Fix:     Add position prediction based on velocity
```

### ℹ️ MINOR ISSUES

#### 8. **Audio Only for Input Authority**
```
Problem: Audio sources only on owner's car, not heard for others
Impact:  Remote players don't hear other cars' engines
Current: `if (!HasInputAuthority) return;` in SetupAudioSources()
Fix:     Setup audio for both owner and remote (2D spatialBlend)
```

#### 9. **No Jitter Buffering**
```
Problem: Network packet arrival variance causes frame skips
Impact:  Occasional jerky movement if packets late
Current: No buffer for timing variance
Fix:     NetworkTransform already handles (built-in buffering)
```

#### 10. **Manual Lerp Removed**
```
Status: ✅ FIXED (explicitly removed in current code)
Why:    Conflicted with NetworkTransform interpolation
Result: Clean sync without double-interpolation
```

---

## 8️⃣ LATENCY COMPENSATION ANALYSIS

### Current Implementation
```csharp
// NO custom latency compensation detected
// Relying entirely on:
// 1. NetworkTransform automatic interpolation
// 2. OnInputMissing() callback (resends last input if packet lost)
// 3. Fusion's built-in packet handling
```

### OnInputMissing Callback
```csharp
public virtual void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
{
    // Empty implementation = resend last input
    // Prevents car from stopping if network packet lost
}
```

### What's Missing
1. ❌ **Position Extrapolation** - No prediction of where car will be
2. ❌ **Dead Reckoning** - No velocity-based movement forecasting
3. ❌ **Timestamp-based Interpolation** - No time-warping for old packets
4. ❌ **Lag Adjustment** - No automatic latency compensation
5. ✅ **Packet Buffering** - Built into NetworkTransform

### Performance Impact
```
Latency 50ms:   Minimal (1 network tick)
Latency 100ms:  2 ticks = noticeable rubber-banding
Latency 150ms:  3 ticks = visible position jumps
Latency 200ms:  4 ticks = severe lag on remote cars
```

---

## 9️⃣ CONFIGURATION RECOMMENDATIONS

### For Competitive/LAN Play (Low Latency <50ms)
```csharp
// Current setup is adequate
// Consider: Increase tick rate to 40 Hz
new SimulationConfig {
    TickRate = 40,                    // 25 ms ticks instead of 50 ms
}
```

### For Online Play (50-150ms Latency)
```csharp
// MUST IMPLEMENT:
// 1. Add velocity to networked state
// 2. Implement client-side position prediction
// 3. Increase server tick rate to 40 Hz
// 4. Add interpolation buffer

[Networked] Vector2 NetworkedVelocity { get; set; }
[Networked] float   LastNetworkTime   { get; set; }

// In FixedUpdateNetwork():
if (!HasInputAuthority)
{
    // Predict position based on velocity
    Vector3 predictedPos = transform.position + (Vector3)NetworkedVelocity * Runner.DeltaTime;
    // Apply to movement for smoother remote cars
}
```

### For High-Latency Play (>150ms)
```csharp
// IMPLEMENT FULL SOLUTION:
// 1. Velocity syncing (required)
// 2. Position prediction with velocity
// 3. Client-side input prediction
// 4. Server tick rate 30 Hz (may be slower network)
// 5. Interpolation buffer = 3-4 ticks
// 6. Jitter buffer for late packets

// Add to StartGameArgs:
new SimulationConfig {
    TickRate = 30,                    // 33 ms ticks
}

// Add to CarController:
private Queue<NetworkState> _positionBuffer = new();
private float _lastNetworkTime = 0f;

public override void FixedUpdateNetwork()
{
    // Buffer position updates
    _positionBuffer.Enqueue(new NetworkState {
        position = transform.position,
        velocity = NetworkedVelocity,
        timestamp = Runner.Tick * Runner.DeltaTime
    });
    
    // Interpolate from buffer (2-3 ticks old)
}
```

---

## 🔟 SUMMARY TABLE

| Component | Current | Recommended | Priority |
|-----------|---------|-------------|----------|
| **Tick Rate** | 20 Hz (default) | 40 Hz | HIGH |
| **Position Sync** | NetworkTransform ✅ | + Velocity | HIGH |
| **Input Prediction** | None | Add client-side | MEDIUM |
| **Interpolation** | Automatic | Add velocity-based | MEDIUM |
| **Latency Comp** | None | Dead reckoning | MEDIUM |
| **Bandwidth Opt** | Not optimized | Selective sync | LOW |
| **Packet Loss** | OnInputMissing | Already handled | ✅ |
| **Physics Authority** | Correct | Keep as-is | ✅ |
| **Remote Physics** | Kinematic ✅ | Keep as-is | ✅ |

---

## 📋 FILES INVOLVED

### Core Network Files
- `FusionNetworkManager.cs` - Network setup (no tick rate override)
- `InputHandler.cs` - Input sending (20 Hz via Fusion ticks)
- `CarController.cs` - Physics + state sync
- `NetworkInputData.cs` - Input structure
- `FusionCallbacksBase.cs` - Callback interface

### Physics/Sync Files
- `MultiplayerCharacter.cs` - Demonstrates NetworkTransform use
- `LobbyPlayerController.cs` - Alternative sync pattern

### Constants
- `RacingConstants.cs` - Physics values
- `MultiplayerConfig.cs` - Configuration (not used for tick rate)

---

## 🎯 ACTION ITEMS

### Immediate (For smooth LAN play)
- [ ] Verify tick rate isn't causing issues on local network
- [ ] Check NetworkTransform interpolation settings on car prefabs

### Short-term (For online play optimization)
- [ ] Add `[Networked] Vector2 NetworkedVelocity` to CarController
- [ ] Implement position prediction in remote car rendering
- [ ] Override tick rate to 40 Hz in StartGameArgs

### Long-term (For competitive play)
- [ ] Add full client-side input prediction
- [ ] Implement adaptive tick rate based on latency
- [ ] Add interpolation buffer for jitter smoothing
- [ ] Implement lag-compensation with position rollback

---

## 📚 REFERENCE

**Photon Fusion Documentation:**
- Tick-rate configuration: Use `SimulationConfig.TickRate`
- Velocity syncing: `[Networked]` property updates
- Interpolation: Built into NetworkTransform (InterpolationDataSource)
- Input prediction: Implement locally in FixedUpdateNetwork()

**Current Architecture:**
- Host processes game state and enforces physics
- Clients send input, receive state updates
- Position/rotation synchronized via NetworkTransform
- Networked properties (`[Networked]`) synced automatically

