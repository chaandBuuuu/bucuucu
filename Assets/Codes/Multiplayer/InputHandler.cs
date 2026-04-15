using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

/// <summary>
/// FIXED:
///   - Re-register callback sau mỗi scene load (lobby → racing và ngược lại)
///   - Client không di chuyển được vì InputHandler chỉ register 1 lần khi Start()
///     nhưng Runner vẫn còn sau scene transition → cần re-register
///   - Dùng SceneManager.sceneLoaded event để tự động re-register
/// </summary>
public class InputHandler : FusionCallbacksBase
{
    private Vector2 _moveInput;
    private bool    _isDrifting;
    private bool    _usePowerup;
    private bool    _isPausing;
    private bool    _pressE;
    private bool    _pressR;
    private bool    _pressF;

    private bool _isRegistered = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // FIX: Lắng nghe scene load để re-register
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(RegisterWhenReady());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        TryUnregister();
    }

    /// <summary>
    /// FIX: Mỗi khi scene load xong, re-register với Runner hiện tại
    /// Đây là nguyên nhân client không di chuyển được —
    /// sau khi load lobby scene, Runner vẫn còn nhưng callback bị mất
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[InputHandler] Scene loaded: {scene.name}, re-registering input...");

        // Unregister cái cũ trước
        TryUnregister();

        // Register lại
        StartCoroutine(RegisterWhenReady());
    }

    private void TryUnregister()
    {
        if (_isRegistered && FusionNetworkManager.Instance?.Runner != null)
        {
            FusionNetworkManager.Instance.Runner.RemoveCallbacks(this);
            _isRegistered = false;
            Debug.Log("[InputHandler] Unregistered from Runner");
        }
    }

    private System.Collections.IEnumerator RegisterWhenReady()
    {
        // Chờ Runner tồn tại và đang chạy
        while (FusionNetworkManager.Instance?.Runner == null ||
               !FusionNetworkManager.Instance.Runner.IsRunning)
        {
            yield return null;
        }

        // FIX: Kiểm tra chưa register với runner này
        if (!_isRegistered)
        {
            FusionNetworkManager.Instance.Runner.AddCallbacks(this);
            _isRegistered = true;
            Debug.Log($"[InputHandler] ✅ Registered with Runner (IsServer={FusionNetworkManager.Instance.Runner.IsServer})");
        }
    }

    private void Update()
    {
        // Racing controls
        _moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        _isDrifting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        _usePowerup = Input.GetKeyDown(KeyCode.Q);

        // Legacy controls
        _pressE = Input.GetKeyDown(KeyCode.E);
        _pressR = Input.GetKeyDown(KeyCode.R);
        _pressF = Input.GetKeyDown(KeyCode.F);

        if (Input.GetKeyDown(KeyCode.P))
            _isPausing = true;
    }

    public override void OnInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(new NetworkInputData
        {
            Direction     = _moveInput,
            MoveDirection = _moveInput,
            IsDrifting    = _isDrifting,
            UsePowerup    = _usePowerup,
            IsPausing     = _isPausing,
            PressE        = _pressE,
            PressR        = _pressR,
            PressF        = _pressF
        });

        // Reset one-time flags
        _isPausing  = false;
        _usePowerup = false;
        _pressE     = false;
        _pressR     = false;
        _pressF     = false;
    }
}