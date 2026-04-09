using UnityEngine;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// Base class cho tất cả abilities
/// </summary>
public abstract class Ability : MonoBehaviour
{
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] protected float manaCost = 0f;
    
    protected float _cooldownRemaining = 0f;
    protected NetworkCharacterController _characterController;

    protected virtual void Awake()
    {
        _characterController = GetComponent<NetworkCharacterController>();
    }

    protected virtual void Update()
    {
        if (_cooldownRemaining > 0)
            _cooldownRemaining -= Time.deltaTime;
    }

    /// <summary>
    /// Thực thi ability
    /// </summary>
    public virtual bool Execute()
    {
        if (!CanExecute()) return false;
        
        _cooldownRemaining = cooldown;
        return true;
    }

    /// <summary>
    /// Kiểm tra có thể thực thi ability không
    /// </summary>
    public virtual bool CanExecute()
    {
        return _cooldownRemaining <= 0;
    }

    public float GetCooldownRemaining() => _cooldownRemaining;
    public float GetCooldownMax() => cooldown;
    public float GetCooldownPercent() => 1f - (Mathf.Clamp01(_cooldownRemaining / cooldown));
}

/// <summary>
/// Quản lý toàn bộ abilities của một nhân vật
/// </summary>
public class AbilityManager : NetworkBehaviour
{
    private Dictionary<string, Ability> _abilities = new Dictionary<string, Ability>();
    private NetworkCharacterController _characterController;

    private void Start()
    {
        _characterController = GetComponent<NetworkCharacterController>();
        
        // Tự động tìm tất cả abilities gắn trên gameObject này
        var abilities = GetComponents<Ability>();
        foreach (var ability in abilities)
        {
            string abilityName = ability.GetType().Name;
            _abilities[abilityName] = ability;
        }

        Debug.Log($"[AbilityManager] Loaded {_abilities.Count} abilities");
    }

    /// <summary>
    /// Thực thi một ability theo tên
    /// </summary>
    public bool TryExecuteAbility(string abilityName)
    {
        if (!HasInputAuthority) return false;
        if (!_abilities.TryGetValue(abilityName, out var ability)) return false;

        return ability.Execute();
    }

    /// <summary>
    /// Lấy ability
    /// </summary>
    public Ability GetAbility(string abilityName)
    {
        _abilities.TryGetValue(abilityName, out var ability);
        return ability;
    }

    public T GetAbility<T>(string abilityName) where T : Ability
    {
        return GetAbility(abilityName) as T;
    }
}
