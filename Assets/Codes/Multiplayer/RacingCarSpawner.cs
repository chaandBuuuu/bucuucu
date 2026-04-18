using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ✅ UPDATED: Spawn xe cho từng player, sau đó gọi MultiCameraManager.RegisterPlayerCar()
/// 
/// Lưu ý quan trọng:
///   - SpawnCar() chỉ chạy trên SERVER (IsServer check)
///   - RegisterPlayerCar() được gọi sau spawn → MultiCameraManager tự lọc local player
///   - Không cần thay đổi logic spawn, chỉ đảm bảo gọi Register đúng chỗ
/// </summary>
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
        var mgr = FusionNetworkManager.Instance;
        if (mgr != null && mgr.Runner != null)
        {
            InitializeWithRunner(mgr.Runner);
        }
        else
        {
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

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _spawnedPlayers.Remove(player);

        var camManager = MultiCameraManager.Instance;
        if (camManager != null)
            camManager.OnPlayerLeft(player);
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

        if (_runner.TryGetPlayerObject(player, out _))
        {
            _spawnedPlayers.Add(player);
            Debug.Log($"[RacingCarSpawner] Player {player} đã có object rồi");
            return;
        }

        if (carPrefabList == null)
        {
            carPrefabList = FusionNetworkManager.Instance.carPrefabList;
            if (carPrefabList == null)
            {
                Debug.LogError("[RacingCarSpawner] carPrefabList không được gán!");
                return;
            }
        }

        int carIndex = FusionNetworkManager.Instance.GetPlayerCarChoice(player);

        if (carIndex < 0 || carIndex >= carPrefabList.carPrefabs.Length)
        {
            Debug.LogWarning($"[RacingCarSpawner] carIndex {carIndex} không hợp lệ → dùng index 0");
            carIndex = 0;
        }

        NetworkObject prefab = carPrefabList.carPrefabs[carIndex];
        if (prefab == null)
        {
            Debug.LogError($"[RacingCarSpawner] Prefab tại index {carIndex} là null!");
            return;
        }

        int idx = (player.PlayerId - 1) % Mathf.Max(1, spawnPoints.Length);
        Vector3 pos = (spawnPoints.Length > idx && spawnPoints[idx] != null)
            ? spawnPoints[idx].position
            : new Vector3((idx - 1.5f) * 4f, -12f, 0);

        Debug.Log($"[RacingCarSpawner] Spawning car for Player {player} at {pos}");

        NetworkObject carObj = _runner.Spawn(prefab, pos, Quaternion.identity, player);
        _runner.SetPlayerObject(player, carObj);
        _spawnedPlayers.Add(player);

        Debug.Log($"[RacingCarSpawner] ✅ Spawn thành công {prefab.name} cho Player {player}, HasInputAuthority={carObj.HasInputAuthority}");

        // ✅ Register camera – MultiCameraManager tự lọc chỉ local player mới tạo vcam
        var camManager = MultiCameraManager.Instance;
        var carController = carObj.GetComponent<CarController>();
        if (camManager != null && carController != null)
        {
            camManager.RegisterPlayerCar(player, carController);
        }
    }

    private void OnDestroy()
    {
        if (_runner != null)
            _runner.RemoveCallbacks(this);
    }
}