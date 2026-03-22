using UnityEngine;
using Fusion;

[RequireComponent(typeof(Rigidbody2D))]
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
    [Networked] private Vector2 NetworkedVelocity { get; set; }
    [Networked] private float   NetworkedRotation { get; set; }

    private ChangeDetector _changes;
    private Rigidbody2D    rb;
    private Vector2        currentVelocity;

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

        if (HasInputAuthority)
        {
            CharacterIndex  = playerData != null ? playerData.characterIndex : 0;
            gameObject.name = $"{GetName(CharacterIndex)}_P{Runner.LocalPlayer.PlayerId}";
        }
        ApplyVisuals();
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(CharacterIndex))
                ApplyVisuals();
        }
    }

    private void ApplyVisuals()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = GetColor(CharacterIndex);
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        Vector2 target = input.Direction.normalized * moveSpeed;
        float   blend  = input.Direction.sqrMagnitude > 0.01f ? acceleration : deceleration;

        currentVelocity   = Vector2.MoveTowards(currentVelocity, target, blend * Runner.DeltaTime);
        rb.linearVelocity = currentVelocity;
        NetworkedVelocity = currentVelocity;

        if (faceDir && input.Direction.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(input.Direction.y, input.Direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
            NetworkedRotation  = transform.eulerAngles.z;
        }
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

    private string GetName(int i) => i switch
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
    public string PlayerNickname => $"Player {Object.InputAuthority.PlayerId}";
}