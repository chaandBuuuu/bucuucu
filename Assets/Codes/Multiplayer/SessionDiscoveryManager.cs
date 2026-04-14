using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// ✅ NEW: Handles session discovery for the menu
/// - Starts a separate runner just to query available rooms
/// - Updates room list when sessions change
/// - Can be stopped when joining/hosting
/// </summary>
public class SessionDiscoveryManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static SessionDiscoveryManager Instance { get; private set; }

    [SerializeField] private NetworkRunner discoveryRunnerPrefab;
    [SerializeField] private float discoveryTimeout = 10f;

    private NetworkRunner _discoveryRunner;
    private bool _isDiscoveryActive = false;
    private List<SessionInfo> _discoveredSessions = new List<SessionInfo>();

    public event Action<List<SessionInfo>> OnSessionListUpdatedEvent;  // ✅ Renamed to avoid conflict with interface method
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
    /// Start session discovery - connect and get available rooms list
    /// </summary>
    public async Task StartDiscovery()
    {
        if (_isDiscoveryActive)
        {
            Debug.Log("[SessionDiscoveryManager] Discovery already active");
            return;
        }

        Debug.Log("[SessionDiscoveryManager] Starting session discovery...");
        _isDiscoveryActive = true;

        // Create discovery runner if not exists
        if (_discoveryRunner == null)
        {
            _discoveryRunner = Instantiate(discoveryRunnerPrefab);
            _discoveryRunner.name = "SessionDiscoveryRunner";
        }

        // Add callbacks
        _discoveryRunner.AddCallbacks(this);

        // Start as Client to query sessions
        var args = new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = "", // Empty to discover, not join specific room
            PlayerCount = 1,
            SceneManager = _discoveryRunner.GetComponent<NetworkSceneManagerDefault>()
                        ?? _discoveryRunner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await _discoveryRunner.StartGame(args);

        if (!result.Ok)
        {
            Debug.LogError($"[SessionDiscoveryManager] Discovery failed: {result.ShutdownReason}");
            _isDiscoveryActive = false;
            OnDiscoveryFailed?.Invoke(result.ShutdownReason.ToString());
            StopDiscovery();
        }
        else
        {
            Debug.Log("[SessionDiscoveryManager] Discovery runner started, waiting for session list...");
            OnDiscoveryConnected?.Invoke();
            
            // Set timeout to stop discovery if no sessions received
            _ = Task.Delay((int)(discoveryTimeout * 1000)).ContinueWith(_ =>
            {
                if (_isDiscoveryActive && _discoveredSessions.Count == 0)
                {
                    Debug.LogWarning("[SessionDiscoveryManager] No sessions found after timeout");
                }
            });
        }
    }

    /// <summary>
    /// Stop discovery and shut down the runner
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
        }
    }

    /// <summary>
    /// Get currently discovered sessions
    /// </summary>
    public List<SessionInfo> GetDiscoveredSessions()
    {
        return new List<SessionInfo>(_discoveredSessions);
    }

    /// <summary>
    /// Get count of discovered sessions
    /// </summary>
    public int GetSessionCount()
    {
        return _discoveredSessions.Count;
    }

    // ==================== CALLBACKS ====================

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[SessionDiscoveryManager] Connected to server");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[SessionDiscoveryManager] Disconnected: {reason}");
        _isDiscoveryActive = false;
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[SessionDiscoveryManager] Connect failed: {reason}");
        OnDiscoveryFailed?.Invoke(reason.ToString());
        _isDiscoveryActive = false;
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    /// ✅ KEY CALLBACK: Called when session list updates
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[SessionDiscoveryManager] Session list updated: {sessionList.Count} sessions");

        _discoveredSessions.Clear();
        _discoveredSessions.AddRange(sessionList);

        // Print available sessions
        foreach (var session in sessionList)
        {
            Debug.Log($"  - {session.Name} ({session.PlayerCount}/{session.MaxPlayers} players)");
        }

        // Notify listeners
        OnSessionListUpdatedEvent?.Invoke(new List<SessionInfo>(_discoveredSessions));
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[SessionDiscoveryManager] Shutdown: {shutdownReason}");
        _isDiscoveryActive = false;
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }
}
