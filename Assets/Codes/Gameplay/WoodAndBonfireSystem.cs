using UnityEngine;
using Fusion;

/// <summary>
/// Quản lý hệ thống gỗ và lửa trại
/// </summary>
public class WoodSystem : NetworkBehaviour
{
    [SerializeField] private GameObject woodPrefab;
    [SerializeField] private int woodLimit = 20;
    [SerializeField] private Vector2 spawnAreaMin = Vector2.zero;
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(20, 20);

    [Networked] private int _woodCount { get; set; }

    private Wood[] _woodPieces;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            _woodCount = woodLimit;
            SpawnWood();
        }
    }

    private void SpawnWood()
    {
        for (int i = 0; i < woodLimit; i++)
        {
            Vector2 randomPos = new Vector2(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y)
            );

            // Spawn wood object (network)
            // Implement network spawning with Fusion
        }
    }

    /// <summary>
    /// Nhặt gỗ
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickupWood(int playerId)
    {
        if (_woodCount > 0)
        {
            _woodCount--;
            Debug.Log($"[WoodSystem] Player {playerId} picked up wood! Remaining: {_woodCount}");
        }
    }
}

/// <summary>
/// Đơn vị gỗ riêng lẻ
/// </summary>
public class Wood : NetworkBehaviour
{
    [SerializeField] private float pickupRange = 0.5f;
    
    [Networked] public bool IsPickedUp { get; private set; }

    private CircleCollider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        if (_collider == null)
            _collider = gameObject.AddComponent<CircleCollider2D>();
        
        _collider.radius = pickupRange;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsPickedUp) return;

        var survivor = collision.GetComponent<NetworkCharacterController>();
        if (survivor != null && survivor.IsSurvivor)
        {
            PickUp(survivor);
        }
    }

    private void PickUp(NetworkCharacterController survivor)
    {
        IsPickedUp = true;
        
        // Add wood to survivor inventory (implement with inventory system)
        Debug.Log($"[Wood] Picked up by {survivor.GetCharacterID()}");
        
        Destroy(gameObject);
    }
}

/// <summary>
/// Lửa trại - điểm thu thập gỗ
/// </summary>
public class Bonfire : NetworkBehaviour
{
    [SerializeField] private int bonfireId;
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private int woodRequired = 5;

    [Networked] public int WoodCollected { get; private set; }
    [Networked] public bool IsLit { get; private set; }

    private CircleCollider2D _interactZone;
    private GameplayStateManager _gameManager;

    private void Awake()
    {
        _interactZone = GetComponent<CircleCollider2D>();
        if (_interactZone == null)
            _interactZone = gameObject.AddComponent<CircleCollider2D>();
        
        _interactZone.radius = interactRange;
        _interactZone.isTrigger = true;
    }

    private void Start()
    {
        _gameManager = GameplayStateManager.Instance;
    }

    /// <summary>
    /// Survivor đặt gỗ vào lửa trại
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddWood(int amount)
    {
        if (IsLit) return;

        WoodCollected += amount;

        if (WoodCollected >= woodRequired)
        {
            IsLit = true;
            Debug.Log($"[Bonfire {bonfireId}] Lit! Wood: {WoodCollected}/{woodRequired}");
            
            if (_gameManager != null)
                _gameManager.RPC_AddWoodToBonfire(bonfireId, amount);
        }
        else
        {
            Debug.Log($"[Bonfire {bonfireId}] Wood: {WoodCollected}/{woodRequired}");
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        var survivor = collision.GetComponent<NetworkCharacterController>();
        if (survivor != null && survivor.IsSurvivor)
        {
            // Allow interaction (implement with input system)
        }
    }
}

/// <summary>
/// Exit Gate - pintu thoát của survivor
/// </summary>
public class ExitGate : NetworkBehaviour
{
    [SerializeField] private float escapeRange = 2f;
    [Networked] public bool IsOpen { get; private set; }
    
    private CircleCollider2D _exitZone;
    private int _escapedCount;

    private void Awake()
    {
        _exitZone = GetComponent<CircleCollider2D>();
        if (_exitZone == null)
            _exitZone = gameObject.AddComponent<CircleCollider2D>();
        
        _exitZone.radius = escapeRange;
        _exitZone.isTrigger = true;
    }

    public override void Spawned()
    {
        // Nếu toàn bộ lửa trại được đốt, mở cửa
        _escapedCount = 0;
        IsOpen = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_OpenGate()
    {
        IsOpen = true;
        Debug.Log("[ExitGate] Gate opened!");
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!IsOpen) return;

        var survivor = collision.GetComponent<NetworkCharacterController>();
        if (survivor != null && survivor.IsSurvivor && !survivor.IsDead)
        {
            EscapeSurvivor(survivor);
        }
    }

    private void EscapeSurvivor(NetworkCharacterController survivor)
    {
        _escapedCount++;
        Debug.Log($"[ExitGate] {survivor.GetCharacterID()} escaped! Total: {_escapedCount}");
        
        // Mark as escaped (disable player, show escape animation)
        survivor.gameObject.SetActive(false);
    }
}
