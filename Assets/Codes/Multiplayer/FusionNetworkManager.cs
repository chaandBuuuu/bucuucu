using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;
using System.Threading.Tasks;

public class FusionNetworkManager : FusionCallbacksBase
{
    public static FusionNetworkManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private int           maxPlayers  = 4;
    [SerializeField] private bool          autoConnect = true;

    [Header("Scene Index")]
    [SerializeField] private int lobbySceneIndex = 1;

    [Header("References")]
    [SerializeField] private PlayerData playerData;

    public NetworkRunner Runner { get; private set; }

    public event Action         OnConnectedEvent;
    public event Action<string> OnDisconnectedEvent;
    public event Action         OnJoinedSessionEvent;
    public event Action<string> OnJoinFailedEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (autoConnect)
            Debug.Log("[FusionNetworkManager] Sẵn sàng. Gọi CreateSession() hoặc JoinSession().");
    }

    public async Task CreateSession(string sessionName)
    {
        await StartRunner(GameMode.Host, sessionName);
    }

    public async Task JoinSession(string sessionName)
    {
        await StartRunner(GameMode.Client, sessionName);
    }

    private async Task StartRunner(GameMode mode, string sessionName)
    {
        if (Runner == null)
        {
            Runner = Instantiate(runnerPrefab);
            Runner.AddCallbacks(this);
        }

        var args = new StartGameArgs
        {
            GameMode     = mode,
            SessionName  = sessionName,
            PlayerCount  = maxPlayers,
            SceneManager = Runner.GetComponent<NetworkSceneManagerDefault>()
                        ?? Runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        Debug.Log($"[FusionNetworkManager] Bắt đầu {mode}: {sessionName}");

        var result = await Runner.StartGame(args);

        if (!result.Ok)
        {
            Debug.LogError($"[FusionNetworkManager] Thất bại: {result.ShutdownReason}");
            OnJoinFailedEvent?.Invoke(result.ShutdownReason.ToString());
        }
        else
        {
            Debug.Log("[FusionNetworkManager] Thành công! Đang load Lobby...");
            OnJoinedSessionEvent?.Invoke();

            // Host load scene, client sẽ tự follow
            if (Runner.IsServer)
            {
                Debug.Log($"[FusionNetworkManager] Host load scene index: {lobbySceneIndex}");
                Runner.LoadScene(SceneRef.FromIndex(lobbySceneIndex));
            }
            else
            {
                Debug.Log("[FusionNetworkManager] Client chờ Host load scene...");
            }
        }
    }

    public void LeaveSession()
    {
        if (Runner != null) { Runner.Shutdown(); Runner = null; }
    }

    public void SetPlayerName(string name)
    {
        if (playerData != null) playerData.playerName = name;
    }

    public void SetSelectedCharacter(int index)
    {
        if (playerData != null)
        {
            playerData.characterIndex = index;
            playerData.isReady        = true;
        }
        Debug.Log($"[FusionNetworkManager] Chọn nhân vật: {index}");
    }

    public override void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[FusionNetworkManager] Đã kết nối");
        OnConnectedEvent?.Invoke();
    }

    public override void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[FusionNetworkManager] Mất kết nối: {reason}");
        OnDisconnectedEvent?.Invoke(reason.ToString());
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        => Debug.Log($"[FusionNetworkManager] Player joined: {player}");

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        => Debug.Log($"[FusionNetworkManager] Player left: {player}");

    public override void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"[FusionNetworkManager] Shutdown: {reason}");
        Runner = null;
    }

    public override void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[FusionNetworkManager] Connect thất bại: {reason}");
        OnJoinFailedEvent?.Invoke(reason.ToString());
    }

    public override void OnSceneLoadDone(NetworkRunner runner)
        => Debug.Log("[FusionNetworkManager] Scene load xong!");

    public override void OnSceneLoadStart(NetworkRunner runner)
        => Debug.Log($"[FusionNetworkManager] Bắt đầu load scene...");

    public bool       IsConnected     => Runner != null && Runner.IsRunning;
    public bool       IsHost          => Runner != null && Runner.IsServer;
    public int        MaxPlayers      => maxPlayers;
    public PlayerData LocalPlayerData => playerData;
}