using UnityEngine;
using Fusion;

/// <summary>
/// Enum cho các loại powerup
/// </summary>
public enum PowerupType
{
    Shield,     // Khiên bảo vệ 3 giây
    Gun,        // Bắn đạn vào xe phía trước
    SpeedBoost, // Tăng tốc độ
    Trap        // Đặt bẫy làm chậm
}

/// <summary>
/// Quản lý inventory powerup của xe.
/// FIX:
///   - SpeedBoost thực sự hoạt động qua CarController.ApplySpeedBoost()
///   - Shield reset màu khi hết hạn
///   - Bullet và Trap dùng [SerializeField] prefab thay vì Resources.Load
///   - FireGun dùng runner.Spawn thay vì Instantiate để đạn xuất hiện trên tất cả client
///   - Xóa _slowRemainingTime không sử dụng
/// </summary>
public class PowerupInventory : NetworkBehaviour
{
    [Header("Prefabs — kéo từ Project vào Inspector")]
    [SerializeField] private NetworkObject bulletPrefab;
    [SerializeField] private NetworkObject trapPrefab;

    private PowerupType? _currentPowerup = null;
    private float        _shieldRemainingTime = 0f;
    private bool         _hasShield = false;
    private Color        _originalColor = Color.white;
    private SpriteRenderer _sr;

    public event System.Action<PowerupType> OnPowerupAcquired;
    public event System.Action<PowerupType> OnPowerupUsed;
    public event System.Action              OnPowerupEmpty;

    public override void Spawned()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
    }

    private void Update()
    {
        if (!_hasShield) return;

        _shieldRemainingTime -= Time.deltaTime;
        if (_shieldRemainingTime <= 0f)
        {
            _hasShield = false;
            // FIX: Reset màu xe về màu gốc khi shield hết hạn
            if (_sr != null) _sr.color = _originalColor;
            Debug.Log("[PowerupInventory] Shield hết hạn");
        }
    }

    public void AddPowerup(PowerupType type)
    {
        _currentPowerup = type;
        OnPowerupAcquired?.Invoke(type);
        Debug.Log($"[PowerupInventory] Nhận powerup: {type}");
    }

    public void UseCurrent()
    {
        if (_currentPowerup == null) return;

        PowerupType type = _currentPowerup.Value;

        switch (type)
        {
            case PowerupType.Shield:     ActivateShield();    break;
            case PowerupType.Gun:        FireGun();           break;
            case PowerupType.SpeedBoost: ActivateSpeedBoost(); break;
            case PowerupType.Trap:       PlaceTrap();         break;
        }

        OnPowerupUsed?.Invoke(type);
        _currentPowerup = null;
        OnPowerupEmpty?.Invoke();
    }

    // ── Powerup implementations ──────────────────────────────────────────────

    private void ActivateShield()
    {
        _hasShield           = true;
        _shieldRemainingTime = RacingConstants.SHIELD_DURATION;

        if (_sr != null)
            _sr.color = new Color(0.5f, 1f, 0.5f, 1f); // Xanh lá = shield active

        Debug.Log("[PowerupInventory] Shield kích hoạt!");
    }

    private void FireGun()
    {
        // Chỉ owner của xe mới bắn (HasInputAuthority)
        if (!HasInputAuthority) return;

        var selfCar = GetComponent<CarController>();
        if (selfCar == null) return;

        // Tìm xe phía trước gần nhất
        CarController targetCar = FindNearestCarAhead(selfCar);
        if (targetCar == null)
        {
            Debug.Log("[PowerupInventory] Không có mục tiêu phía trước");
            return;
        }

        if (bulletPrefab == null)
        {
            Debug.LogError("[PowerupInventory] bulletPrefab chưa được gán trong Inspector!");
            return;
        }

        // FIX: Dùng runner.Spawn để đạn xuất hiện trên TẤT CẢ client
        // Trước đây Instantiate() chỉ tạo local → client khác không thấy đạn
        RPC_SpawnBullet(targetCar.Object.Id, transform.position, transform.rotation);
    }

    /// <summary>
    /// RPC lên server để spawn bullet — server spawn NetworkObject rồi sync xuống.
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SpawnBullet(NetworkId targetId, Vector3 spawnPos, Quaternion spawnRot)
    {
        if (bulletPrefab == null) return;

        // Tìm NetworkObject của target theo ID
        if (!Runner.TryFindObject(targetId, out NetworkObject targetObj)) return;
        var targetCar = targetObj.GetComponent<CarController>();
        if (targetCar == null) return;

        NetworkObject bulletObj = Runner.Spawn(bulletPrefab, spawnPos, spawnRot,
                                               inputAuthority: Object.InputAuthority);
        var bullet = bulletObj.GetComponent<BulletProjectile>();
        if (bullet != null)
            bullet.SetTarget(targetCar);

        Debug.Log($"[PowerupInventory] Đạn được bắn vào {targetObj.name}");
    }

    private void ActivateSpeedBoost()
    {
        // FIX: Gọi API trên CarController thay vì placeholder rỗng
        // CarController.ApplySpeedBoost() set [Networked] SpeedMultiplier → sync mọi client
        var car = GetComponent<CarController>();
        if (car == null) return;

        // Chỉ StateAuthority được gọi ApplySpeedBoost (nó check bên trong)
        // Client gửi RPC lên server
        RPC_RequestSpeedBoost();
        Debug.Log("[PowerupInventory] SpeedBoost kích hoạt!");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSpeedBoost()
    {
        var car = GetComponent<CarController>();
        if (car != null)
            car.ApplySpeedBoost(RacingConstants.SPEED_BOOST_MULTIPLIER,
                                RacingConstants.SPEED_BOOST_DURATION);
    }

    private void PlaceTrap()
    {
        if (!HasInputAuthority) return;

        if (trapPrefab == null)
        {
            Debug.LogError("[PowerupInventory] trapPrefab chưa được gán trong Inspector!");
            return;
        }

        // FIX: Dùng RPC → server spawn trap NetworkObject
        RPC_SpawnTrap(transform.position);
        Debug.Log("[PowerupInventory] Bẫy được đặt!");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SpawnTrap(Vector3 pos)
    {
        if (trapPrefab == null) return;
        Runner.Spawn(trapPrefab, pos, Quaternion.identity);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private CarController FindNearestCarAhead(CarController self)
    {
        var allCars    = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        CarController nearest = null;
        float minDist  = float.MaxValue;

        Vector2 selfPos     = transform.position;
        Vector2 selfForward = transform.up;

        foreach (var car in allCars)
        {
            if (car == self) continue;

            Vector2 dirToCar = ((Vector2)car.transform.position - selfPos).normalized;

            // Chỉ nhắm xe phía trước (dot > 0.5 ≈ trong góc 60° phía trước)
            if (Vector2.Dot(selfForward, dirToCar) <= 0.5f) continue;

            float dist = Vector2.Distance(selfPos, car.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = car;
            }
        }

        return nearest;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public bool          HasShield()         => _hasShield;
    public PowerupType?  GetCurrentPowerup() => _currentPowerup;
    public bool          HasPowerup()        => _currentPowerup.HasValue;
}