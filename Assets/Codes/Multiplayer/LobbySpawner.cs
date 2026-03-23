using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections;

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

    private IEnumerator RegisterWhenReady()
    {
        while (FusionNetworkManager.Instance?.Runner == null)
            yield return null;

        _runner = FusionNetworkManager.Instance.Runner;
        _runner.AddCallbacks(this);
        Debug.Log("[LobbySpawner] Đã đăng ký với Runner");

        if (_runner.IsServer && startButtonObj != null)
            startButtonObj.SetActive(true);

        // Late-spawn cho các player đã join trước khi scene load xong
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
    {
        if (!runner.IsServer) return;
        // FIX: Despawn object khi player rời lobby
        if (runner.TryGetPlayerObject(player, out NetworkObject obj))
            runner.Despawn(obj);
        Debug.Log($"[LobbySpawner] Player left: {player}");
    }

    public void OnStartGameClicked()
    {
        if (_runner == null || !_runner.IsServer) return;
        Debug.Log("[LobbySpawner] Host bắt đầu game!");
        _runner.LoadScene(SceneRef.FromIndex(2));
    }

    private IEnumerator SpawnAsync(NetworkRunner runner, PlayerRef player)
    {
        // Chờ 1 frame để Fusion sẵn sàng
        yield return null;

        if (runner == null || !runner.IsServer) yield break;
        if (runner.TryGetPlayerObject(player, out _)) yield break;

        Vector3 pos = GetSpawnPoint(player);

        // FIX: Bỏ onBeforeSpawned — chỉ truyền pos vào Spawn() là đủ và chính xác.
        // onBeforeSpawned set transform.position nhưng Fusion override lại bằng pos arg → sai vị trí.
        NetworkObject obj = runner.Spawn(
            lobbyPlayerPrefab,
            pos,
            Quaternion.identity,
            inputAuthority: player
        );

        runner.SetPlayerObject(player, obj);
        Debug.Log($"[LobbySpawner] Spawn {player} tại {pos}");
    }

    private Vector3 GetSpawnPoint(PlayerRef player)
    {
        if (spawnPoints.Length == 0) return Vector3.zero;
        int index = (player.PlayerId - 1) % spawnPoints.Length;
        return spawnPoints[index] != null ? spawnPoints[index].position : Vector3.zero;
    }
}