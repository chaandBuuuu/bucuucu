using UnityEngine;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// ✅ FIXED RaceManager:
///   - OnChangedRender để client nhận events đúng
///   - RPC_RestartRace / RPC_BackToLobby gọi trực tiếp Runner.LoadScene (không check IsServer trong RPC_Targets.All)
///   - Reset state đúng cách trước khi load scene
/// </summary>
public class RaceManager : NetworkBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Race Config")]
    [SerializeField] private float finishCountdownDuration = 10f;
    [SerializeField] private int   racingSceneIndex        = 2;
    [SerializeField] private int   lobbySceneIndex         = 1;

    [Header("Finish Line")]
    [SerializeField] private Transform finishLineTransform;

    // ── Networked State ──────────────────────────────────────────────────────
    [Networked] public bool  RaceStarted      { get; private set; }
    [Networked] public bool  RaceFinished     { get; private set; }
    [Networked] public float RaceTimer        { get; private set; }
    [Networked] public int   CountdownCounter { get; private set; } = -1;
    [Networked] public float FinishCountdown  { get; private set; } = -1f;

    // ✅ Triggers cho OnChangedRender → clients nhận events
    [Networked, OnChangedRender(nameof(OnRaceStartedChanged))]
    private bool _raceStartedTrigger { get; set; }

    [Networked, OnChangedRender(nameof(OnRaceFinishedChanged))]
    private bool _raceFinishedTrigger { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerFinishedChanged))]
    private NetworkId _lastFinishedCarId { get; set; }

    // ✅ Trigger khi finish countdown bắt đầu (để clients nhận thông báo ngay)
    [Networked, OnChangedRender(nameof(OnFinishCountdownStarted))]
    private bool _finishCountdownStartedTrigger { get; set; }

    // ── Local ────────────────────────────────────────────────────────────────
    private Dictionary<NetworkId, float> _finishTimes     = new Dictionary<NetworkId, float>();
    private Dictionary<NetworkId, float> _finishDistances = new Dictionary<NetworkId, float>();
    private Dictionary<NetworkId, bool>  _carFinished     = new Dictionary<NetworkId, bool>();
    private List<CarController>          _cachedCars      = new List<CarController>();
    private CarController                _firstFinisher   = null;
    private CarController                _winner          = null;
    private bool                         _isSpawned       = false;
    private float                        _countdownTimer  = 0f;
    private float                        _finishCountdownTimer = 0f;
    private float                        _lastCarCacheTime = 0f;
    private const float                  CAR_CACHE_INTERVAL = 0.5f;

    // ✅ Cache race state cho late joiners
    private List<(CarController car, int position, float finishTime, float finishDistance)> _cachedFinalRankings = null;
    private CarController _cachedWinner = null;

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
            ResetCachedState();
            Debug.Log("[RaceManager] ✅ Initialized");
        }
    }

    // ✅ Reset cached state cho race mới
    private void ResetCachedState()
    {
        _cachedFinalRankings = null;
        _cachedWinner = null;
        _carFinished.Clear();
        _finishTimes.Clear();
        _finishDistances.Clear();
        _firstFinisher = null;
        _winner = null;
        _finishCountdownTimer = 0f;
        _countdownTimer = 0f;
        _finishCountdownStartedTrigger = false;
    }

    // ── OnChangedRender callbacks ─────────────────────────────────────────────

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
        if (_winner != null) OnRaceEnd?.Invoke(_winner);
        Debug.Log("[RaceManager] 🏆 OnRaceFinished → client");
    }

    private void OnPlayerFinishedChanged()
    {
        if (_lastFinishedCarId == default) return;
        var all = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        foreach (var car in all)
        {
            if (car.Object != null && car.Object.Id == _lastFinishedCarId)
            {
                OnPlayerFinish?.Invoke(car);
                return;
            }
        }
    }

    // ✅ Callback khi finish countdown bắt đầu (clients nhận ngay lập tức)
    private void OnFinishCountdownStarted()
    {
        if (!_finishCountdownStartedTrigger) return;
        Debug.Log("[RaceManager] ⏳ Finish countdown started → client");
    }

    // ── FixedUpdateNetwork ────────────────────────────────────────────────────

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // Pre-race countdown
        if (CountdownCounter >= 0)
        {
            _countdownTimer += Runner.DeltaTime;
            int desired = Mathf.Max(0, 3 - Mathf.FloorToInt(_countdownTimer));
            if (desired != CountdownCounter) CountdownCounter = desired;

            if (_countdownTimer >= 3f)
            {
                _countdownTimer     = 0f;
                CountdownCounter    = -1;
                RaceStarted         = true;
                _raceStartedTrigger = true;
                // Host fire trực tiếp
                OnRaceStart?.Invoke();
                Debug.Log("[RaceManager] 🏁 RACE STARTED!");
            }
        }

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

        UpdateCarCache();
    }

    private void UpdateCarCache()
    {
        if (finishLineTransform == null) return;
        float now = Time.time;
        if (now - _lastCarCacheTime > CAR_CACHE_INTERVAL)
        {
            _lastCarCacheTime = now;
            _cachedCars.Clear();
            var all = FindObjectsByType<CarController>(FindObjectsSortMode.None);
            foreach (var c in all)
                if (c.Object != null && !c.IsFinished) _cachedCars.Add(c);
        }
    }

    public Transform GetFinishLineTransform() => finishLineTransform;
    public void SetFinishLinePosition(Transform t) => finishLineTransform = t;

    // ── RPCs ──────────────────────────────────────────────────────────────────

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_StartRace()
    {
        if (!HasStateAuthority) return;
        CountdownCounter = 3;
        _countdownTimer  = 0f;
        Debug.Log("[RaceManager] 🎬 Starting countdown!");
    }

    /// <summary>
    /// ✅ FIX: Chỉ StateAuthority gọi, chỉ server mới LoadScene
    /// RpcTargets.All → tất cả chạy callback, nhưng LoadScene chỉ server gọi được
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void RPC_RestartRace()
    {
        if (!HasStateAuthority) return;
        Debug.Log("[RaceManager] 🔄 Restarting race...");
        Runner.LoadScene(SceneRef.FromIndex(racingSceneIndex));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void RPC_BackToLobby()
    {
        if (!HasStateAuthority) return;
        Debug.Log("[RaceManager] 🏠 Back to lobby...");
        Runner.LoadScene(SceneRef.FromIndex(lobbySceneIndex));
    }

    // ── Race Logic ────────────────────────────────────────────────────────────

    public void RegisterFinishCrossing(CarController car)
    {
        if (!HasStateAuthority || !RaceStarted || RaceFinished) return;

        NetworkId id = car.Object.Id;
        if (_carFinished.TryGetValue(id, out bool done) && done) return;

        _carFinished[id] = true;
        _finishTimes[id] = RaceTimer;
        if (finishLineTransform != null)
            _finishDistances[id] = Vector3.Distance(car.transform.position, finishLineTransform.position);

        car.IsFinished     = true;
        _lastFinishedCarId = id;   // trigger OnChangedRender

        // Host fire trực tiếp
        OnPlayerFinish?.Invoke(car);
        Debug.Log($"[RaceManager] ✅ {car.name} finished at {RaceTimer:F2}s");

        if (_firstFinisher == null)
        {
            _firstFinisher        = car;
            FinishCountdown       = finishCountdownDuration;
            _finishCountdownTimer = 0f;
            // ✅ Trigger clients để họ nhận countdown ngay lập tức
            _finishCountdownStartedTrigger = true;
        }
    }

    private void FinishRace()
    {
        if (RaceFinished) return;
        RaceFinished = true;

        _cachedCars.Clear();
        var all = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        foreach (var c in all) if (c.Object != null) _cachedCars.Add(c);

        var rankings = CalculateFinalRankings();
        if (rankings.Count > 0) _winner = rankings[0].car;

        // ✅ Cache state cho late joiners
        _cachedFinalRankings = rankings;
        _cachedWinner = _winner;

        // Host fire trực tiếp
        OnFinalRankings?.Invoke(rankings);
        if (_winner != null) OnRaceEnd?.Invoke(_winner);

        // Trigger clients
        _raceFinishedTrigger = true;
        Debug.Log($"[RaceManager] 🏆 Winner: {_winner?.name}");
    }

    private List<(CarController car, int position, float finishTime, float finishDistance)> CalculateFinalRankings()
    {
        var stats = new List<(CarController, float, float)>();

        foreach (var car in _cachedCars)
        {
            if (car?.Object == null) continue;
            NetworkId id = car.Object.Id;
            bool      finished = _carFinished.TryGetValue(id, out bool f) && f;
            float     t = finished && _finishTimes.TryGetValue(id, out var ft) ? ft : RaceTimer;
            float     d = finished && _finishDistances.TryGetValue(id, out var fd)
                ? fd
                : finishLineTransform != null
                    ? Vector3.Distance(car.transform.position, finishLineTransform.position)
                    : float.MaxValue;
            stats.Add((car, t, d));
        }

        stats.Sort((a, b) =>
        {
            if (Mathf.Abs(a.Item2 - b.Item2) < 0.01f) return a.Item3.CompareTo(b.Item3);
            return a.Item2.CompareTo(b.Item2);
        });

        var result = new List<(CarController, int, float, float)>();
        for (int i = 0; i < stats.Count; i++)
            result.Add((stats[i].Item1, i + 1, stats[i].Item2, stats[i].Item3));
        return result;
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public bool  IsPlayerFinished(CarController car) =>
        car?.Object != null && _carFinished.TryGetValue(car.Object.Id, out bool f) && f;
    public float GetRaceTime()  => _isSpawned ? RaceTimer : 0f;
    public float GetPlayerFinishTime(CarController car) =>
        car?.Object != null && _finishTimes.TryGetValue(car.Object.Id, out float t) ? t : -1f;
    public CarController GetFirstFinisher() => _firstFinisher;
    public bool          IsSpawned          => _isSpawned;

    // ✅ Late-joiner support: Cung cấp cached state
    public CarController GetCachedWinner() => _cachedWinner;
    public List<(CarController, int, float, float)> GetCachedFinalRankings() => _cachedFinalRankings;
}