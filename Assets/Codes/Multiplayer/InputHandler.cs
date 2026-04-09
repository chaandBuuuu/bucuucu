using UnityEngine;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Thu thập input và gửi lên Fusion mỗi network tick
/// Dùng Input.GetAxisRaw — không cần Input Actions asset
/// </summary>
public class InputHandler : FusionCallbacksBase
{
    private Vector2 _moveInput;
    private bool    _isPausing;
    private bool    _pressE;
    private bool    _pressR;
    private bool    _pressF;

    private void Update()
    {
        _moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

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
        Debug.Log("[InputHandler] Đã đăng ký với Runner");
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
            IsPausing = _isPausing,
            PressE = _pressE,
            PressR = _pressR,
            PressF = _pressF
        });
        _isPausing = false;
    }
}
