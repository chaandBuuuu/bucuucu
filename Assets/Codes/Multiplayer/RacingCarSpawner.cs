using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;

public class RacingCarSpawner : FusionCallbacksBase
{
    [Header("Car Spawn Settings")]
    [SerializeField] private CarPrefabList carPrefabList;   // ← Kéo asset CarPrefabList vào
    [SerializeField] private Transform[] spawnPoints = new Transform[4];

    private NetworkRunner _runner;
    private bool _isRegistered = false;
    private HashSet<PlayerRef> _spawnedPlayers = new HashSet<PlayerRef>();

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

        if (_runner.IsServer)
            StartCoroutine(SpawnAllDelayed());
    }

    private IEnumerator SpawnAllDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        foreach (PlayerRef player in _runner.ActivePlayers)
            SpawnCar(_runner, player);
    }

    private void OnDestroy() => _runner?.RemoveCallbacks(this);

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        StartCoroutine(SpawnCarDelayed(runner, player));
    }

    private IEnumerator SpawnCarDelayed(NetworkRunner runner, PlayerRef player)
    {
        yield return new WaitForSeconds(0.3f);
        SpawnCar(runner, player);
    }

    private void SpawnCar(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.Contains(player)) return;
        if (runner.TryGetPlayerObject(player, out _)) return;

        // LẤY XE MÀ PLAYER ĐÃ CHỌN
        int carIndex = FusionNetworkManager.Instance.GetPlayerCarChoice(player);
        NetworkObject prefab = carPrefabList != null && carPrefabList.carPrefabs.Length > carIndex
            ? carPrefabList.carPrefabs[carIndex]
            : null;

        if (prefab == null)
        {
            Debug.LogError($"[RacingCarSpawner] Không tìm thấy prefab xe index {carIndex}");
            return;
        }

        int idx = (player.PlayerId - 1) % spawnPoints.Length;
        Vector3 pos = spawnPoints[idx] != null ? spawnPoints[idx].position : Vector3.zero;

        var obj = runner.Spawn(prefab, pos, Quaternion.identity, player);
        runner.SetPlayerObject(player, obj);

        Debug.Log($"[RacingCarSpawner] ✅ Spawned {prefab.name} cho Player {player}");
    }
}