using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;

public class RacingCarSpawner : FusionCallbacksBase
{
    [Header("Car Prefabs")]
    [SerializeField] private CarPrefabList carPrefabList;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints = new Transform[4];

    private NetworkRunner _runner;
    private bool _registered = false;
    private HashSet<PlayerRef> _spawnedPlayers = new HashSet<PlayerRef>();

    private void OnEnable()
    {
        // Reset khi scene load lại
        _registered = false;
        _spawnedPlayers.Clear();
    }

    private void Update()
    {
        if (_registered) return;

        var fm = FusionNetworkManager.Instance;
        if (fm == null || fm.Runner == null || !fm.Runner.IsRunning) return;

        _runner = fm.Runner;
        _runner.AddCallbacks(this);
        _registered = true;

        Debug.Log("[RacingCarSpawner] Đã đăng ký callback sau khi load scene");

        if (_runner.IsServer)
            StartCoroutine(SpawnAllPlayersDelayed());
    }

    private IEnumerator SpawnAllPlayersDelayed()
    {
        yield return new WaitForSeconds(1.2f);   // Đợi scene load ổn định

        Debug.Log("[RacingCarSpawner] Bắt đầu spawn tất cả player hiện có...");

        foreach (PlayerRef player in _runner.ActivePlayers)
        {
            SpawnCar(player);
        }
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        Debug.Log($"[RacingCarSpawner] Player {player} joined → Spawn xe");
        StartCoroutine(SpawnCarDelayed(player));
    }

    private IEnumerator SpawnCarDelayed(PlayerRef player)
    {
        yield return new WaitForSeconds(0.6f);
        SpawnCar(player);
    }

    private void SpawnCar(PlayerRef player)
    {
        if (_spawnedPlayers.Contains(player)) return;

        // Kiểm tra đã spawn chưa
        if (_runner.TryGetPlayerObject(player, out _))
        {
            _spawnedPlayers.Add(player);
            return;
        }

        int carIndex = FusionNetworkManager.Instance.GetPlayerCarChoice(player);
        NetworkObject prefab = (carPrefabList != null && carIndex < carPrefabList.carPrefabs.Length) 
            ? carPrefabList.carPrefabs[carIndex] : null;

        if (prefab == null)
        {
            Debug.LogError($"[RacingCarSpawner] Không tìm thấy prefab cho carIndex {carIndex}");
            return;
        }

        int idx = (player.PlayerId - 1) % Mathf.Max(1, spawnPoints.Length);
        Vector3 pos = (spawnPoints.Length > idx && spawnPoints[idx] != null) 
            ? spawnPoints[idx].position 
            : new Vector3((idx - 1.5f) * 4f, -12f, 0);

        NetworkObject carObj = _runner.Spawn(prefab, pos, Quaternion.identity, player);
        _runner.SetPlayerObject(player, carObj);
        _spawnedPlayers.Add(player);

        // Gán camera cho local player
        if (_runner.LocalPlayer == player && Camera.main != null)
        {
            var follow = Camera.main.GetComponent<CameraFollow>() ?? Camera.main.gameObject.AddComponent<CameraFollow>();
            follow.SetTarget(carObj.transform);
        }

        Debug.Log($"[RacingCarSpawner] ✅ Spawn thành công {prefab.name} cho Player {player} tại vị trí {pos}");
    }

    private void OnDestroy()
    {
        if (_runner != null)
            _runner.RemoveCallbacks(this);
    }
}