using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections;
using System.Collections.Generic;

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

    private NetworkRunner      _runner;
    private bool               _isRegistered   = false;
    private HashSet<PlayerRef> _spawnedPlayers = new HashSet<PlayerRef>();

    private void Awake() => Debug.Log("[PlayerSpawner] Awake");

    private void Update()
    {
        if (!_isRegistered) TryRegister();
    }

    private void TryRegister()
    {
        var runner = FusionNetworkManager.Instance?.Runner;
        if (runner == null || !runner.IsRunning) return;

        _runner = runner;
        _runner.AddCallbacks(this);
        _isRegistered = true;
        Debug.Log($"[PlayerSpawner] Đã đăng ký | IsServer={runner.IsServer}");

        if (_runner.IsServer)
            StartCoroutine(SpawnAllDelayed());
    }

    private IEnumerator SpawnAllDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        foreach (PlayerRef player in _runner.ActivePlayers)
            SpawnPlayer(_runner, player);
    }

    private void OnDestroy() => _runner?.RemoveCallbacks(this);

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[PlayerSpawner] OnPlayerJoined: {player}");
        if (!runner.IsServer) return;
        StartCoroutine(SpawnPlayerDelayed(runner, player));
    }

    private IEnumerator SpawnPlayerDelayed(NetworkRunner runner, PlayerRef player)
    {
        yield return new WaitForSeconds(0.3f);
        SpawnPlayer(runner, player);
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _spawnedPlayers.Remove(player);
        if (!runner.IsServer) return;
        if (runner.TryGetPlayerObject(player, out NetworkObject obj))
            runner.Despawn(obj);
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.Contains(player)) return;
        if (runner.TryGetPlayerObject(player, out _))
        {
            _spawnedPlayers.Add(player);
            return;
        }

        Vector3 pos = GetSpawnPoint(player);

        // ✅ Dùng onBeforeSpawned để set position chính xác trước khi object active
        NetworkObject obj = runner.Spawn(
            playerPrefab,
            pos,
            Quaternion.identity,
            inputAuthority: player,
            onBeforeSpawned: (r, networkObj) =>
            {
                // Set position ngay trước khi spawn
                networkObj.transform.position = pos;
                Debug.Log($"[PlayerSpawner] onBeforeSpawned {player} tại {pos}");
            }
        );

        runner.SetPlayerObject(player, obj);
        _spawnedPlayers.Add(player);
        Debug.Log($"[PlayerSpawner] ✅ Spawned {player} tại {pos}");
    }

    private Vector3 GetSpawnPoint(PlayerRef player)
    {
        int index = (player.PlayerId - 1) % spawnPoints.Length;
        return spawnPoints[index];
    }
}