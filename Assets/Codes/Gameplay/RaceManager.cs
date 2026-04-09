using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// Quản lý trạng thái cuộc đua - ĐÃ SỬA hoàn toàn (không còn lỗi IsSpawned)
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

    private bool _isSpawned = false;   // ← FIX: cờ an toàn

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        _isSpawned = true;                    // ← Đánh dấu đã spawn
        RaceStarted = false;
        RaceFinished = false;
        RaceTimer = 0f;
        Debug.Log("[RaceManager] ✅ Race initialized & Spawned!");
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (RaceStarted && !RaceFinished)
        {
            RaceTimer += Runner.DeltaTime;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_StartRace()
    {
        if (!HasStateAuthority) return;
        RaceStarted = true;
        RaceTimer = 0f;
        OnRaceStart?.Invoke();
        Debug.Log("[RaceManager] Race started!");
    }

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

    /// <summary>
    /// Lấy thời gian an toàn (không crash trước khi Spawned)
    /// </summary>
    public float GetRaceTime()
    {
        if (!_isSpawned) return 0f;   // ← FIX: dùng cờ local
        return RaceTimer;
    }

    public CarController GetWinner() => _winner;
}