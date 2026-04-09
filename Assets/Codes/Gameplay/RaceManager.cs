using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// Quản lý trạng thái cuộc đua
/// </summary>
public class RaceManager : NetworkBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Race Config")]
    [SerializeField] private int lapsToWin = 4;
    [SerializeField] private Transform finishLine;
    [SerializeField] private Transform[] checkpoints;

    [Networked] public bool RaceStarted { get; private set; }
    [Networked] public bool RaceFinished { get; private set; }
    [Networked] public float RaceTimer { get; private set; }

    private Dictionary<CarController, int> _carLapCount = new Dictionary<CarController, int>();
    private CarController _winner = null;

    public event System.Action OnRaceStart;
    public event System.Action<CarController> OnRaceEnd;
    public event System.Action<CarController, int> OnLapComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        RaceStarted = false;
        RaceFinished = false;
        RaceTimer = 0f;
        Debug.Log("[RaceManager] Race initialized");
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (RaceStarted && !RaceFinished)
        {
            RaceTimer += Runner.DeltaTime;
        }
    }

    /// <summary>
    /// Start race
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_StartRace()
    {
        if (!HasStateAuthority) return;
        RaceStarted = true;
        RaceTimer = 0f;
        OnRaceStart?.Invoke();
        Debug.Log("[RaceManager] Race started!");
    }

    /// <summary>
    /// Xử lý hoàn thành lap
    /// </summary>
    public void RegisterLapCompletion(CarController car)
    {
        if (!RaceStarted || RaceFinished) return;

        if (!_carLapCount.ContainsKey(car))
            _carLapCount[car] = 0;

        _carLapCount[car]++;
        int laps = _carLapCount[car];

        OnLapComplete?.Invoke(car, laps);
        Debug.Log($"[RaceManager] {car.name} completed lap {laps}");

        if (laps >= lapsToWin)
        {
            FinishRace(car);
        }
    }

    /// <summary>
    /// Kết thúc cuộc đua
    /// </summary>
    private void FinishRace(CarController winner)
    {
        if (RaceFinished) return;

        RaceFinished = true;
        _winner = winner;
        
        OnRaceEnd?.Invoke(winner);
        Debug.Log($"[RaceManager] {winner.name} won the race in {RaceTimer:F2}s!");
    }

    public int GetLapCount(CarController car)
    {
        return _carLapCount.ContainsKey(car) ? _carLapCount[car] : 0;
    }

    public CarController GetWinner() => _winner;
    public float GetRaceTime() => RaceTimer;
}
