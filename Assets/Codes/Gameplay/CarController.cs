using UnityEngine;
using Fusion;

[RequireComponent(typeof(Rigidbody2D))]
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

    // Network properties - synced every frame
    [Networked] private Vector2 NetworkVelocity { get; set; }
    [Networked] private Vector3 NetworkPosition { get; set; }
    [Networked] private float NetworkRotation { get; set; }
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
        _rb.isKinematic = true; // Start as kinematic
    }

    public override void Spawned()
    {
        _powerupInventory = GetComponent<PowerupInventory>() ?? gameObject.AddComponent<PowerupInventory>();

        _localVelocity = Vector2.zero;
        NetworkVelocity = Vector2.zero;
        NetworkPosition = transform.position;
        _currentRotation = transform.rotation.eulerAngles.z;
        NetworkRotation = _currentRotation;

        // ✅ QUAN TRỌNG: Set Rigidbody type dựa trên input authority
        if (HasInputAuthority)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.simulated = true;
            _rb.linearVelocity = Vector2.zero;
            Debug.Log($"[CarController] ✅ Spawned AUTHORITY - {gameObject.name} | RB=Dynamic");
        }
        else
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.simulated = false;
            _rb.linearVelocity = Vector2.zero;
            Debug.Log($"[CarController] ✅ Spawned REMOTE - {gameObject.name} | RB=Kinematic");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsFinished) return;

        if (HasInputAuthority)
        {
            // ================== AUTHORITY PLAYER ==================
            if (GetInput(out NetworkInputData input))
            {
                HandleMovement(input);
                HandlePowerup(input);
            }

            // Apply velocity to Dynamic Rigidbody for physics simulation
            _rb.linearVelocity = _localVelocity;

            // Sync to network EVERY FRAME
            NetworkVelocity = _localVelocity;
            NetworkPosition = transform.position;
            NetworkRotation = _currentRotation;

            if (Runner.Tick % 60 == 0)
                Debug.Log($"[CarController] AUTHORITY {gameObject.name}: Vel={_localVelocity}, Pos={transform.position}");
        }
        else
        {
            // ================== REMOTE PLAYER ==================
            // Apply synced position and rotation
            transform.position = NetworkPosition;
            transform.rotation = Quaternion.AngleAxis(NetworkRotation, Vector3.forward);

            if (Runner.Tick % 60 == 0)
                Debug.Log($"[CarController] REMOTE {gameObject.name}: NetPos={NetworkPosition}, NetVel={NetworkVelocity}");
        }
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

        // Rotation
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