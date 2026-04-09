using UnityEngine;
using Fusion;

/// <summary>
/// Survival #1: Marksman
/// Passive: Starts with 6 Mark Rounds, 3 Mark = 1 Tiger Round, <50% HP = Captain Black buff
/// E: Swing - damage, Mark=Stun, Tiger=Stun+Knockback
/// R: Reload - switch ammo type, apply slowness during reload
/// </summary>
public class Survival1Passive : NetworkBehaviour
{
    [Networked] public int MarkRounds { get; private set; }
    [Networked] public int TigerRounds { get; private set; }
    [Networked] private int _totalRounds { get; set; }

    [SerializeField] private int startMarkRounds = 6;
    [SerializeField] private int markToTigerConversion = 3;
    [SerializeField] private float captainBlackThreshold = 0.5f;
    [SerializeField] private float captainBlackDuration = 8f;
    [SerializeField] private float captainBlackDamageReduction = 0.3f;

    private NetworkCharacterController _controller;

    public override void Spawned()
    {
        _controller = GetComponent<NetworkCharacterController>();
        if (HasStateAuthority)
        {
            MarkRounds = startMarkRounds;
            TigerRounds = 0;
            _totalRounds = startMarkRounds;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // Kiểm tra HP < 50% để áp dụng Captain Black
        float healthPercent = _controller.CurrentHealth / 100f;  // maxHealth
        if (healthPercent < captainBlackThreshold && !_controller.GetStatusEffectManager().HasEffect(StatusEffectType.CaptainBlack))
        {
            _controller.RPC_AddStatusEffect(StatusEffectType.CaptainBlack, captainBlackDuration, captainBlackDamageReduction);
            Debug.Log("[Survival1Passive] Captain Black activated!");
        }
    }

    /// <summary>
    /// Sử dụng một Mark Round
    /// </summary>
    public void UseMarkRound()
    {
        if (MarkRounds > 0)
        {
            MarkRounds--;
            _totalRounds--;

            // Kiểm tra conversion
            if (MarkRounds % markToTigerConversion == 0 && MarkRounds > 0)
            {
                MarkRounds -= markToTigerConversion;
                TigerRounds++;
                Debug.Log($"[Survival1Passive] Mark converted to Tiger! Tigers: {TigerRounds}");
            }
        }
    }

    /// <summary>
    /// Sử dụng một Tiger Round
    /// </summary>
    public void UseTigerRound()
    {
        if (TigerRounds > 0)
        {
            TigerRounds--;
            _totalRounds--;
        }
    }

    public bool CanUseAbilities() => _totalRounds > 0;
}

/// <summary>
/// Survival #1 Ability E
/// </summary>
public class Survival1AbilityE : Ability
{
    [SerializeField] private float damageAmount = 25f;
    [SerializeField] private float markStunDuration = 1.5f;
    [SerializeField] private float tigerStunDuration = 2f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float abilityRange = 2f;

    private Survival1Passive _passive;

    protected override void Awake()
    {
        base.Awake();
        _passive = GetComponent<Survival1Passive>();
    }

    public override bool CanExecute()
    {
        return base.CanExecute() && _passive.CanUseAbilities();
    }

    public override bool Execute()
    {
        if (!base.Execute()) return false;

        bool useMarkRound = _passive.MarkRounds > 0;

        // Tìm hunter trong phạm vi
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, abilityRange);
        
        foreach (var hit in hits)
        {
            var controller = hit.GetComponent<NetworkCharacterController>();
            if (controller != null && controller.IsHunter)
            {
                controller.RPC_TakeDamage(damageAmount);

                if (useMarkRound)
                {
                    controller.RPC_AddStatusEffect(StatusEffectType.Stun, markStunDuration, 1f);
                    _passive.UseMarkRound();
                }
                else if (_passive.TigerRounds > 0)
                {
                    controller.RPC_AddStatusEffect(StatusEffectType.Stun, tigerStunDuration, 1f);
                    // Knockback
                    Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                    _characterController.RPC_Knockback(knockDir, knockbackForce);
                    _passive.UseTigerRound();
                }
            }
        }

        Debug.Log("[Survival1AbilityE] Swing executed!");
        return true;
    }
}

/// <summary>
/// Survival #1 Ability R
/// </summary>
public class Survival1AbilityR : Ability
{
    [SerializeField] private float reloadDuration = 2f;
    [SerializeField] private float slownessDuringReload = 0.5f;

