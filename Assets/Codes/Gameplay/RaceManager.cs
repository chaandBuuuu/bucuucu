using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Fusion;

/// <summary>
/// ✅ UPDATED RACE SYSTEM (v2):
///   - Logic: First to cross finish line → 10s countdown → Calculate rankings by time + distance
///   - Players freeze after crossing finish line
///   - No more lap counting system
///   - Pure finish-line based racing
/// </summary>
public class RaceManager : NetworkBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Race Config")]
    [SerializeField] private float finishCountdownDuration = 10f;  // 10s countdown sau khi first player finish
    
    [Header("Finish Line")]
    [SerializeField] private Transform finishLineTransform;

    // ── Networked State ──────────────────────────────────────────────────────
    [Networked] public bool  RaceStarted      { get; private set; }
    [Networked] public bool  RaceFinished     { get; private set; }
    [Networked] public float RaceTimer        { get; private set; }
    [Networked] public int   CountdownCounter { get; private set; } = -1;  // Pre-race countdown (3,2,1,0)
    [Networked] public float FinishCountdown  { get; private set; } = -1f; // Post-finish countdown (10s)

    // ── Local Tracking ───────────────────────────────────────────────────────
    // Người chơi đã qua đích (NetworkId → thời gian qua đích)
    private Dictionary<NetworkId, float> _finishTimes = new Dictionary<NetworkId, float>();
    // Distance từ người chơi đến finish line khi qua đích
    private Dictionary<NetworkId, float> _finishDistances = new Dictionary<NetworkId, float>();
    // Statusof người chơi (active, finished, frozen)
    private Dictionary<NetworkId, bool> _carFinished = new Dictionary<NetworkId, bool>();
    
    private List<CarController> _cachedCars = new List<CarController>();
    private CarController _firstFinisher = null;
    private bool _isSpawned = false;
    private float _countdownTimer = 0f;
    private float _finishCountdownTimer = 0f;
    private float _lastCarCacheTime = 0f;
    private const float CAR_CACHE_UPDATE_INTERVAL = 0.5f;

    public event System.Action                   OnRaceStart;
    public event System.Action<CarController>    OnRaceEnd;
    public event System.Action<CarController>    OnPlayerFinish;  // Khi player qua đích
    public event System.Action<List<(CarController, int, float, float)>> OnFinalRankings;  // Car, Position, Time, Distance

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        _isSpawned = true;

        if (HasStateAuthority && !RaceStarted)
        {
            RaceStarted  = false;
            RaceFinished = false;
            RaceTimer    = 0f;
            CountdownCounter = -1;
            FinishCountdown = -1f;
            Debug.Log("[RaceManager] ✅ Race initialized (finish-line based)!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // ── Pre-race countdown (3,2,1,0) ────────────────────────────────────
        if (CountdownCounter >= 0)
        {
            _countdownTimer += Runner.DeltaTime;
            int desiredCount = Mathf.Max(0, 3 - Mathf.FloorToInt(_countdownTimer));
            
            if (desiredCount != CountdownCounter)
            {
                CountdownCounter = desiredCount;
                Debug.Log($"[RaceManager] Countdown: {CountdownCounter}");
            }

            if (_countdownTimer >= 3f)
            {
                _countdownTimer = 0f;
                CountdownCounter = -1;
                RaceStarted = true;
                Debug.Log("[RaceManager] 🏁 RACE STARTED!");
                OnRaceStart?.Invoke();
            }
        }

        // ── Main race timer ──────────────────────────────────────────────────
        if (RaceStarted && !RaceFinished)
            RaceTimer += Runner.DeltaTime;

        // ── Post-finish countdown (10s sau khi first player finish) ──────────
        if (FinishCountdown >= 0f)
        {
            _finishCountdownTimer += Runner.DeltaTime;
            float desiredCountdown = Mathf.Max(-0.1f, finishCountdownDuration - _finishCountdownTimer);
            FinishCountdown = desiredCountdown;

            if (_finishCountdownTimer >= finishCountdownDuration)
            {
                _finishCountdownTimer = 0f;
                FinishCountdown = -1f;
                FinishRace();
                Debug.Log("[RaceManager] ⏱️ Finish countdown complete - Race ended!");
            }
        }

        // ── Update distances ─────────────────────────────────────────────────
        UpdatePlayerDistances();
    }

    private void UpdatePlayerDistances()
    {
        if (finishLineTransform == null) return;

        // ✅ OPTIMIZE: Throttle car cache update to avoid FindObjectsByType every frame
        float currentTime = Time.time;
        if (currentTime - _lastCarCacheTime > CAR_CACHE_UPDATE_INTERVAL)
        {
            _lastCarCacheTime = currentTime;
            _cachedCars.Clear();
            var allCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (var car in allCars)
            {
                if (car.Object != null && !car.IsFinished)
                    _cachedCars.Add(car);  // ✅ Only cache active cars
            }
        }

        // ✅ OPTIMIZE: Update distance only for unfinished cars
        Vector3 finishPos = finishLineTransform.position;
        foreach (var car in _cachedCars)
        {
            if (car.Object == null) continue;

            float distance = Vector3.Distance(car.transform.position, finishPos);
            // Only store if car hasn't finished yet
        }
    }

    public void SetFinishLinePosition(Transform finishLine)
    {
        finishLineTransform = finishLine;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_StartRace()
    {
        if (!HasStateAuthority) return;
        CountdownCounter = 3;
        _countdownTimer = 0f;
        Debug.Log("[RaceManager] 🎬 Starting pre-race countdown!");
    }

    /// <summary>
    /// ✅ NEW: Người chơi qua đích
    /// </summary>
    public void RegisterFinishCrossing(CarController car)
    {
        if (!HasStateAuthority) return;
        if (!RaceStarted || RaceFinished) return;

        NetworkId carId = car.Object.Id;

        // Tránh đếm 2 lần
        if (_carFinished.TryGetValue(carId, out bool finished) && finished)
            return;

        _carFinished[carId] = true;
        _finishTimes[carId] = RaceTimer;
        
        // Lưu distance từ finish line
        if (finishLineTransform != null)
        {
            float distance = Vector3.Distance(car.transform.position, finishLineTransform.position);
            _finishDistances[carId] = distance;
        }

        OnPlayerFinish?.Invoke(car);
        Debug.Log($"[RaceManager] ✅ {car.name} finished at {RaceTimer:F2}s");

        // Nếu đây là người finish đầu tiên → bắt đầu 10s countdown
        if (_firstFinisher == null)
        {
            _firstFinisher = car;
            FinishCountdown = finishCountdownDuration;
            _finishCountdownTimer = 0f;
            Debug.Log($"[RaceManager] 🎉 First finisher: {car.name} - Starting 10s countdown!");
        }

        // Freeze player (không cho chạy nữa)
        car.IsFinished = true;
    }

    private void FinishRace()
    {
        if (RaceFinished) return;
        RaceFinished = true;

        // Tính toán kết quả: ranking dựa trên thời gian qua đích + khoảng cách
        var rankings = CalculateFinalRankings();
        
        OnFinalRankings?.Invoke(rankings);
        
        if (rankings.Count > 0)
        {
            var winner = rankings[0];
            Debug.Log($"[RaceManager] 🏆 Race finished! Winner: {winner.Item1.name} (Time: {winner.Item3:F2}s, Distance: {winner.Item4:F2}m)");
            OnRaceEnd?.Invoke(winner.Item1);
        }
    }

    /// <summary>
    /// ✅ Tính toán ranking cuối cùng: Thứ tự = thời gian qua đích + khoảng cách
    /// </summary>
    private List<(CarController car, int position, float finishTime, float finishDistance)> CalculateFinalRankings()
    {
        var rankings = new List<(CarController, int, float, float)>();

        // Tất cả người chơi (kể cả chưa finish)
        List<(CarController car, float finishTime, float finishDistance)> playerStats = new List<(CarController, float, float)>();

        foreach (var car in _cachedCars)
        {
            if (car?.Object == null) continue;
            NetworkId carId = car.Object.Id;

            if (_carFinished.TryGetValue(carId, out bool finished) && finished)
            {
                float time = _finishTimes.TryGetValue(carId, out float t) ? t : RaceTimer;
                float distance = _finishDistances.TryGetValue(carId, out float d) ? d : 0f;
                playerStats.Add((car, time, distance));
            }
            else
            {
                // Người chơi chưa finish → đánh giá dựa trên distance hiện tại
                float distance = finishLineTransform != null 
                    ? Vector3.Distance(car.transform.position, finishLineTransform.position)
                    : float.MaxValue;
                playerStats.Add((car, RaceTimer, distance));  // Fake time = race total time
            }
        }

        // Sort: Primary = finish time (sớm hơn tốt hơn), Secondary = distance (gần hơn tốt hơn)
        playerStats.Sort((a, b) =>
        {
            // Nếu cùng finish → compare distance
            if (Mathf.Abs(a.finishTime - b.finishTime) < 0.01f)
            {
                return a.finishDistance.CompareTo(b.finishDistance);
            }
            // Ngược lại → compare time
            return a.finishTime.CompareTo(b.finishTime);
        });

        // Build rankings
        for (int i = 0; i < playerStats.Count; i++)
        {
            rankings.Add((playerStats[i].car, i + 1, playerStats[i].finishTime, playerStats[i].finishDistance));
        }

        return rankings;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ──────────────────────────────────────────────────────────────────────────

    public bool IsPlayerFinished(CarController car)
    {
        if (car?.Object == null) return false;
        return _carFinished.TryGetValue(car.Object.Id, out bool finished) && finished;
    }

    public float GetRaceTime() => _isSpawned ? RaceTimer : 0f;

    public float GetPlayerFinishTime(CarController car)
    {
        if (car?.Object == null) return -1f;
        return _finishTimes.TryGetValue(car.Object.Id, out float time) ? time : -1f;
    }

    public CarController GetFirstFinisher() => _firstFinisher;
    public bool IsSpawned => _isSpawned;
}