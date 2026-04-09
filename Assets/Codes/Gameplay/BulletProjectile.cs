using UnityEngine;

/// <summary>
/// Đạn bắn từ gun powerup
/// </summary>
public class BulletProjectile : MonoBehaviour
{
    [Header("Bullet Config")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float damage = 20f;

    private CarController _targetCar;
    private Vector2 _moveDirection;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();
        
        _rb.gravityScale = 0f;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (_targetCar != null)
        {
            // Follow target
            Vector2 dirToTarget = (_targetCar.transform.position - transform.position).normalized;
            _rb.linearVelocity = dirToTarget * speed;
        }
        else
        {
            // Move in direction
            _rb.linearVelocity = _moveDirection.normalized * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var car = collision.GetComponent<CarController>();
        if (car != null && car != _targetCar) return; // Only hit target
        
        if (car != null && car == _targetCar)
        {
            // Hit target - apply damage
            car.RPC_ApplySlow(0.5f, 3f); // Slow 50% for 3 seconds
            Destroy(gameObject);
            Debug.Log("[BulletProjectile] Hit!");
        }
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
