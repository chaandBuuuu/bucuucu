using UnityEngine;
using Cinemachine;
using Fusion;
using System.Collections;

/// <summary>
/// ✅ MultiCameraManager – NetworkBehaviour
///
/// QUAN TRỌNG: Object này phải được SERVER SPAWN (không đặt sẵn trong scene).
/// Thêm prefab này vào RacingCarSpawner và spawn trong OnSceneLoadDone.
///
///   [Follow Mode]   : VCam follow xe local player, giới hạn trong mapBoundsCollider
///   [Overview Mode] : VCam cố định nhìn toàn map
///
/// HOST: Enter để toggle → sync tất cả clients qua [Networked]
/// </summary>
public class MultiCameraManager : NetworkBehaviour
{
    public static MultiCameraManager Instance { get; private set; }

    public enum CameraMode { Follow, Overview }

    [Networked, OnChangedRender(nameof(OnCameraModeChanged))]
    public CameraMode CurrentMode { get; set; } = CameraMode.Follow;

    [Header("Follow Camera")]
    [SerializeField] private float followOrthoSize = 10f;
    [SerializeField] private float dampingXY = 0.5f;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -10f);

    [Header("Map Bounds (Confiner2D)")]
    [Tooltip("BoxCollider2D hoặc PolygonCollider2D bao quanh track. Is Trigger = true.")]
    [SerializeField] private Collider2D mapBoundsCollider;

    [Header("Overview Camera")]
    [SerializeField] private bool autoFitOverview = true;
    [SerializeField] private float overviewOrthoSize = 25f;
    [SerializeField] private Vector3 overviewPosition = Vector3.zero;

    private Camera _mainCamera;
    private CinemachineVirtualCamera _followVCam;
    private CinemachineVirtualCamera _overviewVCam;
    private Transform _pendingFollowTarget;

    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Spawned() chạy trên TẤT CẢ clients sau khi server spawn object này
    /// </summary>
    public override void Spawned()
    {
        // Disable tất cả camera ngoài scene GamePlay (Menu camera, v.v.)
        DisableExternalCameras();

        // Setup Main Camera trong scene GamePlay
        SetupMainCamera();

        // Tạo VCam
        CreateFollowVCam();
        CreateOverviewVCam();

        // Chờ 1 frame rồi add Confiner2D
        StartCoroutine(AddConfinerNextFrame());

        Debug.Log($"[MultiCameraManager] ✅ Spawned – StateAuthority={Object.HasStateAuthority}, LocalPlayer={Runner.LocalPlayer}");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Instance = null;
        if (_followVCam != null)  Destroy(_followVCam.gameObject);
        if (_overviewVCam != null) Destroy(_overviewVCam.gameObject);
    }

    private void Update()
    {
        if (Object == null || !Object.HasStateAuthority) return;
        if (Input.GetKeyDown(KeyCode.Return))
            ToggleMode();
    }

    // ─────────────────────────────────────────────────────────────────────
    #region Setup

    private void DisableExternalCameras()
    {
        var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            // Chỉ giữ camera thuộc scene hiện tại (GamePlay scene)
            if (cam.gameObject.scene != gameObject.scene)
            {
                cam.enabled = false;
                var al = cam.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;
                Debug.Log($"[MultiCameraManager] Disabled external camera: {cam.name}");
            }
        }
    }

    private void SetupMainCamera()
    {
        // Tìm Main Camera trong scene GamePlay
        _mainCamera = null;
        var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            if (cam.gameObject.scene == gameObject.scene)
            {
                _mainCamera = cam;
                break;
            }
        }

        // Nếu không có thì tạo mới
        if (_mainCamera == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            _mainCamera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        _mainCamera.tag          = "MainCamera";
        _mainCamera.orthographic = true;
        _mainCamera.rect         = new Rect(0, 0, 1, 1);

        // Disable AudioListener trùng
        var allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (var al in allListeners)
            if (al.gameObject != _mainCamera.gameObject) al.enabled = false;

        if (_mainCamera.GetComponent<CinemachineBrain>() == null)
            _mainCamera.gameObject.AddComponent<CinemachineBrain>();
    }

    private void CreateFollowVCam()
    {
        var go = new GameObject("VCam_Follow");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);

        _followVCam = go.AddComponent<CinemachineVirtualCamera>();
        _followVCam.m_Lens.Orthographic     = true;
        _followVCam.m_Lens.OrthographicSize = followOrthoSize;
        _followVCam.m_Priority              = 10;

        var transposer = _followVCam.AddCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = followOffset;
        transposer.m_XDamping     = dampingXY;
        transposer.m_YDamping     = dampingXY;
        transposer.m_ZDamping     = 0f;
        transposer.m_BindingMode  = CinemachineTransposer.BindingMode.WorldSpace;

        _followVCam.Follow = null;
    }

    private void CreateOverviewVCam()
    {
        if (autoFitOverview && mapBoundsCollider != null)
        {
            Bounds b     = mapBoundsCollider.bounds;
            float aspect = (float)Screen.width / Screen.height;
            overviewOrthoSize = Mathf.Max(b.size.y / 2f, (b.size.x / 2f) / aspect) * 1.05f;
            overviewPosition  = new Vector3(b.center.x, b.center.y, followOffset.z);
        }

        var go = new GameObject("VCam_Overview");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        go.transform.position = overviewPosition;

        _overviewVCam = go.AddComponent<CinemachineVirtualCamera>();
        _overviewVCam.m_Lens.Orthographic     = true;
        _overviewVCam.m_Lens.OrthographicSize = overviewOrthoSize;
        _overviewVCam.m_Priority              = 5;
        _overviewVCam.Follow                  = null;
    }

    private IEnumerator AddConfinerNextFrame()
    {
        yield return null; // chờ VCam init xong

        if (mapBoundsCollider != null && _followVCam != null)
        {
            var confiner = _followVCam.gameObject.AddComponent<CinemachineConfiner2D>();
            confiner.m_BoundingShape2D = mapBoundsCollider;
            confiner.m_Damping         = 0f;
            confiner.InvalidateCache();
            Debug.Log($"[MultiCameraManager] ✅ Confiner2D attached");
        }

        // Set pending target
        if (_pendingFollowTarget != null)
        {
            _followVCam.Follow   = _pendingFollowTarget;
            _pendingFollowTarget = null;
        }

        ApplyCameraMode(CurrentMode);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Mode Switch

    private void ToggleMode()
    {
        CurrentMode = CurrentMode == CameraMode.Follow
            ? CameraMode.Overview
            : CameraMode.Follow;
        Debug.Log($"[MultiCameraManager] 📹 Toggled → {CurrentMode}");
    }

    private void OnCameraModeChanged()
    {
        ApplyCameraMode(CurrentMode);
        Debug.Log($"[MultiCameraManager] 🔄 Synced → {CurrentMode}");
    }

    private void ApplyCameraMode(CameraMode mode)
    {
        if (_followVCam == null || _overviewVCam == null) return;
        _followVCam.m_Priority   = mode == CameraMode.Follow ? 10 : 5;
        _overviewVCam.m_Priority = mode == CameraMode.Follow ? 5  : 10;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Public API

    public void RegisterPlayerCar(PlayerRef player, CarController car)
    {
        if (Runner == null || car == null) return;

        if (player != Runner.LocalPlayer)
        {
            Debug.Log($"[MultiCameraManager] Skip remote Player {player.PlayerId}");
            return;
        }

        if (_followVCam != null)
        {
            _followVCam.Follow = car.transform;
            Debug.Log($"[MultiCameraManager] ✅ Follow → {car.name} (Player {player.PlayerId})");
        }
        else
        {
            _pendingFollowTarget = car.transform;
            Debug.Log($"[MultiCameraManager] ⏳ Buffered → {car.name}");
        }
    }

    public void OnPlayerLeft(PlayerRef player) { }

    #endregion
}