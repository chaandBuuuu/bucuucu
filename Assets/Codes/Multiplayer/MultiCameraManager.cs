using UnityEngine;
using Cinemachine;
using Fusion;
using System.Collections;

/// <summary>
/// ✅ MultiCameraManager – NetworkBehaviour
///
/// QUAN TRỌNG: Được SERVER SPAWN như NetworkObject (không đặt sẵn trong scene).
///
/// FIX: mapBoundsCollider được tìm bằng tag/name lúc runtime
///      → không bị null khi spawn từ prefab
///
/// Setup MapBounds trong scene:
///   - Tạo GameObject tên "MapBounds" (hoặc tag "MapBounds")
///   - Thêm BoxCollider2D bao quanh track, Is Trigger = true
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

    [Header("Map Bounds")]
    [Tooltip("Tên GameObject chứa Collider2D bao quanh map. Script tự tìm lúc runtime.")]
    [SerializeField] private string mapBoundsObjectName = "MapBounds";

    [Header("Overview Camera")]
    [SerializeField] private bool autoFitOverview = true;
    [SerializeField] private float overviewOrthoSize = 25f;
    [SerializeField] private Vector3 overviewPosition = Vector3.zero;

    private Camera _mainCamera;
    private CinemachineVirtualCamera _followVCam;
    private CinemachineVirtualCamera _overviewVCam;
    private Transform _pendingFollowTarget;
    private Collider2D _mapBoundsCollider;   // tìm lúc runtime

    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        // ✅ Tìm MapBounds trong scene lúc runtime (không dùng Inspector reference)
        FindMapBounds();

        DisableExternalCameras();
        SetupMainCamera();
        CreateFollowVCam();
        CreateOverviewVCam();

        StartCoroutine(AddConfinerNextFrame());

        Debug.Log($"[MultiCameraManager] ✅ Spawned – StateAuthority={Object.HasStateAuthority}, LocalPlayer={Runner.LocalPlayer}");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Instance = null;
        if (_followVCam != null)   Destroy(_followVCam.gameObject);
        if (_overviewVCam != null)  Destroy(_overviewVCam.gameObject);
    }

    private void Update()
    {
        if (Object == null || !Object.HasStateAuthority) return;
        if (Input.GetKeyDown(KeyCode.Return))
            ToggleMode();
    }

    // ─────────────────────────────────────────────────────────────────────
    #region Setup

    /// <summary>
    /// Tìm Collider2D của map theo tên GameObject.
    /// Prefab không thể giữ scene reference nên phải tìm lúc runtime.
    /// </summary>
    private void FindMapBounds()
    {
        // Tìm theo tên
        var boundsGO = GameObject.Find(mapBoundsObjectName);
        if (boundsGO != null)
        {
            _mapBoundsCollider = boundsGO.GetComponent<Collider2D>();
            if (_mapBoundsCollider != null)
            {
                Debug.Log($"[MultiCameraManager] ✅ Found MapBounds: {boundsGO.name}");
                return;
            }
        }

        // Fallback: tìm theo tag "MapBounds" nếu có
        var taggedGO = GameObject.FindGameObjectWithTag("MapBounds");
        if (taggedGO != null)
        {
            _mapBoundsCollider = taggedGO.GetComponent<Collider2D>();
            if (_mapBoundsCollider != null)
            {
                Debug.Log($"[MultiCameraManager] ✅ Found MapBounds by tag: {taggedGO.name}");
                return;
            }
        }

        Debug.LogWarning($"[MultiCameraManager] ⚠️ Không tìm thấy '{mapBoundsObjectName}' trong scene. Camera sẽ không bị giới hạn bounds.");
    }

    private void DisableExternalCameras()
    {
        var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
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
        // Tìm camera trong scene GamePlay
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

        if (_mainCamera == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            _mainCamera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            Debug.Log("[MultiCameraManager] Created new Main Camera");
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
        if (autoFitOverview && _mapBoundsCollider != null)
        {
            Bounds b     = _mapBoundsCollider.bounds;
            float aspect = (float)Screen.width / Screen.height;
            overviewOrthoSize = Mathf.Max(b.size.y / 2f, (b.size.x / 2f) / aspect) * 1.05f;
            overviewPosition  = new Vector3(b.center.x, b.center.y, followOffset.z);
            Debug.Log($"[MultiCameraManager] AutoFit overview: pos={overviewPosition}, ortho={overviewOrthoSize:F1}");
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
        // Chờ 1 frame để CinemachineVirtualCamera init xong
        yield return null;

        if (_mapBoundsCollider != null && _followVCam != null)
        {
            var confiner = _followVCam.gameObject.AddComponent<CinemachineConfiner2D>();
            confiner.m_BoundingShape2D = _mapBoundsCollider;
            confiner.m_Damping         = 0f;
            confiner.InvalidateCache();
            Debug.Log($"[MultiCameraManager] ✅ Confiner2D attached → {_mapBoundsCollider.name}");
        }
        else
        {
            Debug.LogWarning("[MultiCameraManager] ⚠️ Confiner2D skipped – mapBoundsCollider null");
        }

        // Set pending target nếu RegisterPlayerCar() đã gọi trước
        if (_pendingFollowTarget != null && _followVCam != null)
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