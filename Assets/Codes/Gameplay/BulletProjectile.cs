using UnityEngine;
using Fusion;

/// <summary>
/// FIX:
///   - Chuyển thành NetworkBehaviour — được spawn qua runner.Spawn() từ PowerupInventory
///     nên tồn tại trên tất cả client. Trước đây Instantiate() chỉ tạo local.
///   - Sửa logic OnTriggerEnter2D: bullet không có target sẽ hit bất kỳ xe nào
///   - Dùng RacingConstants thay vì hardcode
///   - Xóa field damage không sử dụng
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BulletProjectile : NetworkBehaviour
{
    [Header("Bullet Config")]
    [SerializeField] private float speed    = RacingConstants.BULLET_SPEED;
    [SerializeField] private float lifetime = RacingConstants.BULLET_LIFETIME;

    private CarController _targetCar;
    private Vector2       _moveDirection;
    private Rigidbody2D   _rb;
    private bool          _hasHit = false; // tránh hit nhiều lần

    private void Awake()
    {
        _rb             = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
    }

    public override void Spawned()
    {
        // Tự hủy sau lifetime giây — chỉ server despawn
        if (Runner.IsServer)
            StartCoroutine(DespawnAfterLifetime());
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ authority (server) điều khiển vật lý đạn
        if (!HasStateAuthority) return;

        if (_targetCar != null && !_targetCar.IsFinished)
        {
            Vector2 dirToTarget = (_targetCar.transform.position - transform.position).normalized;
            _rb.linearVelocity  = dirToTarget * speed;
        }
        else
        {
            _rb.linearVelocity = _moveDirection.normalized * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Chỉ server xử lý hit để tránh duplicate
        if (!Runner.IsServer) return;
        if (_hasHit)          return;

        var car = collision.GetComponent<CarController>();
        if (car == null) return;

        // FIX: Logic cũ bị ngược — nếu _targetCar = null thì bullet không hit ai cả.
        // Logic mới:
        //   - Có target: chỉ hit đúng target
        //   - Không có target: hit bất kỳ xe nào
        bool shouldHit = (_targetCar == null) || (car == _targetCar);
        if (!shouldHit) return;

        _hasHit = true;
        car.RPC_ApplySlow(RacingConstants.GUN_SLOW_AMOUNT, RacingConstants.GUN_SLOW_DURATION);
        Debug.Log($"[BulletProjectile] Trúng {car.name}!");

        Runner.Despawn(Object);
    }

    private System.Collections.IEnumerator DespawnAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);
        if (Object != null && Object.IsValid)
            Runner.Despawn(Object);
    }

    public void SetTarget(CarController target)
    {
        _targetCar = target;
    }

    public void SetDirection(Vector2 direction)
    {
        _moveDirection = direction;
    }
}