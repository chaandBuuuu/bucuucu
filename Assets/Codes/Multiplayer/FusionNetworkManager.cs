using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class FusionNetworkManager : FusionCallbacksBase
{
    public static FusionNetworkManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private bool autoConnect = true;

    [Header("Scene Index")]
    [SerializeField] private int lobbySceneIndex = 1;
    [SerializeField] private int racingSceneIndex = 2;

    [Header("Racing - Car Selection")]
    [SerializeField] public CarPrefabList carPrefabList;

    // Lưu tên người chơi
    private readonly Dictionary<PlayerRef, string> _playerNames = new Dictionary<PlayerRef, string>();

    // Lưu lựa chọn xe
    private readonly Dictionary<PlayerRef, int> _playerCarChoices = new Dictionary<PlayerRef, int>();

    // ✅ NEW: Session/Room management
    private readonly List<SessionInfo> _availableSessions = new List<SessionInfo>();
    private string _storedPlayerName = "";  // Store player name before joining

    public NetworkRunner Runner { get; private set; }

    public event Action OnConnectedEvent;
    public event Action<string> OnDisconnectedEvent;
    public event Action OnJoinedSessionEvent;
    public event Action<string> OnJoinFailedEvent;
    public event Action<List<SessionInfo>> OnSessionListUpdatedEvent;  // ✅ NEW

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // ✅ NEW: Đảm bảo AudioManager được tạo
        AudioManager.EnsureExists();
    }

    // ================== PLAYER NAME ==================
    public void SetPlayerName(string name)
    {
        if (Runner == null || !Runner.IsRunning)
        {
            Debug.LogWarning("[FusionNetworkManager] Chưa kết nối, không thể lưu tên.");
            return;
        }

        PlayerRef localPlayer = Runner.LocalPlayer;
        _playerNames[localPlayer] = name;
        Debug.Log($"[FusionNetworkManager] Đã đặt tên: {name} cho Player {localPlayer}");
    }

    public string GetPlayerName(PlayerRef player)
    {
        return _playerNames.TryGetValue(player, out string name) ? name : $"Player {player.PlayerId}";
    }

    // ================== CAR CHOICE ==================
    public void RegisterPlayerCarChoice(PlayerRef player, int carIndex)
    {
        _playerCarChoices[player] = carIndex;
        Debug.Log($"[FusionNetworkManager] Player {player} chọn xe index {carIndex}");
    }

    public int GetPlayerCarChoice(PlayerRef player)
    {
        return _playerCarChoices.TryGetValue(player, out int index) ? index : 0;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RegisterCarChoice(PlayerRef player, int carIndex)
    {
        RegisterPlayerCarChoice(player, carIndex);
    }

    // ================== SESSION/ROOM MANAGEMENT ==================
    public List<SessionInfo> GetAvailableSessions()
    {
        return new List<SessionInfo>(_availableSessions);
    }

    public async Task CreateSession(string sessionName)
    {
        // ✅ NEW: Stop discovery before creating
        if (SessionDiscoveryManager.Instance != null)
        {
            SessionDiscoveryManager.Instance.StopDiscovery();
        }

        // Store player name for later use
        if (!string.IsNullOrEmpty(_storedPlayerName))
        {
            Debug.Log($"[FusionNetworkManager] Creating session '{sessionName}' with player '{_storedPlayerName}'");
        }

        await StartRunner(GameMode.Host, sessionName);
    }

    public async Task JoinSession(string sessionName)
    {
        // ✅ NEW: Stop discovery before joining
        if (SessionDiscoveryManager.Instance != null)
        {
            SessionDiscoveryManager.Instance.StopDiscovery();
        }

        await StartRunner(GameMode.Client, sessionName);
    }

    // ✅ NEW: Store player name before joining
    public void SetStoredPlayerName(string name)
    {
        _storedPlayerName = name;
        Debug.Log($"[FusionNetworkManager] Stored player name: {name}");
    }

    public string GetStoredPlayerName()
    {
        return _storedPlayerName;
    }

    private async Task StartRunner(GameMode mode, string sessionName)
    {
        if (Runner == null)
        {
            Runner = Instantiate(runnerPrefab);
            Runner.AddCallbacks(this);
        }

        // ✅ FIX: Increase server tick rate from 20Hz → 40Hz to reduce lag
        // Problem: Server was 20Hz, Client 64Hz = 3.2x mismatch = severe jitter
        // Solution: Match server to 40Hz (25ms per tick) for better sync
        var simConfig = new SimulationConfig()
        {
            TickRate = 40  // 40 Hz = 25ms per network update (was 20Hz = 50ms)
        };

        var args = new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = maxPlayers,
            SimulationConfig = simConfig,  // ✅ Override with 40Hz tick rate
            SceneManager = Runner.GetComponent<NetworkSceneManagerDefault>() 
                        ?? Runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        Debug.Log($"[FusionNetworkManager] ✅ Starting with {simConfig.TickRate}Hz server tick rate (was 20Hz)");
        var result = await Runner.StartGame(args);

        if (!result.Ok)
        {
            Debug.LogError($"Start game failed: {result.ShutdownReason}");
            OnJoinFailedEvent?.Invoke(result.ShutdownReason.ToString());
        }
        else
        {
            OnJoinedSessionEvent?.Invoke();
            if (Runner.IsServer)
            {
                Runner.LoadScene(SceneRef.FromIndex(lobbySceneIndex));
            }
        }
    }

    public void LeaveSession()
    {
        // ✅ NEW: Stop discovery when leaving
        if (SessionDiscoveryManager.Instance != null)
        {
            SessionDiscoveryManager.Instance.StopDiscovery();
        }

        if (Runner != null) Runner.Shutdown();
    }

    // ================== CALLBACKS ==================
    public override void OnConnectedToServer(NetworkRunner runner) => OnConnectedEvent?.Invoke();
    public override void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) 
        => OnDisconnectedEvent?.Invoke(reason.ToString());

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player joined: {player}");
        
        // ✅ FIX: Apply stored player name for local player
        if (player == runner.LocalPlayer && !string.IsNullOrEmpty(_storedPlayerName))
        {
            _playerNames[player] = _storedPlayerName;
            Debug.Log($"[FusionNetworkManager] ✅ Applied stored name '{_storedPlayerName}' for local player {player}");
        }
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
        => Debug.Log($"Player left: {player}");

    public override void OnShutdown(NetworkRunner runner, ShutdownReason reason) 
        => Runner = null;

    // ✅ NEW: Session list update callback (called when available sessions change)
    public override void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _availableSessions.Clear();
        _availableSessions.AddRange(sessionList);

        Debug.Log($"[FusionNetworkManager] Session list updated: {sessionList.Count} sessions available");
        foreach (var session in sessionList)
        {
            Debug.Log($"  - {session.Name} ({session.PlayerCount}/{session.MaxPlayers} players)");
        }

        OnSessionListUpdatedEvent?.Invoke(new List<SessionInfo>(_availableSessions));
    }
    public void RegisterCallbacks(INetworkRunnerCallbacks callbacks)
{
    if (Runner != null)
        Runner.AddCallbacks(callbacks);
}
}