using UnityEngine;
using Fusion;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkTransform))]
public class MultiplayerCharacter : NetworkBehaviour
{
    [Header("Di Chuyển")]
    [SerializeField] private float moveSpeed    = 5f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private bool  faceDir      = true;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator       animator;
    [SerializeField] private PlayerData     playerData;

    [Networked] public  int     CharacterIndex    { get; private set; }
    [Networked] public  string  NetworkedName     { get; private set; }
    [Networked] private Vector2 NetworkedVelocity { get; set; }
    // FIX: Xóa NetworkedPos và NetworkedRot — NetworkTransform component đã tự sync

    private ChangeDetector _changes;
    private Rigidbody2D    rb;
    private Vector2        currentVelocity;
    private bool           _localSetupDone = false;

    private static readonly int AnimSpeed    = Animator.StringToHash("Speed");
    private static readonly int AnimMoveX    = Animator.StringToHash("MoveX");
    private static readonly int AnimMoveY    = Animator.StringToHash("MoveY");
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator       == null) animator       = GetComponent<Animator>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
    }

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        Debug.Log($"[MultiplayerCharacter] Spawned | HasInputAuthority={HasInputAuthority} | HasStateAuthority={HasStateAuthority}");

        if (!HasInputAuthority)
        {
            // FIX: Remote player dùng Kinematic để NetworkTransform điều khiển transform trực tiếp
            rb.bodyType  = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        TrySetupLocalPlayer();
        ApplyVisuals();
    }

    public override void Render()
    {
        if (!_localSetupDone) TrySetupLocalPlayer();

        // FIX: Xóa block Lerp thủ công cho remote player.
        // NetworkTransform đã tự sync và interpolate position/rotation.
        // Nếu tự Lerp thêm sẽ xung đột và làm remote player không move.

        foreach (var change in _changes.DetectChanges(this))
            if (change == nameof(CharacterIndex) || change == nameof(NetworkedName))
                ApplyVisuals();
    }

    private void TrySetupLocalPlayer()
    {
        if (!HasInputAuthority || _localSetupDone) return;
        _localSetupDone = true;

        rb.bodyType  = RigidbodyType2D.Dynamic;
        rb.simulated = true;

        CharacterIndex = playerData != null ? playerData.characterIndex : 0;
        NetworkedName  = playerData != null && !string.IsNullOrEmpty(playerData.playerName)
                         ? playerData.playerName
                         : $"Player {Runner.LocalPlayer.PlayerId}";

        gameObject.name = $"{GetCharName(CharacterIndex)}_P{Runner.LocalPlayer.PlayerId}";
        AttachCamera();
        Debug.Log($"[MultiplayerCharacter] ✅ Local: {NetworkedName} | {GetCharName(CharacterIndex)}");
    }

    private void AttachCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        var follow = cam.GetComponent<CameraFollow>()
                  ?? cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(transform);
        Debug.Log($"[MultiplayerCharacter] Camera → {gameObject.name}");
    }

    private void ApplyVisuals()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = GetColor(CharacterIndex);
    }

    public override void FixedUpdateNetwork()
    {
        // FIX: Dùng HasInputAuthority thay vì HasStateAuthority.
        // HasStateAuthority = Host chạy cho TẤT CẢ player → client không move được.
        // HasInputAuthority = chỉ owner của object mới chạy physics của chính mình.
        if (!HasInputAuthority) return;
        if (!GetInput(out NetworkInputData input)) return;

        Vector2 target  = input.Direction.normalized * moveSpeed;
        float   blend   = input.Direction.sqrMagnitude > 0.01f ? acceleration : deceleration;
        currentVelocity = Vector2.MoveTowards(currentVelocity, target, blend * Runner.DeltaTime);

        rb.linearVelocity = currentVelocity;
        NetworkedVelocity = currentVelocity; // Giữ để Update() dùng cho animation remote

        if (faceDir && input.Direction.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(input.Direction.y, input.Direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
        // FIX: Xóa NetworkedPos/NetworkedRot — NetworkTransform tự sync position và rotation
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

    private string GetCharName(int i) => i switch
    {
        0 => "Hacker", 1 => "Ghost_Hunter", 2 => "Priest", 3 => "Scientist", _ => "Unknown"
    };

    private Color GetColor(int i) => i switch
    {
        0 => new Color(0.8f, 0.3f, 0.3f),
        1 => new Color(0.3f, 0.8f, 0.3f),
        2 => new Color(0.8f, 0.8f, 0.3f),
        3 => new Color(0.3f, 0.3f, 0.8f),
        _ => Color.white
    };

    public bool   IsOwner        => HasInputAuthority;
    public string PlayerNickname => !string.IsNullOrEmpty(NetworkedName)
                                    ? NetworkedName
                                    : $"Player {Object.InputAuthority.PlayerId}";
}