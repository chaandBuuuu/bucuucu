using UnityEngine;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// Quản lý trạng thái game: hunter, survivor, mode, phase
/// </summary>
public enum GamePhase
{
    Waiting,        // Chờ người chơi
    CharacterSelect,// Chọn nhân vật
    Playing,        // Đang chơi
    GameOver        // Kết thúc
}

public enum GameWinner
{
    None,           // Chưa kết thúc
    Hunter,         // Hunter thắng
    Survivors       // Survivors thắng
}

/// <summary>
/// Manages bonfires and wood collection for survivors
/// </summary>
[System.Serializable]
public class BonfireData
{
    public int id;
    public Vector2 position;
    [Networked] public int woodCollected { get; set; }
    [Networked] public bool isLit { get; set; }
    public int woodRequired = 5;

    public BonfireData(int id, Vector2 pos)
    {
        this.id = id;
        this.position = pos;
        woodCollected = 0;
        isLit = false;
    }
}

/// <summary>
/// Quản lý toàn bộ trạng thái trò chơi
/// </summary>
public class GameplayStateManager : NetworkBehaviour
{
    public static GameplayStateManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int totalBonfires = 4;
    [SerializeField] private int woodPerBonfire = 5;

    [Header("References")]
    [SerializeField] private Transform[] bonfireSpawns;

    // Network Properties
    [Networked] public GamePhase CurrentPhase { get; private set; }
    [Networked] public GameWinner GameWinner { get; private set; }
    [Networked] public float GameTimer { get; private set; }
    [Networked] public bool IsGameActive { get; private set; }

    private List<BonfireData> _bonfires = new List<BonfireData>();
    private List<NetworkCharacterController> _hunters = new List<NetworkCharacterController>();
    private List<NetworkCharacterController> _survivors = new List<NetworkCharacterController>();

    public event System.Action<GamePhase> OnPhaseChanged;
    public event System.Action<GameWinner> OnGameEnd;
    public event System.Action OnBonfireLit;
    public event System.Action OnWoodCollected;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentPhase = GamePhase.Waiting;
            GameWinner = GameWinner.None;
            IsGameActive = false;
            GameTimer = 0f;

            // Initialize bonfires
            for (int i = 0; i < totalBonfires; i++)
            {
                _bonfires.Add(new BonfireData(i, bonfireSpawns?[i]?.position ?? Vector2.zero));
            }

            Debug.Log("[GameplayStateManager] Initialized with " + totalBonfires + " bonfires");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (CurrentPhase == GamePhase.Playing)
        {
            GameTimer += Runner.DeltaTime;
            
            // Kiểm tra win conditions mỗi frame
            CheckWinConditions();
        }
    }

    /// <summary>
    /// Bắt đầu trò chơi
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_StartGame()
    {
        if (!HasStateAuthority) return;

        CurrentPhase = GamePhase.Playing;
        IsGameActive = true;
        GameTimer = 0f;

        OnPhaseChanged?.Invoke(CurrentPhase);
        Debug.Log("[GameplayStateManager] Game started!");
    }

    /// <summary>
    /// Kết thúc game
    /// </summary>
    private void EndGame(GameWinner winner)
    {
        CurrentPhase = GamePhase.GameOver;
        GameWinner = winner;
        IsGameActive = false;

        OnGameEnd?.Invoke(winner);
        Debug.Log($"[GameplayStateManager] Game ended! Winner: {winner}");
    }

    /// <summary>
    /// Kiểm tra điều kiện thắng
    /// </summary>
    private void CheckWinConditions()
    {
        // Hunter wins: toàn bộ survivor chết
        int aliveSurvivors = 0;
        foreach (var survivor in _survivors)
        {
            if (survivor != null && !survivor.IsDead)
                aliveSurvivors++;
        }

        if (aliveSurvivors == 0)
        {
            EndGame(GameWinner.Hunter);
            return;
        }

        // Survivors win: toàn bộ lửa trại được đốt + ít nhất 1 người trốn thoát
        int litBonfires = 0;
        foreach (var bonfire in _bonfires)
        {
            if (bonfire.isLit)
                litBonfires++;
        }

        if (litBonfires == totalBonfires)
        {
            // Kiểm tra xem có survivor nào đã escape chưa
            // (Implement escape logic)
            Debug.Log("[GameplayStateManager] All bonfires lit!");
        }
    }

    /// <summary>
    /// Thêm gỗ vào lửa trại
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddWoodToBonfire(int bonfireId, int amount)
    {
        if (bonfireId < 0 || bonfireId >= _bonfires.Count) return;

        var bonfire = _bonfires[bonfireId];
        bonfire.woodCollected += amount;

        // Kiểm tra xem lửa trại đã được đốt chưa
        if (bonfire.woodCollected >= woodPerBonfire && !bonfire.isLit)
        {
            bonfire.isLit = true;
            OnBonfireLit?.Invoke();
            Debug.Log($"[GameplayStateManager] Bonfire {bonfireId} lit!");
        }

        OnWoodCollected?.Invoke();
    }

    /// <summary>
    /// Lấy thông tin lửa trại
    /// </summary>
    public BonfireData GetBonfire(int id)
    {
        if (id >= 0 && id < _bonfires.Count)
            return _bonfires[id];
        return null;
    }

    public List<BonfireData> GetAllBonfires() => new List<BonfireData>(_bonfires);

    /// <summary>
    /// Đăng ký hunter/survivor
    /// </summary>
    public void RegisterCharacter(NetworkCharacterController controller)
    {
        if (controller.IsHunter)
        {
            if (!_hunters.Contains(controller))
                _hunters.Add(controller);
        }
        else if (controller.IsSurvivor)
        {
            if (!_survivors.Contains(controller))
                _survivors.Add(controller);
        }

        Debug.Log($"[GameplayStateManager] Registered {controller.GetRole()} character");
    }
}
