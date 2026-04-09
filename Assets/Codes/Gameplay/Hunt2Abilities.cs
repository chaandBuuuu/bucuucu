using UnityEngine;
using Fusion;

/// <summary>
/// Hunt #2 Abilities
/// Passive: Narrow cone vision, applies slowness and damage when standing in cone
/// E: Light flash - expand vision temporarily
/// R: Narrow vision to line and shoot stun beam
/// F: Place light orbs for vision
/// </summary>
public class Hunt2Passive : MonoBehaviour
{
    [SerializeField] private float coneAngle = 45f;
    [SerializeField] private float coneDistance = 8f;
    [SerializeField] private float statusEffectDuration = 3f;
    [SerializeField] private float slownessMagnitude = 0.4f;
    [SerializeField] private float damagePerSecond = 5f;

    private NetworkCharacterController _controller;
    private LineRenderer _visionCone;

    private void Awake()
    {
        _controller = GetComponent<NetworkCharacterController>();
    }

    private void Start()
    {
        // Tạo visual cho cone vision
        _visionCone = GetComponent<LineRenderer>();
        if (_visionCone == null)
        {
            _visionCone = gameObject.AddComponent<LineRenderer>();
            _visionCone.material = new Material(Shader.Find("Sprites/Default"));
            _visionCone.widthMultiplier = 0.1f;
        }
    }

    private void FixedUpdate()
    {
        if (!_controller.IsHunter || _controller.IsDead) return;

        // Kiểm tra survivor trong phạm vi cone
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, coneDistance);
        
        foreach (var hit in hits)
        {
            var survivorController = hit.GetComponent<NetworkCharacterController>();
            if (survivorController != null && survivorController.IsSurvivor)
            {
                // Kiểm tra xem survivor có trong cone vision không
                Vector3 dirToSurvivor = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.right, dirToSurvivor);

                if (angle < coneAngle / 2f)
                {
                    // Áp dụng slowness và damage
                    survivorController.RPC_AddStatusEffect(StatusEffectType.Slowness, statusEffectDuration, slownessMagnitude);
                    survivorController.RPC_TakeDamage(damagePerSecond * Time.fixedDeltaTime);
                }
            }
        }
    }

    public void DrawVisionCone()
    {
        // Draw cone visual (implement nếu cần)
    }
}

/// <summary>
/// Hunt #2 Ability E
/// Chớp đèn tầm nhìn, mở rộng phạm vi nhìn
/// </summary>
public class Hunt2AbilityE : Ability
{
    [SerializeField] private float expandedFOV = 80f;
    [SerializeField] private float expandDuration = 4f;
    [SerializeField] private float trueSightDuration = 5f;

    private float _fovExpandTimer;

    public override bool Execute()
    {
        if (!base.Execute()) return false;

        _fovExpandTimer = expandDuration;

        // Tìm toàn bộ survivor trong phạm vi mở rộng
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 12f);
        
        foreach (var hit in hits)
        {
            var controller = hit.GetComponent<NetworkCharacterController>();
            if (controller != null && controller.IsSurvivor)
            {
                // Phát hiện survivor thêm True Sight
                controller.RPC_AddStatusEffect(StatusEffectType.TrueSight, trueSightDuration, 1f);
            }
        }

        Debug.Log("[Hunt2AbilityE] Light flash executed!");
        return true;
    }

    protected override void Update()
    {
        base.Update();
        if (_fovExpandTimer > 0)
            _fovExpandTimer -= Time.deltaTime;
    }

    public float GetCurrentFOV()
    {
        return _fovExpandTimer > 0 ? expandedFOV : 45f;
    }
}

/// <summary>
/// Hunt #2 Ability R
/// Hẹp tầm nhìn thành line và bắn tia stun
/// CHỈ SỬ DỤNG KHI CÓ SURVIVAL TRONG TẦM NHÌN
/// </summary>
public class Hunt2AbilityR : Ability
{
    [SerializeField] private float beamNarrowDuration = 2f;
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private float beamDistance = 10f;
    [SerializeField] private float blindnessDuration = 3f;

    private bool _hasTargetInSight;

    public override bool CanExecute()
    {
        // Chỉ thực thi khi có survivor trong tầm nhìn
        return base.CanExecute() && HasSurvivorInSight();
    }

    private bool HasSurvivorInSight()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 10f);
        foreach (var hit in hits)
        {
            var controller = hit.GetComponent<NetworkCharacterController>();
            if (controller != null && controller.IsSurvivor)
            {
                // Kiểm tra xem survivor có trong cone vision không
                Vector3 dirToSurvivor = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.right, dirToSurvivor);
                if (angle < 25f)
                    return true;
            }
        }
        return false;
    }

    public override bool Execute()
    {
        if (!base.Execute()) return false;

        // Bắn tia stun theo hướng phía trước
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, beamDistance);
        
        if (hit.collider != null)
        {
            var controller = hit.collider.GetComponent<NetworkCharacterController>();
            if (controller != null && controller.IsSurvivor)
            {
                controller.RPC_AddStatusEffect(StatusEffectType.Stun, stunDuration, 1f);
                controller.RPC_AddStatusEffect(StatusEffectType.TrueSight, stunDuration, 1f);
            }
        }

        // Hunt #2 bị Blindness sau khi sử dụng
        var hunt2Controller = GetComponent<NetworkCharacterController>();
        hunt2Controller.RPC_AddStatusEffect(StatusEffectType.Blindness, blindnessDuration, 1f);

        Debug.Log("[Hunt2AbilityR] Beam executed!");
        return true;
    }
}

/// <summary>
/// Hunt #2 Ability F
/// Đặt bóng đèn xuống để được tầm nhìn xung quanh
/// </summary>
public class Hunt2AbilityF : Ability
{
    [SerializeField] private GameObject lightOrbPrefab;
    [SerializeField] private float maxLights = 3;
    [SerializeField] private float orbDuration = 10f;
    [SerializeField] private float orbVisionRadius = 5f;
    [SerializeField] private float survivorSlownessInOrb = 0.3f;
    [SerializeField] private float orbStunDuration = 1.5f;

    private float _orbCount;

    public override bool Execute()
    {
        if (!base.Execute()) return false;
        if (_orbCount >= maxLights) return false;

        // Spawn light orb prefab (nếu có)
        // Thực tế sinh viên cần tạo prefab hoặc visual cho light orb
        Debug.Log("[Hunt2AbilityF] Light orb placed!");
        _orbCount++;

        return true;
    }

    public override bool CanExecute()
    {
        return base.CanExecute() && _orbCount < maxLights;
    }
}
