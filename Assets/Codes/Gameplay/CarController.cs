using UnityEngine;
using Fusion;

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

    private Rigidbody2D      _rb;
    private SpriteRenderer   _spriteRenderer;
    private PowerupInventory _powerupInventory;

    // ── Networked state ──────────────────────────────────────────────────────
    [Networked] private Vector2 NetworkVelocity    { get; set; }
    [Networked] private Vector3 NetworkPosition    { get; set; }
    [Networked] private float   NetworkRotation    { get; set; }
    [Networked] public  bool    IsDrifting         { get; private set; }
    [Networked] public  int     LapsCompleted      { get; set; }
    [Networked] public  bool    IsFinished         { get; set; }

    // FIX: Networked speed multiplier thay vì modify maxSpeed local mỗi client
    // Chỉ StateAuthority ghi, tất cả client đọc → không desync
    [Networked] private float SpeedMultiplier { get; set; } = 1f;

    // ── Local state ──────────────────────────────────────────────────────────
    private Vector2 _localVelocity   = Vector2.zero;
    private float   _currentRotation = 0f;
    private bool    _isDrifting      = false;

    public event System.Action<int> OnLapCompleted;
    public event System.Action      OnRaceFinished;

    // ────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _rb             = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();

        _rb.gravityScale  = 0f;
        _rb.constraints   = RigidbodyConstraints2D.FreezeRotation;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
        // isKinematic sẽ được set trong Spawned()
    }

    public override void Spawned()
    {
        _powerupInventory = GetComponent<PowerupInventory>()
                         ?? gameObject.AddComponent<PowerupInventory>();

        _localVelocity   = Vector2.zero;
        NetworkVelocity  = Vector2.zero;
        NetworkPosition  = transform.position;
        _currentRotation = transform.rotation.eulerAngles.z;
        NetworkRotation  = _currentRotation;
        SpeedMultiplier  = 1f;

        if (HasInputAuthority)
        {
            _rb.isKinematic    = false;
            _rb.gravityScale   = 0f;
            _rb.linearVelocity = Vector2.zero;
            Debug.Log($"[CarController] ✅ Spawned AUTHORITY - {gameObject.name}");
        }
        else
        {
            _rb.isKinematic    = true;
            _rb.gravityScale   = 0f;
            _rb.linearVelocity = Vector2.zero;
            Debug.Log($"[CarController] ✅ Spawned REMOTE - {gameObject.name}");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsFinished) return;

        // FIX: Re-sync RB type nếu authority được gán sau Spawned()
        if (HasInputAuthority && _rb.isKinematic)
        {
            _rb.isKinematic    = false;
            _rb.gravityScale   = 0f;
            _rb.linearVelocity = Vector2.zero;
        }

        if (HasInputAuthority)
        {
            if (GetInput(out NetworkInputData input))
            {
                HandleMovement(input);
                HandlePowerup(input);
            }

            _rb.linearVelocity = _localVelocity;

            NetworkVelocity = _localVelocity;
            NetworkPosition = transform.position;
            NetworkRotation = _currentRotation;
        }
        else
        {
            transform.position = NetworkPosition;
            transform.rotation = Quaternion.AngleAxis(NetworkRotation, Vector3.forward);
        }
    }

    private void HandleMovement(NetworkInputData input)
    {
        Vector2 moveDir  = input.MoveDirection;
        _isDrifting      = input.IsDrifting;
        IsDrifting       = _isDrifting;

        // Tốc độ tối đa có tính multiplier (SpeedBoost / Slow)
        float effectiveMaxSpeed = maxSpeed * SpeedMultiplier;

        if (moveDir.magnitude > 0.01f)
        {
            _localVelocity += moveDir.normalized * acceleration * Runner.DeltaTime;
            _localVelocity  = Vector2.ClampMagnitude(_localVelocity, effectiveMaxSpeed);
        }

        float currentFriction = _isDrifting ? driftFriction : friction;
        _localVelocity *= currentFriction;

        if (moveDir.magnitude > 0.01f)
        {
            float targetRotation = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
            float rotSpeed       = _isDrifting
                                 ? rotationSpeed * driftRotationMultiplier
                                 : rotationSpeed;
            _currentRotation = Mathf.LerpAngle(_currentRotation, targetRotation,
                                                rotSpeed * Runner.DeltaTime);
            transform.rotation = Quaternion.AngleAxis(_currentRotation, Vector3.forward);
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
        if (_powerupInventory != null)
            _powerupInventory.AddPowerup(type);
    }

    public PowerupInventory GetPowerupInventory() => _powerupInventory;
    public Vector2          GetVelocity()         => _localVelocity;
    public float            GetSpeed()            => _localVelocity.magnitude;

    /// <summary>
    /// FIX: SpeedBoost API — PowerupInventory gọi hàm này thay vì tự modify maxSpeed.
    /// StateAuthority set SpeedMultiplier → sync tự động đến mọi client.
    /// </summary>
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

    /// <summary>
    /// FIX: Chỉ StateAuthority xử lý slow → set SpeedMultiplier networked.
    /// Trước đây RpcTargets.All gây desync maxSpeed giữa các client.
    /// </summary>
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