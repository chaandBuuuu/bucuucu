using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// ✅ FIXED RaceManager:
///   - OnChangedRender để client nhận event đúng (không chỉ host)
///   - RPC_RestartRace để host broadcast restart
///   - RPC_BackToLobby để quay về lobby
/// </summary>
public class RaceManager : NetworkBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Race Config")]
    [SerializeField] private float finishCountdownDuration = 10f;

    [Header("Finish Line")]
    [SerializeField] private Transform finishLineTransform;

    // ── Networked State ──────────────────────────────────────────────────────
    [Networked] public bool  RaceStarted      { get; private set; }
    [Networked] public bool  RaceFinished     { get; private set; }
    [Networked] public float RaceTimer        { get; private set; }
    [Networked] public int   CountdownCounter { get; private set; } = -1;
    [Networked] public float FinishCountdown  { get; private set; } = -1f;

    // ✅ FIX: Networked booleans để trigger events trên client qua OnChangedRender
    [Networked, OnChangedRender(nameof(OnRaceStartedChanged))]
    private bool _raceStartedTrigger { get; set; }

    [Networked, OnChangedRender(nameof(OnRaceFinishedChanged))]
    private bool _raceFinishedTrigger { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerFinishedChanged))]
    private NetworkId _lastFinishedCarId { get; set; }

    // ── Local Tracking ───────────────────────────────────────────────────────
    private Dictionary<NetworkId, float> _finishTimes     = new Dictionary<NetworkId, float>();
    private Dictionary<NetworkId, float> _finishDistances = new Dictionary<NetworkId, float>();
    private Dictionary<NetworkId, bool>  _carFinished     = new Dictionary<NetworkId, bool>();

    private List<CarController> _cachedCars        = new List<CarController>();
    private CarController       _firstFinisher     = null;
    private CarController       _winner            = null;
    private bool                _isSpawned         = false;
    private float               _countdownTimer    = 0f;
    private float               _finishCountdownTimer = 0f;
    private float               _lastCarCacheTime  = 0f;
    private const float         CAR_CACHE_UPDATE_INTERVAL = 0.5f;

    // ✅ Events – fire trên tất cả clients nhờ OnChangedRender
    public event System.Action                                           OnRaceStart;
    public event System.Action<CarController>                            OnRaceEnd;
    public event System.Action<CarController>                            OnPlayerFinish;
    public event System.Action<List<(CarController, int, float, float)>> OnFinalRankings;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void Spawned()
    {
        _isSpawned = true;

        if (HasStateAuthority)
        {
            RaceStarted      = false;
            RaceFinished     = false;
            RaceTimer        = 0f;
            CountdownCounter = -1;
            FinishCountdown  = -1f;
            Debug.Log("[RaceManager] ✅ Initialized");
        }
    }

    // ── OnChangedRender callbacks (chạy trên TẤT CẢ clients) ────────────────

    private void OnRaceStartedChanged()
    {
        if (!_raceStartedTrigger) return;
        Debug.Log("[RaceManager] 🏁 OnRaceStart → client");
        OnRaceStart?.Invoke();
    }

    private void OnRaceFinishedChanged()
    {
        if (!_raceFinishedTrigger) return;
        var rankings = CalculateFinalRankings();
        OnFinalRankings?.Invoke(rankings);
        if (_winner != null)
            OnRaceEnd?.Invoke(_winner);
        Debug.Log("[RaceManager] 🏆 OnRaceFinished → client");
    }

    private void OnPlayerFinishedChanged()
    {
        if (_lastFinishedCarId == default) return;
        // Tìm car theo NetworkId
        var allCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        foreach (var car in allCars)
        {
            if (car.Object != null && car.Object.Id == _lastFinishedCarId)
            {
                OnPlayerFinish?.Invoke(car);
                Debug.Log($"[RaceManager] OnPlayerFinish → {car.name} on client");
                return;
            }
        }
    }

    // ── FixedUpdateNetwork ───────────────────────────────────────────────────

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // Pre-race countdown
        if (CountdownCounter >= 0)
        {
            _countdownTimer += Runner.DeltaTime;
            int desired = Mathf.Max(0, 3 - Mathf.FloorToInt(_countdownTimer));

            if (desired != CountdownCounter)
            {
                CountdownCounter = desired;
                Debug.Log($"[RaceManager] Countdown: {CountdownCounter}");
            }

            if (_countdownTimer >= 3f)
            {
                _countdownTimer      = 0f;
                CountdownCounter     = -1;
                RaceStarted          = true;
                _raceStartedTrigger  = true;   // ✅ Trigger OnChangedRender trên clients
                Debug.Log("[RaceManager] 🏁 RACE STARTED!");
            }
        }

        // Race timer
        if (RaceStarted && !RaceFinished)
            RaceTimer += Runner.DeltaTime;

        // Post-finish countdown
        if (FinishCountdown >= 0f)
        {
            _finishCountdownTimer += Runner.DeltaTime;
            FinishCountdown = Mathf.Max(-0.1f, finishCountdownDuration - _finishCountdownTimer);

            if (_finishCountdownTimer >= finishCountdownDuration)
            {
                _finishCountdownTimer = 0f;
                FinishCountdown       = -1f;
                FinishRace();
            }
        }

        UpdatePlayerDistances();
    }

    private void UpdatePlayerDistances()
    {
        if (finishLineTransform == null) return;

        float now = Time.time;
        if (now - _lastCarCacheTime > CAR_CACHE_UPDATE_INTERVAL)
        {
            _lastCarCacheTime = now;
            _cachedCars.Clear();
            var allCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (var car in allCars)
                if (car.Object != null && !car.IsFinished)
                    _cachedCars.Add(car);
        }
    }

    public void SetFinishLinePosition(Transform finishLine)
    {
        finishLineTransform = finishLine;
    }

    // ── RPCs ─────────────────────────────────────────────────────────────────

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_StartRace()
    {
        if (!HasStateAuthority) return;
        CountdownCounter = 3;
        _countdownTimer  = 0f;
        Debug.Log("[RaceManager] 🎬 Starting countdown!");
    }

    /// <summary>
    /// ✅ Host restart race → broadcast cho tất cả clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RestartRace()
    {
        Debug.Log("[RaceManager] 🔄 Restarting race...");
        // Load lại scene GamePlay
        if (Runner != null && Runner.IsServer)
            Runner.LoadScene(SceneRef.FromIndex(2));
    }

    /// <summary>
    /// ✅ Host back to lobby → broadcast cho tất cả clients
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BackToLobby()
    {
        Debug.Log("[RaceManager] 🏠 Back to lobby...");
        if (Runner != null && Runner.IsServer)
            Runner.LoadScene(SceneRef.FromIndex(1));
    }

    // ── Race Logic ───────────────────────────────────────────────────────────

    public void RegisterFinishCrossing(CarController car)
    {
        if (!HasStateAuthority) return;
        if (!RaceStarted || RaceFinished) return;

        NetworkId carId = car.Object.Id;
        if (_carFinished.TryGetValue(carId, out bool done) && done) return;

        _carFinished[carId]  = true;
        _finishTimes[carId]  = RaceTimer;

        if (finishLineTransform != null)
            _finishDistances[carId] = Vector3.Distance(car.transform.position, finishLineTransform.position);

        car.IsFinished       = true;
        _lastFinishedCarId   = carId;  // ✅ Trigger OnChangedRender → OnPlayerFinish trên clients

        // Host fire event trực tiếp
        OnPlayerFinish?.Invoke(car);

        Debug.Log($"[RaceManager] ✅ {car.name} finished at {RaceTimer:F2}s");

        if (_firstFinisher == null)
        {
            _firstFinisher        = car;
            FinishCountdown       = finishCountdownDuration;
            _finishCountdownTimer = 0f;
            Debug.Log($"[RaceManager] 🎉 First finisher: {car.name}");
        }
    }

    private void FinishRace()
    {
        if (RaceFinished) return;
        RaceFinished = true;

        // Cache tất cả cars cho rankings
        _cachedCars.Clear();
        var all = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        foreach (var c in all)
            if (c.Object != null) _cachedCars.Add(c);

        var rankings = CalculateFinalRankings();
        if (rankings.Count > 0) _winner = rankings[0].car;

        // Host fire events trực tiếp
        OnFinalRankings?.Invoke(rankings);
        if (_winner != null) OnRaceEnd?.Invoke(_winner);

        // ✅ Trigger OnChangedRender → clients cũng nhận
        _raceFinishedTrigger = true;

        Debug.Log($"[RaceManager] 🏆 Race finished! Winner: {_winner?.name}");
    }

    private List<(CarController car, int position, float finishTime, float finishDistance)> CalculateFinalRankings()
    {
        var stats = new List<(CarController car, float time, float dist)>();

        foreach (var car in _cachedCars)
        {
            if (car?.Object == null) continue;
            NetworkId id = car.Object.Id;

            if (_carFinished.TryGetValue(id, out bool finished) && finished)
            {
                float t = _finishTimes.TryGetValue(id, out var ft) ? ft : RaceTimer;
                float d = _finishDistances.TryGetValue(id, out var fd) ? fd : 0f;
                stats.Add((car, t, d));
            }
            else
            {
                float d = finishLineTransform != null
                    ? Vector3.Distance(car.transform.position, finishLineTransform.position)
                    : float.MaxValue;
                stats.Add((car, RaceTimer, d));
            }
        }

        stats.Sort((a, b) =>
        {
            if (Mathf.Abs(a.time - b.time) < 0.01f) return a.dist.CompareTo(b.dist);
            return a.time.CompareTo(b.time);
        });

        var result = new List<(CarController, int, float, float)>();
        for (int i = 0; i < stats.Count; i++)
            result.Add((stats[i].car, i + 1, stats[i].time, stats[i].dist));
        return result;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public bool  IsPlayerFinished(CarController car) =>
        car?.Object != null && _carFinished.TryGetValue(car.Object.Id, out bool f) && f;

    public float GetRaceTime() => _isSpawned ? RaceTimer : 0f;

    public float GetPlayerFinishTime(CarController car) =>
        car?.Object != null && _finishTimes.TryGetValue(car.Object.Id, out float t) ? t : -1f;

    public CarController GetFirstFinisher() => _firstFinisher;
    public bool IsSpawned => _isSpawned;
}