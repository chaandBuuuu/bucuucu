using UnityEngine;
using Fusion;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class CarController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float acceleration = RacingConstants.CAR_ACCELERATION;
    [SerializeField] private float maxSpeed     = RacingConstants.CAR_MAX_SPEED;
    [SerializeField] private float friction     = RacingConstants.CAR_FRICTION;
    [SerializeField] private float driftFriction = RacingConstants.CAR_DRIFT_FRICTION;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed           = RacingConstants.CAR_ROTATION_SPEED;
    [SerializeField] private float driftRotationMultiplier = RacingConstants.CAR_DRIFT_ROTATION_MULTIPLIER;

    [Header("Player Nameplate")]
    [SerializeField] private float nameplateOffsetY = -1.5f;  // Vị trí dưới xe
    [SerializeField] private float nameplateFontSize = 4f;

    [Header("Audio")]
    [SerializeField] private AudioClip engineSoundClip;           // ✅ Âm thanh khi di chuyển
    [SerializeField] private AudioClip driftBrakeSoundClip;       // ✅ Âm thanh khi cua/giảm tốc
    [SerializeField] private float audioVolume = 0.8f;            // Volume cho car audio

    private Rigidbody2D      _rb;
    private SpriteRenderer   _spriteRenderer;
    private PowerupInventory _powerupInventory;
    private TextMeshPro      _nameplateText;  // ✅ Tên người chơi
    private string           _cachedPlayerName = "";  // ✅ Cache tên người chơi
    
    // ✅ Audio Sources cho engine và drift/brake sounds
    private AudioSource _engineAudioSource;
    private AudioSource _driftBrakeAudioSource;

    // ── Networked state ──────────────────────────────────────────────────────
    [Networked] public  bool    IsDrifting    { get; private set; }
    [Networked] public  int     LapsCompleted { get; set; }
    [Networked] public  bool    IsFinished    { get; set; }
    [Networked] private float   SpeedMultiplier { get; set; } = 1f;
    [Networked] public  Vector2 NetworkVelocity { get; set; } = Vector2.zero;  // ✅ NEW: Sync velocity for smooth remote car interpolation

    // ── Local state ──────────────────────────────────────────────────────────
    private Vector2 _localVelocity   = Vector2.zero;
    private float   _currentRotation = 0f;
    private bool    _isDrifting      = false;
    private bool    _inputEnabled    = true;  // ✅ NEW: Lock/unlock input

    public event System.Action<int> OnLapCompleted;
    public event System.Action      OnRaceFinished;

    private void Awake()
    {
        _rb             = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _rb.gravityScale   = 0f;
        _rb.constraints    = RigidbodyConstraints2D.FreezeRotation;
        _rb.linearDamping  = 0f;
        _rb.angularDamping = 0f;
    }

    public override void Spawned()
    {
        // ✅ DISABLED: _powerupInventory = GetComponent<PowerupInventory>() ?? gameObject.AddComponent<PowerupInventory>();

        _localVelocity   = Vector2.zero;
        _currentRotation = transform.rotation.eulerAngles.z;
        SpeedMultiplier  = 1f;

        // ✅ NEW: Tạo nameplate cho người chơi
        CreatePlayerNameplate();

        // ✅ NEW: Setup audio sources
        SetupAudioSources();

        // Authority setup cho Rigidbody
        if (HasInputAuthority)
        {
            _rb.bodyType       = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.zero;
            Debug.Log($"[CarController] ✅ Spawned AUTHORITY - {gameObject.name}");
        }
        else
        {
            _rb.bodyType       = RigidbodyType2D.Kinematic;   // Remote car dùng NetworkTransform
            Debug.Log($"[CarController] ✅ Spawned REMOTE - {gameObject.name}");
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // ✅ Stop audio sources khi despawn
        if (_engineAudioSource != null && _engineAudioSource.isPlaying)
            _engineAudioSource.Stop();
        
        if (_driftBrakeAudioSource != null && _driftBrakeAudioSource.isPlaying)
            _driftBrakeAudioSource.Stop();

        Debug.Log("[CarController] 🔇 Audio sources stopped on despawn");
    }

    /// <summary>
    /// ✅ NEW: Tạo floating nameplate dưới xe để hiển thị tên người chơi
    /// </summary>
    private void CreatePlayerNameplate()
    {
        if (Object == null) return;

        // Lấy tên người chơi từ FusionNetworkManager
        string playerName = "Player";
        if (FusionNetworkManager.Instance != null)
        {
            playerName = FusionNetworkManager.Instance.GetPlayerName(Object.InputAuthority);
        }

        // ✅ Cache tên người chơi
        _cachedPlayerName = playerName;

        // Tạo GameObject con cho nameplate
        GameObject nameplateGO = new GameObject("Nameplate");
        nameplateGO.transform.SetParent(transform);
        nameplateGO.transform.localPosition = new Vector3(0, nameplateOffsetY, 0);

        // Thêm TextMeshPro
        _nameplateText = nameplateGO.AddComponent<TextMeshPro>();
        _nameplateText.text = playerName;
        _nameplateText.alignment = TextAlignmentOptions.Center;
        _nameplateText.fontSize = nameplateFontSize;
        _nameplateText.color = Color.white;

        // ✅ Use TextMeshPro's built-in outline (no need for separate component)
        _nameplateText.outlineWidth = 0.2f;
        _nameplateText.outlineColor = Color.black;

        Debug.Log($"[CarController] ✅ Created nameplate: {playerName}");
    }

    /// <summary>
    /// ✅ NEW: Lấy tên người chơi (cached từ nameplate)
    /// </summary>
    public string GetPlayerName()
    {
        if (!string.IsNullOrEmpty(_cachedPlayerName))
            return _cachedPlayerName;
        
        if (Object != null && FusionNetworkManager.Instance != null)
            return FusionNetworkManager.Instance.GetPlayerName(Object.InputAuthority);
        
        return gameObject.name;
    }

    /// <summary>
    /// ✅ NEW: Setup audio sources cho engine và drift/brake sounds
    /// Chỉ setup cho local player (HasInputAuthority)
    /// </summary>
    private void SetupAudioSources()
    {
        if (!HasInputAuthority) return;  // Chỉ setup cho local player

        // ✅ Engine Audio Source
        var engineObj = new GameObject("EngineAudio");
        engineObj.transform.SetParent(transform);
        engineObj.transform.localPosition = Vector3.zero;
        _engineAudioSource = engineObj.AddComponent<AudioSource>();
        _engineAudioSource.clip = engineSoundClip;
        _engineAudioSource.volume = audioVolume;
        _engineAudioSource.loop = true;
        _engineAudioSource.playOnAwake = false;
        _engineAudioSource.spatialBlend = 0f;  // 2D audio

        // ✅ Drift/Brake Audio Source
        var driftObj = new GameObject("DriftBrakeAudio");
        driftObj.transform.SetParent(transform);
        driftObj.transform.localPosition = Vector3.zero;
        _driftBrakeAudioSource = driftObj.AddComponent<AudioSource>();
        _driftBrakeAudioSource.clip = driftBrakeSoundClip;
        _driftBrakeAudioSource.volume = audioVolume;
        _driftBrakeAudioSource.loop = true;
        _driftBrakeAudioSource.playOnAwake = false;
        _driftBrakeAudioSource.spatialBlend = 0f;  // 2D audio

        Debug.Log("[CarController] ✅ Audio sources setup completed");
    }

    public override void FixedUpdateNetwork()
    {
        if (IsFinished) return;

        // Re-enable physics cho owner nếu bị reset
        if (HasInputAuthority && _rb.bodyType == RigidbodyType2D.Kinematic)
        {
            _rb.bodyType       = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.zero;
        }

        // ✅ NEW: Check if input is enabled
        if (!_inputEnabled)
        {
            // Apply friction only, no input
            _localVelocity *= friction;
            if (HasInputAuthority || HasStateAuthority)
            {
                _rb.linearVelocity = _localVelocity;
            }
            return;
        }

        // ✅ OPTIMIZE: Get input once per frame
        if (GetInput(out NetworkInputData input))
        {
            HandleMovement(input);
            HandlePowerup(input);
        }

        // ✅ Only apply velocity on simulating machine (owner + server)
        if (HasInputAuthority || HasStateAuthority)
        {
            // ✅ OPTIMIZE: Only update rigidbody if velocity changed significantly
            Vector2 newVelocity = _localVelocity;
            if (Vector2.Distance(_rb.linearVelocity, newVelocity) > 0.01f)
            {
                _rb.linearVelocity = newVelocity;
            }

            // ✅ NEW: Sync velocity to network for remote car interpolation
            NetworkVelocity = _localVelocity;
        }
        else if (_rb.bodyType == RigidbodyType2D.Kinematic)
        {
            // ✅ FIX: Remote cars (Kinematic) apply synced velocity directly
            // NO interpolation/lerp - just direct assignment for smooth movement
            _rb.linearVelocity = NetworkVelocity;
        }
    }

    private void HandleMovement(NetworkInputData input)
    {
        Vector2 moveDir  = input.MoveDirection;
        _isDrifting      = input.IsDrifting;
        IsDrifting       = _isDrifting;

        float effectiveMaxSpeed = maxSpeed * SpeedMultiplier;

        // ✅ FIX: Apply friction ONLY when NOT accelerating (coasting to stop)
        if (moveDir.magnitude > 0.01f)
        {
            // 🏎️ ACCELERATE: No friction resistance when actively moving
            _localVelocity += moveDir.normalized * acceleration * Runner.DeltaTime;
            _localVelocity  = Vector2.ClampMagnitude(_localVelocity, effectiveMaxSpeed);

            // ✅ NEW: Reactive grip when drifting + steering
            // Nếu player steering lại (opposite/perpendicular direction), apply temporary high-grip friction
            if (_isDrifting && _localVelocity.magnitude > 5f)
            {
                float steeringDot = Vector2.Dot(moveDir.normalized, _localVelocity.normalized);
                if (steeringDot < 0.7f)  // Steering away from momentum direction
                {
                    // Temporary high grip (0.90) to "catch" the drift faster
                    _localVelocity *= 0.90f;
                    Debug.Log($"[CarController] 🎯 Reactive grip applied | Steering angle: {Mathf.Acos(steeringDot) * Mathf.Rad2Deg:F0}°");
                }
            }
        }
        else
        {
            // 🛑 COAST: Apply friction only when NOT moving
            float currentFriction = _isDrifting ? driftFriction : friction;
            _localVelocity *= currentFriction;
        }

        // Rotation
        if (moveDir.magnitude > 0.01f)
        {
            float targetRotation = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
            float rotSpeed       = _isDrifting
                                 ? rotationSpeed * driftRotationMultiplier
                                 : rotationSpeed;

            _currentRotation = Mathf.LerpAngle(_currentRotation, targetRotation, rotSpeed * Runner.DeltaTime);
            transform.rotation = Quaternion.AngleAxis(_currentRotation, Vector3.forward);
        }

        // ✅ NEW: Play audio effects based on movement state
        PlayAudioEffects(moveDir, moveDir.magnitude > 0.01f);
    }

    /// <summary>
    /// ✅ NEW: Play audio effects based on movement state
    /// - Engine sound: phát liên tục khi bấm phím, ngắt khi thả
    /// - Drift/Brake sound: phát liên tục khi cua/brake, ngắt khi thả
    /// </summary>
    private void PlayAudioEffects(Vector2 moveDir, bool isMoving)
    {
        if (_engineAudioSource == null || _driftBrakeAudioSource == null) return;

        // ✅ Engine audio: phát khi di chuyển, ngắt khi thả
        if (isMoving)
        {
            if (!_engineAudioSource.isPlaying && _engineAudioSource.clip != null)
            {
                _engineAudioSource.Play();
                Debug.Log("[CarController] 🔊 Engine sound started");
            }
        }
        else
        {
            if (_engineAudioSource.isPlaying)
            {
                _engineAudioSource.Stop();
                Debug.Log("[CarController] 🔇 Engine sound stopped");
            }
        }

        // ✅ Drift/Brake audio: phát khi cua hoặc brake, ngắt khi thả
        bool isDriftingOrBraking = _isDrifting || (!isMoving && _localVelocity.magnitude > 5f);
        if (isDriftingOrBraking)
        {
            if (!_driftBrakeAudioSource.isPlaying && _driftBrakeAudioSource.clip != null)
            {
                _driftBrakeAudioSource.Play();
                Debug.Log("[CarController] 🔊 Drift/Brake sound started");
            }
        }
        else
        {
            if (_driftBrakeAudioSource.isPlaying)
            {
                _driftBrakeAudioSource.Stop();
                Debug.Log("[CarController] 🔇 Drift/Brake sound stopped");
            }
        }
    }

    private void HandlePowerup(NetworkInputData input)
    {
        if (input.UsePowerup && _powerupInventory != null)
            _powerupInventory.UseCurrent();
    }

    // ── Public API ───────────────────────────────────────────────────────────
    public void PickupPowerup(PowerupType type)
    {
        // ✅ DISABLED: Powerup system removed
        // if (_powerupInventory != null)
        //     _powerupInventory.AddPowerup(type);
    }

    /// ✅ NEW: Enable/disable input
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        Debug.Log($"[CarController] Input {(enabled ? "enabled" : "disabled")} for {gameObject.name}");
    }

    public PowerupInventory GetPowerupInventory() => _powerupInventory;
    public Vector2          GetVelocity()         => _localVelocity;
    public float            GetSpeed()            => _localVelocity.magnitude;

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (!HasStateAuthority) return;
        StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }

    private System.Collections.IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        SpeedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        SpeedMultiplier = 1f;
    }

    // ── RPCs ─────────────────────────────────────────────────────────────────
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CompleteLap()
    {
        LapsCompleted++;
        OnLapCompleted?.Invoke(LapsCompleted);

        if (LapsCompleted >= RacingConstants.RACE_LAPS_TO_WIN)
        {
            IsFinished = true;
            OnRaceFinished?.Invoke();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplySlow(float slowAmount, float duration)
    {
        StartCoroutine(SlowCoroutine(slowAmount, duration));
    }

    private System.Collections.IEnumerator SlowCoroutine(float slowAmount, float duration)
    {
        SpeedMultiplier = 1f - slowAmount;
        yield return new WaitForSeconds(duration);
        SpeedMultiplier = 1f;
    }
}