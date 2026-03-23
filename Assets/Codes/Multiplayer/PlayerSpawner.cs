using UnityEngine;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Spawn nhân vật trong game scene
/// Gắn vào GameObject trong scene Game
/// </summary>
public class PlayerSpawner : FusionCallbacksBase
{
    [Header("Spawn Settings")]
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Vector3[]     spawnPoints = new Vector3[4]
    {
        new Vector3(-5, 0, 0),
        new Vector3( 5, 0, 0),
        new Vector3(-5, 5, 0),
        new Vector3( 5, 5, 0)
    };

    private NetworkRunner _runner;
    private bool          _isRegistered = false;

    private void Awake()
    {
        Debug.Log("[PlayerSpawner] Awake");
    }

    private void Update()
    {
        // ✅ Thử đăng ký mỗi frame cho đến khi thành công
        // Giống cách InputHandler làm — đảm bảo không bị miss
        if (!_isRegistered)
            TryRegister();
    }

    private void TryRegister()
    {
        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner == null || !runner.IsRunning) return;

        _runner = runner;
        _runner.AddCallbacks(this);
        _isRegistered = true;
        Debug.Log($"[PlayerSpawner] Đã đăng ký | IsServer={runner.IsServer}");

        // Spawn ngay cho các player đã có mặt
        if (_runner.IsServer)
        {
            foreach (PlayerRef player in _runner.ActivePlayers)
                SpawnPlayer(_runner, player);
        }
    }

    private void OnDestroy()
    {
        _runner?.RemoveCallbacks(this);
    }

    public override void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[PlayerSpawner] OnSceneLoadDone");
        if (!runner.IsServer) return;

        foreach (PlayerRef player in runner.ActivePlayers)
            SpawnPlayer(runner, player);
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[PlayerSpawner] OnPlayerJoined: {player}");
        if (!runner.IsServer) return;
        SpawnPlayer(runner, player);
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        if (runner.TryGetPlayerObject(player, out NetworkObject obj))
            runner.Despawn(obj);
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        // Tránh spawn 2 lần
        if (runner.TryGetPlayerObject(player, out _))
        {
            Debug.Log($"[PlayerSpawner] {player} đã có object, bỏ qua.");
            return;
        }

        Vector3       pos = GetSpawnPoint(player);
        NetworkObject obj = runner.Spawn(
            playerPrefab,
            pos,
            Quaternion.identity,
            inputAuthority: player  // ✅ gán inputAuthority đúng player
        );
        runner.SetPlayerObject(player, obj);
        Debug.Log($"[PlayerSpawner] Spawned {player} tại {pos}");
    }

    private Vector3 GetSpawnPoint(PlayerRef player)
    {
        int index = (player.PlayerId - 1) % spawnPoints.Length;
        return spawnPoints[index];
    }
}