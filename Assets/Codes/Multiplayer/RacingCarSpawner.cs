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

    [Header("Camera Manager")]
    [Tooltip("Prefab của MultiCameraManager (NetworkObject) – server spawn khi scene load")]
    [SerializeField] private NetworkObject cameraManagerPrefab;

    private NetworkRunner _runner;
    private bool _registered = false;
    private HashSet<PlayerRef> _spawnedPlayers = new HashSet<PlayerRef>();

    private void Start()
    {
        var mgr = FusionNetworkManager.Instance;
        if (mgr != null && mgr.Runner != null)
            InitializeWithRunner(mgr.Runner);
        else
            StartCoroutine(WaitForRunnerAndInit());
    }

    private IEnumerator WaitForRunnerAndInit()
    {
        float timeout = 10f, elapsed = 0f;
        while (elapsed < timeout)
        {
            var mgr = FusionNetworkManager.Instance;
            if (mgr != null && mgr.Runner != null && mgr.Runner.IsRunning)
            {
                InitializeWithRunner(mgr.Runner);
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.LogError("[RacingCarSpawner] Timeout: Runner không sẵn sàng!");
    }

    public void InitializeWithRunner(NetworkRunner runner)
    {
        if (_registered) return;
        _runner = runner;
        _runner.AddCallbacks(this);
        _registered = true;

        Debug.Log($"[RacingCarSpawner] ✅ Registered. IsServer={_runner.IsServer}");

        if (_runner.IsServer && _runner.IsRunning)
            StartCoroutine(SpawnCameraManagerThenCars());
    }

    private void OnEnable()
    {
        _registered = false;
        _spawnedPlayers.Clear();
    }

    public override void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;
        Debug.Log("[RacingCarSpawner] OnSceneLoadDone");
        StartCoroutine(SpawnCameraManagerThenCars());
    }

    /// <summary>
    /// ✅ Spawn MultiCameraManager trước → chờ → rồi spawn cars
    /// </summary>
    private IEnumerator SpawnCameraManagerThenCars()
    {
        // Spawn CameraManager nếu chưa có instance
        if (cameraManagerPrefab != null && MultiCameraManager.Instance == null)
        {
            _runner.Spawn(cameraManagerPrefab, Vector3.zero, Quaternion.identity);
            Debug.Log("[RacingCarSpawner] ✅ Spawned MultiCameraManager prefab");

            // Chờ để Spawned() chạy xong trên tất cả clients
            yield return new WaitForSeconds(0.5f);
        }
        else if (cameraManagerPrefab == null)
        {
            Debug.LogWarning("[RacingCarSpawner] ⚠️ cameraManagerPrefab chưa gán! Camera sẽ không hoạt động.");
        }

        StartCoroutine(SpawnAllPlayersDelayed());
    }

    public override void OnSceneLoadStart(NetworkRunner runner)
    {
        _spawnedPlayers.Clear();
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        StartCoroutine(SpawnCarDelayed(player));
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _spawnedPlayers.Remove(player);
        MultiCameraManager.Instance?.OnPlayerLeft(player);
    }

    private IEnumerator SpawnAllPlayersDelayed()
    {
        yield return new WaitForSeconds(1.5f);
        while (_runner == null || !_runner.IsRunning) yield return null;

        Debug.Log("[RacingCarSpawner] Spawning all players...");
        foreach (PlayerRef player in _runner.ActivePlayers)
            SpawnCar(player);

        yield return new WaitForSeconds(2f);
        StartCountdownIfReady();
    }

    private void StartCountdownIfReady()
    {
        if (!_runner.IsServer) return;
        var raceManager = RaceManager.Instance;
        if (raceManager == null) return;

        int expected = 0;
        foreach (var _ in _runner.ActivePlayers) expected++;

        if (_spawnedPlayers.Count >= expected && expected > 0)
            raceManager.RPC_StartRace();
    }

    private IEnumerator SpawnCarDelayed(PlayerRef player)
    {
        yield return new WaitForSeconds(0.6f);
        SpawnCar(player);
    }

    private void SpawnCar(PlayerRef player)
    {
        if (_spawnedPlayers.Contains(player)) return;

        if (_runner.TryGetPlayerObject(player, out _))
        {
            _spawnedPlayers.Add(player);
            return;
        }

        if (carPrefabList == null)
        {
            carPrefabList = FusionNetworkManager.Instance.carPrefabList;
            if (carPrefabList == null) { Debug.LogError("[RacingCarSpawner] carPrefabList null!"); return; }
        }

        int carIndex = FusionNetworkManager.Instance.GetPlayerCarChoice(player);
        if (carIndex < 0 || carIndex >= carPrefabList.carPrefabs.Length) carIndex = 0;

        NetworkObject prefab = carPrefabList.carPrefabs[carIndex];
        if (prefab == null) { Debug.LogError($"[RacingCarSpawner] Prefab null tại index {carIndex}!"); return; }

        int idx = (player.PlayerId - 1) % Mathf.Max(1, spawnPoints.Length);
        Vector3 pos = (spawnPoints.Length > idx && spawnPoints[idx] != null)
            ? spawnPoints[idx].position
            : new Vector3((idx - 1.5f) * 4f, -12f, 0);

        NetworkObject carObj = _runner.Spawn(prefab, pos, Quaternion.identity, player);
        _runner.SetPlayerObject(player, carObj);
        _spawnedPlayers.Add(player);

        Debug.Log($"[RacingCarSpawner] ✅ Spawned {prefab.name} cho Player {player} tại {pos}");

        var carController = carObj.GetComponent<CarController>();
        if (MultiCameraManager.Instance != null && carController != null)
            MultiCameraManager.Instance.RegisterPlayerCar(player, carController);
    }

    private void OnDestroy()
    {
        if (_runner != null) _runner.RemoveCallbacks(this);
    }
}