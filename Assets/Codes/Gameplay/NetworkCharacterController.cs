using UnityEngine;
using Fusion;

/// <summary>
/// Điều khiển nhân vật trong gameplay với role-based logic
/// </summary>
public class NetworkCharacterController : NetworkBehaviour
{
    [Header("Character Config")]
    [SerializeField] private CharacterID characterID;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [Networked] public float CurrentHealth { get; private set; }
    [Networked] public bool IsDead { get; private set; }
    
    [Header("Movement")]
    [SerializeField] private float baseSpeed = 5f;
    [Networked] private Vector2 NetworkedVelocity { get; set; }
    
    private Rigidbody2D _rb;
    private StatusEffectManager _statusEffectManager;
    private AbilityManager _abilityManager;
    private CharacterRole _role;
    private ChangeDetector _changes;

    public event System.Action OnHealthChanged;
    public event System.Action OnDeath;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _statusEffectManager = GetComponent<StatusEffectManager>();
        _abilityManager = GetComponent<AbilityManager>();

        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();
        if (_statusEffectManager == null)
            _statusEffectManager = gameObject.AddComponent<StatusEffectManager>();
        if (_abilityManager == null)
            _abilityManager = gameObject.AddComponent<AbilityManager>();

        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        CurrentHealth = maxHealth;
        
        // Xác định role
        _role = CharacterDatabase.Instance.GetRole(characterID);
        Debug.Log($"[NetworkCharacterController] Character {characterID} spawned with role {_role}");
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;

        if (IsDead) return;

        // Lấy input từ player
        if (GetInput(out NetworkInputData input))
        {
            HandleMovement(input.MoveDirection);
            HandleAbilities(input);
        }
    }

    private void HandleMovement(Vector2 moveDirection)
    {
        // Tính toán tốc độ dựa trên status effects
        float speedMultiplier = _statusEffectManager.CalculateSpeedMultiplier(baseSpeed);
        
        Vector2 velocity = moveDirection.normalized * baseSpeed * speedMultiplier;
        NetworkedVelocity = velocity;
        _rb.linearVelocity = velocity;
    }

    private void HandleAbilities(NetworkInputData input)
    {
        // E ability
        if (input.PressE)
            _abilityManager.TryExecuteAbility("AbilityE");

        // R ability
        if (input.PressR)
            _abilityManager.TryExecuteAbility("AbilityR");

        // F ability (chỉ Hunter có)
        if (input.PressF && _role == CharacterRole.Hunter)
            _abilityManager.TryExecuteAbility("AbilityF");
    }

    /// <summary>
    /// Gây damage lên nhân vật
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage)
    {
        if (!HasStateAuthority || IsDead) return;

        float actualDamage = _statusEffectManager.CalculateDamage(damage);
        CurrentHealth -= actualDamage;

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            IsDead = true;
            OnDeath?.Invoke();
            Debug.Log($"[NetworkCharacterController] {characterID} died!");
        }
        else
        {
            OnHealthChanged?.Invoke();
        }
    }

    /// <summary>
    /// Thêm status effect
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_AddStatusEffect(StatusEffectType effectType, float duration, float magnitude)
    {
        _statusEffectManager.AddEffect(effectType, duration, magnitude);
    }

    public StatusEffectManager GetStatusEffectManager() => _statusEffectManager;
    public CharacterRole GetRole() => _role;
    public CharacterID GetCharacterID() => characterID;
    public bool IsHunter => _role == CharacterRole.Hunter;
    public bool IsSurvivor => _role == CharacterRole.Survivor;
}
