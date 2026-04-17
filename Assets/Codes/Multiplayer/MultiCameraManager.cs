using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ✅ UPDATED: Multi-camera system cho split-screen racing + single camera toggle
/// - Hỗ trợ 1-4 players với dynamic viewport sizing
/// - 2x2 grid layout cho split-screen
/// - HOST: Enter key để toggle giữa single camera (host only) vs split-screen (tất cả players)
/// - PARTICIPANTS: Luôn để thấy camera của họ (thay vì không thấy gì)
/// </summary>
public class MultiCameraManager : MonoBehaviour
{
    public static MultiCameraManager Instance { get; private set; }

    [Header("Camera Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, -10);
    [SerializeField] private float orthoSize = 10f;

    [Header("Host Single Camera")]
    [SerializeField] private Vector3 singleCameraOffset = new Vector3(0, 0, -10);
    [SerializeField] private float singleCameraOrthoSize = 15f;

    // ── Camera Modes ─────────────────────────────────────────────────────
    private enum CameraMode
    {
        SingleCamera,    // Chỉ camera của host
        SplitScreen      // Tất cả players
    }

    private CameraMode _currentMode = CameraMode.SplitScreen;
    private Dictionary<PlayerRef, Camera> _playerCameras = new Dictionary<PlayerRef, Camera>();
    private Dictionary<PlayerRef, CarController> _playerCars = new Dictionary<PlayerRef, CarController>();
    private NetworkRunner _runner;
    private bool _isHost = false;
    // ✅ Cache viewport rects để tránh allocation lặp lại
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

        _isHost = _runner.IsServer;
        Debug.Log($"[MultiCameraManager] IsHost={_isHost}");

        // Khởi tạo camera cho tất cả active players
        InitializeCameras();
    }

    private void Update()
    {
        // ✅ HOST ONLY: Enter key để toggle camera mode
        if (_isHost && Input.GetKeyDown(KeyCode.Return))
        {
            ToggleCameraMode();
        }
    }

    /// <summary>
    /// Toggle camera mode: SingleCamera ↔ SplitScreen (HOST ONLY)
    /// </summary>
    private void ToggleCameraMode()
    {
        _currentMode = _currentMode == CameraMode.SingleCamera ? CameraMode.SplitScreen : CameraMode.SingleCamera;
        
        Debug.Log($"[MultiCameraManager] 📹 Camera Mode: {_currentMode}");

        // Clear existing cameras
        foreach (var cam in _playerCameras.Values)
        {
            if (cam != null) Destroy(cam.gameObject);
        }
        _playerCameras.Clear();

        // Reinitialize cameras with new mode
        InitializeCameras();
    }

    private void InitializeCameras()
    {
        if (_runner == null || !_runner.IsRunning) return;

        var activePlayers = _runner.ActivePlayers.ToList();
        int playerCount = activePlayers.Count;
        Debug.Log($"[MultiCameraManager] Initializing cameras cho {playerCount} players (Mode: {_currentMode})");

        if (_currentMode == CameraMode.SingleCamera && _isHost)
        {
            // Host only camera (fixed top-down)
            CreateSingleCamera(activePlayers[0]);
        }
        else
        {
            // Split-screen for all players
            int cameraIndex = 0;
            foreach (PlayerRef player in activePlayers)
            {
                CreateCameraForPlayer(player, cameraIndex, playerCount);
                cameraIndex++;
            }
        }
    }

    /// <summary>
    /// Create single camera (host only, centered on host)
    /// </summary>
    private void CreateSingleCamera(PlayerRef player)
    {
        if (_playerCameras.TryGetValue(player, out var oldCam) && oldCam != null)
            Destroy(oldCam.gameObject);

        GameObject camObj = new GameObject($"Camera_SingleMode_Host");
        Camera cam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();
        CameraFollowTarget follower = camObj.AddComponent<CameraFollowTarget>();

        cam.orthographic = true;
        cam.orthographicSize = singleCameraOrthoSize;
        cam.rect = new Rect(0, 0, 1, 1);  // Full screen

        _playerCameras[player] = cam;
        Debug.Log($"[MultiCameraManager] Created single camera for host at full screen");

        // Make camera follow the host's car
        if (_playerCars.TryGetValue(player, out var car) && car != null)
        {
            follower.SetTarget(car.transform);
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
        camObj.AddComponent<AudioListener>();  // ✅ IMPORTANT: AudioListener for sound
        CameraFollowTarget follower = camObj.AddComponent<CameraFollowTarget>();

        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.depth = index;  // Z-depth để render order đúng

        // ✅ Set viewport dựa trên số players
        Rect viewport = GetViewportRect(index, totalPlayers);
        cam.rect = viewport;

        _playerCameras[player] = cam;
        
        // ✅ Log whether this is local player
        bool isLocalPlayer = (_runner.LocalPlayer == player);
        Debug.Log($"[MultiCameraManager] Created camera for Player {player.PlayerId} (Local: {isLocalPlayer}) at viewport {viewport}");
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

        // ✅ Re-initialize all cameras to adjust for new player
        InitializeCameras();
        
        Debug.Log($"[MultiCameraManager] Player joined, re-initialized cameras. Total: {playerCount}");
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
        
        Debug.Log($"[MultiCameraManager] Player left, re-initialized cameras. Total: {playerCount}");
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
