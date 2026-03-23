using UnityEngine;
using Fusion;
using Fusion.Sockets;

public class LobbySpawner : FusionCallbacksBase
{
    [Header("Spawn Settings")]
    [SerializeField] private NetworkObject lobbyPlayerPrefab;
    [SerializeField] private Transform[]   spawnPoints = new Transform[4];

    [Header("UI - chỉ Host thấy")]
    [SerializeField] private GameObject startButtonObj;

    private NetworkRunner _runner;

    private void Start()
    {
        if (startButtonObj != null) startButtonObj.SetActive(false);
        StartCoroutine(RegisterWhenReady());
    }

    private System.Collections.IEnumerator RegisterWhenReady()
    {
        while (FusionNetworkManager.Instance?.Runner == null)
            yield return null;

        _runner = FusionNetworkManager.Instance.Runner;
        _runner.AddCallbacks(this);
        Debug.Log("[LobbySpawner] Đã đăng ký với Runner");

        if (_runner.IsServer && startButtonObj != null)
            startButtonObj.SetActive(true);

        // Late-spawn cho các player đã join
        if (_runner.IsServer)
        {
            foreach (PlayerRef player in _runner.ActivePlayers)
            {
                if (!_runner.TryGetPlayerObject(player, out _))
                    yield return SpawnAsync(_runner, player);
            }
        }
    }

    private void OnDestroy()
    {
        _runner?.RemoveCallbacks(this);
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        if (runner.TryGetPlayerObject(player, out _)) return;
        StartCoroutine(SpawnAsync(runner, player));
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        => Debug.Log($"[LobbySpawner] Player left: {player}");

    public void OnStartGameClicked()
    {
        if (_runner == null || !_runner.IsServer) return;
        Debug.Log("[LobbySpawner] Host bắt đầu game!");
        _runner.LoadScene(SceneRef.FromIndex(2));
    }

    // ✅ Dùng async spawn để tránh lỗi "Failed to load prefab synchronously"
    private System.Collections.IEnumerator SpawnAsync(NetworkRunner runner, PlayerRef player)
    {
        // Chờ 1 frame để Fusion sẵn sàng
        yield return null;

        if (runner == null || !runner.IsServer) yield break;
        if (runner.TryGetPlayerObject(player, out _)) yield break;

        Vector3 pos = GetSpawnPoint(player);
        runner.Spawn(lobbyPlayerPrefab, pos, Quaternion.identity, inputAuthority: player);
        Debug.Log($"[LobbySpawner] Spawn {player} tại {pos}");
    }

    private Vector3 GetSpawnPoint(PlayerRef player)
    {
        if (spawnPoints.Length == 0) return Vector3.zero;
        int index = (player.PlayerId - 1) % spawnPoints.Length;
        return spawnPoints[index] != null ? spawnPoints[index].position : Vector3.zero;
    }
}