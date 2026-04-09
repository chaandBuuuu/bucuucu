using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// Các loại status effect trong trò chơi
/// </summary>
public enum StatusEffectType
{
    Slowness,           // Giảm tốc độ
    Stun,               // Bất động
    Swiftness,          // Tăng tốc độ
    TrueSight,          // Bị phát hiện
    Blindness,          // Mù mắt
    CaptainBlack,       // Xóa debuff, giảm damage
    Burn                // Đốt cháy
}

[System.Serializable]
public class StatusEffect
{
    public StatusEffectType effectType;
    public float duration;
    public float elapsedTime;
    public float magnitude;  // Độ mạnh của hiệu ứng (% slow, stun time, etc)
    
    public bool IsActive => elapsedTime < duration;
    public float RemainingTime => Mathf.Max(0, duration - elapsedTime);
    
    public StatusEffect(StatusEffectType type, float duration, float magnitude = 1f)
    {
        effectType = type;
        this.duration = duration;
        this.magnitude = magnitude;
        elapsedTime = 0f;
    }

    public void Update(float deltaTime)
    {
        elapsedTime += deltaTime;
    }
}

/// <summary>
/// Quản lý các status effects của một nhân vật
/// </summary>
public class StatusEffectManager : NetworkBehaviour
{
    [Networked] private TickTimer statusEffectInterval { get; set; }
    
    private List<StatusEffect> _activeEffects = new List<StatusEffect>();
    private bool _isDirty = false;

    public event System.Action<StatusEffectType> OnEffectAdded;
    public event System.Action<StatusEffectType> OnEffectRemoved;

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;

        // Update tất cả active effects
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].Update(Runner.DeltaTime);
            if (!_activeEffects[i].IsActive)
            {
                var removed = _activeEffects[i].effectType;
                _activeEffects.RemoveAt(i);
                OnEffectRemoved?.Invoke(removed);
                _isDirty = true;
            }
        }
    }

    /// <summary>
    /// Thêm một status effect mới
    /// </summary>
    public void AddEffect(StatusEffectType type, float duration, float magnitude = 1f)
    {
        if (!HasInputAuthority) return;

        // Remove duplicates nếu cần
        RemoveEffect(type);
        
        var effect = new StatusEffect(type, duration, magnitude);
        _activeEffects.Add(effect);
        OnEffectAdded?.Invoke(type);
        _isDirty = true;
    }

    /// <summary>
    /// Xóa một status effect
    /// </summary>
    public void RemoveEffect(StatusEffectType type)
    {
        _activeEffects.RemoveAll(e => e.effectType == type);
        OnEffectRemoved?.Invoke(type);
        _isDirty = true;
    }

    /// <summary>
    /// Xóa toàn bộ debuffs (dùng cho Captain Black)
    /// </summary>
    public void ClearDebuffs()
    {
        var debuffTypes = new[] { 
            StatusEffectType.Slowness, 
            StatusEffectType.Stun, 
            StatusEffectType.Burn,
            StatusEffectType.Blindness 
        };

        foreach (var type in debuffTypes)
        {
            RemoveEffect(type);
        }
    }

    /// <summary>
    /// Kiểm tra xem nhân vật có bị ảnh hưởng bởi một effect không
    /// </summary>
    public bool HasEffect(StatusEffectType type)
    {
        return _activeEffects.Exists(e => e.effectType == type && e.IsActive);
    }

    /// <summary>
    /// Lấy độ mạnh của một effect (nếu có)
    /// </summary>
    public float GetEffectMagnitude(StatusEffectType type)
    {
        var effect = _activeEffects.Find(e => e.effectType == type && e.IsActive);
        return effect != null ? effect.magnitude : 0f;
    }

    /// <summary>
    /// Tính toán tốc độ cuối cùng dựa trên hiệu ứng
    /// </summary>
    public float CalculateSpeedMultiplier(float baseSpeed)
    {
        float multiplier = 1f;

        // Slowness: giảm tốc độ
        if (HasEffect(StatusEffectType.Slowness))
            multiplier *= (1f - GetEffectMagnitude(StatusEffectType.Slowness));

        // Swiftness: tăng tốc độ
        if (HasEffect(StatusEffectType.Swiftness))
            multiplier *= (1f + GetEffectMagnitude(StatusEffectType.Swiftness));

        // Stun: không di chuyển
        if (HasEffect(StatusEffectType.Stun))
            multiplier = 0f;

        return multiplier;
    }

    /// <summary>
    /// Tính toán damage cuối cùng dựa trên hiệu ứng
    /// </summary>
    public float CalculateDamage(float baseDamage)
    {
        float finalDamage = baseDamage;

        // Captain Black: giảm damage
        if (HasEffect(StatusEffectType.CaptainBlack))
            finalDamage *= (1f - GetEffectMagnitude(StatusEffectType.CaptainBlack));

        return finalDamage;
    }

    public List<StatusEffect> GetActiveEffects()
    {
        return new List<StatusEffect>(_activeEffects);
    }
}
