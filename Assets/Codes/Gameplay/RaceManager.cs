using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// FIX:
///   - RegisterLapCompletion() thêm HasStateAuthority check → tránh client ghi trực tiếp
///     vào [Networked] state (sẽ throw exception trong Fusion)
///   - Spawned() chỉ reset state khi race chưa bắt đầu → an toàn khi object respawn
///   - _carLapCount dùng NetworkId thay vì CarController reference để tránh stale ref
/// </summary>
public class RaceManager : NetworkBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Race Config")]
    [SerializeField] private int lapsToWin = RacingConstants.RACE_LAPS_TO_WIN;

    [Networked] public bool  RaceStarted  { get; private set; }
    [Networked] public bool  RaceFinished { get; private set; }
    [Networked] public float RaceTimer    { get; private set; }

    // FIX: Dùng NetworkId làm key để tránh stale reference sau scene reload
    private Dictionary<NetworkId, int> _carLapCount = new Dictionary<NetworkId, int>();
    private CarController _winner = null;
    private bool _isSpawned = false;

    public event System.Action                   OnRaceStart;
    public event System.Action<CarController>    OnRaceEnd;
    public event System.Action<CarController, int> OnLapComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        _isSpawned = true;

        // FIX: Chỉ reset khi race CHƯA bắt đầu
        // Tránh trường hợp NetworkObject bị despawn/respawn giữa chừng làm mất state
        if (HasStateAuthority && !RaceStarted)
        {
            RaceStarted  = false;
            RaceFinished = false;
            RaceTimer    = 0f;
            Debug.Log("[RaceManager] ✅ Race initialized!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (RaceStarted && !RaceFinished)
            RaceTimer += Runner.DeltaTime;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_StartRace()
    {
        if (!HasStateAuthority) return;
        RaceStarted = true;
        RaceTimer   = 0f;
        OnRaceStart?.Invoke();
        Debug.Log("[RaceManager] Race bắt đầu!");
    }

    /// <summary>
    /// FIX: Thêm HasStateAuthority guard — chỉ server được ghi networked state.
    /// Trước đây không có guard → client gọi trực tiếp sẽ throw Fusion exception.
    /// </summary>
    public void RegisterLapCompletion(CarController car)
    {
        // FIX: Authority check
        if (!HasStateAuthority) return;
        if (!RaceStarted || RaceFinished) return;

        NetworkId carId = car.Object.Id;

        if (!_carLapCount.ContainsKey(carId))
            _carLapCount[carId] = 0;

        _carLapCount[carId]++;
        int laps = _carLapCount[carId];

        OnLapComplete?.Invoke(car, laps);
        Debug.Log($"[RaceManager] {car.name} hoàn thành vòng {laps}/{lapsToWin}");

        if (laps >= lapsToWin)
            FinishRace(car);
    }

    private void FinishRace(CarController winner)
    {
        if (RaceFinished) return;
        RaceFinished = true;
        _winner      = winner;
        OnRaceEnd?.Invoke(winner);
        Debug.Log($"[RaceManager] {winner.name} thắng sau {RaceTimer:F2}s!");
    }

    public int GetLapCount(CarController car)
    {
        if (car == null || car.Object == null) return 0;
        return _carLapCount.TryGetValue(car.Object.Id, out int count) ? count : 0;
    }

    public float GetRaceTime()
    {
        if (!_isSpawned) return 0f;
        return RaceTimer;
    }

    public CarController GetWinner() => _winner;
}