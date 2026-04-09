using UnityEngine;
using Fusion;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkTransform))]   // ← Thêm như MultiplayerCharacter
public class CarController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float friction = 0.95f;
    [SerializeField] private float driftFriction = 0.92f;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float driftRotationMultiplier = 1.5f;

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;

    // Giữ lại những Networked cần thiết cho gameplay
    [Networked] public bool IsDrifting { get; private set; }
    [Networked] public int LapsCompleted { get; set; }
    [Networked] public bool IsFinished { get; set; }

    private Vector2 _localVelocity = Vector2.zero;
    private float _currentRotation = 0f;
    private bool _isDrifting = false;
    private PowerupInventory _powerupInventory;

    public event System.Action<int> OnLapCompleted;
    public event System.Action OnRaceFinished;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();
        
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
    }

    public override void Spawned()
    {
        _powerupInventory = GetComponent<PowerupInventory>() ?? gameObject.AddComponent<PowerupInventory>();

        _localVelocity = Vector2.zero;
        _currentRotation = transform.rotation.eulerAngles.z;

        // === CÁCH LÀM GIỐNG LOBBY ===
        if (HasInputAuthority)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.simulated = true;
        }
        else
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.simulated = false;
        }

        Debug.Log($"[CarController] ✅ Spawned - {gameObject.name} | HasInputAuthority={HasInputAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        if (IsFinished) return;

        if (HasInputAuthority)
        {
            // ================== LOCAL PLAYER (Owner) ==================
            if (GetInput(out NetworkInputData input))
            {
                HandleMovement(input);
                HandlePowerup(input);
            }

            // Đẩy velocity vào physics (giống LobbyPlayerController)
            _rb.linearVelocity = _localVelocity;
        }
        // Remote player: KHÔNG làm gì cả → NetworkTransform tự sync
    }

    private void HandleMovement(NetworkInputData input)
    {
        Vector2 moveDir = input.MoveDirection;
        _isDrifting = input.IsDrifting;
        IsDrifting = _isDrifting;

        // Acceleration + Clamp
        if (moveDir.magnitude > 0.01f)
        {
            _localVelocity += moveDir.normalized * acceleration * Runner.DeltaTime;
            _localVelocity = Vector2.ClampMagnitude(_localVelocity, maxSpeed);
        }

        // Friction (drift hoặc normal)
        float currentFriction = _isDrifting ? driftFriction : friction;
        _localVelocity *= currentFriction;

        // Rotation (giống hệt code cũ)
        if (moveDir.magnitude > 0.01f)
        {
            float targetRotation = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
            float rotSpeed = _isDrifting ? rotationSpeed * driftRotationMultiplier : rotationSpeed;
            
            _currentRotation = Mathf.LerpAngle(_currentRotation, targetRotation, rotSpeed * Runner.DeltaTime);
            transform.rotation = Quaternion.AngleAxis(_currentRotation, Vector3.forward);
        }
    }

    private void HandlePowerup(NetworkInputData input)
    {
        if (input.UsePowerup && _powerupInventory != null)
            _powerupInventory.UseCurrent();
    }

    public void PickupPowerup(PowerupType type)
    {
        if (_powerupInventory != null)
            _powerupInventory.AddPowerup(type);
    }

    public PowerupInventory GetPowerupInventory() => _powerupInventory;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CompleteLap()
    {
        LapsCompleted++;
        OnLapCompleted?.Invoke(LapsCompleted);

        if (LapsCompleted >= 4)
        {
            IsFinished = true;
            OnRaceFinished?.Invoke();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ApplySlow(float slowAmount, float duration)
    {
        StartCoroutine(SlowCoroutine(slowAmount, duration));
    }

    private System.Collections.IEnumerator SlowCoroutine(float slowAmount, float duration)
    {
        float originalMax = maxSpeed;
        maxSpeed *= (1f - slowAmount);
        yield return new WaitForSeconds(duration);
        maxSpeed = originalMax;
    }

    public Vector2 GetVelocity() => _localVelocity;
    public float GetSpeed() => _localVelocity.magnitude;
}