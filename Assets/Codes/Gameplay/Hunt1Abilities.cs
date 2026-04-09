using UnityEngine;
using Fusion;

/// <summary>
/// Hunt #1 Abilities
/// Passive: Slower attacks, leaves root trails for speedup, roots slow survivors
/// E: Vine pull
/// R: Flower bloom with slowness and True Sight
/// F: Dash forward
/// </summary>
public class Hunt1AbilityE : Ability
{
    [SerializeField] private float vinePullDistance = 5f;
    [SerializeField] private float vinePullForce = 10f;
    [SerializeField] private float pullDuration = 0.5f;

    private Rigidbody2D[] _targetRigidbodies;
    private float _pullTimer;

    /// <summary>
    /// Tạo một đường dây kéo người chơi lại
    /// </summary>
    public override bool Execute()
    {
        if (!base.Execute()) return false;

        // Tìm toàn bộ survivor trong phạm vi
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, vinePullDistance);
        
        foreach (var hit in hits)
        {
            var controller = hit.GetComponent<NetworkCharacterController>();
            if (controller != null && controller.IsSurvivor)
            {
                // Phát RPC để tất cả client biết
                controller.RPC_PulledByVine(transform.position, vinePullForce);
            }
        }

        Debug.Log("[Hunt1AbilityE] Vine pull executed!");
        return true;
    }

    public override bool CanExecute()
    {
        return base.CanExecute();
    }
}

/// <summary>
/// Hunt #1 Ability R
/// </summary>
public class Hunt1AbilityR : Ability
{
    [SerializeField] private float bloomRadius = 3f;
    [SerializeField] private float slownessDuration = 5f;
    [SerializeField] private float slownessAmount = 0.3f;

    /// <summary>
    /// Tạo một nụ hoa tại một điểm
    /// </summary>
    public override bool Execute()
    {
        if (!base.Execute()) return false;

        // Tìm survivor trong phạm vi
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, bloomRadius);
        
        foreach (var hit in hits)
        {
            var controller = hit.GetComponent<NetworkCharacterController>();
            if (controller != null && controller.IsSurvivor)
            {
                // Thêm slowness
                controller.RPC_AddStatusEffect(StatusEffectType.Slowness, slownessDuration, slownessAmount);
                // Thêm True Sight
                controller.RPC_AddStatusEffect(StatusEffectType.TrueSight, slownessDuration, 1f);
            }
        }

        Debug.Log("[Hunt1AbilityR] Flower bloom executed!");
        return true;
    }
}

/// <summary>
/// Hunt #1 Ability F
/// Lướt về phía trước, gây damage nếu dính survivor, bị stun nếu không dính ai
/// </summary>
public class Hunt1AbilityF : Ability
{
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float damageAmount = 20f;
    [SerializeField] private float stunDuration = 2f;

    private float _dashTimer;
    private Vector2 _dashDirection;
    private bool _hitSomeone;
    private Rigidbody2D _rb;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Hunt #1 lướt về phía trước
    /// </summary>
    public override bool Execute()
    {
        if (!base.Execute()) return false;

        _dashDirection = transform.right;  // Hướng người chơi đang nhìn
        _dashTimer = dashDuration;
        _hitSomeone = false;

        Debug.Log("[Hunt1AbilityF] Dash executed!");
        return true;
    }

    protected override void Update()
    {
        base.Update();

        if (_dashTimer > 0)
        {
            _dashTimer -= Time.deltaTime;
            
            // Di chuyển theo hướng dash
            Vector2 movement = _dashDirection * dashSpeed * Time.deltaTime;
            _rb.linearVelocity = movement;

            // Kiểm tra va chạm với survivor
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _dashDirection, 0.5f);
            if (hit.collider != null)
            {
                var controller = hit.collider.GetComponent<NetworkCharacterController>();
                if (controller != null && controller.IsSurvivor && !_hitSomeone)
                {
                    _hitSomeone = true;
                    controller.RPC_TakeDamage(damageAmount);
                    Debug.Log("[Hunt1AbilityF] Hit survivor!");
                }
            }
        }
        else if (_dashTimer <= 0 && cooldown - _cooldownRemaining < dashDuration + 0.1f)
        {
            // Kết thúc dash
            _rb.linearVelocity = Vector2.zero;

            if (!_hitSomeone)
            {
                // Bị stun nếu không dính ai
                _characterController.RPC_AddStatusEffect(StatusEffectType.Stun, stunDuration, 1f);
                Debug.Log("[Hunt1AbilityF] Stunned - didn't hit anyone!");
            }
        }
    }
}

/// <summary>
/// Passive cho Hunt #1: Để lại rễ cây khi di chuyển
/// </summary>
public class Hunt1Passive : MonoBehaviour
{
    [SerializeField] private GameObject rootPrefab;
    [SerializeField] private float rootDuration = 5f;
    [SerializeField] private float spawnInterval = 0.3f;
    [SerializeField] private float rootSpeedBoost = 0.4f;
    [SerializeField] private float survivorSlowness = 0.3f;

    private float _lastRootSpawn;
    private Vector2 _lastPosition;
    private Rigidbody2D _rb;
    private NetworkCharacterController _controller;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _controller = GetComponent<NetworkCharacterController>();
        _lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (!_controller.IsHunter || _controller.IsDead) return;

        Vector2 movement = (Vector2)transform.position - _lastPosition;
        
        if (movement.magnitude > 0.1f && Time.time - _lastRootSpawn > spawnInterval)
        {
            _lastRootSpawn = Time.time;
            SpawnRoot();
        }

        _lastPosition = transform.position;
    }

    private void SpawnRoot()
    {
        // Simulate root spawn (thực tế cần instantiate visual element)
        Debug.Log($"[Hunt1Passive] Root spawned at {transform.position}");
    }

    /// <summary>
    /// Kiểm tra nếu Hunt đang đứng trên root của chính mình -> tăng tốc
    /// </summary>
    public float GetPassiveSpeedBoost()
    {
        // TODO: Implement root tracking
        return rootSpeedBoost;
    }
}
