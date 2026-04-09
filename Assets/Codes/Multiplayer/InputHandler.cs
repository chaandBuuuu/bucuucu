using UnityEngine;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Thu thập input cho racing game
/// WASD - Di chuyển, Shift - Drift, Q - Use Powerup
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
        // Đợi Runner sẵn sàng
        while (FusionNetworkManager.Instance?.Runner == null || !FusionNetworkManager.Instance.Runner.IsRunning)
            yield return null;

        FusionNetworkManager.Instance.Runner.AddCallbacks(this);
        Debug.Log("[InputHandler] ✅ Input handler đã đăng ký");
    }

    private void OnDestroy()
    {
        FusionNetworkManager.Instance?.Runner?.RemoveCallbacks(this);
    }

    public override void OnInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(new NetworkInputData
        {
            Direction = _moveInput,
            MoveDirection = _moveInput,
            IsDrifting = _isDrifting,
            UsePowerup = _usePowerup,
            IsPausing = _isPausing,
            PressE = _pressE,
            PressR = _pressR,
            PressF = _pressF
        });
        _isPausing = false;
    }
}

