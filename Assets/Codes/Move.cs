using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-Down 2D Character Controller sử dụng Unity Input System mới
/// Yêu cầu: Rigidbody2D + PlayerInput component trên cùng GameObject
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class TopDownController : MonoBehaviour
{
    [Header("Di Chuyển")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;

    [Header("Xoay Nhân Vật")]
    [SerializeField] private bool faceMovementDirection = true;

    [Header("Animation (tuỳ chọn)")]
    [SerializeField] private Animator animator;

    // Internal
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVelocity;

    // Animator parameter hashes (tối ưu hiệu năng)
    private static readonly int AnimSpeed    = Animator.StringToHash("Speed");
    private static readonly int AnimMoveX    = Animator.StringToHash("MoveX");
    private static readonly int AnimMoveY    = Animator.StringToHash("MoveY");
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");

    // -------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Tự tìm Animator nếu chưa gán
        if (animator == null)
            animator = GetComponent<Animator>();

        // Cấu hình Rigidbody2D cho top-down
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    // -------------------------------------------------------
    // Input Callbacks (gọi bởi PlayerInput component)
    // Đặt tên Action trong Input Actions asset trùng với tên hàm bên dưới,
    // hoặc dùng chế độ "Send Messages" / "Broadcast Messages".
    // -------------------------------------------------------

    /// <summary>Nhận input từ action "Move"</summary>
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // -------------------------------------------------------
    // Movement Logic
    // -------------------------------------------------------

    private void HandleMovement()
    {
        // Tính vận tốc mục tiêu
        Vector2 targetVelocity = moveInput.normalized * moveSpeed;

        // Chọn tốc độ gia/giảm tốc tuỳ theo có input hay không
        float blendFactor = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;

        // Làm mượt chuyển động
        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            blendFactor * Time.fixedDeltaTime
        );

        rb.linearVelocity = currentVelocity;

        // Xoay nhân vật theo hướng di chuyển
        if (faceMovementDirection && moveInput.sqrMagnitude > 0.01f)
            FaceDirection(moveInput);

        // Cập nhật animation
        UpdateAnimation();
    }

    private void FaceDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = currentVelocity.magnitude;
        bool isMoving = speed > 0.05f;

        animator.SetFloat(AnimSpeed,    speed);
        animator.SetBool(AnimIsMoving,  isMoving);

        // Dùng input thay vì velocity để blend tree mượt hơn
        if (isMoving)
        {
            animator.SetFloat(AnimMoveX, moveInput.x);
            animator.SetFloat(AnimMoveY, moveInput.y);
        }
    }

    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------

    /// <summary>Thay đổi tốc độ di chuyển lúc runtime</summary>
    public void SetMoveSpeed(float newSpeed) => moveSpeed = Mathf.Max(0f, newSpeed);

    /// <summary>Khoá / mở khoá di chuyển</summary>
    public void SetMovementEnabled(bool enabled)
    {
        if (!enabled)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            currentVelocity = Vector2.zero;
        }
        this.enabled = enabled;
    }
}