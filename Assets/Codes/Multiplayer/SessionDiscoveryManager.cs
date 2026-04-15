using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Handles session discovery for the menu.
/// - Connects to Photon Lobby to receive room list
/// - Updates room list automatically when sessions change
/// - Can be stopped when joining/hosting
/// </summary>
public class SessionDiscoveryManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static SessionDiscoveryManager Instance { get; private set; }

    [SerializeField] private NetworkRunner discoveryRunnerPrefab;

    private NetworkRunner _discoveryRunner;
    private bool _isDiscoveryActive = false;
    private List<SessionInfo> _discoveredSessions = new List<SessionInfo>();

    public event Action<List<SessionInfo>> OnSessionListUpdatedEvent;
    public event Action OnDiscoveryConnected;
    public event Action<string> OnDiscoveryFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        StopDiscovery();
    }

    /// <summary>
    /// Start session discovery — joins the Photon lobby to receive room list.
    /// FIX 1: Use JoinSessionLobby directly (no StartGame needed for discovery).
    /// FIX 2: Await JoinSessionLobby so we can handle failure.
    /// </summary>
    public async Task StartDiscovery()
    {
        if (_isDiscoveryActive)
        {
            Debug.Log("[SessionDiscoveryManager] Discovery đang chạy, bỏ qua...");
            // FIX 3: Re-fire cached sessions so late subscribers still get the list
            if (_discoveredSessions.Count > 0)
                OnSessionListUpdatedEvent?.Invoke(new List<SessionInfo>(_discoveredSessions));
            return;
        }

        Debug.Log("[SessionDiscoveryManager] 🔍 Bắt đầu tìm phòng...");
        _isDiscoveryActive = true;
        _discoveredSessions.Clear();

        if (_discoveryRunner == null)
        {
            _discoveryRunner = Instantiate(discoveryRunnerPrefab);
            _discoveryRunner.name = "SessionDiscoveryRunner";
        }

        _discoveryRunner.AddCallbacks(this);

        // ✅ FIX 1: Dùng JoinSessionLobby trực tiếp thay vì StartGame với GameMode.Client
        // GameMode.Client + SessionName="" sẽ fail hoặc không vào được lobby
        // JoinSessionLobby tự xử lý kết nối đến Photon Cloud và trigger OnSessionListUpdated
        var result = await _discoveryRunner.JoinSessionLobby(SessionLobby.ClientServer);

        if (!result.Ok)
        {
            Debug.LogError($"[SessionDiscoveryManager] ❌ JoinSessionLobby thất bại: {result.ShutdownReason}");
            _isDiscoveryActive = false;
            OnDiscoveryFailed?.Invoke(result.ShutdownReason.ToString());
            return;
        }

        Debug.Log("[SessionDiscoveryManager] ✅ Đã vào Lobby! Đang chờ danh sách phòng...");
        OnDiscoveryConnected?.Invoke();
    }

    /// <summary>
    /// Stop discovery and shut down the runner.
    /// </summary>
    public void StopDiscovery()
    {
        if (!_isDiscoveryActive) return;

        Debug.Log("[SessionDiscoveryManager] Stopping discovery...");
        _isDiscoveryActive = false;

        if (_discoveryRunner != null)
        {
            _discoveryRunner.RemoveCallbacks(this);
            _discoveryRunner.Shutdown();
            _discoveryRunner = null;  // ✅ Null sau shutdown để StartDiscovery có thể tạo mới
        }
    }

    public List<SessionInfo> GetDiscoveredSessions() => new List<SessionInfo>(_discoveredSessions);
    public int GetSessionCount() => _discoveredSessions.Count;

    // ==================== CALLBACKS ====================

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[SessionDiscoveryManager] ✅ Connected to server");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[SessionDiscoveryManager] Disconnected: {reason}");
        _isDiscoveryActive = false;
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[SessionDiscoveryManager] ❌ Connect failed: {reason}");
        OnDiscoveryFailed?.Invoke(reason.ToString());
        _isDiscoveryActive = false;
    }

    /// <summary>
    /// ✅ KEY CALLBACK: Photon calls this whenever the session list changes.
    /// </summary>
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[SessionDiscoveryManager] 📋 Session list updated: {sessionList.Count} sessions");

        _discoveredSessions.Clear();
        _discoveredSessions.AddRange(sessionList);

        foreach (var session in sessionList)
            Debug.Log($"  → {session.Name} ({session.PlayerCount}/{session.MaxPlayers})");

        OnSessionListUpdatedEvent?.Invoke(new List<SessionInfo>(_discoveredSessions));
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[SessionDiscoveryManager] Shutdown: {shutdownReason}");
        _isDiscoveryActive = false;
    }

    // ===== Unused interface stubs =====
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}