    private Survival1Passive _passive;
    private float _reloadTimer;

    protected override void Awake()
    {
        base.Awake();
        _passive = GetComponent<Survival1Passive>();
    }

    public override bool Execute()
    {
        if (!base.Execute()) return false;

        // Thêm slowness trong quá trình nạp đạn
        _characterController.RPC_AddStatusEffect(StatusEffectType.Slowness, reloadDuration, slownessDuringReload);
        _reloadTimer = reloadDuration;

        Debug.Log("[Survival1AbilityR] Reload executed!");
        return true;
    }
}

/// <summary>
/// Survival #2: Boombox Player
/// Passive: Moving away = Swiftness, Hunter nearby = Slowness
/// E: Place Boombox for Swiftness/Slowness
/// R: Clap hand for Stun effect
/// </summary>
public class Survival2Passive : MonoBehaviour
{
    [SerializeField] private float swiftnessMagnitude = 0.2f;
    [SerializeField] private float hunterDetectionRange = 5f;
    [SerializeField] private float slownessMagnitude = 0.3f;

    private NetworkCharacterController _controller;
    private Vector2 _lastPosition;

    private void Awake()
    {
        _controller = GetComponent<NetworkCharacterController>();
    }

    private void FixedUpdate()
    {
        if (_controller == null) return;

        Vector2 movement = (Vector2)transform.position - _lastPosition;

        // Nếu di chuyển ra xa thì được Swiftness
        if (movement.magnitude > 0.1f)
        {
            _controller.GetStatusEffectManager().AddEffect(StatusEffectType.Swiftness, 0.5f, swiftnessMagnitude);
        }

        // Kiểm tra nếu có hunter gần
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hunterDetectionRange);
        foreach (var hit in hits)
        {
            var hunterController = hit.GetComponent<NetworkCharacterController>();
            if (hunterController != null && hunterController.IsHunter)
            {
                _controller.GetStatusEffectManager().AddEffect(StatusEffectType.Slowness, 0.5f, slownessMagnitude);
            }
        }

        _lastPosition = transform.position;
    }
}

/// <summary>
/// Survival #2 Ability E
/// </summary>
public class Survival2AbilityE : Ability
{
    [SerializeField] private GameObject boomboxPrefab;
    [SerializeField] private float swiftnessMagnitude = 0.3f;
    [SerializeField] private float slownessMagnitude = 0.3f;
    [SerializeField] private float effectRadius = 4f;
    [SerializeField] private float boomboxDuration = 8f;

    public override bool Execute()
    {
        if (!base.Execute()) return false;

        // Spawn boombox (visual element)
        Debug.Log("[Survival2AbilityE] Boombox placed!");
        return true;
    }
}

/// <summary>
/// Survival #2 Ability R
/// </summary>
public class Survival2AbilityR : Ability
{
    [SerializeField] private float clapRadius = 3f;
    [SerializeField] private float clapConeAngle = 90f;
    [SerializeField] private float selfStunDuration = 1f;
    [SerializeField] private float hunterStunDuration = 1.5f;
    [SerializeField] private float survivorKnockback = 3f;

    public override bool Execute()
    {
        if (!base.Execute()) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, clapRadius);
        
        foreach (var hit in hits)
        {
            var controller = hit.GetComponent<NetworkCharacterController>();
            if (controller != null)
            {
                if (controller.IsHunter)
                {
                    controller.RPC_AddStatusEffect(StatusEffectType.Stun, hunterStunDuration, 1f);
                }
                else if (controller.IsSurvivor && controller != _characterController)
                {
                    // Knockback other survivors
                    Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                    controller.RPC_Knockback(knockDir, survivorKnockback);
                }
            }
        }

        // Self stun
        _characterController.RPC_AddStatusEffect(StatusEffectType.Stun, selfStunDuration, 1f);

        Debug.Log("[Survival2AbilityR] Clap executed!");
        return true;
    }
}

/// <summary>
/// Survival #3: Lumberjack
/// Passive: Holding wood = Swiftness
/// E: Detect wood around
/// R: Throw wood at hunter (ONLY WHEN HOLDING WOOD)
/// </summary>
public class Survival3Passive : MonoBehaviour
{
    [SerializeField] private float swiftnessMagnitude = 0.4f;

