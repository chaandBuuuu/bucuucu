using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ✅ Multi-camera system cho split-screen racing
/// - Hỗ trợ 1-4 players với dynamic viewport sizing
/// - 2x2 grid layout cho split-screen
/// - Mỗi camera theo dõi player riêng biệt
/// </summary>
public class MultiCameraManager : MonoBehaviour
{
    public static MultiCameraManager Instance { get; private set; }

    [Header("Camera Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, -10);
    [SerializeField] private float orthoSize = 10f;

    private Dictionary<PlayerRef, Camera> _playerCameras = new Dictionary<PlayerRef, Camera>();
    private Dictionary<PlayerRef, CarController> _playerCars = new Dictionary<PlayerRef, CarController>();
    private NetworkRunner _runner;
    // ✅ OPTIMIZE: Cache viewport rects to avoid repeated Rect allocations
    private Dictionary<int, Rect> _viewportRectCache = new Dictionary<int, Rect>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _runner = FindAnyObjectByType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.LogError("[MultiCameraManager] Không tìm thấy NetworkRunner");
            return;
        }

        // Khởi tạo camera cho tất cả active players
        InitializeCameras();
    }

    private void InitializeCameras()
    {
        if (_runner == null || !_runner.IsRunning) return;

        var activePlayers = _runner.ActivePlayers.ToList();
        int playerCount = activePlayers.Count;
        Debug.Log($"[MultiCameraManager] Initializing cameras cho {playerCount} players");

        int cameraIndex = 0;
        foreach (PlayerRef player in activePlayers)
        {
            CreateCameraForPlayer(player, cameraIndex, playerCount);
            cameraIndex++;
        }
    }

    private void CreateCameraForPlayer(PlayerRef player, int index, int totalPlayers)
    {
        // Xóa camera cũ nếu tồn tại
        if (_playerCameras.TryGetValue(player, out var oldCam) && oldCam != null)
        {
            Destroy(oldCam.gameObject);
        }

        // Tạo camera GameObject mới
        GameObject camObj = new GameObject($"Camera_Player{player.PlayerId}");
        Camera cam = camObj.AddComponent<Camera>();
        AudioListener audioListener = camObj.AddComponent<AudioListener>();
        CameraFollowTarget follower = camObj.AddComponent<CameraFollowTarget>();

        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.depth = index;  // Z-depth để render order đúng

        // ✅ Set viewport dựa trên số players
        Rect viewport = GetViewportRect(index, totalPlayers);
        cam.rect = viewport;

        _playerCameras[player] = cam;
        Debug.Log($"[MultiCameraManager] Created camera for Player {player.PlayerId} at viewport {viewport}");
    }

    private Rect GetViewportRect(int playerIndex, int totalPlayers)
    {
        // ✅ OPTIMIZE: Cache calculated viewports
        int cacheKey = playerIndex * 100 + totalPlayers;
        if (_viewportRectCache.TryGetValue(cacheKey, out Rect cached))
            return cached;

        Rect viewport = totalPlayers switch
        {
            1 => new Rect(0, 0, 1, 1),  // Full screen

            2 => playerIndex == 0
                ? new Rect(0, 0.5f, 1, 0.5f)   // Top
                : new Rect(0, 0, 1, 0.5f),     // Bottom

            3 => playerIndex == 0
                ? new Rect(0, 0.5f, 1, 0.5f)      // Top full
                : playerIndex == 1
                    ? new Rect(0, 0, 0.5f, 0.5f)      // Bottom left
                    : new Rect(0.5f, 0, 0.5f, 0.5f),   // Bottom right

            4 => // 2x2 grid
                new Rect((playerIndex % 2) * 0.5f, (1 - playerIndex / 2 - 1) * 0.5f, 0.5f, 0.5f),

            _ => new Rect(0, 0, 1, 1)
        };

        _viewportRectCache[cacheKey] = viewport;
        return viewport;
    }

    public void RegisterPlayerCar(PlayerRef player, CarController car)
    {
        _playerCars[player] = car;

        if (_playerCameras.TryGetValue(player, out var cam) && cam != null)
        {
            var follower = cam.GetComponent<CameraFollowTarget>();
            if (follower != null)
                follower.SetTarget(car.transform);
            Debug.Log($"[MultiCameraManager] Registered car for Player {player.PlayerId}");
        }
    }

    public Camera GetPlayerCamera(PlayerRef player)
    {
        return _playerCameras.TryGetValue(player, out var cam) ? cam : null;
    }

    public void OnPlayerJoined(PlayerRef player)
    {
        var activePlayers = _runner.ActivePlayers.ToList();
        int playerCount = activePlayers.Count;
        int index = activePlayers.IndexOf(player);

        if (index >= 0)
        {
            CreateCameraForPlayer(player, index, playerCount);
        }
    }

    public void OnPlayerLeft(PlayerRef player)
    {
        if (_playerCameras.TryGetValue(player, out var cam) && cam != null)
        {
            Destroy(cam.gameObject);
        }
        _playerCameras.Remove(player);
        _playerCars.Remove(player);

        // Re-initialize all cameras với số player mới
        var activePlayers = _runner.ActivePlayers.ToList();
        int playerCount = activePlayers.Count;
        if (playerCount > 0)
        {
            InitializeCameras();
        }
    }
}

/// <summary>
/// Component attach vào camera để smooth follow target
/// </summary>
public class CameraFollowTarget : MonoBehaviour
{
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    private Transform _target;

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 desired = _target.position + offset;
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        transform.position = smoothed;
    }
}
