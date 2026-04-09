using UnityEngine;
using Fusion;
using Fusion.Sockets;

public class InputHandler : FusionCallbacksBase
{
    private Vector2 _moveInput;
    private bool    _isDrifting;
    private bool    _usePowerup;
    private bool    _isPausing;
    private bool    _pressE;
    private bool    _pressR;
    private bool    _pressF;

    private void Awake()
    {
        // <<< THÊM DÒNG NÀY >>>
        DontDestroyOnLoad(gameObject);
        // InputHandler sẽ sống qua scene load (lobby → racing)
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

    private void Start()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private System.Collections.IEnumerator RegisterWhenReady()
    {
        while (FusionNetworkManager.Instance?.Runner == null)
            yield return null;

        FusionNetworkManager.Instance.Runner.AddCallbacks(this);
        Debug.Log("[InputHandler] Input handler registered");
    }

    private void OnDestroy()
    {
        // An toàn khi shutdown
        if (FusionNetworkManager.Instance?.Runner != null)
            FusionNetworkManager.Instance.Runner.RemoveCallbacks(this);
    }

    public override void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Debug.Log($"[InputHandler] OnInput called - MoveInput={_moveInput}, IsDrifting={_isDrifting}");
        
        input.Set(new NetworkInputData
        {
            Direction      = _moveInput,
            MoveDirection  = _moveInput,
            IsDrifting     = _isDrifting,
            UsePowerup     = _usePowerup,
            IsPausing      = _isPausing,
            PressE         = _pressE,
            PressR         = _pressR,
            PressF         = _pressF
        });
        
        // Reset one-time flags
        _isPausing = false;
        _usePowerup = false;
    }
}