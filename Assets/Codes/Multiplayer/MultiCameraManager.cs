using UnityEngine;
using Cinemachine;
using Fusion;
using System.Collections;

/// <summary>
/// ✅ Camera system – Cinemachine 2.x (2.10.7) + Photon Fusion
///
///   [Follow Mode]   : CinemachineVirtualCamera follow xe local player
///                     + CinemachineConfiner2D giới hạn trong Collider2D của map
///   [Overview Mode] : CinemachineVirtualCamera cố định nhìn toàn bộ map
///
/// HOST: nhấn Enter → toggle mode → sync tất cả clients qua [Networked]
///
/// FIX: CinemachineConfiner2D phải được AddComponent SAU 1 frame
///      để CinemachineVirtualCamera kịp khởi tạo xong → dùng coroutine
///
/// Setup:
///   1. Tạo GameObject "MapBounds" → BoxCollider2D bao quanh track → Is Trigger = true
///   2. Gắn script này vào NetworkObject trong scene
///   3. Xóa CameraFollow.cs cũ khỏi Main Camera
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
    private Transform _pendingFollowTarget;   // buffer nếu RegisterPlayerCar() gọi trước vcam sẵn sàng

    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        SetupMainCamera();
        StartCoroutine(InitVCamsNextFrame());
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
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
    #region Init (coroutine – chờ 1 frame cho VCam khởi tạo xong)

    private IEnumerator InitVCamsNextFrame()
    {
        // Bước 1: Tạo VCam (chưa add Confiner)
        CreateFollowVCam();
        CreateOverviewVCam();

        // Bước 2: Chờ 1 frame để Cinemachine khởi tạo VCam xong
        yield return null;

        // Bước 3: Bây giờ mới add Confiner2D
        if (mapBoundsCollider != null && _followVCam != null)
        {
            var confiner = _followVCam.gameObject.AddComponent<CinemachineConfiner2D>();
            confiner.m_BoundingShape2D = mapBoundsCollider;
            confiner.m_Damping         = 0f;
            confiner.InvalidateCache();
            Debug.Log($"[MultiCameraManager] ✅ Confiner2D attached → {mapBoundsCollider.name}");
        }

        // Bước 4: Set pending follow target nếu đã có
        if (_pendingFollowTarget != null && _followVCam != null)
        {
            _followVCam.Follow = _pendingFollowTarget;
            _pendingFollowTarget = null;
        }

        // Bước 5: Áp dụng mode
        ApplyCameraMode(CurrentMode);

        Debug.Log($"[MultiCameraManager] ✅ VCams ready – StateAuthority={Object.HasStateAuthority}, LocalPlayer={Runner.LocalPlayer}");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Camera Setup

    private void SetupMainCamera()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            _mainCamera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        _mainCamera.orthographic = true;
        _mainCamera.rect = new Rect(0, 0, 1, 1);

        if (_mainCamera.GetComponent<CinemachineBrain>() == null)
            _mainCamera.gameObject.AddComponent<CinemachineBrain>();
    }

    private void CreateFollowVCam()
    {
        var go = new GameObject("VCam_Follow");
        _followVCam = go.AddComponent<CinemachineVirtualCamera>();
        _followVCam.m_Lens.Orthographic    = true;
        _followVCam.m_Lens.OrthographicSize = followOrthoSize;
        _followVCam.m_Priority              = 10;

        var transposer = _followVCam.AddCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = followOffset;
        transposer.m_XDamping     = dampingXY;
        transposer.m_YDamping     = dampingXY;
        transposer.m_ZDamping     = 0f;
        transposer.m_BindingMode  = CinemachineTransposer.BindingMode.WorldSpace;

        // ⚠️ KHÔNG add CinemachineConfiner2D ở đây
        // → add sau 1 frame trong InitVCamsNextFrame()

        _followVCam.Follow = null;
    }

    private void CreateOverviewVCam()
    {
        if (autoFitOverview && mapBoundsCollider != null)
        {
            Bounds b     = mapBoundsCollider.bounds;
            float aspect = (float)Screen.width / Screen.height;
            float sizeH  = b.size.y / 2f;
            float sizeW  = (b.size.x / 2f) / aspect;
            overviewOrthoSize = Mathf.Max(sizeH, sizeW) * 1.05f;
            overviewPosition  = new Vector3(b.center.x, b.center.y, followOffset.z);
            Debug.Log($"[MultiCameraManager] AutoFit: pos={overviewPosition}, ortho={overviewOrthoSize:F1}");
        }

        var go = new GameObject("VCam_Overview");
        go.transform.position = overviewPosition;

        _overviewVCam = go.AddComponent<CinemachineVirtualCamera>();
        _overviewVCam.m_Lens.Orthographic    = true;
        _overviewVCam.m_Lens.OrthographicSize = overviewOrthoSize;
        _overviewVCam.m_Priority              = 5;

        _overviewVCam.Follow = null;
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

        if (mode == CameraMode.Follow)
        {
            _followVCam.m_Priority   = 10;
            _overviewVCam.m_Priority = 5;
        }
        else
        {
            _followVCam.m_Priority   = 5;
            _overviewVCam.m_Priority = 10;
        }
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
            // VCam đã sẵn sàng → set ngay
            _followVCam.Follow = car.transform;
            Debug.Log($"[MultiCameraManager] ✅ Follow → {car.name} (Player {player.PlayerId})");
        }
        else
        {
            // VCam chưa sẵn sàng (coroutine chưa xong) → buffer lại
            _pendingFollowTarget = car.transform;
            Debug.Log($"[MultiCameraManager] ⏳ Buffered follow target → {car.name}");
        }
    }

    public void OnPlayerLeft(PlayerRef player) { }

    #endregion
}