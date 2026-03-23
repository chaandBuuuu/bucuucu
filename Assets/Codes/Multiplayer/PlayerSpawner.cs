using UnityEngine;
using Fusion;
using Fusion.Sockets;

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

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    private NetworkRunner _runner;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Start()
    {
        if (FusionNetworkManager.Instance?.Runner != null)
            StartCoroutine(RegisterWithRunner(FusionNetworkManager.Instance.Runner));
        else
            StartCoroutine(WaitAndRegister());
    }

    private System.Collections.IEnumerator WaitAndRegister()
    {
        while (FusionNetworkManager.Instance?.Runner == null)
            yield return null;
        yield return RegisterWithRunner(FusionNetworkManager.Instance.Runner);
    }

    private System.Collections.IEnumerator RegisterWithRunner(NetworkRunner runner)
    {
        _runner = runner;
        _runner.AddCallbacks(this);
        Debug.Log("[PlayerSpawner] Đã đăng ký với Runner");

        // Late-spawn cho các player đã join
        if (_runner.IsServer)
        {
            foreach (PlayerRef player in _runner.ActivePlayers)
                yield return SpawnAsync(_runner, player);
        }
    }

    private void OnDestroy()
    {
        _runner?.RemoveCallbacks(this);
    }

    public override void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[PlayerSpawner] Scene load xong, spawn players...");
        if (!runner.IsServer) return;
        StartCoroutine(SpawnAllAsync(runner));
    }

    private System.Collections.IEnumerator SpawnAllAsync(NetworkRunner runner)
    {
        foreach (PlayerRef player in runner.ActivePlayers)
            yield return SpawnAsync(runner, player);
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        StartCoroutine(SpawnAsync(runner, player));
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && runner.TryGetPlayerObject(player, out NetworkObject obj))
            runner.Despawn(obj);
    }

    // ✅ Async spawn — tránh lỗi "Failed to load prefab synchronously"
    private System.Collections.IEnumerator SpawnAsync(NetworkRunner runner, PlayerRef player)
    {
        yield return null; // chờ 1 frame

        if (runner == null || !runner.IsServer) yield break;
        if (runner.TryGetPlayerObject(player, out _))
        {
            Debug.Log($"[PlayerSpawner] {player} đã có object, bỏ qua.");
            yield break;
        }

        Vector3       pos = GetSpawnPoint(player);
        NetworkObject obj = runner.Spawn(playerPrefab, pos, Quaternion.identity, inputAuthority: player);
        runner.SetPlayerObject(player, obj);
        Debug.Log($"[PlayerSpawner] Spawn {player} tại {pos}");
    }

    public void AttachCameraToLocalPlayer(Transform target)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;
        var follow = mainCamera.GetComponent<CameraFollow>()
                  ?? mainCamera.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(target);
    }

    private Vector3 GetSpawnPoint(PlayerRef player)
    {
        int index = (player.PlayerId - 1) % spawnPoints.Length;
        return spawnPoints[index];
    }
}