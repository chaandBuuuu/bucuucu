using UnityEngine;

/// <summary>
/// Enum cho các loại powerup
/// </summary>
public enum PowerupType
{
    Shield,      // 3 giây khiên
    Gun,         // Bắn đạn, Q để kích hoạt
    SpeedBoost,  // Tăng tốc độ
    Trap         // Đặt bẫy làm chậm
}

/// <summary>
/// Quản lý inventory powerup của xe
/// </summary>
public class PowerupInventory : MonoBehaviour
{
    [SerializeField] private int maxPowerups = 1;
    private PowerupType? _currentPowerup = null;
    private float _shieldRemainingTime = 0f;
    private float _slowRemainingTime = 0f;
    private bool _hasShield = false;

    public event System.Action<PowerupType> OnPowerupAcquired;
    public event System.Action<PowerupType> OnPowerupUsed;
    public event System.Action OnPowerupEmpty;

    private void Update()
    {
        // Update shield timer
        if (_hasShield)
        {
            _shieldRemainingTime -= Time.deltaTime;
            if (_shieldRemainingTime <= 0)
            {
                _hasShield = false;
                Debug.Log("[PowerupInventory] Shield expired");
            }
        }

        // Update slow timer
        if (_slowRemainingTime > 0)
        {
            _slowRemainingTime -= Time.deltaTime;
        }
    }

    public void AddPowerup(PowerupType type)
    {
        _currentPowerup = type;
        OnPowerupAcquired?.Invoke(type);
    }

    public void UseCurrent()
    {
        if (_currentPowerup == null) return;

        PowerupType type = _currentPowerup.Value;
        
        switch (type)
        {
            case PowerupType.Shield:
                ActivateShield();
                break;
            case PowerupType.Gun:
                FireGun();
                break;
            case PowerupType.SpeedBoost:
                ActivateSpeedBoost();
                break;
            case PowerupType.Trap:
                PlaceTrap();
                break;
        }

        OnPowerupUsed?.Invoke(type);
        _currentPowerup = null;
    }

    private void ActivateShield()
    {
        _hasShield = true;
        _shieldRemainingTime = 3f;
        
        // Visual: tô màu xanh lá
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(0.5f, 1f, 0.5f, 1f);
        
        Debug.Log("[PowerupInventory] Shield activated!");
    }

    private void FireGun()
    {
        var carController = GetComponent<CarController>();
        if (carController == null) return;

        // Tìm xe phía trước gần nhất
        var allCars = FindObjectsByType<CarController>();
        CarController targetCar = null;
        float minDistance = float.MaxValue;

        Vector2 carPos = transform.position;
        Vector3 carForward = transform.up;

        foreach (var car in allCars)
        {
            if (car == carController) continue;
            
            Vector2 targetPos = car.transform.position;
            Vector2 dirToCar = (targetPos - carPos).normalized;
            
            // Chỉ target xe phía trước
            if (Vector2.Dot(carForward, dirToCar) > 0.5f)
            {
                float distance = Vector2.Distance(carPos, targetPos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetCar = car;
                }
            }
        }

        if (targetCar != null)
        {
            // Spawn bullet
            var bulletPrefab = Resources.Load<BulletProjectile>("Prefabs/Bullet");
            if (bulletPrefab != null)
            {
                var bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                bullet.SetTarget(targetCar);
                Debug.Log("[PowerupInventory] Gun fired!");
            }
        }
    }

    private void ActivateSpeedBoost()
    {
        var carController = GetComponent<CarController>();
        if (carController != null)
        {
            // Tạm thời tăng maxSpeed
            StartCoroutine(SpeedBoostCoroutine());
        }
        Debug.Log("[PowerupInventory] Speed boost activated!");
    }

    private System.Collections.IEnumerator SpeedBoostCoroutine()
    {
        var carController = GetComponent<CarController>();
        // Access to maxSpeed through reflection or public method
        // For now, just a placeholder
        yield return new WaitForSeconds(5f);
    }

    private void PlaceTrap()
    {
        // Tạo trap object tại vị trí hiện tại
        var trapPrefab = Resources.Load<TrapObject>("Prefabs/Trap");
        if (trapPrefab != null)
        {
            Instantiate(trapPrefab, transform.position, Quaternion.identity);
            Debug.Log("[PowerupInventory] Trap placed!");
        }
    }

    public bool HasShield() => _hasShield;
    public PowerupType? GetCurrentPowerup() => _currentPowerup;
    public bool HasPowerup() => _currentPowerup.HasValue;
}
