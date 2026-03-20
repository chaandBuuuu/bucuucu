using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

/// <summary>
/// Điều khiển nhân vật trong lobby
/// Cho phép di chuyển tự do, không cần chọn nhân vật trước
/// Khác với MultiplayerCharacter (game scene)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class LobbyPlayerController : MonoBehaviourPun
{
    [Header("Di Chuyển")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;

    [Header("Xoay Nhân Vật")]
    [SerializeField] private bool faceMovementDirection = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Player Display")]
    [SerializeField] private TextMesh nameDisplay; // TextMesh trên đầu player
    [SerializeField] private PlayerInput playerInput;

    // Internal
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private Vector2 networkPosition;
    private float networkRotation;

    // Animator parameter hashes
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
    private static readonly int AnimMoveY = Animator.StringToHash("MoveY");
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        // Khởi tạo vị trí network
        networkPosition = transform.position;
        networkRotation = transform.eulerAngles.z;
    }

    private void Start()
    {
        // Chỉ owner mới có input
        if (!photonView.IsMine)
        {
            if (playerInput != null)
                playerInput.enabled = false;
            return;
        }

        // Enable input cho owner - rất quan trọng!
        if (playerInput != null)
            playerInput.enabled = true;
        else
            Debug.LogWarning("[LobbyPlayerController] PlayerInput not found. Make sure it's on the same GameObject.");

        // Update tên hiển thị
        gameObject.name = $"LobbyPlayer_{PhotonNetwork.LocalPlayer.NickName}";
        
        // Set color based on player trong room
        int playerIndex = GetPlayerIndex();
        SetPlayerColor(playerIndex);
        
        // Hiển thị tên lên đầu
        DisplayPlayerName(PhotonNetwork.LocalPlayer.NickName);

        Debug.Log("[LobbyPlayerController] Ready for input! Use WASD to move.");
    }

    private void Update()
    {
        if (!photonView.IsMine)
            return;

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            // Non-owner smooth movement
            SmoothNetworkMovement();
            return;
        }

        HandleMovement();
    }

    /// <summary>Nhận input từ "Move" action</summary>
    public void OnMove(InputValue value)
    {
        if (!photonView.IsMine)
            return;

        moveInput = value.Get<Vector2>();
    }

    private void HandleMovement()
    {
        // Tính vận tốc mục tiêu
        Vector2 targetVelocity = moveInput.normalized * moveSpeed;

        // Gia/giảm tốc
        float blendFactor = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;

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

    private void SmoothNetworkMovement()
    {
        if (photonView.IsMine)
            return;

        // Lerp vị trí
        transform.position = Vector2.Lerp(
            transform.position,
            networkPosition,
            Time.fixedDeltaTime * 5f
        );

        // Lerp xoay
        float currentRotation = transform.eulerAngles.z;
        float newRotation = Mathf.LerpAngle(currentRotation, networkRotation, Time.fixedDeltaTime * 5f);
        transform.rotation = Quaternion.AngleAxis(newRotation, Vector3.forward);
    }

    private void DisplayPlayerName(string playerName)
    {
        if (nameDisplay != null)
        {
            nameDisplay.text = playerName;
        }
        else
        {
            // Tạo text display nếu chưa có
            GameObject nameObject = new GameObject("NameDisplay");
            nameObject.transform.SetParent(transform);
            nameObject.transform.localPosition = Vector3.zero + Vector3.up * 1.5f;

            TextMesh textMesh = nameObject.AddComponent<TextMesh>();
            textMesh.text = playerName;
            textMesh.fontSize = 4;
            textMesh.alignment = TextAlignment.Center;

            nameDisplay = textMesh;
        }
    }

    private void SetPlayerColor(int index)
    {
        Color[] colors = new Color[4]
        {
            new Color(0.8f, 0.3f, 0.3f, 1f), // Đỏ
            new Color(0.3f, 0.8f, 0.3f, 1f), // Xanh lá
            new Color(0.8f, 0.8f, 0.3f, 1f), // Vàng
            new Color(0.3f, 0.3f, 0.8f, 1f)  // Xanh dương
        };

        if (spriteRenderer != null && index >= 0 && index < colors.Length)
        {
            spriteRenderer.color = colors[index];
        }
    }

    private int GetPlayerIndex()
    {
        if (!PhotonNetwork.InRoom)
            return 0;

        int index = 0;
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                return index;
            index++;
        }

        return 0;
    }

    /// <summary>Serialize position/rotation cho network</summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.eulerAngles.z);
            stream.SendNext(currentVelocity);
        }
        else
        {
            networkPosition = (Vector2)stream.ReceiveNext();
            networkRotation = (float)stream.ReceiveNext();
            currentVelocity = (Vector2)stream.ReceiveNext();
        }
    }

    public bool IsOwner => photonView.IsMine;
    public string PlayerName => PhotonNetwork.LocalPlayer.NickName;
}
