using UnityEngine;
using Cinemachine;
using Fusion;
using System.Collections;

/// <summary>
/// ✅ MultiCameraManager – NetworkBehaviour
///
/// FIX CHÍNH:
///   - NetworkObject spawn bởi Fusion nằm trong DontDestroyOnLoad
///   - KHÔNG dùng gameObject.scene để so sánh (sẽ sai)
///   - Disable camera theo tên scene: chỉ disable camera thuộc scene "Menu"
///   - Tìm Main Camera theo tag "MainCamera" trong scene GamePlay
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
    [Tooltip("Tên GameObject chứa Collider2D bao quanh map trong scene GamePlay.")]
    [SerializeField] private string mapBoundsObjectName = "MapBounds";

    [Header("Overview Camera")]
    [SerializeField] private bool autoFitOverview = true;
    [SerializeField] private float overviewOrthoSize = 25f;
    [SerializeField] private Vector3 overviewPosition = Vector3.zero;

    private Camera _mainCamera;
    private CinemachineVirtualCamera _followVCam;
    private CinemachineVirtualCamera _overviewVCam;
    private Transform _pendingFollowTarget;
    private Collider2D _mapBoundsCollider;

    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        FindMapBounds();
        DisableMenuCamera();       // ✅ Chỉ disable camera của Menu scene
        SetupMainCamera();         // ✅ Tìm/tạo Main Camera trong GamePlay scene
        CreateFollowVCam();
        CreateOverviewVCam();
        StartCoroutine(AddConfinerNextFrame());
        StartCoroutine(AutoRegisterLocalCar());   // ✅ Tự tìm xe local nếu chưa được register

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

    private void FindMapBounds()
    {
        var go = GameObject.Find(mapBoundsObjectName);
        if (go != null)
        {
            _mapBoundsCollider = go.GetComponent<Collider2D>();
            if (_mapBoundsCollider != null)
            {
                Debug.Log($"[MultiCameraManager] ✅ Found MapBounds: {go.name} in scene '{go.scene.name}'");
                return;
            }
        }
        Debug.LogWarning($"[MultiCameraManager] ⚠️ Không tìm thấy '{mapBoundsObjectName}'");
    }

    /// <summary>
    /// Chỉ disable camera thuộc scene "Menu" (hoặc scene không phải GamePlay).
    /// KHÔNG disable camera trong GamePlay scene.
    /// </summary>
    private void DisableMenuCamera()
    {
        var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            string sceneName = cam.gameObject.scene.name;
            // Disable nếu camera nằm trong scene Menu hoặc DontDestroyOnLoad
            // Giữ lại camera trong scene GamePlay
            if (sceneName == "Menu" || sceneName == "DontDestroyOnLoad")
            {
                cam.enabled = false;
                var al = cam.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;
                Debug.Log($"[MultiCameraManager] Disabled camera '{cam.name}' in scene '{sceneName}'");
            }
        }
    }

    private void SetupMainCamera()
    {
        // Tìm Main Camera trong scene GamePlay (tag = MainCamera)
        _mainCamera = null;

        var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            // Camera trong GamePlay scene (không phải Menu, không phải DontDestroyOnLoad)
            string sn = cam.gameObject.scene.name;
            if (sn != "Menu" && sn != "DontDestroyOnLoad" && cam.enabled)
            {
                _mainCamera = cam;
                Debug.Log($"[MultiCameraManager] Using camera '{cam.name}' in scene '{sn}'");
                break;
            }
        }

        // Nếu vẫn không tìm được → tạo mới
        if (_mainCamera == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            _mainCamera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            Debug.Log("[MultiCameraManager] Created new Main Camera");
        }

        _mainCamera.tag          = "MainCamera";
        _mainCamera.orthographic = true;
        _mainCamera.rect         = new Rect(0, 0, 1, 1);

        // Đảm bảo chỉ có 1 AudioListener
        var allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (var al in allListeners)
            if (al.gameObject != _mainCamera.gameObject)
                al.enabled = false;

        if (_mainCamera.GetComponent<CinemachineBrain>() == null)
            _mainCamera.gameObject.AddComponent<CinemachineBrain>();

        Debug.Log($"[MultiCameraManager] ✅ Main Camera ready: '{_mainCamera.name}'");
    }

    private void CreateFollowVCam()
    {
        var go = new GameObject("VCam_Follow");
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
        }

        var go = new GameObject("VCam_Overview");
        go.transform.position = overviewPosition;

        _overviewVCam = go.AddComponent<CinemachineVirtualCamera>();
        _overviewVCam.m_Lens.Orthographic     = true;
        _overviewVCam.m_Lens.OrthographicSize = overviewOrthoSize;
        _overviewVCam.m_Priority              = 5;
        _overviewVCam.Follow                  = null;
    }

    private IEnumerator AddConfinerNextFrame()
    {
        yield return null;

        if (_mapBoundsCollider != null && _followVCam != null)
        {
            var confiner = _followVCam.gameObject.AddComponent<CinemachineConfiner2D>();
            confiner.m_BoundingShape2D = _mapBoundsCollider;
            confiner.m_Damping         = 0f;
            confiner.InvalidateCache();
            Debug.Log($"[MultiCameraManager] ✅ Confiner2D attached → {_mapBoundsCollider.name}");
        }

        if (_pendingFollowTarget != null && _followVCam != null)
        {
            _followVCam.Follow   = _pendingFollowTarget;
            _pendingFollowTarget = null;
            Debug.Log($"[MultiCameraManager] ✅ Applied pending follow target");
        }

        ApplyCameraMode(CurrentMode);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Mode Switch

    private void ToggleMode()
    {
        CurrentMode = CurrentMode == CameraMode.Follow ? CameraMode.Overview : CameraMode.Follow;
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
    #region Auto Register

    /// <summary>
    /// Tự động tìm xe của local player và register nếu chưa được register.
    /// Fallback cho client.
    /// </summary>
    private IEnumerator AutoRegisterLocalCar()
    {
        float timeout = 10f, elapsed = 0f;

        while (elapsed < timeout)
        {
            if (_followVCam != null && _followVCam.Follow != null)
                yield break;

            var allCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (var car in allCars)
            {
                if (car.HasInputAuthority)
                {
                    RegisterPlayerCar(Runner.LocalPlayer, car);
                    yield break;
                }
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        Debug.LogWarning("[MultiCameraManager] ⚠️ AutoRegister timeout");
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