    private NetworkCharacterController _controller;
    [Networked] private bool _holdingWood { get; set; }

    public bool IsHoldingWood => _holdingWood;

    private void Awake()
    {
        _controller = GetComponent<NetworkCharacterController>();
    }

    private void FixedUpdate()
    {
        if (_holdingWood && _controller != null)
        {
            _controller.GetStatusEffectManager().AddEffect(StatusEffectType.Swiftness, 0.5f, swiftnessMagnitude);
        }
    }

    public void SetHoldingWood(bool holding)
    {
        _holdingWood = holding;
    }
}

/// <summary>
/// Survival #3 Ability E
/// </summary>
public class Survival3AbilityE : Ability
{
    [SerializeField] private float detectionRange = 8f;

    public override bool Execute()
    {
        if (!base.Execute()) return false;

        // Detect gỗ xung quanh (sẽ implement với wood system)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange);
        
        int woodCount = 0;
        foreach (var hit in hits)
        {
            // TODO: Check if it's a wood object
            woodCount++;
        }

        Debug.Log($"[Survival3AbilityE] Detected {woodCount} wood pieces!");
        return true;
    }
}

/// <summary>
/// Survival #3 Ability R
/// </summary>
public class Survival3AbilityR : Ability
{
    [SerializeField] private float throwDistance = 7f;
    [SerializeField] private float stunDuration = 1.5f;

    private Survival3Passive _passive;

    protected override void Awake()
    {
        base.Awake();
        _passive = GetComponent<Survival3Passive>();
    }

    public override bool CanExecute()
    {
        return base.CanExecute() && _passive.IsHoldingWood;
    }

    public override bool Execute()
    {
        if (!base.Execute()) return false;

        // Tìm hunter trong phạm vi
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, throwDistance);
        
        if (hit.collider != null)
        {
            var controller = hit.collider.GetComponent<NetworkCharacterController>();
            if (controller != null && controller.IsHunter)
            {
                controller.RPC_AddStatusEffect(StatusEffectType.Stun, stunDuration, 1f);
                _passive.SetHoldingWood(false);
            }
        }

        Debug.Log("[Survival3AbilityR] Wood thrown!");
        return true;
    }
}

/// <summary>
/// Survival #4: Support
/// Passive: Near bonfire = Slowness
/// E: Swing for Stun
/// R: Tap ground for Swiftness aura
/// </summary>
public class Survival4Passive : MonoBehaviour
{
    [SerializeField] private float slownessNearBonfire = 0.2f;
    [SerializeField] private float bonfireDetectionRange = 3f;

    private NetworkCharacterController _controller;

    private void Awake()
    {
        _controller = GetComponent<NetworkCharacterController>();
    }

    private void FixedUpdate()
    {
        if (_controller == null) return;

        // Kiểm tra gần lửa trại
        // TODO: Check if near bonfire
        // Nếu gần thì thêm slowness
    }
}

/// <summary>
/// Survival #4 Ability E
/// </summary>
public class Survival4AbilityE : Ability
{
    [SerializeField] private float stunDuration = 1f;
    [SerializeField] private float abilityRange = 2f;

    public override bool Execute()
    {
        if (!base.Execute()) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, abilityRange);
        
        foreach (var hit in hits)
        {
            var controller = hit.GetComponent<NetworkCharacterController>();
            if (controller != null && controller.IsHunter)
            {
                controller.RPC_AddStatusEffect(StatusEffectType.Stun, stunDuration, 1f);
            }
        }

        Debug.Log("[Survival4AbilityE] Swing executed!");
        return true;
    }
}

/// <summary>
/// Survival #4 Ability R
/// </summary>
public class Survival4AbilityR : Ability
{
    [SerializeField] private float swiftnessMagnitude = 0.3f;
    [SerializeField] private float swiftnessDuration = 3f;
    [SerializeField] private float effectRadius = 4f;

    public override bool Execute()
    {
        if (!base.Execute()) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, effectRadius);
        
        foreach (var hit in hits)
        {
            var controller = hit.GetComponent<NetworkCharacterController>();
            if (controller != null && controller.IsSurvivor)
            {
                controller.RPC_AddStatusEffect(StatusEffectType.Swiftness, swiftnessDuration, swiftnessMagnitude);
            }
        }

        Debug.Log("[Survival4AbilityR] Tap executed!");
        return true;
    }
}
