using UnityEngine;
using Fusion;

/// <summary>
/// Điều khiển xe đua với cơ chế đà
/// </summary>
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

    [Header("Components")]
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;

    [Networked] private Vector2 NetworkVelocity { get; set; }
    [Networked] private float CurrentRotation { get; set; }
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
    }

    public override void Spawned()
    {
        CurrentRotation = _currentRotation;
        NetworkVelocity = _localVelocity;
        _powerupInventory = GetComponent<PowerupInventory>();
        
        if (_powerupInventory == null)
            _powerupInventory = gameObject.AddComponent<PowerupInventory>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (IsFinished) return;

        if (GetInput(out NetworkInputData input))
        {
            HandleMovement(input);
            HandlePowerup(input);
        }

        _rb.linearVelocity = _localVelocity;
    }

    private void HandleMovement(NetworkInputData input)
    {
        Vector2 moveDir = input.MoveDirection;
        
        // Drift logic
        _isDrifting = input.IsDrifting;
        IsDrifting = _isDrifting;

        // Acceleration
        if (moveDir.magnitude > 0)
        {
            _localVelocity += moveDir.normalized * acceleration * Runner.DeltaTime;
            _localVelocity = Vector2.ClampMagnitude(_localVelocity, maxSpeed);
        }

        // Friction/Deceleration
        float friction = _isDrifting ? driftFriction : this.friction;
        _localVelocity *= friction;

        // Rotation
        if (moveDir.magnitude > 0)
        {
            float targetRotation = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
            float rotSpeed = _isDrifting ? rotationSpeed * driftRotationMultiplier : rotationSpeed;
            
            _currentRotation = Mathf.LerpAngle(_currentRotation, targetRotation, rotSpeed * Runner.DeltaTime);
            transform.rotation = Quaternion.AngleAxis(_currentRotation, Vector3.forward);
        }

        CurrentRotation = _currentRotation;
        NetworkVelocity = _localVelocity;
    }

    private void HandlePowerup(NetworkInputData input)
    {
        if (input.UsePowerup && _powerupInventory != null)
        {
            _powerupInventory.UseCurrent();
        }
    }

    /// <summary>
    /// Pickup powerup
    /// </summary>
    public void PickupPowerup(PowerupType type)
    {
        if (_powerupInventory != null)
        {
            _powerupInventory.AddPowerup(type);
            Debug.Log($"[CarController] Picked up {type}");
        }
    }

    /// <summary>
    /// Use powerup
    /// </summary>
    public void UsePowerup()
    {
        if (_powerupInventory != null && HasInputAuthority)
        {
            _powerupInventory.UseCurrent();
        }
    }

    /// <summary>
    /// Complete lap
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CompleteLap()
    {
        LapsCompleted++;
        OnLapCompleted?.Invoke(LapsCompleted);
        Debug.Log($"[CarController] Lap {LapsCompleted} completed!");

        // Check if finished (4 laps)
        if (LapsCompleted >= 4)
        {
            IsFinished = true;
            OnRaceFinished?.Invoke();
            Debug.Log($"[CarController] Race finished!");
        }
    }

    /// <summary>
    /// Apply slow effect
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ApplySlow(float slowAmount, float duration)
    {
        StartCoroutine(SlowCoroutine(slowAmount, duration));
    }

    private System.Collections.IEnumerator SlowCoroutine(float slowAmount, float duration)
    {
        float originalMaxSpeed = maxSpeed;
        maxSpeed *= (1f - slowAmount);

        yield return new WaitForSeconds(duration);

        maxSpeed = originalMaxSpeed;
    }

    public PowerupInventory GetPowerupInventory() => _powerupInventory;
    public Vector2 GetVelocity() => _localVelocity;
    public float GetSpeed() => _localVelocity.magnitude;
}
