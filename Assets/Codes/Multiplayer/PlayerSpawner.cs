using UnityEngine;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Spawn nhân vật trong game scene
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

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    private NetworkRunner _runner;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Start()
    {
        // Đăng ký ngay trong Start, không dùng coroutine nữa
        if (FusionNetworkManager.Instance?.Runner != null)
        {
            RegisterWithRunner(FusionNetworkManager.Instance.Runner);
        }
        else
        {
            // Fallback: dùng coroutine nếu Runner chưa sẵn sàng
            StartCoroutine(RegisterWhenReady());
        }
    }

    private void RegisterWithRunner(NetworkRunner runner)
    {
        _runner = runner;
        _runner.AddCallbacks(this);
        Debug.Log("[PlayerSpawner] Đã đăng ký với Runner");

        // Spawn ngay cho tất cả player hiện có
        if (_runner.IsServer)
        {
            foreach (PlayerRef player in _runner.ActivePlayers)
            {
                SpawnPlayer(_runner, player);
            }
        }
    }

    private System.Collections.IEnumerator RegisterWhenReady()
    {
        while (FusionNetworkManager.Instance?.Runner == null)
            yield return null;

        RegisterWithRunner(FusionNetworkManager.Instance.Runner);
    }

    private void OnDestroy()
    {
        _runner?.RemoveCallbacks(this);
    }

    // ✅ Callback khi scene load xong — spawn tất cả player
    public override void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[PlayerSpawner] Scene load xong, bắt đầu spawn...");
        if (!runner.IsServer) return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            SpawnPlayer(runner, player);
        }
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        SpawnPlayer(runner, player);
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && runner.TryGetPlayerObject(player, out NetworkObject obj))
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
        Debug.Log("[PlayerSpawner] Camera gắn xong");
    }

    private Vector3 GetSpawnPoint(PlayerRef player)
    {
        int index = (player.PlayerId - 1) % spawnPoints.Length;
        return spawnPoints[index];
    }
}