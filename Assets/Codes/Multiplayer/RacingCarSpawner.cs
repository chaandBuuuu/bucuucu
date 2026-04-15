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

    private void Start()
    {
        // Thử lấy runner ngay khi scene load xong
        var mgr = FusionNetworkManager.Instance;
        if (mgr != null && mgr.Runner != null)
        {
            InitializeWithRunner(mgr.Runner);
        }
        else
        {
            // Runner chưa sẵn sàng → chờ
            StartCoroutine(WaitForRunnerAndInit());
        }
    }

    private IEnumerator WaitForRunnerAndInit()
    {
        Debug.Log("[RacingCarSpawner] Waiting for Runner...");

        float timeout = 10f;
        float elapsed = 0f;

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

        Debug.LogError("[RacingCarSpawner] Timeout: Runner không sẵn sàng sau 10 giây!");
    }

    public void InitializeWithRunner(NetworkRunner runner)
    {
        if (_registered) return;

        _runner = runner;
        _runner.AddCallbacks(this);
        _registered = true;

        Debug.Log($"[RacingCarSpawner] ✅ Đã đăng ký callbacks với Runner. IsServer={_runner.IsServer}");

        // Nếu đã là server và runner đang chạy → spawn các player hiện có ngay
        // (vì OnSceneLoadDone đã miss trước khi spawner tồn tại)
        if (_runner.IsServer && _runner.IsRunning)
        {
            Debug.Log("[RacingCarSpawner] Server đang chạy → spawn players hiện có");
            StartCoroutine(SpawnAllPlayersDelayed());
        }
    }

    private void OnEnable()
    {
        _registered = false;
        _spawnedPlayers.Clear();
    }

    // Callback: scene load xong (chỉ fire nếu spawner đã được đăng ký trước khi scene load)
    public override void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        Debug.Log("[RacingCarSpawner] OnSceneLoadDone → spawning cars");
        StartCoroutine(SpawnAllPlayersDelayed());
    }

    public override void OnSceneLoadStart(NetworkRunner runner)
    {
        _spawnedPlayers.Clear();
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        Debug.Log($"[RacingCarSpawner] Player {player} joined → Spawn xe");
        StartCoroutine(SpawnCarDelayed(player));
    }

    private IEnumerator SpawnAllPlayersDelayed()
    {
        yield return new WaitForSeconds(1.5f);

        while (_runner == null || _runner.SceneManager == null || !_runner.IsRunning)
            yield return null;

        Debug.Log("[RacingCarSpawner] Spawning all active players...");

        foreach (PlayerRef player in _runner.ActivePlayers)
        {
            SpawnCar(player);
        }
    }

    private IEnumerator SpawnCarDelayed(PlayerRef player)
    {
        yield return new WaitForSeconds(0.6f);
        SpawnCar(player);
    }

    private void SpawnCar(PlayerRef player)
    {
        if (_spawnedPlayers.Contains(player)) return;

        // Kiểm tra đã có object chưa
        if (_runner.TryGetPlayerObject(player, out _))
        {
            _spawnedPlayers.Add(player);
            Debug.Log($"[RacingCarSpawner] Player {player} đã có object rồi");
            return;
        }

        // Đảm bảo carPrefabList có sẵn
        if (carPrefabList == null)
        {
            carPrefabList = FusionNetworkManager.Instance.carPrefabList;
            if (carPrefabList == null)
            {
                Debug.LogError("[RacingCarSpawner] carPrefabList không được gán! Hãy kiểm tra FusionNetworkManager");
                return;
            }
        }

        int carIndex = FusionNetworkManager.Instance.GetPlayerCarChoice(player);

        // Validate car index
        if (carIndex < 0 || carIndex >= carPrefabList.carPrefabs.Length)
        {
            Debug.LogWarning($"[RacingCarSpawner] carIndex {carIndex} không hợp lệ (có {carPrefabList.carPrefabs.Length} xe). Dùng index 0");
            carIndex = 0;
        }

        NetworkObject prefab = carPrefabList.carPrefabs[carIndex];

        if (prefab == null)
        {
            Debug.LogError($"[RacingCarSpawner] Prefab tại index {carIndex} là null! Hãy kiểm tra CarPrefabList.asset");
            return;
        }

        int idx = (player.PlayerId - 1) % Mathf.Max(1, spawnPoints.Length);
        Vector3 pos = (spawnPoints.Length > idx && spawnPoints[idx] != null)
            ? spawnPoints[idx].position
            : new Vector3((idx - 1.5f) * 4f, -12f, 0);

        Debug.Log($"[RacingCarSpawner] Spawning car for Player {player} (LocalPlayer={_runner.LocalPlayer}) at {pos}");

        NetworkObject carObj = _runner.Spawn(prefab, pos, Quaternion.identity, player);
        _runner.SetPlayerObject(player, carObj);
        _spawnedPlayers.Add(player);

        Debug.Log($"[RacingCarSpawner] ✅ Spawn thành công {prefab.name} cho Player {player} tại {pos}, HasInputAuthority={carObj.HasInputAuthority}");

        // Register camera
        var camManager = MultiCameraManager.Instance;
        if (camManager != null && carObj.GetComponent<CarController>() != null)
        {
            camManager.RegisterPlayerCar(player, carObj.GetComponent<CarController>());
            Debug.Log($"[RacingCarSpawner] Registered camera for Player {player}");
        }
    }

    private void OnDestroy()
    {
        if (_runner != null)
            _runner.RemoveCallbacks(this);
    }
}