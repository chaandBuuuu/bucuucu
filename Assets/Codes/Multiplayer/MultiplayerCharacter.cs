using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

/// <summary>
/// Mở rộng TopDownController để hỗ trợ Multiplayer với Photon
/// - Đồng bộ vị trí, hướng, animation giữa các player
/// - Chỉ lấy input từ owner của player
/// - Tối ưu network bandwidth
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PhotonView))]
public class MultiplayerCharacter : MonoBehaviourPun, IPunObservable
{
    [Header("Di Chuyển")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;

    [Header("Xoay Nhân Vật")]
    [SerializeField] private bool faceMovementDirection = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Character Setup")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int characterIndex = 0;

    [Header("Network Settings")]
    [SerializeField] private float networkUpdateRate = 0.1f; // Gửi dữ liệu mỗi 0.1s

    // Internal
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private Vector2 networkPosition;
    private float networkRotation;
    private float lastNetworkUpdate = 0f;

    // Animator parameter hashes
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
    private static readonly int AnimMoveY = Animator.StringToHash("MoveY");
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Tự tìm Animator nếu chưa gán
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Cấu hình Rigidbody2D cho top-down
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // Khởi tạo vị trí network
        networkPosition = transform.position;
        networkRotation = transform.eulerAngles.z;
    }

    private void Start()
    {
        // Nếu không phải owner, disable PlayerInput
        if (!photonView.IsMine)
        {
            DisableLocalInput();
        }
        else
        {
            // Setup nhân vật cho owner
            SetupCharacter();
        }
    }

    private void Update()
    {
        // Chỉ owner mới xử lý input
        if (!photonView.IsMine)
            return;

        // Cập nhật animation
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        // Chỉ owner mới xử lý movement
        if (!photonView.IsMine)
        {
            // Non-owner sẽ smooth chuyển động dựa trên dữ liệu nhận được
            SmoothNetworkMovement();
            return;
        }

        HandleMovement();
    }

    /// <summary>Setup nhân vật dựa trên characterIndex</summary>
    private void SetupCharacter()
    {
        characterIndex = PhotonNetworkManager.Instance.SelectedCharacterIndex;
        SetCharacterVisuals(characterIndex);
        
        gameObject.name = $"{PhotonNetworkManager.Instance.SelectedCharacter}_P{photonView.Owner.ActorNumber}";
    }

    /// <summary>Đặt visual nhân vật dựa trên index</summary>
    private void SetCharacterVisuals(int index)
    {
        // Tùy chỉnh sprite, color, model dựa vào nhân vật
        string characterName = index switch
        {
            0 => "Hacker",
            1 => "Ghost_Hunter",
            2 => "Priest",
            3 => "Scientist",
            _ => "Unknown"
        };

        // Có thể load prefab khác hoặc đặt sprite khác tùy vào character
        // VD: Load từ Resources/Characters/{characterName}
        
        if (spriteRenderer != null)
        {
            // Tô màu khác nhau cho mỗi nhân vật
            spriteRenderer.color = GetCharacterColor(index);
        }

        Debug.Log($"[MultiplayerCharacter] Thiết lập nhân vật: {characterName} (Index: {index})");
    }

    /// <summary>Lấy màu nhân vật dựa trên index</summary>
    private Color GetCharacterColor(int index)
    {
        return index switch
        {
            0 => new Color(0.8f, 0.3f, 0.3f, 1f), // Hacker - Đỏ
            1 => new Color(0.3f, 0.8f, 0.3f, 1f), // Ghost Hunter - Xanh lá
            2 => new Color(0.8f, 0.8f, 0.3f, 1f), // Priest - Vàng
            3 => new Color(0.3f, 0.3f, 0.8f, 1f), // Scientist - Xanh dương
            _ => Color.white
        };
    }

    /// <summary>Vô hiệu hóa input cho non-owner</summary>
    private void DisableLocalInput()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        // Nằm yên (không di chuyển)
        moveInput = Vector2.zero;
    }

    /// <summary>Nhận input từ "Move" action</summary>
    public void OnMove(InputValue value)
    {
        if (!photonView.IsMine)
            return;

        moveInput = value.Get<Vector2>();
    }

    // ==================== Movement Logic ====================

    private void HandleMovement()
    {
        // Tính vận tốc mục tiêu
        Vector2 targetVelocity = moveInput.normalized * moveSpeed;

        // Gia/giảm tốc
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
    }

    private void FaceDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        float speed = currentVelocity.magnitude;
        animator.SetFloat(AnimSpeed, speed);
        animator.SetFloat(AnimMoveX, moveInput.x);
        animator.SetFloat(AnimMoveY, moveInput.y);
        animator.SetBool(AnimIsMoving, speed > 0.1f);
    }

    /// <summary>Smooth network movement cho non-owner players</summary>
    private void SmoothNetworkMovement()
    {
        // Nếu là owner, không làm gì
        if (photonView.IsMine)
            return;

        // Lerp vị trí từ hiện tại tới networkPosition
        transform.position = Vector2.Lerp(
            transform.position,
            networkPosition,
            Time.fixedDeltaTime * 5f // 5x tốc độ sync
        );

        // Lerp xoay
        float currentRotation = transform.eulerAngles.z;
        float newRotation = Mathf.LerpAngle(currentRotation, networkRotation, Time.fixedDeltaTime * 5f);
        transform.rotation = Quaternion.AngleAxis(newRotation, Vector3.forward);
    }

    // ==================== Photon IPunObservable ====================

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Owner gửi dữ liệu
            stream.SendNext(transform.position);
            stream.SendNext(transform.eulerAngles.z);
            stream.SendNext(currentVelocity);
            stream.SendNext(moveInput);
        }
        else
        {
            // Non-owner nhận dữ liệu
            networkPosition = (Vector2)stream.ReceiveNext();
            networkRotation = (float)stream.ReceiveNext();
            currentVelocity = (Vector2)stream.ReceiveNext();
            moveInput = (Vector2)stream.ReceiveNext();

            // Cập nhật animation
            UpdateAnimation();
        }
    }

    // ==================== Public Methods ====================

    /// <summary>Kiểm tra có phải owner không</summary>
    public bool IsOwner => photonView.IsMine;

    /// <summary>Lấy character index</summary>
    public int CharacterIndex => characterIndex;

    /// <summary>Lấy actor number của player này</summary>
    public int PlayerActorNumber => photonView.Owner.ActorNumber;

    /// <summary>Lấy tên player</summary>
    public string PlayerNickname => photonView.Owner.NickName;
}
