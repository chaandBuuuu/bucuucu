using UnityEngine;
using Fusion;

/// <summary>
/// Điều khiển nhân vật lobby — Fusion version
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkTransform))] // FIX: thêm NetworkTransform để sync position cho remote player
public class LobbyPlayerController : NetworkBehaviour
{
    [Header("Di Chuyển")]
    [SerializeField] private float moveSpeed    = 5f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private bool  faceDir      = true;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator       animator;
    [SerializeField] private TextMesh       nameDisplay;

    [Networked] private Vector2 NetworkedVelocity { get; set; }

    private Rigidbody2D rb;
    private Vector2     currentVelocity;

    private static readonly int AnimSpeed    = Animator.StringToHash("Speed");
    private static readonly int AnimMoveX    = Animator.StringToHash("MoveX");
    private static readonly int AnimMoveY    = Animator.StringToHash("MoveY");
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator       == null) animator       = GetComponent<Animator>();
    }

    public override void Spawned()
    {
        // FIX: Remote player dùng Kinematic để NetworkTransform điều khiển transform
        if (!HasInputAuthority)
        {
            rb.bodyType  = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        // FIX: Setup visual cho TẤT CẢ player (cả local lẫn remote), không chỉ HasInputAuthority
        int colorIdx = (Object.InputAuthority.PlayerId - 1) % 4;
        SetColor(colorIdx);
        ShowName($"Player {Object.InputAuthority.PlayerId}");

        if (HasInputAuthority)
        {
            gameObject.name = $"LobbyPlayer_{Runner.LocalPlayer}";
            Debug.Log("[LobbyPlayerController] Sẵn sàng!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        // FIX: Dùng HasInputAuthority thay vì chỉ GetInput
        // GetInput() trả true cho cả StateAuthority (Host) → Host chạy physics cho client
        // HasInputAuthority đảm bảo chỉ owner mới move chính mình
        if (!HasInputAuthority) return;
        if (!GetInput(out NetworkInputData input)) return;

        Vector2 target  = input.Direction.normalized * moveSpeed;
        float   blend   = input.Direction.sqrMagnitude > 0.01f ? acceleration : deceleration;

        currentVelocity   = Vector2.MoveTowards(currentVelocity, target, blend * Runner.DeltaTime);
        rb.linearVelocity = currentVelocity;
        NetworkedVelocity = currentVelocity;

        if (faceDir && input.Direction.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(input.Direction.y, input.Direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
        // NetworkTransform tự sync position/rotation — không cần thêm gì
    }

    private void Update()
    {
        if (animator == null) return;
        Vector2 vel   = HasInputAuthority ? currentVelocity : NetworkedVelocity;
        float   speed = vel.magnitude;
        animator.SetFloat(AnimSpeed,   speed);
        animator.SetFloat(AnimMoveX,   vel.x);
        animator.SetFloat(AnimMoveY,   vel.y);
        animator.SetBool(AnimIsMoving, speed > 0.1f);
    }

    private void ShowName(string playerName)
    {
        if (nameDisplay != null) { nameDisplay.text = playerName; return; }
        var obj = new GameObject("NameDisplay");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.up * 1.5f;
        var tm = obj.AddComponent<TextMesh>();
        tm.text = playerName; tm.fontSize = 4; tm.alignment = TextAlignment.Center;
        nameDisplay = tm;
    }

    private void SetColor(int index)
    {
        Color[] colors = {
            new Color(0.8f, 0.3f, 0.3f),
            new Color(0.3f, 0.8f, 0.3f),
            new Color(0.8f, 0.8f, 0.3f),
            new Color(0.3f, 0.3f, 0.8f)
        };
        if (spriteRenderer != null && index >= 0 && index < colors.Length)
            spriteRenderer.color = colors[index];
    }
}