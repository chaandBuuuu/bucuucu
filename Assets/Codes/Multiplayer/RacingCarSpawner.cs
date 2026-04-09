using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawn cars cho racing game
/// </summary>
public class RacingCarSpawner : FusionCallbacksBase
{
    [Header("Car Spawn Settings")]
    [SerializeField] private NetworkObject carPrefab;
    [SerializeField] private Vector3[] spawnPoints = new Vector3[4]
    {
        new Vector3(0, -5, 0),
        new Vector3(5, -5, 0),
        new Vector3(-5, -5, 0),
        new Vector3(0, 5, 0)
    };

    private NetworkRunner _runner;
    private bool _isRegistered = false;
    private HashSet<PlayerRef> _spawnedPlayers = new HashSet<PlayerRef>();

    private void Update()
    {
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
        Debug.Log($"[RacingCarSpawner] Registered | IsServer={runner.IsServer}");

        if (_runner.IsServer)
            StartCoroutine(SpawnAllDelayed());
    }

    private IEnumerator SpawnAllDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        foreach (PlayerRef player in _runner.ActivePlayers)
            SpawnCar(_runner, player);
    }

    private void OnDestroy()
    {
        if (_runner != null)
            _runner.RemoveCallbacks(this);
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[RacingCarSpawner] OnPlayerJoined: {player}");
        if (!runner.IsServer) return;
        StartCoroutine(SpawnCarDelayed(runner, player));
    }

    private IEnumerator SpawnCarDelayed(NetworkRunner runner, PlayerRef player)
    {
        yield return new WaitForSeconds(0.3f);
        SpawnCar(runner, player);
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _spawnedPlayers.Remove(player);
        if (!runner.IsServer) return;
        if (runner.TryGetPlayerObject(player, out NetworkObject obj))
            runner.Despawn(obj);
    }

    private void SpawnCar(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.Contains(player)) return;
        if (runner.TryGetPlayerObject(player, out _))
        {
            _spawnedPlayers.Add(player);
            return;
        }

        Vector3 pos = GetSpawnPoint(player);

        NetworkObject carObj = runner.Spawn(
            carPrefab,
            pos,
            Quaternion.identity,
            inputAuthority: player
        );

        runner.SetPlayerObject(player, carObj);
        _spawnedPlayers.Add(player);
        Debug.Log($"[RacingCarSpawner] ✅ Spawned car for {player} at {pos}");
    }

    private Vector3 GetSpawnPoint(PlayerRef player)
    {
        int index = player.PlayerId % spawnPoints.Length;
        return spawnPoints[index];
    }
}
