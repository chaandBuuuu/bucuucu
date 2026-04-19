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
///   - EnsureExists() để tạo InputHandler nếu chưa tồn tại (giống AudioManager)
/// </summary>
public class InputHandler : FusionCallbacksBase
{
    private static InputHandler _instance;
    
    private Vector2 _moveInput;
    private bool    _isDrifting;
    private bool    _usePowerup;
    private bool    _isPausing;
    private bool    _pressE;
    private bool    _pressR;
    private bool    _pressF;

    private bool _isRegistered = false;

    /// <summary>
    /// ✅ FIXED: Ensure InputHandler exists (create if needed)
    /// </summary>
    public static void EnsureExists()
    {
        if (_instance != null)
        {
            Debug.Log($"[InputHandler] ✅ Instance already exists: {_instance.gameObject.name}");
            return;
        }

        // Search for existing instance in scene
        var existingHandler = FindAnyObjectByType<InputHandler>();
        if (existingHandler != null)
        {
            _instance = existingHandler;
            Debug.Log($"[InputHandler] ✅ Found existing instance: {existingHandler.gameObject.name}");
            return;
        }

        // Create new if doesn't exist
        var go = new GameObject("InputHandler");
        var handler = go.AddComponent<InputHandler>();
        // Instance set in Awake
        Debug.Log("[InputHandler] ✅ Created new InputHandler instance");
    }

    private void Awake()
    {
        // ✅ FIXED: Prevent duplicate InputHandlers
        var existingInstance = FindAnyObjectByType<InputHandler>();
        if (existingInstance != null && existingInstance != this)
        {
            Debug.LogWarning($"[InputHandler] ⚠️ Destroying duplicate InputHandler. Existing: {existingInstance.gameObject.name}, This: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[InputHandler] ✅ Singleton set: {gameObject.name}");
    }

    private void Start()
    {
        // ✅ FIXED: Check if we're the active singleton
        if (_instance != this)
        {
            Debug.LogWarning($"[InputHandler] ⚠️ This instance is not the singleton! Destroying.");
            Destroy(gameObject);
            return;
        }

        // FIX: Lắng nghe scene load để re-register
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(RegisterWhenReady());
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Debug.Log("[InputHandler] ✅ Unsubscribed from scene load events");
        }
    }

    /// <summary>
    /// FIX: Mỗi khi scene load xong, re-register với Runner hiện tại
    /// Đây là nguyên nhân client không di chuyển được —
    /// sau khi load lobby scene, Runner vẫn còn nhưng callback bị mất
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ✅ FIXED: Only process if we're the active singleton
        if (_instance != this)
        {
            Debug.LogWarning($"[InputHandler] ⚠️ OnSceneLoaded called on non-singleton instance. Ignoring.");
            return;
        }

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
        int maxAttempts = 300; // 5 second timeout (60 frames * 5)
        int attempts = 0;
        
        // ✅ FIXED: Chờ Runner tồn tại và đang chạy với timeout
        while (attempts < maxAttempts &&
               (FusionNetworkManager.Instance?.Runner == null ||
                !FusionNetworkManager.Instance.Runner.IsRunning))
        {
            attempts++;
            yield return null;
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogError("[InputHandler] ❌ TIMEOUT: Runner never became available!");
            yield break;
        }

        // ✅ FIXED: Kiểm tra chưa register với runner này
        if (!_isRegistered)
        {
            var runner = FusionNetworkManager.Instance.Runner;
            if (runner != null && runner.IsRunning)
            {
                runner.AddCallbacks(this);
                _isRegistered = true;
                Debug.Log($"[InputHandler] ✅ Registered with Runner (IsServer={runner.IsServer}, LocalPlayer={runner.LocalPlayer})");
            }
            else
            {
                Debug.LogError("[InputHandler] ❌ Cannot register: Runner is null or not running!");
            }
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