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

    [Header("Network Objects to Spawn")]
    [Tooltip("Prefab MultiCameraManager (NetworkObject)")]
    [SerializeField] private NetworkObject cameraManagerPrefab;
    [Tooltip("Prefab ChatNetworkHandler (NetworkObject)")]
    [SerializeField] private NetworkObject chatHandlerPrefab;
    [Tooltip("Prefab GameEndVoteHandler (NetworkObject)")]
    [SerializeField] private NetworkObject voteHandlerPrefab;

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
            { InitializeWithRunner(mgr.Runner); yield break; }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.LogError("[RacingCarSpawner] Timeout!");
    }

    public void InitializeWithRunner(NetworkRunner runner)
    {
        if (_registered) return;
        _runner = runner;
        _runner.AddCallbacks(this);
        _registered = true;
        Debug.Log($"[RacingCarSpawner] ✅ Registered. IsServer={_runner.IsServer}");

        if (_runner.IsServer && _runner.IsRunning)
            StartCoroutine(SpawnNetworkObjectsThenCars());
    }

    private void OnEnable() { _registered = false; _spawnedPlayers.Clear(); }

    public override void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;
        StartCoroutine(SpawnNetworkObjectsThenCars());
    }

    public override void OnSceneLoadStart(NetworkRunner runner) => _spawnedPlayers.Clear();

    private IEnumerator SpawnNetworkObjectsThenCars()
    {
        yield return SpawnIfNeeded(cameraManagerPrefab, "MultiCameraManager", () => MultiCameraManager.Instance == null);
        yield return SpawnIfNeeded(chatHandlerPrefab,   "ChatNetworkHandler",  () => ChatNetworkHandler.Instance  == null);
        yield return SpawnIfNeeded(voteHandlerPrefab,   "GameEndVoteHandler",  () => GameEndVoteHandler.Instance  == null);
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(SpawnAllPlayersDelayed());
    }

    private IEnumerator SpawnIfNeeded(NetworkObject prefab, string label, System.Func<bool> check)
    {
        if (prefab == null) { Debug.LogWarning($"[RacingCarSpawner] ⚠️ {label} prefab chưa gán"); yield break; }
        if (!check()) yield break;

        bool failed = false;
        try { _runner.Spawn(prefab, Vector3.zero, Quaternion.identity); }
        catch (System.Exception e) { Debug.LogWarning($"[RacingCarSpawner] {label} retry: {e.Message}"); failed = true; }

        if (failed) { yield return null; _runner.Spawn(prefab, Vector3.zero, Quaternion.identity); }

        float t = 0f;
        while (check() && t < 3f) { t += Time.deltaTime; yield return null; }
        Debug.Log($"[RacingCarSpawner] ✅ {label} ready");
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
        foreach (PlayerRef p in _runner.ActivePlayers) SpawnCar(p);
        yield return new WaitForSeconds(2f);
        StartCountdownIfReady();
    }

    private void StartCountdownIfReady()
    {
        if (!_runner.IsServer) return;
        var rm = RaceManager.Instance;
        if (rm == null) return;
        int expected = 0;
        foreach (var _ in _runner.ActivePlayers) expected++;
        if (_spawnedPlayers.Count >= expected && expected > 0) rm.RPC_StartRace();
    }

    private IEnumerator SpawnCarDelayed(PlayerRef player)
    {
        yield return new WaitForSeconds(0.6f);
        SpawnCar(player);
    }

    private void SpawnCar(PlayerRef player)
    {
        if (_spawnedPlayers.Contains(player)) return;
        if (_runner.TryGetPlayerObject(player, out _)) { _spawnedPlayers.Add(player); return; }

        if (carPrefabList == null)
        {
            carPrefabList = FusionNetworkManager.Instance.carPrefabList;
            if (carPrefabList == null) { Debug.LogError("[RacingCarSpawner] carPrefabList null!"); return; }
        }

        int cidx = FusionNetworkManager.Instance.GetPlayerCarChoice(player);
        if (cidx < 0 || cidx >= carPrefabList.carPrefabs.Length) cidx = 0;

        NetworkObject prefab = carPrefabList.carPrefabs[cidx];
        if (prefab == null) { Debug.LogError($"[RacingCarSpawner] Prefab null idx={cidx}!"); return; }

        int si  = (player.PlayerId - 1) % Mathf.Max(1, spawnPoints.Length);
        Vector3 pos = (spawnPoints.Length > si && spawnPoints[si] != null)
            ? spawnPoints[si].position
            : new Vector3((si - 1.5f) * 4f, -12f, 0);

        NetworkObject car = _runner.Spawn(prefab, pos, Quaternion.identity, player);
        _runner.SetPlayerObject(player, car);
        _spawnedPlayers.Add(player);
        Debug.Log($"[RacingCarSpawner] ✅ Spawned {prefab.name} → Player {player}");

        var ctrl = car.GetComponent<CarController>();
        if (MultiCameraManager.Instance != null && ctrl != null)
            MultiCameraManager.Instance.RegisterPlayerCar(player, ctrl);
    }

    private void OnDestroy() { if (_runner != null) _runner.RemoveCallbacks(this); }
}