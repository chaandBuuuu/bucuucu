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

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        StartCoroutine(RegisterWhenReady());
    }

    private System.Collections.IEnumerator RegisterWhenReady()
    {
        while (FusionNetworkManager.Instance?.Runner == null)
            yield return null;

        _runner = FusionNetworkManager.Instance.Runner;
        _runner.AddCallbacks(this);
        Debug.Log("[PlayerSpawner] Đã đăng ký với Runner");
    }

    private void OnDestroy()
    {
        _runner?.RemoveCallbacks(this);
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        Vector3       pos = GetSpawnPoint(player);
        NetworkObject obj = runner.Spawn(playerPrefab, pos, Quaternion.identity, inputAuthority: player);
        runner.SetPlayerObject(player, obj);
        Debug.Log($"[PlayerSpawner] Spawn {player} tại {pos}");
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && runner.TryGetPlayerObject(player, out NetworkObject obj))
            runner.Despawn(obj);
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
